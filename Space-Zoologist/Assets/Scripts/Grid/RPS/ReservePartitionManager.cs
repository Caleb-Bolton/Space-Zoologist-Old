using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A manager for managing how the reserve is "separated" for each population.
/// </summary>
public class ReservePartitionManager : MonoBehaviour
{
    /// <summary> Singleton instance. </summary>
    public static ReservePartitionManager ins;

    /// <summary> Maximum number of populations allowed. </summary>
    public const int maxPopulation = 64;

    /// <summary> The list of populations, not guaranteed to be sorted. </summary>
    public List<Population> Populations { get; private set; }

    // Two way dictionary for population-id exchange
    /// <summary> A dictionary that stores id by population. </summary>
    public Dictionary<Population, int> PopulationToID { get; private set; }
    /// <summary> A dictionary that stores population by id. </summary>
    public Dictionary<int, Population> PopulationByID { get; private set; }

    // A list of opened ids for use
    private Queue<int> openID;
    private int lastRecycledID;

    /// <summary> A map that represents the reserve and who can access each tile
    /// The long is a bit mask with the bit (IDth bit) representing a population. </summary>
    public Dictionary<Vector3Int, long> AccessMap { get; private set; }

    /// <summary> Amount of accessible tiles for each population. </summary>
    public Dictionary<Population, int> Spaces { get; private set; }

    /// <summary> Amount of shared tiles between each pair of populations. Ordered by [id, [id, shared tiles]]. </summary>
    public Dictionary<int, long[]> SharedSpaces { get; private set; }

    /// <summary> A list of populations to be loaded on startup. </summary>
    [SerializeField] List<Population> populationsOnStartUp = default;

    /// <summary>
    /// The number of each type of terrain that the population has access to.
    /// Order is as the order of appearance in TileType definition. i.e. int[0] = # of rocks, etc.
    /// </summary>
    private Dictionary<Population, int[]> TypesOfTerrain;

    /// <summary>
    /// Stores how much a population would prefer to travel through each tile on the map.
    /// </summary>
    public Dictionary<Vector3Int, byte[]> PopulationPreference;

    public TerrainTile Liquid;

    private void Awake()
    {
        // Variable initializations
        if (ins != null && this != ins)
        {
            Destroy(this);
        }
        else
        {
            ins = this;
        }

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
        Spaces = new Dictionary<Population, int>();
        SharedSpaces = new Dictionary<int, long[]>();
        TypesOfTerrain = new Dictionary<Population, int[]>();
        PopulationPreference = new Dictionary<Vector3Int, byte[]>();
    }

    private void Start()
    {
        // Load pre-existing populations
        foreach (Population population in populationsOnStartUp)
        {
            AddPopulation(population);
        }
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

            // generate the map with the new id  
            GenerateMap(population);
            
        }
    }

    /// <summary>
    /// Remove a population from the RPM.
    /// </summary>
    public void RemovePopulation(Population population)
    {
        Populations.Remove(population);
        TypesOfTerrain.Remove(population);
        openID.Enqueue(PopulationToID[population]);
        PopulationByID.Remove(PopulationToID[population]);  // free ID
        PopulationToID.Remove(population);  // free ID
        
    }

    /// <summary>
    /// Cleanup the map for the update or recycle of id.
    /// </summary>
    /// <param name="id">The id (bit) to be cleaned on AccessMap</param>
    void CleanupAccessMap(int id)
    {
        foreach (Vector3Int loc in AccessMap.Keys)
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
    /// [deprecated] Populate the access map for a population with depth first search. 15-20% slower than scanline flood fill
    /// </summary>
    /// <param name="population">The population to be generated, assumed to be in Populations</param>
    private void GenerateMap(Population population)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        HashSet<Vector3Int> accessible = new HashSet<Vector3Int>();
        HashSet<Vector3Int> unaccessible = new HashSet<Vector3Int>();
        Vector3Int cur;

        float range = population.Species.Range;

        // id of the current population
        int id = PopulationToID[population];
        
        // Number of shared tiles
        long[] SharedTiles = new long[maxPopulation];

        TypesOfTerrain[population] = new int[(int)TileType.TypesOfTiles];

        // starting location
        Vector3Int origin = FindObjectOfType<TileSystem>().WorldToCell(population.transform.position);
        stack.Push(origin);

        TileSystem _tileSystem = FindObjectOfType<TileSystem>();

        // iterate until no tile left in list, ends in iteration 1 if population.location is not accessible
        while (stack.Count > 0)
        {
            // next point
            cur = stack.Pop();

            if (accessible.Contains(cur) || unaccessible.Contains(cur))
            {
                // checked before, move on
                continue;
            }
            else if (Vector3Int.Distance(origin, cur) > range) {
                unaccessible.Add(cur);
                continue;
            }

            // check if tilemap has tile and if population can access the tile (e.g. some cannot move through water)
            TerrainTile tile = _tileSystem.GetTerrainTileAtLocation(cur);
            if (tile != null && population.Species.AccessibleTerrain.Contains(tile.type))
            {
                // save the accessible location
                accessible.Add(cur);

                TypesOfTerrain[population][(int)tile.type]++;

                // save how much a population would prefer to travel through the tile
                if (!PopulationPreference.ContainsKey(cur))
                {
                    PopulationPreference[cur] = new byte[maxPopulation];
                }
                if (population.Species.TilePreference.ContainsKey(tile.type))
                    PopulationPreference[cur][id] = population.Species.TilePreference[tile.type];

                if (!AccessMap.ContainsKey(cur))
                {
                    AccessMap.Add(cur, 0L);
                }
                AccessMap[cur] |= 1L << PopulationToID[population];

                // Collect info on how the population's space overlaps with others
                // ~ 20% of algorithm cost
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
        }

        // Amount of accessible area
        Spaces[population] = accessible.Count;

        // Store the info on overlapping space
        SharedSpaces[id] = SharedTiles;

        // Update the new info for pre-existing populations
        for (int i = 0; i < SharedSpaces[id].Length; i++) {
            if (PopulationByID.ContainsKey(i) && SharedSpaces[id][i] != 0) {
                SharedSpaces[i][id] = SharedSpaces[id][i];
            }
        }
    }

    /// <summary>
    /// Populate the access map for a population with scanline flood fill. 15-20% faster than 4 way flood fill.
    /// </summary>
    /// <param name="population">The population to be generated, assumed to be in Populations</param>
    private void ScanlineGenerateMap(Population population)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        HashSet<Vector3Int> accessed = new HashSet<Vector3Int>();
        Vector3Int cur;
        bool openAbove, openBelow;

        // id of the current population
        int id = PopulationToID[population];

        // Number of shared tiles
        long[] SharedTiles = new long[maxPopulation];

        // Save the amount of tiles by terrain reached by the population (used for need systems)
        TypesOfTerrain[population] = new int[(int)TileType.TypesOfTiles];

        // Starting location
        Vector3Int origin = FindObjectOfType<TileSystem>().WorldToCell(population.transform.position);
        stack.Push(origin);

        TileSystem _tileSystem = FindObjectOfType<TileSystem>();


        // iterate until no tile left in list, ends in iteration 1 if population.location is not accessible
        while (stack.Count > 0)
        {
            // next point
            cur = stack.Pop();

            // go left while possible
            Vector3Int left = cur + Vector3Int.left;
            TerrainTile tile = _tileSystem.GetTerrainTileAtLocation(left);
            while (tile != null && !accessed.Contains(cur) && population.Species.AccessibleTerrain.Contains(tile.type)) {
                cur = left;
                left = cur + Vector3Int.left;
                tile = _tileSystem.GetTerrainTileAtLocation(left);
            }

            // whether the tile above or below is fill-able
            openAbove = openBelow = false;

            // fill the right tile while possible
            Vector3Int right = cur + Vector3Int.right;
            tile = _tileSystem.GetTerrainTileAtLocation(cur);
            while (tile != null && population.Species.AccessibleTerrain.Contains(tile.type))
            {
                // save the accessible location
                accessed.Add(cur);

                // data for need system
                TypesOfTerrain[population][(int)tile.type]++;

                // save how much a population would prefer to travel through the tile
                if (!PopulationPreference.ContainsKey(cur))
                {
                    PopulationPreference[cur] = new byte[maxPopulation];
                }
                if (population.Species.TilePreference.ContainsKey(tile.type))
                    PopulationPreference[cur][id] = population.Species.TilePreference[tile.type];

                // populate the access map
                if (!AccessMap.ContainsKey(cur))
                {
                    AccessMap.Add(cur, 0L);
                }
                AccessMap[cur] |= 1L << PopulationToID[population];

                // Collect info on the population's overlapping space with others
                for (int i = 0; i < Populations.Count; i++)
                {
                    SharedTiles[i] += (AccessMap[cur] >> PopulationToID[Populations[i]]) & 1L;
                }

                // Save the left most tile of every scannable section from the line above or below
                if (!openAbove && !accessed.Contains(cur + Vector3Int.up) && _tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.up) != null && population.Species.AccessibleTerrain.Contains(_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.up).type))
                {
                    stack.Push(cur + Vector3Int.up);
                    openAbove = true;
                }
                else if (openAbove && (_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.up) == null || !population.Species.AccessibleTerrain.Contains(_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.up).type))) {
                    openAbove = false;
                }

                if (!openBelow && !accessed.Contains(cur + Vector3Int.down) && _tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.down) != null && population.Species.AccessibleTerrain.Contains(_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.down).type))
                {
                    stack.Push(cur + Vector3Int.down);
                    openBelow = true;
                }
                else if (openBelow && (_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.down) == null || !population.Species.AccessibleTerrain.Contains(_tileSystem.GetTerrainTileAtLocation(cur + Vector3Int.down).type)))
                {
                    openBelow = false;
                }

                // move right
                cur = right;
                right = cur + Vector3Int.right;
                tile = _tileSystem.GetTerrainTileAtLocation(cur);
            }
        }

        // Amount of accessible area
        Spaces[population] = accessed.Count;

        // Store the info on overlapping space
        SharedSpaces[id] = SharedTiles;

        // Update the new area info for pre-existing populations
        for (int i = 0; i < SharedSpaces[id].Length; i++)
        {
            if (PopulationByID.ContainsKey(i) && SharedSpaces[id][i] != 0)
            {
                SharedSpaces[i][id] = SharedSpaces[id][i];
            }
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
    public void UpdateAccessMapChangedAt(Vector3Int[] positions)
    {
        List<int> UnaffectedID = new List<int>();
        HashSet<Population> AffectedPopulations = new HashSet<Population>();
        foreach (Population population in Populations)
        {
            UnaffectedID.Add(PopulationToID[population]);
        }

        foreach (Vector3Int position in positions) {
            if (!AccessMap.ContainsKey(position)) {
                continue;
            } else {
                long mask = AccessMap[position];
                for (int i = 0; i < UnaffectedID.Count; i++) {
                    if (((mask >> UnaffectedID[i]) & 1L) == 1L) {
                        AffectedPopulations.Add(PopulationByID[UnaffectedID[i]]);
                        UnaffectedID.RemoveAt(i);
                    }
                }
            }
        }

        // Most intuitive implementation: recalculate map for all affected populations
        foreach (Population population in AffectedPopulations) {
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
        List<Vector3Int> list = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, long> position in AccessMap)
        {
            if (CanAccess(population, position.Key))
            {
                list.Add(position.Key);
            }
        }
        return list;
    }

    // Will need to make the grid the size of the max tilemap size
    public AnimalPathfinding.Grid GetGridWithAccess(Population population, Tilemap temp)
    {
        bool[,] tileGrid = new bool[temp.size.x, temp.size.y];
        foreach (KeyValuePair<Vector3Int, long> position in AccessMap)
        {
            //Debug.Log("(" + (position.Key.x + (temp.origin.x * -1))+ ", " + (position.Key.y + (temp.origin.y * -1)) + ")");
            if (CanAccess(population, position.Key))
            {
                tileGrid[position.Key.x + (temp.origin.x * -1), position.Key.y + (temp.origin.y * -1)] = true;
                // Debug.Log("(" + (position.Key.x + (temp.origin.x * -1))+ ", " + (position.Key.y + (temp.origin.y * -1)) + ")");
            }
            else
            {
                tileGrid[position.Key.x + (temp.origin.x * -1), position.Key.y + (temp.origin.y * -1)] = false;
            }
        }
        return new AnimalPathfinding.Grid(tileGrid);
    }

    /// <summary>
    /// Check if a population can access toWorldPos.
    /// </summary>
    public bool CanAccess(Population population, Vector3 toWorldPos)
    {
        // convert to map position
        Vector3Int mapPos = FindObjectOfType<TileSystem>().WorldToCell(toWorldPos);
        return CanAccess(population, mapPos);
    }

    /// <summary>
    /// Check if a population can access CellPos.
    /// </summary>
    public bool CanAccess(Population population, Vector3Int cellPos)
    {
        // if accessible
        // check if the nth bit is set (i.e. accessible for the population)
        if (AccessMap.ContainsKey(cellPos))
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
        List<Vector3Int> AccessibleArea_A = GetLocationsWithAccess(populationA);
        List<Vector3Int> AccessibleArea_B = GetLocationsWithAccess(populationB);

        foreach (Vector3Int cellPos in AccessibleArea_A)
        {
            if (AccessibleArea_B.Contains(cellPos))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Go through Populations and return a list of populations that has access to the tile corresponding to toWorldPos.
    /// </summary>
    public List<Population> GetPopulationsWithAccessTo(Vector3 toWorldPos)
    {
        // convert to map position
        Vector3Int cellPos = FindObjectOfType<TileSystem>().WorldToCell(toWorldPos);
        return GetPopulationsWithAccessTo(cellPos);
    }

    /// <summary>
    /// Go through Populations and return a list of populations that have access to the tile corresponding to cellPos.
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
    public int[] GetTypesOfTiles(Population population) {
        return TypesOfTerrain[population];
    }

    /// <summary>
    /// Get a population's preference on a cell position.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="cellPos"></param>
    /// <returns></returns>
    //public byte GetPreference(Population population, Vector3Int cellPos) {
    //    if (!PopulationPreference.ContainsKey(cellPos) || PopulationPreference[cellPos][PopulationToID[population]] == 0)
    //    {
    //        return 255;
    //    }
    //    else {
    //        return PopulationPreference[cellPos][PopulationToID[population]];
    //    }
    //}
}