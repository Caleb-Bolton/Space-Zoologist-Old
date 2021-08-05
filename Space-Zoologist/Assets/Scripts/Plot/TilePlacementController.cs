using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
public class TilePlacementController : MonoBehaviour
{
    private enum PlacementResult { Placed, Restricted, AlreadyExisted }
    public bool isBlockMode { get; set; } = false;
    public bool PlacementPaused { get; private set; }
    [SerializeField] private Camera currentCamera = default;
    public bool isPreviewing { get; set; } = false;
    private bool godMode = false;
    private Vector3Int dragStartPosition = Vector3Int.zero;
    private Vector3Int lastMouseCellPosition = Vector3Int.zero;
    private Vector3Int currentMouseCellPosition = Vector3Int.zero;
    private Grid grid;
    private Vector3Int lastPlacedTile;
    private List<GameTile> referencedTiles = new List<GameTile>();
    private bool isFirstTile;
    public bool isErasing = false;
    public Tilemap[] allTilemaps { get { return tilemaps; } }
    [SerializeField] private Tilemap[] tilemaps = default; // Order according to GridUtils.TileLayer
    public GameTile[] gameTiles { get; private set; } = default;
    public HashSet<Vector3Int> addedTiles = new HashSet<Vector3Int>(); // All NEW tiles
    private Dictionary<Vector3Int, Dictionary<Color, Tilemap>> removedTileColors = new Dictionary<Vector3Int, Dictionary<Color, Tilemap>>();
    private HashSet<Vector3Int> triedToPlaceTiles = new HashSet<Vector3Int>(); // New tiles and same tile
    private HashSet<Vector3Int> neighborTiles = new HashSet<Vector3Int>();
    private Dictionary<GameTile, List<Tilemap>> colorLinkedTiles = new Dictionary<GameTile, List<Tilemap>>();
    private BuildBufferManager buildBufferManager;
    private int lastCornerX;
    private int lastCornerY;
    [SerializeField] private GridSystem GridSystem = default;
    private void Awake()
    {
        this.gameTiles = Resources.LoadAll("Tiles", typeof(GameTile)).Cast<GameTile>().ToArray(); // Load tiles form resources
        grid = GetComponent<Grid>();
        foreach (Tilemap tilemap in tilemaps)// Construct list of affected colors
        {
            List<Vector3Int> colorInitializeTiles = new List<Vector3Int>();
            /*            if (tilemap.TryGetComponent(out TileColorManager tileColorManager))
                        {
                            foreach (GameTile tile in tileColorManager.linkedTiles)
                            {
                                if (!colorLinkedTiles.ContainsKey(tile))
                                {
                                    colorLinkedTiles.Add(tile, new List<Tilemap>());
                                }
                                colorLinkedTiles[tile].Add(tilemap);
                            }
                        }*/
            referencedTiles = this.gameTiles.ToList();
            // are different linked tiles (water) supposed to have differing color?
            //RenderColorOfColorLinkedTiles(colorInitializeTiles);
            referencedTiles.Clear();
        }
        this.gameObject.GetComponent<PlotIO>().Initialize();
        this.buildBufferManager = FindObjectOfType<BuildBufferManager>();
    }
    private void Update()
    {
        if (isPreviewing) // Update for preview
        {
            Vector3 mouseWorldPosition = currentCamera.ScreenToWorldPoint(Input.mousePosition);
            this.currentMouseCellPosition = grid.WorldToCell(mouseWorldPosition);
            this.PlacementPaused = false;
            if (this.currentMouseCellPosition != this.lastMouseCellPosition || this.isFirstTile)
            {
                if (isErasing)
                {
                    this.EraseTile();
                    return;
                }
                if (isBlockMode)
                {
                    UpdatePreviewBlock();
                }
                else
                {
                    UpdatePreviewPen();
                }
                this.lastMouseCellPosition = this.currentMouseCellPosition;
            }
        }
    }
    /// <summary>
    /// Start tile placement preview.
    /// </summary>
    /// <param name="tileID">The ID of the tile to preview its placement.</param>
    public void StartPreview(string tileID, bool godMode = false)
    {
        this.godMode = godMode;
        Vector3 mouseWorldPosition = currentCamera.ScreenToWorldPoint(Input.mousePosition);
        this.dragStartPosition = this.grid.WorldToCell(mouseWorldPosition);
        if (!Enum.IsDefined(typeof(TileType), tileID))
        {
            throw new System.ArgumentException(tileID + " was not found in the TilePlacementController's tiles");
        }
        this.isPreviewing = true;
        foreach (GameTile tile in gameTiles)
        {
            if (tile.type == (TileType)Enum.Parse(typeof(TileType), tileID))
            {
                // TODO remove when initial liquid values implemented
                if (tile.type.Equals(TileType.Liquid))
                {
                    tile.defaultContents = new float[] { 0.98f, 0, 0.02f };
                }
                this.referencedTiles.Add(tile);
            }
        }
        this.isFirstTile = true;
    }
    public void StopPreview()
    {
        this.godMode = false;
        isPreviewing = false;
        lastMouseCellPosition = Vector3Int.zero;
        GridSystem.ConfirmPlacement();

        // Set terrain modified flag
        this.GridSystem.HasTerrainChanged = true;
        this.GridSystem.ChangedTiles.UnionWith(addedTiles);

        // Clear all dics
        this.referencedTiles.Clear();
        this.removedTileColors.Clear();
        this.addedTiles.Clear();
        this.triedToPlaceTiles.Clear();
    }
    // does not account for walls
    public void EraseTile()
    {
        foreach (GameTile tile in this.referencedTiles)
        {
            GameTile currentTile = GridSystem.GetGameTileAt(this.currentMouseCellPosition);
            if (currentTile != null && currentTile == tile)
            {
                GridSystem.RemoveTile(this.currentMouseCellPosition);
            }
        }
    }
    public int PlacedTileCount()
    {
        return addedTiles.Count();
    }

    public void RevertChanges() // Go through each change and revert back to original
    {
        GridSystem.Revert();
        // figure out what is going on here
        /*
        if (tilemap.TryGetComponent(out TileContentsManager tileAttributes))
        {
            List<Vector3Int> changedTiles = tileAttributes.changedTilesPositions;
            changedTiles.AddRange(tileAttributes.addedTilePositions);
            tileAttributes.Revert();
            RenderColorOfColorLinkedTiles(changedTiles);
        }*/
        foreach (Vector3Int colorChangedTiles in removedTileColors.Keys)
        {
            removedTileColors[colorChangedTiles].Values.First().SetColor(colorChangedTiles, removedTileColors[colorChangedTiles].Keys.First());
        }
        removedTileColors.Clear();
        addedTiles.Clear();
        triedToPlaceTiles.Clear();
        StopPreview();
    }

    /*
    public void RenderColorOfColorLinkedTiles(List<Vector3Int> changedTiles) // Update color for linked tiles.
    {
        foreach (GameTile tile in referencedTiles)
        {
            if (colorLinkedTiles.Keys.Contains(tile))
            {
                foreach (Tilemap tilemap in colorLinkedTiles[tile])
                {
                    TileColorManager tileColorManager = tilemap.GetComponent<TileColorManager>();
                    foreach (Vector3Int addedTileLocation in changedTiles)
                    {
                        foreach (GameTile managedTile in tileColorManager.managedTiles)
                        {
                            foreach (Vector3Int affectedTileLocation in this.TileSystem.AllCellLocationsOfTileInRange(addedTileLocation, tileColorManager.coloringMethod.affectedRange, managedTile))
                            {
                                tileColorManager.SetTileColor(affectedTileLocation, managedTile);
                            }
                        }
                    }
                }
            }
        }
    }*/

    private void UpdatePreviewPen()
    {
        if (isFirstTile)
        {
            PlaceTile(currentMouseCellPosition);
            return;
        }
        if (!GridSystem.FourNeighborTileLocations(currentMouseCellPosition).Contains(lastPlacedTile)) // Detect non-continuous points, and linearly interpolate to fill the gaps
        {
            if (currentMouseCellPosition.x == lastPlacedTile.x)// Handles divide by zero exception
            {
                foreach (int y in GridSystem.Range(lastPlacedTile.y, currentMouseCellPosition.y))
                {
                    Vector3Int location = new Vector3Int(lastPlacedTile.x, y, currentMouseCellPosition.z);
                    PlaceTile(location);
                }
            }
            else
            {
                float gradient = (currentMouseCellPosition.y - lastPlacedTile.y) / (currentMouseCellPosition.x - lastPlacedTile.x);
                foreach (float x in GridSystem.RangeFloat(GridSystem.IncreaseMagnitude(lastPlacedTile.x, -0.5f), currentMouseCellPosition.x))
                {
                    float interpolatedY = gradient * (x - lastPlacedTile.x);
                    int incrementY = GridSystem.RoundTowardsZeroInt(interpolatedY);
                    Vector3Int interpolateTileLocation = new Vector3Int(GridSystem.RoundTowardsZeroInt(x), lastPlacedTile.y + incrementY, lastPlacedTile.z);
                    PlaceTile(interpolateTileLocation);
                }
            }
        }
        PlaceTile(currentMouseCellPosition);
    }

    private void UpdatePreviewBlock()
    {
        if (isFirstTile)
        {
            PlaceTile(dragStartPosition, false);
            lastCornerX = dragStartPosition.x;
            lastCornerY = dragStartPosition.y;
        }
        HashSet<Vector3Int> tilesToRemove = new HashSet<Vector3Int>();
        HashSet<Vector3Int> tilesToAdd = new HashSet<Vector3Int>();
        HashSet<Vector3Int> supposedTiles = new HashSet<Vector3Int>();
        foreach (int x in GridSystem.Range(dragStartPosition.x, currentMouseCellPosition.x))
        {
            foreach (int y in GridSystem.Range(dragStartPosition.y, currentMouseCellPosition.y))
            {
                supposedTiles.Add(new Vector3Int(x, y, currentMouseCellPosition.z));
                tilesToRemove.Add(new Vector3Int(x, y, currentMouseCellPosition.z));
            }
        }
        tilesToRemove.ExceptWith(addedTiles); // Forcing removal of all tiles not in bound to avoid leftover tile not being removed due to lagging and tick skipping, possible optimization
        Vector3Int sweepLocation = Vector3Int.zero;
        sweepLocation.z = currentMouseCellPosition.z;
        bool isXShrinking = (currentMouseCellPosition.x - dragStartPosition.x) * (currentMouseCellPosition.x - lastCornerX) < 0;
        bool isYShrinking = (currentMouseCellPosition.y - dragStartPosition.y) * (currentMouseCellPosition.y - lastCornerY) < 0;
        if (currentMouseCellPosition.x != lastCornerX || !isXShrinking)
        {
            foreach (int x in GridSystem.Range(lastCornerX, currentMouseCellPosition.x))
            {
                foreach (int y in GridSystem.Range(dragStartPosition.y, currentMouseCellPosition.y))
                {
                    sweepLocation.x = x;
                    sweepLocation.y = y;
                    tilesToAdd.Add(sweepLocation);
                }
            }
        }
        if (currentMouseCellPosition.y != lastCornerY || !isYShrinking)
        {
            foreach (int x in GridSystem.Range(dragStartPosition.x, currentMouseCellPosition.x))
            {
                foreach (int y in GridSystem.Range(lastCornerY, currentMouseCellPosition.y))
                {
                    sweepLocation.x = x;
                    sweepLocation.y = y;
                    if (!tilesToRemove.Contains(sweepLocation) && !tilesToAdd.Contains(sweepLocation))
                    {
                        tilesToAdd.Add(sweepLocation);
                    }
                }
            }
        }
        lastCornerX = currentMouseCellPosition.x;
        lastCornerY = currentMouseCellPosition.y;
    }

    private bool IsPlacable(Vector3Int cellPosition)
    {
        if (currentMouseCellPosition == dragStartPosition)
        {
            return true;
        }
        foreach (Vector3Int location in GridSystem.FourNeighborTileLocations(cellPosition))
        {
            if (triedToPlaceTiles.Contains(location))
            {
                return true;
            }
        }
        return false;
    }


    private PlacementResult PlaceTile(Vector3Int cellPosition, bool checkPlacable = true) //Main function controls tile placement
    {
        if (IsPlacable(cellPosition) || !checkPlacable)
        {
            // Check availability
            foreach (GameTile tile in referencedTiles)
            {
                // If animal/food at location
                if (!IsPositionFree(cellPosition))
                {
                    return PlacementResult.Restricted;
                }
                // If same tile
                if (this.GridSystem.GetGameTileAt(cellPosition) == tile)
                {
                    this.triedToPlaceTiles.Add(cellPosition);
                    return PlacementResult.AlreadyExisted;
                }
            }
            foreach (GameTile tile in referencedTiles)
            {
                GridSystem.AddTile(cellPosition, tile, godMode);
            }
            this.triedToPlaceTiles.Add(cellPosition);
            this.addedTiles.Add(cellPosition);
            
            return PlacementResult.Placed;
        }
        return PlacementResult.Restricted;
    }
    private void GetNeighborCellLocations(Vector3Int cellLocation, GameTile tile, Tilemap targetTilemap)
    {
        foreach (Vector3Int tileToCheck in GridSystem.FourNeighborTileLocations(cellLocation))
        {
            if (!neighborTiles.Contains(tileToCheck) && targetTilemap.GetTile(tileToCheck) == tile)
            {
                neighborTiles.Add(tileToCheck);
                GetNeighborCellLocations(tileToCheck, tile, targetTilemap);
            }
        }
    }
    private bool IsPositionFree(Vector3Int cellLocation)
    {
        if (godMode)
        {
            return true;
        }
        if (!GridSystem.IsWithinGridBounds(cellLocation))
        {
            return false;
        }
        GridSystem.TileData tileData = GridSystem.GetTileData(cellLocation);
        if (tileData.Food)
        {
            return false;
        }
        if (this.buildBufferManager.IsConstructing(cellLocation.x, cellLocation.y))
        {
            return false;
        }
        return true;
    }
}