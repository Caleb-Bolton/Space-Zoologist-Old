﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Setup function to give back home locations of given population
/// <summary>
/// Translates the tilemap into a 2d array for keeping track of object locations.
/// </summary>
/// PlaceableArea transparency can be increased or decreased when adding it
public class GridSystem : MonoBehaviour
{
    [HideInInspector]
    public PlacementValidation PlacementValidation = default;
    [SerializeField] public Grid Grid = default;
    [SerializeField] public Tilemap PlacableArea = default;
    [SerializeField] public SpeciesReferenceData SpeciesReferenceData = default;
    [SerializeField] private ReservePartitionManager RPM = default;
    [SerializeField] private TileSystem TileSystem = default;
    [SerializeField] private PopulationManager PopulationManager = default;
    [SerializeField] private Tilemap TilePlacementValidation = default;
    [SerializeField] private GameTile Tile = default;
    [Header("Used to define 2d array")]
    [SerializeField] private int ReserveWidth = default;
    [SerializeField] private int ReserveHeight = default;
    public Vector3Int startTile = default;
    // Food and home locations updated when added, animal locations updated when the store opens up.
    public CellData[,] CellGrid = default;
    public TileData TilemapData = default;

    private void Awake()
    {
        this.CellGrid = new CellData[this.ReserveWidth, this.ReserveHeight];
        for (int i=0; i<this.ReserveWidth; i++)
        {
            for (int j=0; j<this.ReserveHeight; j++)
            {
                Vector3Int loc = new Vector3Int(i, j, 0);
                if (startTile == default && PlacableArea.HasTile(loc))
                {
                    startTile = loc;
                }
                this.CellGrid[i, j] = new CellData(PlacableArea.HasTile(loc));
            }
        }
    }

    private void Start()
    {
        this.PlacementValidation = this.gameObject.GetComponent<PlacementValidation>();
        this.PlacementValidation.Initialize(this, this.TileSystem, this.SpeciesReferenceData);
    }

    public bool isCellinGrid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < ReserveWidth && y < ReserveHeight;
    }

    /// <summary>
    /// Called when the store is opened
    /// </summary>
    public void UpdateAnimalCellGrid()
    {
        // Reset previous locations
        for (int i=0; i<this.CellGrid.GetLength(0); i++)
        {
            for (int j=0; j<this.CellGrid.GetLength(1); j++)
            {
                this.CellGrid[i, j].ContainsAnimal = false;
                this.CellGrid[i, j].HomeLocation = false;
                this.CellGrid[i, j].ContainsLiquid = false;
            }
        }
        // Could be broken up for better efficiency since iterating through population twice
        // this.PopulationHomeLocations = this.RecalculateHomeLocation();
        // Update locations and grab reference to animal GameObject (for future use)
        foreach (Population population in this.PopulationManager.Populations)
        {
            foreach (GameObject animal in population.AnimalPopulation)
            {
                Vector3Int animalLocation = this.Grid.WorldToCell(animal.transform.position);
                this.CellGrid[animalLocation.x, animalLocation.y].ContainsAnimal = true;
                this.CellGrid[animalLocation.x, animalLocation.y].Animal = animal;
            }
        }
    }

    public Vector3Int FindClosestLiquidSource(Population population, GameObject animal)
    {
        Vector3Int itemLocation = new Vector3Int(-1, -1, -1);
        float closestDistance = 10000f;
        float localDistance = 0f;
        for (int x = 0; x < this.CellGrid.GetLength(0); x++)
        {
            for (int y = 0; y < this.CellGrid.GetLength(1); y++)
            {
                // if contains liquid tile, check neighbors accessibility
                GameTile tile = this.TileSystem.GetGameTileAt(new Vector3Int(x, y, 0));
                if (tile != null && tile.type == TileType.Liquid)
                {
                    this.CellGrid[x, y].ContainsLiquid = true;
                    if (population.Grid.IsAccessible(x + 1, y))
                    {
                        localDistance = this.CalculateDistance(animal.transform.position.x, animal.transform.position.y, x, y);
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x + 1, y, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x - 1, y))
                    {
                        localDistance = this.CalculateDistance(animal.transform.position.x, animal.transform.position.y, x, y);
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x - 1, y, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x, y + 1))
                    {
                        localDistance = this.CalculateDistance(animal.transform.position.x, animal.transform.position.y, x, y);
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x, y + 1, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x, y - 1))
                    {
                        localDistance = this.CalculateDistance(animal.transform.position.x, animal.transform.position.y, x, y);
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x, y - 1, 0);
                        }
                    }
                    
                }
            }
        }
        return itemLocation;
    }

    public bool IsWithinGridBounds(Vector3 mousePosition)
    {
        Vector3Int loc = Grid.WorldToCell(mousePosition);
        return PlacableArea.HasTile(loc);
    }

    // Will need to make the grid the size of the max tilemap size
    public AnimalPathfinding.Grid GetGridWithAccess(Population population)
    {
        // Debug.Log("Setting up pathfinding grid");
        bool[,] tileGrid = new bool[ReserveWidth, ReserveHeight];
        this.SetupMovementBoundaires(tileGrid);
        for (int x=0; x<ReserveWidth; x++)
        {
            for (int y=0; y<ReserveHeight; y++)
            {
                if (RPM.CanAccess(population, new Vector3Int(x, y, 0)))
                {
                    tileGrid[x, y] = true;
                }
                else
                {
                    tileGrid[x, y] = false;
                }
            }
        }
        return new AnimalPathfinding.Grid(tileGrid, this.Grid);
    }

    private float CalculateDistance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
    }

    private void SetupMovementBoundaires(bool[,] tileGrid)
    {
        for (int i = 0; i < this.ReserveWidth; i++)
        {
            for (int j = 0; j < this.ReserveHeight; j++)
            {
                Vector3Int loc = new Vector3Int(i, j, 0);
                tileGrid[i, j] = PlacableArea.HasTile(loc);
            }
        }
    }

    public void AddFood(Vector3Int gridPosition, int size, GameObject foodSource)
    {
        int radius = size / 2;
        Vector3Int pos;
        int offset = 0;
        if (size % 2 == 0)
        {
            offset = 1;
        }
        // Check if the whole object is in bounds
        for (int x = (-1) * (radius - offset); x <= radius; x++)
        {
            for (int y = (-1) * (radius - offset); y <= radius; y++)
            {
                pos = gridPosition;
                pos.x += x;
                pos.y += y;
                CellGrid[pos.x, pos.y].ContainsFood = true;
                CellGrid[pos.x, pos.y].Food = foodSource;
            }
        }
    }

    // Remove the food source on this tile, a little inefficient
    public void RemoveFood(Vector3Int gridPosition)
    {
        Vector3Int pos = gridPosition;
        if (!CellGrid[pos.x, pos.y].ContainsFood) return;

        GameObject foodSource = CellGrid[pos.x, pos.y].Food;
        int size = foodSource.GetComponent<FoodSource>().Species.Size;

        for (int dx = -size; dx < size; dx++)
        {
            for (int dy = -size; dy < size; dy++)
            {
                CellData cell = CellGrid[pos.x + dx, pos.y + dy];
                if (cell.ContainsFood && ReferenceEquals(cell.Food, foodSource))
                {
                    CellGrid[pos.x + dx, pos.y + dy].ContainsFood = false;
                    CellGrid[pos.x + dx, pos.y + dy].Food = null;
                }
            }
        }
    }

    public struct CellData
    {
        public CellData(bool inBounds)
        {
            this.ContainsFood = false;
            this.ContainsAnimal = false;
            this.Food = null;
            this.Animal = null;
            this.Machine = null;
            this.ContainsLiquid = false;
            this.HomeLocation = false;
            this.OutOfBounds = !inBounds;
        }

        public bool ContainsLiquid { get; set; }
        public GameObject Machine { get; set; }
        public bool ContainsFood { get; set; }
        public GameObject Food { get; set; }
        public bool ContainsAnimal { get; set; }
        public GameObject Animal { get; set; }
        public bool HomeLocation { get; set; }
        public bool OutOfBounds { get; set; }
    }

    public struct TileData
    {
        public TileData(int n)
        {
            this.NumDirtTiles = 0;
            this.NumGrassTiles = 0;
            this.NumSandTiles = 0;
        }

        public int NumGrassTiles { get; set; }
        public int NumDirtTiles { get; set; }
        public int NumSandTiles { get; set; }
    }
}
