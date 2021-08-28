using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Hadnles need value update for all  `Terrain` type needs
/// </summary>
public class TerrainNeedSystem : NeedSystem
{
    //The order of terrain from least number of population preferences to most.
    private static readonly TileType[] terrainOrder = new TileType[] {TileType.Sand, TileType.Liquid, TileType.Dirt, TileType.Swamp, TileType.Stone, TileType.Grass};

    //The ratios of terrain allocation sorted by dominance
    private static readonly Dictionary<TileType, Dictionary<SpeciesType, float>> dominanceRatiosByTileType = new Dictionary<TileType, Dictionary<SpeciesType, float>>
    {
        { TileType.Grass, new Dictionary<SpeciesType, float> { {SpeciesType.Anteater, 0.4f}, {SpeciesType.Cow, 0.3f}, {SpeciesType.Goat, 0.2f}, {SpeciesType.Spider, 0.1f} } },
        { TileType.Dirt, new Dictionary<SpeciesType, float> { {SpeciesType.Goat, 0.6f}, {SpeciesType.Cow, 0.4f} } },
        { TileType.Sand, new Dictionary<SpeciesType, float> { {SpeciesType.Spider, 1f} } },
        { TileType.Stone, new Dictionary<SpeciesType, float> { {SpeciesType.Spider, 0.6f}, {SpeciesType.Anteater, 0.4f} } },
        { TileType.Swamp, new Dictionary<SpeciesType, float> { {SpeciesType.Slug, 0.6f}, {SpeciesType.Anteater, 0.4f} } },
        { TileType.Liquid, new Dictionary<SpeciesType, float> { {SpeciesType.Slug, 1f} } }
    };

    // For `Population` consumers
    private readonly ReservePartitionManager rpm = null;

    // For `FoodSource` consumers
    private GridSystem gridSystem = null;

    public TerrainNeedSystem(ReservePartitionManager rpm, GridSystem gridSystem, NeedType needType = NeedType.Terrain) : base(needType)
    {
        this.rpm = rpm;
        this.gridSystem = gridSystem;

        EventManager.Instance.SubscribeToEvent(EventType.TerrainChange, () =>
        {
            this.isDirty = true;
        });
    }

    // TODO: refactor update system to use this
    public void calculateConnectedTerrain()
    {
        foreach (Population population in Consumers)
        {
            CountConnectedTerrain(population);
        }
    }

    /// <summary>
    /// Count number of connected terrain of each type for terrain need
    /// </summary>
    /// <param name="population"></param>
    /// <returns></returns>
    public Dictionary<TileType, List<int>> CountConnectedTerrain(Population population)
    {
        List<Vector3Int> accessiblePositions = rpm.GetLocationsWithAccess(population);

        HashSet<Vector3Int> closed = new HashSet<Vector3Int>();

        Dictionary<TileType, List<int>> ConnectedTilesByType = new Dictionary<TileType, List<int>>();


        foreach (Vector3Int position in accessiblePositions)
        {
            if (closed.Contains(position)) continue;

            TileType type = gridSystem.GetGameTileAt(position).type;
            if (!ConnectedTilesByType.ContainsKey(type))
            {
                ConnectedTilesByType.Add(type, new List<int>());
            }
            ConnectedTilesByType[type].Add(DFS(population, position, ref closed, type));
        }

        // For debugging
        foreach (KeyValuePair<TileType, List<int>> pair in ConnectedTilesByType)
        {
            string output = population.name + " ";
            output += pair.Key + ": {";
            for (int i = 0; i < pair.Value.Count; i++)
            {
                output += pair.Value[i];
                if (i != pair.Value.Count - 1)
                    output += ", ";
            }
            output += "}";
            Debug.Log(output);
        }

        return ConnectedTilesByType;
    }

    static int[] rowNbr = { -1, 0, 0, 1 };
    static int[] colNbr = { 0, -1, 1, 0 };
    private int DFS(Population population, Vector3Int position, ref HashSet<Vector3Int> closed, TileType type)
    {

        int total = 1;

        closed.Add(position);

        for (int i = 0; i < rowNbr.Length; i++)
        {
            Vector3Int next = position;
            next += new Vector3Int(colNbr[i], rowNbr[i], 0);
            if (closed.Contains(next)) continue;
            if (rpm.CanAccess(population, position) && gridSystem.GetGameTileAt(next) != null && gridSystem.GetGameTileAt(next).type == type)
            {
                total += DFS(population, next, ref closed, type);
            }
        }
        return total;
    }

    /// <summary>
    /// Get the terrian conposition of the consumers and update the need values
    /// </summary>
    public override void UpdateSystem()
    {
        // Unmark dirty when there is not consumer then exit
        if (this.Consumers.Count == 0)
        {
            this.isDirty = false;
            return;
        }
        
        //All tiles in the grid that can be accessed by some population, organized by tiletype
        Dictionary<TileType, HashSet<Vector3Int>> accessibleTilesByTileType = new Dictionary<TileType, HashSet<Vector3Int>>();
        //All populations currently in the level, organized by speciesType
        Dictionary<SpeciesType, HashSet<Population>> populationsBySpeciesType = new Dictionary<SpeciesType, HashSet<Population>>();
        //Number of tiles needed by a given population
        Dictionary<Population, int> tilesNeeded = new Dictionary<Population, int>();
        //Number of tiles allocated to a given population
        Dictionary<Population, Dictionary<TileType, int>> tilesAllocated = new Dictionary<Population, Dictionary<TileType, int>>();
        
        int sumAllocatedTiles(Population population)
        {
            int sum = 0;
            foreach(TileType tile in tilesAllocated[population].Keys)
            {
                int tileContribution = (tile == TileType.Grass ? 2 : 1);
                sum += tilesAllocated[population][tile] * tileContribution;
            }
            return sum;
        }

        //Organize the populations currently in the level by their SpeciesType
        foreach(Population population in Consumers.OfType<Population>())
        {
            SpeciesType type = population.Species.Species;
            if(!populationsBySpeciesType.ContainsKey(type))
                populationsBySpeciesType.Add(type, new HashSet<Population>());

            populationsBySpeciesType[type].Add(population);
        }

        //Calculate the tiles needed for each population and add all the tiles that the population can access to the tile dictionary
        foreach(HashSet<Population> populations in populationsBySpeciesType.Values)
        {
            foreach (Population population in populations)
            {
                tilesNeeded[population] = population.Count * population.species.TerrainTilesRequired;
                tilesAllocated[population] = new Dictionary<TileType, int>();

                foreach(Vector3Int position in rpm.GetLocationsWithAccess(population))
                {
                    TileType type = gridSystem.GetTileData(position).currentTile.type;

                    if(!accessibleTilesByTileType.ContainsKey(type))
                        accessibleTilesByTileType.Add(type, new HashSet<Vector3Int>());

                    accessibleTilesByTileType[type].Add(position);
                }
            }
        }

        //Iterate through all of the tiles and assign them to populations according to their dominance ratios
        foreach (TileType tile in terrainOrder)
        {
            //If there are no tiles of this tile type in the level, move on to the next tile type
            if(!accessibleTilesByTileType.ContainsKey(tile))
                continue;

            //Get all of the populations that require this TileType
            List<Population> accessiblePopulations = new List<Population>();
            foreach(SpeciesType species in dominanceRatiosByTileType[tile].Keys)
            {
                if(populationsBySpeciesType.ContainsKey(species))
                    accessiblePopulations.AddRange(populationsBySpeciesType[species]);
            }

            //If no populations require this tileType, move on to next tile type
            if(accessiblePopulations.Count == 0)
                continue;

            //Keep assigning tiles to the accessiblePopulations until either:
            // 1. All populations in accessiblePopulations have their terrain need satisfied, or
            // 2. There are no tiles left to assign
            while(true)
            {
                //Convert the remaining accessible tiles into a list so we can remove tiles from the HashSet as we iterate on it
                Vector3Int[] tileArray = accessibleTilesByTileType[tile].ToArray<Vector3Int>();
                if(tileArray.Length == 0)
                    break;

                //Calculate the number of tiles of the current tile type allocated to a population taking into account its dominance ratio (lower ratios return a higher weighted value, resulting in less total tiles received)
                float allocatedTilesWeighted(Population population)
                {
                    if(!tilesAllocated[population].ContainsKey(tile))
                        return 0;
                        
                    return tilesAllocated[population][tile] / dominanceRatiosByTileType[tile][population.species.Species];
                }

                //Find the population with the least tiles allocated (weighted). Also find the next lowest weighted allocation, to use as a stopping place
                Population populationMostInNeed = null;
                float minAllocation = float.MaxValue;
                float secondLeastAllocation = float.MaxValue;
                foreach(Population population in accessiblePopulations)
                {
                    //If this population has already satisfied its terrain need, move on to the next one
                    if(sumAllocatedTiles(population) >= tilesNeeded[population])
                        continue;

                    float allocation = allocatedTilesWeighted(population);
                    if(allocation < minAllocation)
                    {
                        populationMostInNeed = population;
                        secondLeastAllocation = minAllocation;
                        minAllocation = allocation;
                    }
                }

                //If there are no populations left that need the current tile type, move on to the next tile type
                if(populationMostInNeed == null)
                    break;

                //Protect against the edge case where a population needs this terrain type but can't reach any of the tiles
                bool populationCanAccess = false;

                //Iterate through the array of all tiles of the current tile type. Stop if:
                // 1. There are no tiles left
                // 2. This population surpasses the next most-in-need population for tiles allocated
                // 3. This population has its terrain need satisfied
                int tileIndex = 0;
                while(tileIndex < tileArray.Length && allocatedTilesWeighted(populationMostInNeed) < secondLeastAllocation) //  && sumAllocatedTiles(populationMostInNeed) < tilesNeeded[populationMostInNeed]
                {
                    if (rpm.CanAccess(populationMostInNeed, tileArray[tileIndex]))
                    {
                        accessibleTilesByTileType[tile].Remove(tileArray[tileIndex]);

                        if(!tilesAllocated[populationMostInNeed].ContainsKey(tile))
                            tilesAllocated[populationMostInNeed][tile] = 0;

                        ++tilesAllocated[populationMostInNeed][tile];

                        populationCanAccess = true;
                    }

                    ++tileIndex;
                }

                //If we iterate through all of the current tile type and this population couldn't reach a single tile, remove them from the list of accessible populations
                if(!populationCanAccess)
                    accessiblePopulations.Remove(populationMostInNeed);
            }
        }

        //Go through each tile type and update the need value for that tile for each population that needs it
        foreach (TileType tile in terrainOrder)
        {
            foreach(HashSet<Population> populationSet in populationsBySpeciesType.Values)
            {
                foreach(Population population in populationSet)
                {
                    string needName = tile.ToString();
                    // if (needName.Equals("Liquid"))
                    // {
                    //     population.UpdateNeed(needName, rpm.GetLiquidComposition(population).Count);
                    //     Debug.Log(needName + " tiles allocated to " + population.Species.SpeciesName + ": " + rpm.GetLiquidComposition(population).Count);
                    // } else 
                    if(tilesAllocated[population].ContainsKey(tile)) {
                        population.UpdateNeed(needName, tilesAllocated[population][tile] * (tile == TileType.Grass ? 2 : 1));
                        //Debug.Log(needName + " tiles allocated to " + population.Species.SpeciesName + ": " + tilesAllocated[population][tile]);
                    }
                }
            }
        }

        foreach (FoodSource foodSource in Consumers.OfType<FoodSource>())
        {
            int[] terrainCountsByType = new int[(int)TileType.TypesOfTiles];
            terrainCountsByType = gridSystem.CountOfTilesInArea(gridSystem.WorldToCell(foodSource.GetPosition()), foodSource.Species.Size);
            // Update need values
            foreach (var (count, index) in terrainCountsByType.WithIndex())
            {
                string needName = ((TileType)index).ToString();

                if (foodSource.GetNeedValues().ContainsKey(needName))
                {
                    // if (needName.Equals("Liquid"))
                    // {
                    //     int liquidCount = gridSystem.CountOfTilesInRange(gridSystem.WorldToCell(foodSource.GetPosition()), foodSource.Species.RootRadius)[index];
                    //     //Debug.Log(foodSource.name + " updated " + needName + " with value: " + liquidCount);
                    //     foodSource.UpdateNeed(needName, liquidCount);
                    //     continue;
                    // }
                    //Debug.Log(foodSource.name + " updated " + needName + " with value: " + count);
                    foodSource.UpdateNeed(needName, count);
                }
            }
        }

        this.isDirty = false;
    }

    //Compares two populations based on the number of tiles allocated, weighted by their dominance ratio
    private class DominanceComparer : IComparer<Population>
    {
        private Dictionary<Population, Dictionary<TileType, int>> tilesAllocated;
        private TileType tileType;
        public DominanceComparer(TileType tile, Dictionary<Population, Dictionary<TileType, int>> allocated)
        {
            tileType = tile;
            tilesAllocated = allocated;
        }

        public int Compare(Population x, Population y)
        {
            return (int) 
               (tilesAllocated[x][tileType] / dominanceRatiosByTileType[tileType][x.Species.Species] -
                tilesAllocated[y][tileType] / dominanceRatiosByTileType[tileType][y.Species.Species]);
        }
    }
}

/// <summary>
/// To use .WithIndex
/// </summary>
public static class ForeachExtension
{
    public static  IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
    {
        return self.Select((item, index) => (item, index));
    }
}