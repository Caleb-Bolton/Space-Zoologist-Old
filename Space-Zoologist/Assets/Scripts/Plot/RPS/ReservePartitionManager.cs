using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A manager for managing how the reserve is "separated" for each population.
/// </summary>
public class ReservePartitionManager : MonoBehaviour
{
    // Singleton
    //public static ReservePartitionManager ins;

    // Maximum number of populations allowed
    public const int maxPopulation = 64;

    // The list of populations, not guaranteed to be ordered
    public List<Population> Populations { get; private set; }

    // A two-way dictionary that stores populations' id
    public Dictionary<Population, int> PopulationToID { get; private set; }
    public Dictionary<int, Population> PopulationByID { get; private set; }

    // A list of opened ids for use
    private Queue<int> openID;
    private int lastRecycledID;

    // A map that represents the reserve and who can access each tile
    // The long is a bit mask with the bit (IDth bit) representing a population
    public Dictionary<Vector3Int, long> AccessMap { get; private set; }

    // Accessible and preferred terrain area for each population
    public Dictionary<Population, List<Vector3Int>> NeededArea { get; private set; }
    public Dictionary<Population, List<Vector3Int>> TraversableOnlyArea { get; private set; }

    // Amount of shared space with each population <id, <id, shared tiles> >
    public Dictionary<int, long[]> SharedSpaces { get; private set; }

    public Dictionary<Population, int[]> TypesOfTerrain;

    public Dictionary<Population, List<float[]>> PopulationAccessibleLiquidCompositions => this.populationAccessibleLiquidCompositions;
    private Dictionary<Population, List<float[]>> populationAccessibleLiquidCompositions;

    public Dictionary<Population, List<Vector3Int>> PopulationAccessibleLiquidLocations => this.populationAccessibleLiquidLocations;
    private Dictionary<Population, List<Vector3Int>> populationAccessibleLiquidLocations;

    public GameTile Liquid;
    private TileDataController gridSystem = default;
    public void Initialize()
    {
        // Variable initializations
        gridSystem = GameManager.Instance.m_tileDataController;

        // long mask is limited to 64 bits

        openID = new Queue<int>();
        lastRecycledID = maxPopulation - 1; // 63
        for (int i = maxPopulation - 1; i >= 0; i--)
        {
            openID.Enqueue(i);
        }
        Populations = new List<Population>();
        PopulationToID = new Dictionary<Population, int>();
        PopulationByID = new Dictionary<int, Population>();
        AccessMap = new Dictionary<Vector3Int, long>();
        
        NeededArea = new Dictionary<Population, List<Vector3Int>>();
        TraversableOnlyArea = new Dictionary<Population, List<Vector3Int>>();
        
        SharedSpaces = new Dictionary<int, long[]>();
        TypesOfTerrain = new Dictionary<Population, int[]>();
        populationAccessibleLiquidCompositions = new Dictionary<Population, List<float[]>>();
        populationAccessibleLiquidLocations = new Dictionary<Population, List<Vector3Int>>();

        EventManager.Instance.SubscribeToEvent(EventType.PopulationExtinct, (eventData) => this.RemovePopulation((Population)eventData));
    }

    /// <summary>
    /// Add a population to the RPM.
    /// </summary>
    public void AddPopulation(Population population)
    {
        if (!Populations.Contains(population))
        {
            // ignore their old id and assign it a new one
            int id = openID.Dequeue();

            // since IDs after maxPopulation-1 are recycled ids, we need to do clean up old values
            if (id == lastRecycledID) CleanupAccessMapForRecycledID();
            PopulationToID.Add(population, id);
            PopulationByID.Add(id, population);
            Populations.Add(population);

            TypesOfTerrain.Add(population, new int[(int)TileType.TypesOfTiles]);
            // generate the map with the new id
            GenerateMap(population);

        }
    }

    /// <summary>
    /// Remove a population from the RPM.
    /// </summary>
    public void RemovePopulation(Population population)
    {
        if (!Populations.Contains(population))
        {
            return;
        }
        Populations.Remove(population);
        TypesOfTerrain.Remove(population);
        openID.Enqueue(PopulationToID[population]);
        PopulationByID.Remove(PopulationToID[population]);  // free ID
        PopulationToID.Remove(population);  // free ID
        CleanupAccessMapForRecycledID();

    }

    /// <summary>
    /// Cleanup the map for the update or recycle of id.
    /// </summary>
    /// <param name="id">The id (bit) to be cleaned on AccessMap</param>
    void CleanupAccessMap(int id)
    {
        List<Vector3Int> accessMapKeys = new List<Vector3Int>(AccessMap.Keys);

        foreach (Vector3Int loc in accessMapKeys)
        {
            // set the values to 0 through bit masking
            AccessMap[loc] &= ~(1L << id);
        }
    }

    /// <summary>
    /// Called internally when ID is recycled.
    /// </summary>
    void CleanupAccessMapForRecycledID()
    {
        foreach (int id in openID)
        {
            CleanupAccessMap(id);
            lastRecycledID = id;
        }
    }

    /// <summary>
    /// Populate the access map for a population with depth first search.
    /// </summary>
    /// <param name="population">The population to be generated, assumed to be in Populations</param>
    /// <remarks>When this is called that means the terrain had changed for sure</remarks>
    private void GenerateMap(Population population)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        HashSet<Vector3Int> accessible = new HashSet<Vector3Int>();
        HashSet<Vector3Int> unaccessible = new HashSet<Vector3Int>();
        Vector3Int cur;
        
        List<Vector3Int> newNeededLocations = new List<Vector3Int>();
        List<Vector3Int> newTraversableOnlyLocations = new List<Vector3Int>();
        
        List<Vector3Int> newLiquidLocations = new List<Vector3Int>();
        List<float[]> newLiquidCompositions = new List<float[]>();
        List<LiquidBody> newLiquidBodies = new List<LiquidBody>();
        // cache needs for performance to avoid repeating costly linq searches
        var treeNeeds = population.species.RequiredTreeNeeds;
        var neededTerrain = population.species.NeededTerrain;
        var traversableOnlyTerrain = population.species.TraversableOnlyTerrain;
        var accessibleTerrain = population.species.AccessibleTerrain;
        
        if (!this.NeededArea.ContainsKey(population))
        {
            this.NeededArea.Add(population, new List<Vector3Int>());
        }
        
        if (!this.TraversableOnlyArea.ContainsKey(population))
        {
            this.TraversableOnlyArea.Add(population, new List<Vector3Int>());
        }

        // Number of shared tiles
        long[] SharedTiles = new long[maxPopulation];
        TileDataController gridSystemReference = GameManager.Instance.m_tileDataController;

        // starting location
        Vector3Int location = gridSystemReference.WorldToCell(population.transform.position);
        stack.Push(location);

        // Clear TypesOfTerrain for given population
        this.TypesOfTerrain[population] = new int[(int)TileType.TypesOfTiles];
        // iterate until no tile left in list, ends in iteration 1 if population.location is not accessible
        int counter = 0;
        while (stack.Count > 0)
        {
            // next point
            cur = stack.Pop();
            counter++;
            if (accessible.Contains(cur) || unaccessible.Contains(cur))
            {
                // checked before, move on
                continue;
            }
            // Check tiles that are under construction, make them inaccessible
            //if (this.buildBufferManager.IsConstructing(cur.x,cur.y))
            //{
            //    unaccessible.Add(cur);
            //    population.HasAccessibilityChanged = true;
            //    continue;
            //}
            // check if tilemap has tile and if population can access the tile (e.g. some cannot move through water)
            GameTile tile = gridSystemReference.GetGameTileAt(cur);
            // Get liquid tile info
            if (tile != null && tile.type == TileType.Liquid)
            {
                float[] composition = new float[] { 0, 0, 0 };
                LiquidbodyController.Instance.GetLiquidContentsAt(cur, out composition, out bool constructing);

                if (!this.populationAccessibleLiquidCompositions.ContainsKey(population))
                {
                    this.populationAccessibleLiquidCompositions.Add(population, new List<float[]>());
                }

                if (!this.populationAccessibleLiquidLocations.ContainsKey(population))
                {
                    this.populationAccessibleLiquidLocations.Add(population, new List<Vector3Int>());
                }

                newLiquidCompositions.Add(composition);
                newLiquidLocations.Add(cur);
            }
            
            // Tile validity logic
            
            bool isTileNull = (tile == null);
            bool isTileNeeded = !isTileNull && gridSystem.IsNeededTileForAnimal(
                population.species,
                cur,
                treeNeeds,
                accessibleTerrain,
                neededTerrain);
            
            
            bool isTileOnlyTraversable = !isTileNull && gridSystem.IsTraversableOnlyTileForAnimal(
                population.species, 
                cur , 
                treeNeeds, 
                traversableOnlyTerrain);
            
            if (isTileNeeded || isTileOnlyTraversable)
            {
                // save the accessible location
                accessible.Add(cur);

                // Save to needed locations
                if (isTileNeeded)
                {
                    newNeededLocations.Add(cur);
                }
                
                // Save to only traversable locations
                if (isTileOnlyTraversable)
                {
                    newTraversableOnlyLocations.Add(cur);
                }

                TypesOfTerrain[population][(int)tile.type]++;

                if (!AccessMap.ContainsKey(cur))
                {
                    AccessMap.Add(cur, 0L);
                }
                AccessMap[cur] |= 1L << PopulationToID[population];

                // Collect info on how the population's space overlaps with others
                for (int i = 0; i < Populations.Count; i++)
                {
                    SharedTiles[i] += (AccessMap[cur] >> PopulationToID[Populations[i]]) & 1L;
                }

                // check all 4 tiles around, may be too expensive/awaiting optimization
                stack.Push(cur + Vector3Int.left);
                stack.Push(cur + Vector3Int.up);
                stack.Push(cur + Vector3Int.right);
                stack.Push(cur + Vector3Int.down);
            }
            else
            {
                // save the Vector3Int since it is already checked
                unaccessible.Add(cur);
            }
            population.HasAccessibilityChanged = true;
        }
        // Amount of accessible area
        //Spaces[population] = accessible.Count;
        // Store the info on overlapping space
        int id = PopulationToID[population];
        SharedSpaces[id] = SharedTiles;

        // Update the new info for pre-existing populations
        for (int i = 0; i < SharedSpaces[id].Length; i++) {
            if (PopulationByID.ContainsKey(i) && SharedSpaces[id][i] != 0) {
                SharedSpaces[i][id] = SharedSpaces[id][i];
            }
        }
        // Update space
        if (population.HasAccessibilityChanged)
        {
            this.NeededArea[population] = newNeededLocations;
            this.TraversableOnlyArea[population] = newTraversableOnlyLocations;
            this.populationAccessibleLiquidCompositions[population] = newLiquidCompositions;
            this.populationAccessibleLiquidLocations[population] = newLiquidLocations;
        }
    }

    /// <summary>
    /// Manually update the access map for every population in Populations.
    /// </summary>
    public void UpdateAccessMap()
    {
        AccessMap = new Dictionary<Vector3Int, long>();
        foreach (Population population in Populations)
        {
            GenerateMap(population);
        }
    }

    /// <summary>
    /// Update any populations that have access to the given positions.
    /// </summary>
    /// <param name="positions">The tiles that were updated (added wall, river, etc.)</param>
    public void UpdateAccessMapChangedAt(List<Vector3Int> positions)
    {
        List<int> UnaffectedID = new List<int>();
        HashSet<Population> AffectedPopulations = new HashSet<Population>();
        foreach (Population population in Populations)
        {
            UnaffectedID.Add(PopulationToID[population]);
        }

        foreach (Vector3Int position in positions)
        {
            if (!AccessMap.ContainsKey(position))
            {
                continue;
            }
            else
            {
                long mask = AccessMap[position];
                for (int i = 0; i < UnaffectedID.Count; i++)
                {
                    if (((mask >> UnaffectedID[i]) & 1L) == 1L)
                    {
                        AffectedPopulations.Add(PopulationByID[UnaffectedID[i]]);
                        UnaffectedID.RemoveAt(i);
                    }
                }
            }
        }

        // Most intuitive implementation: recalculate map for all affected populations
        foreach (Population population in AffectedPopulations)
        {
            Debug.Log("Updating access map for " + population.gameObject.name);
            CleanupAccessMap(PopulationToID[population]);
            GenerateMap(population);
        }
    }

    /// <summary>
    /// Get a list of all locations that can be accessed by this population.
    /// </summary>
    /// <param name="population"></param>
    /// <returns></returns>
    public List<Vector3Int> GetLocationsWithAccess(Population population)
    {
        var list = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, long> position in AccessMap)
        {
            if (CanAccess(population, position.Key))
            {
                list.Add(position.Key);
            }
        }
        return list;
    }
    
    public HashSet<Vector3Int> GetLocationsSetWithAccess(Population population)
    {
        var set = new HashSet<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, long> position in AccessMap)
        {
            if (CanAccess(population, position.Key))
            {
                set.Add(position.Key);
            }
        }
        return set;
    }

    /// <summary>
    /// Check if a population can access toWorldPos.
    /// </summary>
    public bool CanAccess(Population population, Vector3 toWorldPos)
    {
        // convert to map position
        Vector3Int mapPos = GameManager.Instance.m_tileDataController.WorldToCell(toWorldPos);
        return CanAccess(population, mapPos);
    }

    /// <summary>
    /// Check if a population can access CellPos.
    /// </summary>
    public bool CanAccess(Population population, Vector3Int cellPos)
    {
        // if accessible
        // check if the nth bit is set (i.e. accessible for the population)
         if (AccessMap.ContainsKey(cellPos) && PopulationToID.ContainsKey(population))
        {
            if (((AccessMap[cellPos] >> PopulationToID[population]) & 1L) == 1L)
            {
                return true;
            }
        }

        // population can't access the position
        return false;
    }

    /// <summary>
    /// Check if populationA's and populationB's accessible area overlaps.
    /// </summary>
    /// <param name="populationA">Ususally the consumer population</param>
    /// <param name="populationB">Ususally the consumed population</param>
    /// <remarks><c>populationA</c> and <c>populationB</c> is interchangeable</remarks>
    /// <returns>True is two population's accessible area overlaps, false otherwise</returns>
    public bool CanAccessPopulation(Population populationA, Population populationB)
    {
        var accessibleAreaA = GetLocationsSetWithAccess(populationA);
        var accessibleAreaB = GetLocationsSetWithAccess(populationB);

        foreach (var location in accessibleAreaA)
        {
            if (accessibleAreaB.Contains(location))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if populationA's and populationB's accessible area overlaps.
    /// </summary>
    /// <param name="populationA">Ususally the consumer population</param>
    /// <param name="populationB">Ususally the consumed population</param>
    /// <remarks><c>populationA</c> and <c>populationB</c> is interchangeable</remarks>
    /// <returns>True is two population's accessible area overlaps, false otherwise</returns>
    public int NumOverlapTiles(Population populationA, Population populationB)
    {
        var accessibleAreaA = GetLocationsSetWithAccess(populationA);
        var accessibleAreaB = GetLocationsSetWithAccess(populationB);
        accessibleAreaA.IntersectWith(accessibleAreaB);

        return accessibleAreaA.Count;
    }

    public int NumOverlapTiles(HashSet<Vector3Int> accessA, HashSet<Vector3Int> accessB)
    {
        int count = 0;
        foreach (var location in accessA)
        {
            if (accessB.Contains(location))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Go through Populations and return a list of populations that has access to the tile corresponding to toWorldPos.
    /// </summary>
    public List<Population> GetPopulationsWithAccessTo(Vector3 toWorldPos)
    {
        // convert to map position
        Vector3Int cellPos = GameManager.Instance.m_tileDataController.WorldToCell(toWorldPos);

        List<Population> accessible = new List<Population>();
        foreach (Population population in Populations)
        {
            // utilize CanAccess()
            if (CanAccess(population, cellPos))
            {
                accessible.Add(population);
            }
        }
        return accessible;
    }

    /// <summary>
    /// Go through Populations and return a list of populations that has access to the tile corresponding to toWorldPos.
    /// </summary>
    public List<Population> GetPopulationsWithAccessTo(Vector3Int cellPos)
    {
        List<Population> accessible = new List<Population>();
        foreach (Population population in Populations)
        {
            // utilize CanAccess()
            if (CanAccess(population, cellPos))
            {
                accessible.Add(population);
            }
        }
        return accessible;
    }

    /// <summary>
    /// Returns the number of each types of tile the population has access to. The position in the array represent the type,
    /// with the same order as the enum TileType.
    /// </summary>
    /// <param name="population"></param>
    /// <returns></returns>
    public int[] GetTypesOfTiles(Population population) 
    {
        return TypesOfTerrain[population];
    }

    /// <summary>
    /// Get a list of the food sources that this population can access
    /// </summary>
    /// <param name="population"></param>
    /// <returns></returns>
    public List<FoodSource> GetAccessibleFoodSources(Population population)
    {
        if (population == null)
            throw new ArgumentNullException(
                "Cannot get the accessible food sources for population 'null'");

        if (!NeededArea.ContainsKey(population) && !TraversableOnlyArea.ContainsKey(population))
            throw new ArgumentException(
                $"Population '{population}' has no list of accessible area associated with it");

        // Get the area that this population can access, both needed and traversable only
        HashSet<Vector3Int> area = new HashSet<Vector3Int>(NeededArea[population]);
        area.UnionWith(TraversableOnlyArea[population]);

        // Local function checks if this food source has any cell position
        // in the set of positions that the population can access
        bool AnyPositionInArea(FoodSource food)
        {
            foreach (Vector3Int cell in food.GetAllCellPositions())
            {
                if (area.Contains(cell)) return true;
            }
            return false;
        }

        // Find all food sources with a position within the accessible area
        return GameManager
            .Instance
            .m_foodSourceManager
            .FoodSources
            .FindAll(source => AnyPositionInArea(source));
    }

    public List<LiquidBody> GetAccessibleLiquidBodies(Population population)
    {
        if (population == null)
            throw new ArgumentNullException(
                "Cannot get the accessible food sources for population 'null'");

        if (!populationAccessibleLiquidLocations.ContainsKey(population))
            throw new ArgumentException(
                $"Population '{population}' has no list of accessible area associated with it");

        HashSet<LiquidBody> accessibleBodies = new HashSet<LiquidBody>();

        // Iterate over each shore position that the population can access
        foreach (Vector3Int shorePosition in populationAccessibleLiquidLocations[population])
        {
            // Iterate over each liquid body in the main controller
            foreach (LiquidBody body in LiquidbodyController.Instance.liquidBodies)
            {
                // If we do not already have this liquid body in the set
                // and the body contains this tile then add it to the set
                if (!accessibleBodies.Contains(body) && body.ContainsTile(shorePosition))
                {
                    accessibleBodies.Add(body);
                }
            }
        }

        return new List<LiquidBody>(accessibleBodies);
    }

    /// <summary>
    /// Return a dictionary that maps the food source
    /// to a list of populations that can access it
    /// </summary>
    /// <returns></returns>
    public Dictionary<FoodSource, List<Population>> FoodCompetition()
    {
        Dictionary<FoodSource, List<Population>> result = new Dictionary<FoodSource, List<Population>>();

        // Go through every population
        foreach (Population population in Populations)
        {
            // Get a list of the food sources that this population accesses
            List<FoodSource> accessibleFood = GetAccessibleFoodSources(population);

            // Go through each food source 
            foreach (FoodSource food in accessibleFood)
            {
                // Get the need on the population corresponding to this food source
                NeedData foodNeed = population.Species.Needs.Get(food.Species.ID);

                // Check if the population needs this food before adding it to the dictionary
                if (foodNeed.Needed)
                {
                    // If this food is not in the dictionary yet then add it
                    if (!result.ContainsKey(food))
                    {
                        result.Add(food, new List<Population>());
                    }

                    // Add this population to the list
                    result[food].Add(population);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Return a dictionary that maps the position on the grid
    /// with a list of populations that compete for that terrain tile
    /// </summary>
    /// <returns></returns>
    public Dictionary<Vector3Int, List<Population>> TerrainCompetition()
    {
        Dictionary<Vector3Int, List<Population>> result = new Dictionary<Vector3Int, List<Population>>();

        // Go through each entry in the accessible area
        foreach (KeyValuePair<Population, List<Vector3Int>> kvp in NeededArea)
        {
            // Go through each cell position in the list
            foreach (Vector3Int cell in kvp.Value)
            {
                // Add a new list to the dictionary if it does not exist yet
                if (!result.ContainsKey(cell))
                {
                    result.Add(cell, new List<Population>());
                }

                // Add this population to the list contending for this tile
                result[cell].Add(kvp.Key);
            }
        }

        return result;
    }

    public List<float[]> GetLiquidComposition(Population population)
    {
        if (!this.populationAccessibleLiquidCompositions.ContainsKey(population))
        {
            return null;
        }

        return this.populationAccessibleLiquidCompositions[population];
    }

    public List<Vector3Int> GetLiquidLocations(Population population)
    {
        if (!this.populationAccessibleLiquidLocations.ContainsKey(population))
        {
            return null;
        }

        return this.populationAccessibleLiquidLocations[population];
    }
}