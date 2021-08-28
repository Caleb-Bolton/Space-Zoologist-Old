using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

// Setup function to give back home locations of given population
/// <summary>
/// Translates the tilemap into a 2d array for keeping track of object locations.
/// </summary>
/// PlaceableArea transparency can be increased or decreased when adding it
public class GridSystem : MonoBehaviour
{
    #region Enumerations
    private enum Direction2D { X, Y }
    private enum CellStatus { Other, Self, Walked }
    private enum MoveType { Towards, Parallel1, Parallel2, Away }
    // using prime numbers instead of binary for flagging LMAO
    public enum TileFlag
    {
        HIGHLIGHT_FLAG = 0x2
    }
    private const float FLAG_VALUE_MULTIPLIER = 256f;
    #endregion

    #region Component References
    [SerializeField] public Grid Grid = default;
    [SerializeField] public SpeciesReferenceData SpeciesReferenceData = default;
    [SerializeField] private ReservePartitionManager RPM = default;
    [SerializeField] private PopulationManager PopulationManager = default;
    [SerializeField] public Tilemap Tilemap;
    private BuildBufferManager buildBufferManager;
    #endregion

    [Header("Used to define 2d array")]
    [SerializeField] private int ReserveWidth = default;
    [SerializeField] private int ReserveHeight = default;
    // Food and home locations updated when added, animal locations updated when the store opens up.
    // grid is accessed using [y, x] due to 2d array structure
    private TileData[,] TileDataGrid = default;
    public HashSet<Vector3Int> ChangedTiles = new HashSet<Vector3Int>();
    public HashSet<LiquidBody> liquidBodies { get; private set; } = new HashSet<LiquidBody>();
    public List<LiquidBody> previewBodies { get; private set; } = new List<LiquidBody>();
    private HashSet<Vector3Int> RemovedTiles = new HashSet<Vector3Int>();
    [SerializeField] private int quickCheckIterations = 6; //Number of tiles to quick check, if can't reach another tile within this many walks, try to generate new body by performing full check
                                                           // Increment by 2 makes a difference. I.E. even numbers, at least 6 to account for any missing tile in 8 surrounding tiles


    public Vector3Int startTile = default;
    List<Vector3Int> HighlightedTiles;
    private Texture2D TilemapTexture;

    public bool HasTerrainChanged = false;

    #region Monobehaviour Callbacks
    private void Awake()
    {
        // tilelayermanager stuff
        //InitializeTileLayerManager();
    }

    // do something about this
    private void InitializeTileLayerManager()
    {
        this.liquidBodies = new HashSet<LiquidBody>();
        this.previewBodies = new List<LiquidBody>();
        this.RemovedTiles = new HashSet<Vector3Int>();
        this.Tilemap.ClearAllTiles();
    }

    private void Start()
    {
        this.buildBufferManager = FindObjectOfType<BuildBufferManager>();

        for (int i = 0; i < ReserveWidth; ++i)
        {
            for (int j = 0; j < ReserveHeight; ++j)
            {
                ApplyChangesToTilemap(new Vector3Int(i, j, 0));
            }
        }

        try
        {
            EventManager.Instance.SubscribeToEvent(EventType.StoreOpened, () =>
            {
                this.ChangedTiles.Clear();
            });

            List<Vector3Int> changedTilesNoWall = new List<Vector3Int>();
            foreach (Vector3Int tilePosition in ChangedTiles)
            {
                if (this.GetGameTileAt(tilePosition).type != TileType.Wall)
                    changedTilesNoWall.Add(tilePosition);
            }

            EventManager.Instance.SubscribeToEvent(EventType.StoreClosed, () =>
            {
                // Invoke event and pass the changed tiles that are not walls
                EventManager.Instance.InvokeEvent(EventType.TerrainChange, changedTilesNoWall);
            });
        }
        catch { };
    }

    // note that this will not work on web browsers, will need to find alternate solution
    private void OnApplicationQuit()
    {
        // turn off grid toggle no matter what (material is permanently changed)
        Tilemap.GetComponent<TilemapRenderer>().sharedMaterial.SetFloat("_GridOverlayToggle", 0);
    }
    #endregion

    #region I/O
    public void ParseSerializedGrid(SerializedGrid serializedGrid, GameTile[] gameTiles)
    {
        InitializeTileLayerManager();

        // set grid dimensions
        ReserveWidth = serializedGrid.width;
        ReserveHeight = serializedGrid.height;
        this.TileDataGrid = new TileData[this.ReserveHeight, this.ReserveWidth];

        // set up the information textures
        TilemapTexture = new Texture2D(ReserveWidth + 1, ReserveHeight + 1);
        // make black texture
        for (int i = 0; i < ReserveWidth; ++i)
        {
            for (int j = 0; j < ReserveHeight; ++j)
                TilemapTexture.SetPixel(i, j, new Color(0, 0, 0, 0));
        }
        TilemapTexture.filterMode = FilterMode.Point;
        TilemapTexture.wrapMode = TextureWrapMode.Repeat;

        Vector3Int tilePosition = new Vector3Int();
        Dictionary<int, HashSet<Vector3Int>> liquidbodyIDToTiles = new Dictionary<int, HashSet<Vector3Int>>();

        foreach (SerializedLiquidBody serializedLiquidBody in serializedGrid.serializedTilemap.SerializedLiquidBodies)
            liquidbodyIDToTiles.Add(serializedLiquidBody.BodyID, new HashSet<Vector3Int>());

        foreach (SerializedTileData serializedTileData in serializedGrid.serializedTilemap.SerializedTileDatas)
        {
            // if the tile id is negative
            if (serializedTileData.TileID == -1)
            {
                for (int i = 0; i < serializedTileData.Repetitions; ++i)
                {
                    TileDataGrid[tilePosition.y, tilePosition.x] = new TileData(tilePosition, null);
                    TileDataGrid[tilePosition.y, tilePosition.x].isTilePlaceable = serializedTileData.Placeable;

                    // move x over first
                    tilePosition.x += 1;

                    // then add to the y after if overflow
                    if (tilePosition.x >= ReserveWidth)
                    {
                        tilePosition.y += 1;
                        tilePosition.x = 0;
                    }
                }
            }
            else
            {
                // set the starting tile here for flood filling
                if (startTile == default && serializedTileData.TileID != (int)TileType.Wall)
                    startTile = tilePosition;

                // search through game tiles for correct tile
                foreach (GameTile gameTile in gameTiles)
                {
                    if ((int)gameTile.type == serializedTileData.TileID)
                    {
                        // add tiles based on repetitions of the same tile
                        for (int i = 0; i < serializedTileData.Repetitions; ++i)
                        {
                            // manually add the tile (may turn into a method later)
                            TileDataGrid[tilePosition.y, tilePosition.x] = new TileData(tilePosition, gameTile);
                            TileDataGrid[tilePosition.y, tilePosition.x].isTilePlaceable = serializedTileData.Placeable;

                            // if it is a liquid, add it to the dictionary
                            if (gameTile.type == TileType.Liquid)
                                liquidbodyIDToTiles[serializedTileData.LiquidBodyID].Add(tilePosition);

                            // set the tile type in the red channel
                            Color pixelColor = TilemapTexture.GetPixel(tilePosition.x, tilePosition.y);
                            // add 1 to ensure a null tile type at 0
                            pixelColor.r = (serializedTileData.TileID + 1) / FLAG_VALUE_MULTIPLIER;
                            TilemapTexture.SetPixel(tilePosition.x, tilePosition.y, pixelColor);

                            // move the tile position along
                            tilePosition.x += 1;

                            // then add to the y after if overflow
                            if (tilePosition.x >= ReserveWidth)
                            {
                                tilePosition.y += 1;
                                tilePosition.x = 0;
                            }
                        }
                    }
                }
            }
        }

        foreach (SerializedLiquidBody serializedLiquidBody in serializedGrid.serializedTilemap.SerializedLiquidBodies)
        {
            // create the liquidbody based on previously parsed tiles
            LiquidBody liquidBody = new LiquidBody(liquidbodyIDToTiles[serializedLiquidBody.BodyID], serializedLiquidBody.Contents, serializedLiquidBody.BodyID);

            // add the liquidbody reference to the tile data
            foreach (Vector3Int liquidTilePosition in liquidbodyIDToTiles[serializedLiquidBody.BodyID])
                TileDataGrid[liquidTilePosition.y, liquidTilePosition.x].currentLiquidBody = liquidBody;

            // add it to the list
            this.liquidBodies.Add(liquidBody);
        }

        // add texture to material
        TilemapTexture.Apply();
        TilemapRenderer renderer = Tilemap.GetComponent<TilemapRenderer>();
        renderer.sharedMaterial.SetTexture("_GridInformationTexture", TilemapTexture);
        Tilemap.GetComponent<TilemapRenderer>().sharedMaterial.SetVector("_GridTextureDimensions", new Vector2(ReserveWidth, ReserveHeight));
        HighlightedTiles = new List<Vector3Int>();

        for (int i = 0; i < ReserveWidth; ++i)
        {
            for (int j = 0; j < ReserveHeight; ++j)
            {
                ApplyChangesToTilemap(new Vector3Int(i, j, 0));
            }
        }
    }

    public SerializedTilemap SerializedTilemap()
    {
        // Debug.Log("Serialize " + this.tilemap.name);
        // temporarily make new list until full implementation
        return new SerializedTilemap("Tilemap", TileDataGrid, ReserveWidth, ReserveHeight, this.liquidBodies);
    }
    #endregion

    #region Tile Accessors

    /// <summary>
    /// Method to access the TileDataGrid without confusion due to 2d array behaviour.
    /// </summary>
    /// <param name="tilePosition">Vector of tile position.</param>
    /// <returns>Tile data at that position.</returns>
    public TileData GetTileData(Vector3Int tilePosition)
    {
        if (IsWithinGridBounds(tilePosition)) {
            TileData td = TileDataGrid[tilePosition.y, tilePosition.x];

            if (td != null)
                return td;
        }

        return null;
    }

    public void AddTile(Vector3Int tilePosition, GameTile tile, bool godmode = false)
    {
        TileData tileData = GetTileData(tilePosition);

        if (godmode)
        {
            if (IsWithinGridBounds(tilePosition))
            {
                if (tile.type == TileType.Liquid)
                    tileData.PreviewLiquidBody(MergeLiquidBodies(tilePosition, tile));
                if (tile.type != TileType.Wall)
                    TileDataGrid[tilePosition.y, tilePosition.x].isTilePlaceable = true;
                tileData.PreviewReplacement(tile);
                ChangedTiles.Add(tilePosition);
                ApplyChangesToTilemap(tilePosition);
            }
            else if ((tilePosition.x >= ReserveWidth && tilePosition.y > 0) ||
                (tilePosition.x > 0 && tilePosition.y >= ReserveHeight))
            {
                // if it isn't within grid bounds and is bigger than the current reserve size
                // change the array such that it will now contain the new tile
                TileData[,] TileDataGridCopy = TileDataGrid.Clone() as TileData[,];

                // get new maximum boundaries
                int maxWidth = tilePosition.x >= ReserveWidth ? tilePosition.x + 1 : ReserveWidth;
                int maxHeight = tilePosition.y >= ReserveHeight ? tilePosition.y + 1 : ReserveHeight;
                // create the new array and texture
                TileDataGrid = new TileData[maxHeight, maxWidth];
                Texture2D newTilemapTexture = new Texture2D(maxWidth + 1, maxHeight + 1);

                // fill the array and texture with values
                for (int i = 0; i < TileDataGrid.GetLength(1); ++i)
                {
                    for (int j = 0; j < TileDataGrid.GetLength(0); ++j)
                    {
                        if (i < ReserveWidth && j < ReserveHeight)
                        {
                            TileDataGrid[j, i] = TileDataGridCopy[j, i];
                            newTilemapTexture.SetPixel(i, j, TilemapTexture.GetPixel(i, j));
                        }
                        else
                        {
                            TileDataGrid[j, i] = new TileData(new Vector3Int(i, j, 0));
                            newTilemapTexture.SetPixel(i, j, new Color(0, 0, 0, 0));
                        }
                    }
                }

                // update old values
                ReserveWidth = maxWidth;
                ReserveHeight = maxHeight;
                TileDataGrid[tilePosition.y, tilePosition.x] = new TileData(tilePosition);
                // set placeable here, but idk what to do lol
                if (tile.type != TileType.Wall)
                    TileDataGrid[tilePosition.y, tilePosition.x].isTilePlaceable = true;

                tileData = GetTileData(tilePosition);

                // add texture to the buffer
                newTilemapTexture.filterMode = FilterMode.Point;
                newTilemapTexture.wrapMode = TextureWrapMode.Repeat;
                newTilemapTexture.Apply();
                TilemapTexture = newTilemapTexture;

                TilemapRenderer renderer = Tilemap.GetComponent<TilemapRenderer>();
                renderer.sharedMaterial.SetTexture("_GridInformationTexture", TilemapTexture);
                Tilemap.GetComponent<TilemapRenderer>().sharedMaterial.SetVector("_GridTextureDimensions", new Vector2(ReserveWidth, ReserveHeight));

                if (tile.type == TileType.Liquid)
                    tileData.PreviewLiquidBody(MergeLiquidBodies(tilePosition, tile));
                tileData.PreviewReplacement(tile);
                ChangedTiles.Add(tilePosition);
                ApplyChangesToTilemap(tilePosition);
            }
        }
        else
        {
            if (tileData != null)
            {
                if (tile.type == TileType.Liquid)
                    tileData.PreviewLiquidBody(MergeLiquidBodies(tilePosition, tile));
                tileData.PreviewReplacement(tile);
                ChangedTiles.Add(tilePosition);
                ApplyChangesToTilemap(tilePosition);
            }
        }
        // TODO update color of liquidbody
    }
    public void RemoveTile(Vector3Int tilePosition)
    {
        TileData tileData = GetTileData(tilePosition);

        if (tileData != null)
        {
            RemovedTiles.Add(tilePosition);

            // resize the array if row/column is empty and is in outer ranges (may work on later)

            tileData.isTilePlaceable = false;
            if (tileData.currentTile.type == TileType.Liquid)
                DivideLiquidBody(tilePosition);
            tileData.PreviewReplacement(null);
            ChangedTiles.Add(tilePosition);
            ApplyChangesToTilemap(tilePosition);
        }
    }


    /// <summary>
    /// Returns TerrainTile(inherited from Tilebase) at given location of a cell within the Grid.
    /// </summary>
    /// <param name="cellLocation"> Position of the cell. </param>
    /// <returns></returns>
    public GameTile GetGameTileAt(Vector3Int cellLocation)
    {
        if (IsWithinGridBounds(cellLocation))
            return Tilemap.GetTile<GameTile>(cellLocation);

        return null;
    }

    /// <summary>
    /// Whether a tile exists at given location, regardless to overlapping
    /// </summary>
    /// <param name="cellLocation"></param>
    /// <param name="tile"></param>
    /// <returns></returns>
    public bool TileExistsAtLocation(Vector3Int cellLocation, GameTile tile)
    {
        return Tilemap.GetTile(cellLocation) == tile;
    }

    /// <summary>
    /// Returns contents within a tile, e.g. Liquid Composition. If tile has no content, returns null.
    /// </summary>
    /// <param name="cellPosition"> Position of the cell. </param>
    /// <returns></returns>
    public float[] GetTileContentsAt(Vector3Int cellPosition, GameTile tile = null)
    {
        tile = tile == null ? GetGameTileAt(cellPosition) : tile;
        if (tile != null)
        {
            return GetTileData(cellPosition).currentLiquidBody.contents;
        }
        return null;
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

                if (GetTileData(pos) != null)
                    GetTileData(pos).Food = foodSource;
            }
        }
    }

    // Remove the food source on this tile, a little inefficient
    public void RemoveFood(Vector3Int gridPosition)
    {
        Vector3Int pos = gridPosition;
        GameObject foodSource = GetTileData(pos).Food;
        if (!foodSource) return;

        int size = foodSource.GetComponent<FoodSource>().Species.Size;

        for (int dx = -size; dx < size; dx++)
        {
            for (int dy = -size; dy < size; dy++)
            {
                TileData cell = GetTileData(new Vector3Int(pos.x + dx, pos.y + dy, 0));
                if (cell.Food && ReferenceEquals(cell.Food, foodSource))
                {
                    cell.Food = null;
                }
            }
        }
    }
    #endregion

    // no idea what this function actually does (naming wrong?)
    public void ConfirmPlacement()
    {
        foreach (Vector3Int tilePosition in this.ChangedTiles)
        {
            if (GetTileData(tilePosition) != null)
                GetTileData(tilePosition).ConfirmReplacement();
        }
        foreach (LiquidBody previewLiquidBody in this.previewBodies) // If there is liquid body
        {
            foreach (Vector3Int tilePosition in previewLiquidBody.tiles)
            {
                GetTileData(tilePosition).ConfirmReplacement();
            }
            this.liquidBodies.ExceptWith(previewLiquidBody.referencedBodies);
            previewLiquidBody.ClearReferencedBodies();
            this.GenerateNewLiquidBodyID(previewLiquidBody);
            this.liquidBodies.Add(previewLiquidBody);
        }
        foreach (Vector3Int tilePosition in this.RemovedTiles)
        {
            this.GetTileData(tilePosition).ConfirmReplacement();
        }
        this.ClearAll();
    }
    // no idea either
    public void Revert()
    {
        foreach (LiquidBody previewLiquidBody in this.previewBodies) // If there is liquid body
        {
            this.ChangedTiles.UnionWith(previewLiquidBody.tiles);
            previewLiquidBody.Clear();
        }
        foreach (Vector3Int changedTilePosition in this.ChangedTiles)
        {
            TileData tileData = GetTileData(changedTilePosition);

            tileData.Revert();
            this.ApplyChangesToTilemap(changedTilePosition);
            if (tileData.currentTile == null)
                tileData.Clear();
        }
        this.ClearAll();
    }
    // no idea either
    private void ClearAll()
    {
        this.RemovedTiles = new HashSet<Vector3Int>();
        this.ChangedTiles = new HashSet<Vector3Int>();
        this.previewBodies = new List<LiquidBody>();
    }
    private void ApplyChangesToTilemap(Vector3Int tilePosition)
    {
        TileData data = GetTileData(tilePosition);

        if (data == null || data.currentTile == null)
        {
            Color tilePixel = TilemapTexture.GetPixel(tilePosition.x, tilePosition.y);
            tilePixel.r = 0;
            TilemapTexture.SetPixel(tilePosition.x, tilePosition.y, tilePixel);
            TilemapTexture.Apply();

            Tilemap.SetTile(tilePosition, null);
        }
        else
        {
            Color tilePixel = TilemapTexture.GetPixel(tilePosition.x, tilePosition.y);
            tilePixel.r = ((int)data.currentTile.type + 1) / FLAG_VALUE_MULTIPLIER;
            TilemapTexture.SetPixel(tilePosition.x, tilePosition.y, tilePixel);
            TilemapTexture.Apply();

            Tilemap.SetTile(tilePosition, data.currentTile);
            Tilemap.SetTileFlags(tilePosition, TileFlags.None);
            Tilemap.SetColor(tilePosition, data.currentColor);
        }
    }

    #region Liquidbody Methods
    private void UpdatePreviewLiquidBody(LiquidBody liquidBody)
    {
        foreach (Vector3Int tileCellPosition in liquidBody.tiles)
        {
            GetTileData(tileCellPosition).PreviewLiquidBody(liquidBody);
        }
    }

    /// <summary>
    /// Changes composition of liquidbody at the specified position.
    /// Note: This only applies to liquid so far
    /// </summary>
    /// <param name="tilePosition">Vector of tile position.</param>
    /// <param name="contents">Contents to be added.</param>
    public void SetLiquidComposition(Vector3Int tilePosition, float[] contents)
    {
        TileData tileData = GetTileData(tilePosition);

        // check if it is even liquid
        if (tileData.currentTile.type != TileType.Liquid)
        {
            Debug.LogError(tilePosition + "Not a liquid tile.");
            return;
        }

        if (tileData != null)
        {
            contents.CopyTo(tileData.currentLiquidBody.contents, 0);

            EventManager.Instance.InvokeEvent(EventType.LiquidChange, tilePosition);
            // TODO Update color
            return;
        }

        Debug.LogError(tilePosition + "Does not have tile present");
    }
    /// <summary>
    /// Returns all liquid tiles belong to the same liquid body at the given location
    /// </summary>
    /// <param name="location">Cell location of any liquid tile within the body to change</param>
    /// <returns></returns>
    public List<Vector3Int> GetLiquidBodyPositions(Vector3Int location)
    {
        List<Vector3Int> liquidBodyTiles = new List<Vector3Int>();
        GameTile terrainTile = GetGameTileAt(location);
        liquidBodyTiles.Add(location);
        GetNeighborLiquidLocations(location, terrainTile, liquidBodyTiles);
        return liquidBodyTiles;
    }

    private void GetNeighborLiquidLocations(Vector3Int location, GameTile tile, List<Vector3Int> liquidBodyTiles)
    {
        foreach (Vector3Int tileToCheck in FourNeighborTileLocations(location))
        {
            if (
                Tilemap.GetTile(tileToCheck) == tile &&
                GetTileContentsAt(tileToCheck, tile) != null &&
                !liquidBodyTiles.Contains(tileToCheck))
            {
                liquidBodyTiles.Add(tileToCheck);
                GetNeighborLiquidLocations(tileToCheck, tile, liquidBodyTiles);
            }
            GameTile thisTile = (GameTile)Tilemap.GetTile(tileToCheck);
            float[] contents = GetTileContentsAt(tileToCheck, tile);
        }
    }

    // i don't even know where to begin
    private void GenerateNewLiquidBodyID(LiquidBody previewLiquidBody)
    {
        int newID = 1;
        while (true)
        {
            bool isSame = false;
            foreach (LiquidBody liquidBody in this.liquidBodies)
            {
                if (liquidBody.bodyID == newID)
                {
                    isSame = true;
                }
            }
            if (!isSame)
            {
                previewLiquidBody.bodyID = newID;
                break;
            }
            newID++;
        }
    }

    /// <summary>
    /// Merge Current Tile to existing liquid bodies
    /// </summary>
    /// <param name="tilePosition">Vector position of tile.</param>
    /// <param name="tile">Tile to be checked.</param>
    /// <returns>New liquid body created.</returns>
    private LiquidBody MergeLiquidBodies(Vector3Int tilePosition, GameTile tile)
    {
        HashSet<LiquidBody> neighborLiquidBodies = new HashSet<LiquidBody>();
        // look through neighbor tiles and count their liquid bodies
        foreach (Vector3Int neighborPosition in FourNeighborTileLocations(tilePosition))
        {
            TileData neighborTileData = GetTileData(neighborPosition);

            if (neighborTileData != null)
            {
                if (neighborTileData.currentTile == tile)
                {
                    neighborLiquidBodies.Add(neighborTileData.currentLiquidBody);
                }
            }
        }
        switch (neighborLiquidBodies.Count)
        {
            case 0: // Create new body
                HashSet<Vector3Int> newBodyTiles = new HashSet<Vector3Int>();
                newBodyTiles.Add(tilePosition);
                LiquidBody newBody = new LiquidBody(newBodyTiles, tile.defaultContents);
                this.previewBodies.Add(newBody);
                return newBody;

            case 1: // Extend the new one drawn, or extend existing body
                List<LiquidBody> liquidBodyL = neighborLiquidBodies.ToList();
                if (liquidBodyL[0].bodyID == 0) // Preview Liquid Body, newly placed tile
                {
                    liquidBodyL[0].AddTile(tilePosition, tile.defaultContents);
                    return liquidBodyL[0];
                }
                LiquidBody extendedBody = new LiquidBody(liquidBodyL[0], tilePosition);
                foreach (Vector3Int position in extendedBody.tiles)
                {
                    this.GetTileData(position).PreviewLiquidBody(extendedBody);
                }
                this.previewBodies.Add(extendedBody);
                return extendedBody;

            default: // Merge Multiple bodies, including new bodies generated by the placement
                LiquidBody mergedBody = new LiquidBody(neighborLiquidBodies);
                mergedBody.AddTile(tilePosition, tile.defaultContents);
                this.previewBodies.Add(mergedBody);
                foreach (LiquidBody liquidBody in mergedBody.referencedBodies)
                {
                    if (liquidBody.bodyID == 0)
                    {
                        previewBodies.Remove(liquidBody);
                    }
                }
                UpdatePreviewLiquidBody(mergedBody);
                return mergedBody;
        }
    }

    /// <summary>
    /// Divide existing liquid bodies
    /// </summary>
    /// <param name="tilePosition">Vector position of tile.</param>
    private void DivideLiquidBody(Vector3Int tilePosition)
    {
        HashSet<Vector3Int> remainingTiles = new HashSet<Vector3Int>();
        remainingTiles.UnionWith(GetTileData(tilePosition).currentLiquidBody.tiles);
        remainingTiles.ExceptWith(RemovedTiles);
        List<Vector3Int> neighborTiles = new List<Vector3Int>();
        foreach (Vector3Int neighborTile in FourNeighborTileLocations(tilePosition)) //Filter available liquid tiles
        {
            if (remainingTiles.Contains(neighborTile))
            {
                neighborTiles.Add(neighborTile);
            }
        }
        if (neighborTiles.Count <= 1) // No tile or only one tile. don't try anything
        {
            return;
        }
        bool isContinueous = true;
        CellStatus[,] historyArray = InitializeHistoryArray(tilePosition, remainingTiles); //Create a matrix as large as meaningful iterations can go, centered at the given position. 
        historyArray[quickCheckIterations / 2, quickCheckIterations / 2] = CellStatus.Other; //Make sure the origin is other. 
        List<Vector3Int> startingTiles = new List<Vector3Int>(neighborTiles); //Copy the list to use later
        while (neighborTiles.Count > 1)
        {
            if (QuickContinuityTest(tilePosition, neighborTiles[0], neighborTiles[1], historyArray))
            {
                neighborTiles.RemoveAt(0);
                ResetHistoryArray(historyArray);
                continue;
            }
            isContinueous = false;
            break;
        }
        Debug.Log("Quick continuity check result: " + isContinueous);
        if (!isContinueous) //perform complicated check and generate new bodies if necessary
        {
            List<LiquidBody> newBodies = GenerateDividedBodies(GetTileData(tilePosition).currentLiquidBody, tilePosition, startingTiles, remainingTiles);
            if (newBodies != null)
            {
                previewBodies.AddRange(newBodies);
                foreach (LiquidBody liquidBody in newBodies) // Remove Referenced Preview Body
                {
                    liquidBody.RemoveNestedReference();
                }
            }
        }
    }

    /// <summary>
    /// Creates list of divided liquid bodies.
    /// </summary>
    /// <param name="dividedBody">Liquid body to be divided.</param>
    /// <param name="dividePoint">Point of initial division.</param>
    /// <param name="startingTiles">Tiles of the initial liquid body.</param>
    /// <param name="remainingTiles">Leftover tiles after division.</param>
    /// <returns></returns>
    private List<LiquidBody> GenerateDividedBodies(LiquidBody dividedBody, Vector3Int dividePoint, List<Vector3Int> startingTiles, HashSet<Vector3Int> remainingTiles)
    {
        List<LiquidBody> newBodies = new List<LiquidBody>();
        foreach (Vector3Int tile in startingTiles)
        {
            bool skip = false;
            foreach (LiquidBody liquidBody in newBodies) //If a prior body generation included this tile, then skip generation using this tile
            {
                if (liquidBody.tiles.Contains(tile))
                {
                    skip = true;
                    Debug.Log(tile.ToString() + "skipped");
                    break;
                }
            }
            if (!skip)
            {
                newBodies.Add(new LiquidBody(dividedBody, remainingTiles, dividePoint, tile));

                Debug.Log("Generated divided body at" + tile.ToString() + "divide point: " + dividePoint.ToString());
            }
            if (remainingTiles.Count == 0)
            {
                Debug.Log("Tile depleted");
                break;
            }
        }
        if (newBodies.Count == 1) //They still belong to the same body, do not add new body
        {
            Debug.Log("Still same body");
            return null;
        }
        foreach (LiquidBody body in newBodies)
        {
            foreach (Vector3Int tilePosition in body.tiles)
            {
                this.GetTileData(tilePosition).PreviewLiquidBody(body);
            }
        }
        return newBodies;
    }
    #endregion

    #region Traversal Methods
    // TODO not performing as expected drawing left to right, also does not repathfind when stuck. 
    private bool QuickContinuityTest(Vector3Int origin, Vector3Int start, Vector3Int stop, CellStatus[,] historyArray) 
    {
        int iterations = 0;
        int maxDisplacement = this.quickCheckIterations / 2 - 1; // TODO optimization, when trying to access outside possible bound, terminate immediately
        int[] displacement = new int[2] { start.x - origin.x, start.y - origin.y };
        //Debug.Log("Starting point: " + displacement[0].ToString() + displacement[1].ToString());
        int[] targetDisplacement = new int[2] { stop.x - origin.x, stop.y - origin.y };
        TranslateDisplacementToArray(displacement, historyArray);
        while (iterations < this.quickCheckIterations)
        {
            if (displacement[0] == targetDisplacement[0] && displacement[1] == targetDisplacement[1]) // Success
            {
                return true;
            }
            // Try move towards target
            Direction2D direction = Mathf.Abs(displacement[0]) < Mathf.Abs(displacement[1]) ? Direction2D.X : Direction2D.Y; // Move to direction with smaller displacement
            // TODO refine path finding, repathfind in a different order to avoid dead ends if steps not depleted, especially change the order of the parallel movements.
            //Move Towards target direction
            if (CanMove(displacement, targetDisplacement, direction, historyArray, MoveType.Towards) || CanMove(displacement, targetDisplacement, SwitchDirection(direction), historyArray, MoveType.Towards))
            {
                iterations++;
                continue;
            }
            //Move in parallel
            if (CanMove(displacement, targetDisplacement, direction, historyArray, MoveType.Parallel1) || CanMove(displacement, targetDisplacement, SwitchDirection(direction), historyArray, MoveType.Parallel1))
            {
                iterations++;
                continue;
            }
            //Move Away from target direction
            if (CanMove(displacement, targetDisplacement, direction, historyArray, MoveType.Away) || CanMove(displacement, targetDisplacement, SwitchDirection(direction), historyArray, MoveType.Away))
            {
                iterations++;
                continue;
            }
            //Debug.Log("stuck");
            return false; //Stuck in somewhere
        }
        //Debug.Log("max iterations");
        return false;
    }
    private bool CanMove(int[] displacement, int[] targetDisplacement, Direction2D direction, CellStatus[,] historyArray, MoveType moveType)
    {
        int adder;
        switch (moveType)
        {
            case MoveType.Towards:
                adder = PathfindTowards(displacement[(int)direction], targetDisplacement[(int)direction]);
                break;
            case MoveType.Away:
                adder = -PathfindTowards(displacement[(int)direction], targetDisplacement[(int)direction]);
                break;
            case MoveType.Parallel1:
                adder = 1;
                break;
            default:
                adder = -1;
                break;
        }
        if (adder == 0)
        {
            return false;
        }
        int[] newDisp = new int[2] { displacement[0], displacement[1] };
        newDisp[(int)direction] += adder;
        if (TranslateDisplacementToArray(newDisp, historyArray))
        {
            displacement[(int)direction] += adder;
            //Debug.Log("passed");
            return true;
        }
        if (moveType == MoveType.Parallel1)
        {
            return CanMove(displacement, targetDisplacement, direction, historyArray, MoveType.Parallel2);
        }
        return false;
    }
    private int PathfindTowards(int start, int end)
    {
        if (start > end)
        {
            return -1;
        }
        if (start < end)
        {
            return 1;
        }
        return 0;
    }
    /// <summary>
    /// If successfully updated history array, then returns true, otherwise returns false
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="cellPosition"></param>
    /// <param name="historyArray"></param>
    /// <returns></returns>
    private bool TranslateDisplacementToArray(int[] displacement, CellStatus[,] historyArray)
    {
        int maxDisplacement = quickCheckIterations / 2 - 1;
        int x = displacement[0] + maxDisplacement;
        int y = displacement[1] + maxDisplacement;
        if (x < 0 || x >= quickCheckIterations - 1 || y < 0 || y >= quickCheckIterations - 1) // Out of bound
        {
            return false;
        }
        if (historyArray[x, y] != CellStatus.Self)
        {
            return false;
        }
        historyArray[x, y] = CellStatus.Walked;
        return true;
    }

    private void ResetHistoryArray(CellStatus[,] historyArray)
    {
        for (int i = 0; i < quickCheckIterations - 1; i++)
        {
            for (int j = 0; j < quickCheckIterations - 1; j++)
            {
                if (historyArray[i, j] == CellStatus.Walked)
                {
                    historyArray[i, j] = CellStatus.Self;
                }
            }
        }
    }
    private CellStatus[,] InitializeHistoryArray(Vector3Int originPosition, HashSet<Vector3Int> selfTiles)
    {
        CellStatus[,] historyArray = new CellStatus[quickCheckIterations - 1, quickCheckIterations - 1];
        Vector3Int startPoint = new Vector3Int(originPosition.x - quickCheckIterations / 2 + 1, originPosition.y - quickCheckIterations / 2 + 1, originPosition.z);
        Vector3Int cellToScan = new Vector3Int(startPoint.x, startPoint.y, startPoint.z);
        for (int i = 0; i < quickCheckIterations - 1; i++)
        {
            cellToScan.x = startPoint.x;
            cellToScan.x += i;
            for (int j = 0; j < quickCheckIterations - 1; j++)
            {
                cellToScan.y = startPoint.y;
                cellToScan.y += j;
                if (selfTiles.Contains(cellToScan))
                {
                    historyArray[i, j] = CellStatus.Self;
                }
            }
        }
        return historyArray;
    }
    #endregion

    #region Flagging and Tile Effects System
    /// <summary>
    /// Applies the flags to the tile in texture data. Flags can create different visual effects depending on the flag/tile.
    /// </summary>
    /// <param name="tilemap">Tilemap to be affected (mostly for terrain).</param>
    /// <param name="tilePosition">Position of tile in grid space.</param>
    /// <param name="flag">Compiled flags to be set.</param>
    /// <param name="color">Color of the tile (in cases of highlighting or other effects that require color).</param>
    public void ApplyFlagToTileTexture(Tilemap tilemap, Vector3Int tilePosition, TileFlag flag, Color color)
    {
        Color tilePixel = TilemapTexture.GetPixel(tilePosition.x, tilePosition.y);

        // currently only in alpha channel, can expand to rgb as well
        // note that color channels can only contain [0...1] due to samplers
        int alphaBitMask = (int)(tilePixel.a * FLAG_VALUE_MULTIPLIER);
        int flagInt = (int)flag;

        // if nothing there, just set the flag
        if (alphaBitMask == 0)
            alphaBitMask = flagInt;
        // otherwise if the flag isn't already there then add it
        else if (alphaBitMask % flagInt != 0)
            alphaBitMask *= flagInt;
        // if there is already the flag do nothing
        else
            return;

        tilemap.SetColor(tilePosition, color);

        tilePixel.a = (float)alphaBitMask / FLAG_VALUE_MULTIPLIER;
        TilemapTexture.SetPixel(tilePosition.x, tilePosition.y, tilePixel);
        TilemapTexture.Apply();

        // add to propertyblock
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        renderer.sharedMaterial.SetTexture("_GridInformationTexture", TilemapTexture);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tilemap">Tilemap to be affected (mostly for terrain).</param>
    /// <param name="tilePosition">Position of tile in grid space.</param>
    /// <param name="flag">Compiled flags to be set.</param>
    public void RemoveFlagsFromTileTexture(Tilemap tilemap, Vector3Int tilePosition, TileFlag flag)
    {
        Color tilePixel = TilemapTexture.GetPixel(tilePosition.x, tilePosition.y);

        int alphaBitMask = (int)(tilePixel.a * FLAG_VALUE_MULTIPLIER);
        int flagInt = (int)flag;

        // return if flag is not there
        if (alphaBitMask == 0 || alphaBitMask % flagInt != 0)
            return;

        alphaBitMask /= flagInt;

        if (flag == TileFlag.HIGHLIGHT_FLAG)
            tilemap.SetColor(tilePosition, Color.white);

        tilePixel.a = (float)alphaBitMask / FLAG_VALUE_MULTIPLIER;
        TilemapTexture.SetPixel(tilePosition.x, tilePosition.y, tilePixel);
        TilemapTexture.Apply();

        // add to propertyblock
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        renderer.sharedMaterial.SetTexture("_GridInformationTexture", TilemapTexture);
    }
    public void ToggleGridOverlay()
    {
        // toggle using shader here
        float currentToggleValue = Tilemap.GetComponent<TilemapRenderer>().material.GetFloat("_GridOverlayToggle");
        // set up methods for updating all or only some tilemaps
        Tilemap.GetComponent<TilemapRenderer>().sharedMaterial.SetFloat("_GridOverlayToggle", currentToggleValue == 0 ? 1 : 0);
    }

    public void ClearHighlights()
    {
        foreach (Vector3Int tilePosition in HighlightedTiles)
            RemoveFlagsFromTileTexture(Tilemap, tilePosition, TileFlag.HIGHLIGHT_FLAG);

        HighlightedTiles.Clear();
    }

    public void HighlightTile(Vector3Int tilePosition, Color color)
    {
        if (!HighlightedTiles.Contains(tilePosition))
        {
            HighlightedTiles.Add(tilePosition);
            
            ApplyFlagToTileTexture(Tilemap, tilePosition, TileFlag.HIGHLIGHT_FLAG, color);
        }
    }

    // super inefficient but not priority rn
    public void HighlightRadius(Vector3Int tilePosition, Color color, float radius)
    {
        for (int i = 0; i < ReserveHeight; ++i)
        {
            for (int j = 0; j < ReserveWidth; ++j)
            {
                Vector3Int loopedPosition = new Vector3Int(j, i, 0);
                if (Vector3Int.Distance(tilePosition, loopedPosition) <= radius)
                    HighlightTile(loopedPosition, color);
            }
        }
    }
    #endregion

    #region Boolean Methods and Validation

    public bool IsPodPlacementValid(Vector3 mousePosition, AnimalSpecies species)
    {
        Vector3Int gridPosition = WorldToCell(mousePosition);
        return this.CheckSurroundingTerrain(gridPosition, species);
    }

    public bool IsFoodPlacementValid(Vector3 mousePosition, Item selectedItem = null, FoodSourceSpecies species = null)
    {
        if (selectedItem)
            species = SpeciesReferenceData.FoodSources[selectedItem.ID];

        Vector3Int gridPosition = WorldToCell(mousePosition);
        return CheckSurroudingTiles(gridPosition, species);
    }

    private bool CheckSurroundingTerrain(Vector3Int cellPosition, AnimalSpecies selectedSpecies)
    {
        Vector3Int pos;
        GameTile tile;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                pos = cellPosition;
                pos.x += x;
                pos.y += y;
                tile = GetGameTileAt(pos);
                if (tile == null)
                {
                    return false;
                }
                
                HighlightTile(pos, selectedSpecies.AccessibleTerrain.Contains(tile.type) ? Color.green : Color.red);
            }
        }
        tile = GetGameTileAt(cellPosition);
        return selectedSpecies.AccessibleTerrain.Contains(tile.type);
    }

    private bool CheckSurroudingTiles(Vector3Int cellPosition, FoodSourceSpecies species)
    {
        // size 1 -> rad 0, size 3 -> rad 1 ...
        int radius = species.Size / 2;
        Vector3Int pos;
        bool isValid = true;
        int offset = 0;
        // Size is even, offset by 1
        if (species.Size % 2 == 0)
        {
            offset = 1;
        }
        // Check if the whole object is in bounds
        for (int x = (-1 - offset) * (radius - offset); x <= radius; x++)
        {
            for (int y = (-1 - offset) * (radius - offset); y <= radius; y++)
            {
                pos = cellPosition;
                pos.x += x;
                pos.y += y;
                if (!IsFoodPlacementValid(pos, species))
                {
                    isValid = false;
                    HighlightTile(pos, Color.red);
                }
                else
                {
                    HighlightTile(pos, Color.green);
                }
            }
        }
        return isValid;
    }

    public bool IsOnWall(Vector3Int pos)
    {
        // Prevent placing on walls
        GameTile selectedTile = GetGameTileAt(pos);
        if (selectedTile != null && selectedTile.type.Equals(TileType.Wall))
        {
            return true;
        }
        return false;
    }

    // helper function that checks the validity at one tile
    private bool IsFoodPlacementValid(Vector3Int pos, FoodSourceSpecies species)
    {
        if (!IsWithinGridBounds(pos))
        {
            return false;
        }

        // Prevent placing on items already there.
        TileData tileData = GetTileData(pos);
        if (tileData == null || tileData.Food)
        {
            return false;
        }
        if (IsOnWall(pos)) return false;
        if (this.buildBufferManager.IsConstructing(pos.x, pos.y))
        {
            return false;
        }
        GameTile selectedTile = GetGameTileAt(pos);

        if (selectedTile)
        {
            // Make sure the tile is acceptable
            foreach (TileType acceptablTerrain in species.AccessibleTerrain)
            {
                if (selectedTile.type.Equals(acceptablTerrain))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsCellinGrid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < ReserveWidth && y < ReserveHeight;
    }

    public bool IsWithinGridBounds(Vector3 mousePosition)
    {
        Vector3Int loc = Grid.WorldToCell(mousePosition);

        return IsCellinGrid(loc.x, loc.y);
    }
    #endregion

    // figure out if this still works
    public void UpdateAnimalCellGrid()
    {
        for (int i = 0; i < this.TileDataGrid.GetLength(0); i++)
        {
            for (int j = 0; j < this.TileDataGrid.GetLength(1); j++)
            {
                if (this.TileDataGrid[i, j] != null)
                {
                    this.TileDataGrid[i, j].Animal = null;
                    this.TileDataGrid[i, j].HomeLocation = false;
                }
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
                GetTileData(animalLocation).Animal = animal;
            }
        }
    }

    // this too
    public void updateVisualPlacement(Vector3Int gridPosition, Item selectedItem)
    {
        if (SpeciesReferenceData.FoodSources.ContainsKey(selectedItem.ID))
        {
            FoodSourceSpecies species = SpeciesReferenceData.FoodSources[selectedItem.ID];
            CheckSurroudingTiles(gridPosition, species);
        }
        else if (SpeciesReferenceData.AnimalSpecies.ContainsKey(selectedItem.ID))
        {
            AnimalSpecies species = SpeciesReferenceData.AnimalSpecies[selectedItem.ID];
            CheckSurroundingTerrain(gridPosition, species);
        }
        else
        {
            // TODO figure out how to determine if tile is placable
            // gridOverlay.HighlightTile(gridPosition, Color.green);
        }
    }

    #region Tile Utility and Calculations
    public Vector3Int GetReserveDimensions()
    {
        return new Vector3Int(ReserveWidth, ReserveHeight, 0);
    }


    public Vector3Int FindClosestLiquidSource(Population population, GameObject animal)
    {
        Vector3Int itemLocation = new Vector3Int(-1, -1, -1);
        float closestDistance = 10000f;
        float localDistance = 0f;
        for (int x = 0; x < ReserveWidth; x++)
        {
            for (int y = 0; y < ReserveHeight; y++)
            {
                // if contains liquid tile, check neighbors accessibility
                GameTile tile = GetGameTileAt(new Vector3Int(x, y, 0));
                if (tile != null && tile.type == TileType.Liquid)
                {
                    localDistance = Vector2.Distance(animal.transform.position, new Vector2(x, y));

                    if (population.Grid.IsAccessible(x + 1, y))
                    {
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x + 1, y, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x - 1, y))
                    {
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x - 1, y, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x, y + 1))
                    {
                        if (localDistance < closestDistance)
                        {
                            closestDistance = localDistance;
                            itemLocation = new Vector3Int(x, y + 1, 0);
                        }
                    }
                    if (population.Grid.IsAccessible(x, y - 1))
                    {
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

    // Will need to make the grid the size of the max tilemap size
    public AnimalPathfinding.Grid GetGridWithAccess(Population population)
    {
        // Debug.Log("Setting up pathfinding grid");
        bool[,] tileGrid = new bool[ReserveWidth, ReserveHeight];
        for (int x = 0; x < ReserveWidth; x++)
        {
            for (int y = 0; y < ReserveHeight; y++)
            {
                tileGrid[x, y] = RPM.CanAccess(population, new Vector3Int(x, y, 0));
            }
        }
        return new AnimalPathfinding.Grid(tileGrid, this.Grid);
    }
    /// <summary>
    /// Returns the cell location of the tile closest to the given center. List contains multiple if more than one tile at same distance.
    /// </summary>
    /// <param name="centerCellLocation">The cell location to calculate distance to</param>
    /// <param name="tile">The tile of interest</param>
    /// <param name="scanRange">1/2 side length of the scaned square (radius if isCircleMode = true)</param>
    /// <param name="isCircleMode">Enable circular scan. Default to false, scans a square of side length of scanRange * 2 + 1</param>
    /// <returns></returns>
    public List<Vector3Int> CellLocationsOfClosestTiles(Vector3Int centerCellLocation, GameTile tile, int scanRange = 8, bool isCircleMode = false)
    {
        int i = 0;
        int posX = 0;
        int posY = 0;
        List<Vector3Int> closestTiles = new List<Vector3Int>();
        while (i < scanRange)
        {
            if (isCircleMode && Mathf.Sqrt(posX * posX + posY * posY) > scanRange)
            {
                i++;
                posX = i;
                posY = 0;
                continue;
            }
            if (posX == 0)
            {
                if (GetGameTileAt(centerCellLocation) == tile)
                {
                    closestTiles.Add(centerCellLocation);
                    break;
                }
                i++;
                posX = i;
                continue;
            }
            if (posX == posY)
            {
                if (IsTileInAnyOfFour(posX, posY, centerCellLocation, tile))
                {
                    return TileCellLocationsInFour(posX, posY, centerCellLocation, tile);
                }
                i++;
                posX = i;
                posY = 0;
            }
            else
            {
                if (IsTileInAnyOfEight(posX, posY, centerCellLocation, tile))
                {
                    return TileCellLocationsInEight(posX, posY, centerCellLocation, tile);
                }
                posY++;
            }
        }
        return closestTiles;
    }

    /// <summary>
    /// Returns distance of cloest tiles with different tile contents. e.g.Liquid composition
    /// </summary>
    /// <param name="centerCellLocation">The cell location to calculate distance to</param>
    /// <param name="tile">The tile of interest</param>
    /// <param name="scanRange">1/2 side length of the scaned square (radius if isCircleMode = true)</param>
    /// <param name="isCircleMode">Enable circular scan. Default to false, scans a square of side length of scanRange * 2 + 1</param>
    /// <returns></returns>
    public Dictionary<float[], float> DistancesToClosestTilesOfEachBody(Vector3Int centerCellLocation, GameTile tile, int scanRange = 8, bool isCircleMode = false)
    {
        int[] distance3 = new int[3];
        int i = 0;
        int posX = 0;
        int posY = 0;
        Dictionary<float[], float> compositionDistancePairs = new Dictionary<float[], float>();
        while (i < scanRange)
        {
            float distance = Mathf.Sqrt(posX * posX + posY * posY);
            if (isCircleMode && distance > scanRange)
            {
                i++;
                posX = i;
                posY = 0;
                continue;
            }
            if (posX == 0)
            {
                if (GetGameTileAt(centerCellLocation) == tile)
                {
                    compositionDistancePairs.Add(GetTileContentsAt(centerCellLocation, tile), distance);
                }
                i++;
                posX = i;
                continue;
            }
            if (posX == posY)
            {
                if (IsTileInAnyOfFour(posX, posY, centerCellLocation, tile))
                {
                    foreach (Vector3Int cellLocation in TileCellLocationsInFour(posX, posY, centerCellLocation, tile))
                    {
                        float[] contents = GetTileContentsAt(cellLocation, tile);
                        if (!compositionDistancePairs.Keys.Contains(contents))
                        {
                            compositionDistancePairs.Add(GetTileContentsAt(cellLocation, tile), distance);
                        }
                    }
                }
                i++;
                posX = i;
                posY = 0;
            }
            else
            {
                if (IsTileInAnyOfEight(posX, posY, centerCellLocation, tile))
                {
                    foreach (Vector3Int cellLocation in TileCellLocationsInEight(posX, posY, centerCellLocation, tile))
                    {
                        float[] contents = GetTileContentsAt(cellLocation, tile);
                        if (!compositionDistancePairs.Keys.Contains(contents))
                        {
                            compositionDistancePairs.Add(GetTileContentsAt(cellLocation, tile), distance);
                        }
                    }
                }
                posY++;
            }
        }
        return compositionDistancePairs;
    }

    /// <summary>
    /// Returns distance of closet tile from a given cell position. Returns -1 if not found.
    /// </summary>
    /// <param name="centerCellLocation">The cell location to calculate distance to</param>
    /// <param name="tile">The tile of interest</param>
    /// <param name="scanRange">1/2 side length of the scanned square (radius if isCircleMode = true)</param>
    /// <param name="isCircleMode">Enable circular scan. Default to false, scans a square of side length of scanRange * 2 + 1</param>
    /// <returns></returns>
    public float DistanceToClosestTile(Vector3Int centerCellLocation, GameTile tile, int scanRange = 8, bool isCircleMode = false)
    {
        int i = 0;
        int posX = 0;
        int posY = 0;
        while (i < scanRange)
        {
            float distance = Mathf.Sqrt(posX * posX + posY * posY);
            if (isCircleMode && distance > scanRange)
            {
                i++;
                posX = i;
                posY = 0;
                continue;
            }
            if (posX == 0)
            {
                if (GetGameTileAt(centerCellLocation) == tile)
                {
                    return 0;
                }
                else
                {
                    i++;
                    posX = i;
                    continue;
                }
            }
            if (posX == posY)
            {
                if (IsTileInAnyOfFour(posX, posY, centerCellLocation, tile))
                {
                    return distance;
                }
                i++;
                posX = i;
                posY = 0;
            }
            else
            {
                if (IsTileInAnyOfEight(posX, posY, centerCellLocation, tile))
                {
                    return distance;
                }
                posY++;
            }
        }
        return -1;
    }

    /// <summary>
    /// Return all cell locations within a range from a center point
    /// </summary>
    /// <param name="centerCellLocation">Starting center point</param>
    /// <param name="scanRange">search radius</param>
    /// <returns></returns>
    /// TODO not working, don't use until fixed
    public List<Vector3Int> AllCellLocationsinRange(Vector3Int centerCellLocation, int scanRange)
    {
        List<Vector3Int> tileLocations = new List<Vector3Int>();
        Vector3Int scanLocation = new Vector3Int(0, 0, centerCellLocation.z);
        foreach (int x in Range(-scanRange, scanRange))
        {
            foreach (int y in Range(-scanRange, scanRange))
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance > scanRange)
                {
                    continue;
                }
                scanLocation.x = x + centerCellLocation.x;
                scanLocation.y = y + centerCellLocation.y;
                tileLocations.Add(scanLocation);
            }
        }
        return tileLocations;
    }


    /// <summary>
    /// Returns a list of locations of all tiles in a certain range
    /// </summary>
    /// <param name="centerCellLocation">The cell location to calculate range from</param>
    /// <param name="tile">The tile of interest</param>
    /// <param name="scanRange">1/2 side length of the scanned square (radius if isCircleMode = true)</param>
    /// <param name="isCircleMode">Enable circular scan. Default to false, scans a square of side length of scanRange * 2 + 1</param>
    /// <returns></returns>
    public List<Vector3Int> AllCellLocationsOfTileInRange(Vector3Int centerCellLocation, int scanRange, GameTile tile, bool isCircleMode = false)
    {
        List<Vector3Int> tileLocations = new List<Vector3Int>();
        Vector3Int scanLocation = new Vector3Int(0, 0, centerCellLocation.z);
        foreach (int x in Range(-scanRange, scanRange))
        {
            foreach (int y in Range(-scanRange, scanRange))
            {
                if (isCircleMode)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance > scanRange)
                    {
                        continue;
                    }
                }
                scanLocation.x = x + centerCellLocation.x;
                scanLocation.y = y + centerCellLocation.y;
                if (GetGameTileAt(scanLocation) == tile)
                {
                    tileLocations.Add(scanLocation);
                }
            }
        }
        return tileLocations;
    }

    /// <summary>
    /// Return the count of different types of tiles within a radius range of a given cell location
    /// </summary>
    /// <param name="centerCellLocation">The location of the center cell</param>
    /// <param name="scanRange">The radius range to look for</param>
    public int[] CountOfTilesInRange(Vector3Int centerCellLocation, int scanRange)
    {
        int[] typesOfTileWithinRadius = new int[(int)TileType.TypesOfTiles];
        Vector3Int scanLocation = new Vector3Int(0, 0, centerCellLocation.z);
        foreach (int x in Range(-scanRange, scanRange))
        {
            foreach (int y in Range(-scanRange, scanRange))
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance > scanRange)
                {
                    continue;
                }

                scanLocation.x = x + centerCellLocation.x;
                scanLocation.y = y + centerCellLocation.y;

                GameTile tile = GetGameTileAt(scanLocation);
                if (tile)
                {
                    typesOfTileWithinRadius[(int)tile.type]++;
                }
            }
        }
        return typesOfTileWithinRadius;
    }

    public int[] CountOfTilesInArea(Vector3Int centerCellLocation, int size)
    {
        int[] typesOfTileWithinRadius = new int[(int)TileType.TypesOfTiles];
        int radius = size / 2;
        Vector3Int pos;
        int offset = 0;
        if (size % 2 == 0)
        {
            centerCellLocation = new Vector3Int(centerCellLocation.x - 1, centerCellLocation.y - 1, 0);
            offset = 1;
        }
        // Check if the whole object is in bounds
        for (int x = (-1) * (radius - offset); x <= radius; x++)
        {
            for (int y = (-1) * (radius - offset); y <= radius; y++)
            {
                pos = centerCellLocation;
                pos.x += x;
                pos.y += y;
                GameTile tile = GetGameTileAt(pos);
                if (tile)
                {
                    typesOfTileWithinRadius[(int)tile.type]++;
                }
            }
        }
        return typesOfTileWithinRadius;
    }

    /// <summary>
    /// Scan from all the liquid tiles within a radius range and return all different liquid compositions
    /// </summary>
    /// <param name="centerCellLocation">The location of the center cell</param>
    /// <param name="scanRange">The radius range to look for</param>
    /// <param name="scanRing">Scans the full circle when false. Scans only the outermost ring when true.</param>
    /// <returns>A list of the compositions, can have a length of 0</returns>
    public List<float[]> GetLiquidCompositionWithinRange(Vector3Int centerCellLocation, int scanRange, bool scanRing = false)
    {
        List<float[]> liquidCompositions = new List<float[]>();

        Vector3Int scanLocation = new Vector3Int(0, 0, centerCellLocation.z);
        foreach (int x in Range(-scanRange, scanRange))
        {
            foreach (int y in Range(-scanRange, scanRange))
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance > scanRange)
                {
                    continue;
                }

                if(scanRing && distance <= scanRange - 1)
                {
                    continue;
                }

                scanLocation.x = x + centerCellLocation.x;
                scanLocation.y = y + centerCellLocation.y;
                LiquidBody liquid = this.GetTileData(scanLocation) != null ? this.GetTileData(scanLocation).currentLiquidBody : null;
                if (liquid != null)
                {
                    liquidCompositions.Add(liquid.contents);

                }
            }
        }

        return liquidCompositions;
    }

    /// <summary>
    /// Whether any of given tile is within a given range. 
    /// </summary>
    /// <param name="centerCellLocation">The cell location to calculate range from</param>
    /// <param name="tile">The tile of interest</param>
    /// <param name="scanRange">1/2 side length of the scanned square (radius if isCircleMode = true)</param>
    /// <param name="isCircleMode">Enable circular scan. Default to false, scans a square of side length of scanRange * 2 + 1</param>
    /// <returns></returns>
    public bool IsAnyTileInRange(Vector3Int centerCellLocation, int scanRange, GameTile tile, bool isCircleMode = false)
    {
        if (DistanceToClosestTile(centerCellLocation, tile, scanRange, isCircleMode) == -1)
        {
            return false;
        }
        return true;
    }

    private bool IsTileInAnyOfFour(int distanceX, int distanceY, Vector3Int subjectCellLocation, GameTile tile)
    {
        Vector3Int cell_1 = new Vector3Int(subjectCellLocation.x + distanceX, subjectCellLocation.y + distanceY, subjectCellLocation.z);
        Vector3Int cell_2 = new Vector3Int(subjectCellLocation.x + distanceX, subjectCellLocation.y - distanceY, subjectCellLocation.z);
        Vector3Int cell_3 = new Vector3Int(subjectCellLocation.x - distanceX, subjectCellLocation.y + distanceY, subjectCellLocation.z);
        Vector3Int cell_4 = new Vector3Int(subjectCellLocation.x - distanceX, subjectCellLocation.y - distanceY, subjectCellLocation.z);
        if (GetGameTileAt(cell_1) == tile ||
           GetGameTileAt(cell_2) == tile ||
           GetGameTileAt(cell_3) == tile ||
           GetGameTileAt(cell_4) == tile)
        {
            return true;
        }
        return false;
    }

    private bool IsTileInAnyOfEight(int distanceX, int distanceY, Vector3Int subjectCellLocation, GameTile tile)
    {
        if (IsTileInAnyOfFour(distanceX, distanceY, subjectCellLocation, tile))
        {
            return true;
        }
        else
        {
            Vector3Int cell_1 = new Vector3Int(subjectCellLocation.x + distanceY, subjectCellLocation.y + distanceX, subjectCellLocation.z);
            Vector3Int cell_2 = new Vector3Int(subjectCellLocation.x - distanceY, subjectCellLocation.y + distanceX, subjectCellLocation.z);
            Vector3Int cell_3 = new Vector3Int(subjectCellLocation.x + distanceY, subjectCellLocation.y - distanceX, subjectCellLocation.z);
            Vector3Int cell_4 = new Vector3Int(subjectCellLocation.x - distanceY, subjectCellLocation.y - distanceX, subjectCellLocation.z);
            if (GetGameTileAt(cell_1) == tile ||
               GetGameTileAt(cell_2) == tile ||
               GetGameTileAt(cell_3) == tile ||
               GetGameTileAt(cell_4) == tile)
            {
                return true;
            }
        }
        return false;
    }

    private List<Vector3Int> TileCellLocationsInFour(int distanceX, int distanceY, Vector3Int subjectCellLocation, GameTile tile)
    {
        Vector3Int cell_1 = new Vector3Int(subjectCellLocation.x + distanceX, subjectCellLocation.y + distanceY, subjectCellLocation.z);
        Vector3Int cell_2 = new Vector3Int(subjectCellLocation.x + distanceX, subjectCellLocation.y - distanceY, subjectCellLocation.z);
        Vector3Int cell_3 = new Vector3Int(subjectCellLocation.x - distanceX, subjectCellLocation.y + distanceY, subjectCellLocation.z);
        Vector3Int cell_4 = new Vector3Int(subjectCellLocation.x - distanceX, subjectCellLocation.y - distanceY, subjectCellLocation.z);
        List<Vector3Int> cells = new List<Vector3Int> { cell_1, cell_2, cell_3, cell_4 };
        List<Vector3Int> results = new List<Vector3Int>();
        foreach (Vector3Int cell in cells)
        {
            if (GetGameTileAt(cell) == tile)
            {
                results.Add(cell);
            }
        }
        return results;
    }

    private List<Vector3Int> TileCellLocationsInEight(int distanceX, int distanceY, Vector3Int subjectCellLocation, GameTile tile)
    {
        Vector3Int cell_1 = new Vector3Int(subjectCellLocation.x + distanceY, subjectCellLocation.y + distanceX, subjectCellLocation.z);
        Vector3Int cell_2 = new Vector3Int(subjectCellLocation.x - distanceY, subjectCellLocation.y + distanceX, subjectCellLocation.z);
        Vector3Int cell_3 = new Vector3Int(subjectCellLocation.x + distanceY, subjectCellLocation.y - distanceX, subjectCellLocation.z);
        Vector3Int cell_4 = new Vector3Int(subjectCellLocation.x - distanceY, subjectCellLocation.y - distanceX, subjectCellLocation.z);
        List<Vector3Int> cells = new List<Vector3Int> { cell_1, cell_2, cell_3, cell_4 };
        List<Vector3Int> results = new List<Vector3Int>();
        foreach (Vector3Int cell in cells)
        {
            if (GetGameTileAt(cell) == tile)
            {
                results.Add(cell);
            }
        }
        results.AddRange(TileCellLocationsInFour(distanceX, distanceY, subjectCellLocation, tile));
        return results;
    }

    /// <summary>
    /// Returns four cell locations next to the given cell location
    /// </summary>
    /// <param name="cellLocation"></param>
    /// <returns></returns>
    public static List<Vector3Int> FourNeighborTileLocations(Vector3Int cellLocation)
    {
        List<Vector3Int> fourNeighborTiles = new List<Vector3Int>();
        fourNeighborTiles.Add(new Vector3Int(cellLocation.x - 1, cellLocation.y, cellLocation.z));
        fourNeighborTiles.Add(new Vector3Int(cellLocation.x + 1, cellLocation.y, cellLocation.z));
        fourNeighborTiles.Add(new Vector3Int(cellLocation.x, cellLocation.y - 1, cellLocation.z));
        fourNeighborTiles.Add(new Vector3Int(cellLocation.x, cellLocation.y + 1, cellLocation.z));
        return fourNeighborTiles;
    }
    #endregion

    #region Math Utility
    private Direction2D SwitchDirection(Direction2D direction)
    {
        return direction == Direction2D.X ? Direction2D.Y : Direction2D.X;
    }

    /// <summary>
    /// Convert a world position to cell positions on the grid.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return Grid.WorldToCell(worldPosition);
    }

    public static Vector3Int SignsVector3(Vector3 vector)
    {
        Vector3Int result = new Vector3Int()
        {
            x = vector.x == 0 ? 0 : (int)(Mathf.Sign(vector.x)),
            y = vector.y == 0 ? 0 : (int)(Mathf.Sign(vector.y)),
            z = vector.z == 0 ? 0 : (int)(Mathf.Sign(vector.z))
        };
        return result;
    }

    public static Vector3Int SignsRoundToIntVector3(Vector3 vector)
    {
        Vector3Int signs = SignsVector3(vector);
        Vector3Int result = new Vector3Int()
        {
            x = signs.x < 0 ? Mathf.FloorToInt(vector.x) : Mathf.CeilToInt(vector.x),
            y = signs.y < 0 ? Mathf.FloorToInt(vector.y) : Mathf.CeilToInt(vector.y),
            z = signs.z < 0 ? Mathf.FloorToInt(vector.z) : Mathf.CeilToInt(vector.z),
        };

        return result;
    }

    /// <summary>
    /// Rounds down or up whichever way is further away from the origin provided.
    /// </summary>
    /// <param name="vector"> The vector to round. </param>
    /// <param name="origin"> The origin that the vector rounds with respect to. </param>
    /// <returns></returns>
    public static Vector3Int SignsRoundToIntVector3(Vector3 vector, Vector3Int origin)
    {
        Vector3Int result = SignsRoundToIntVector3(vector - origin) + origin;

        return result;
    }

    /// <summary>
    /// Returns int from start to end (inclusive), default stepping is 1.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static IEnumerable<int> Range(int start, int end, int step = 1)
    {
        if (start < end)
        {
            for (int i = start; i <= end; i += step)
            {
                yield return i;
            }
        }
        else
        {
            for (int i = start; i >= end; i -= step)
                yield return i;
        }
    }
    /// <summary>
    /// Returns FLOAT from start to end (inclusive), default stepping is 1.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static IEnumerable<float> RangeFloat(float start, float end, float step = 1)
    {
        if (start < end)
        {
            for (float i = start; i <= end; i += step)
            {
                yield return i;
            }
        }
        else
        {
            for (float i = start; i >= end; i -= step)
                yield return i;
        }
    }

    /// <summary>
    /// Round a number towards zero
    /// </summary>
    /// <param name="n"> Number to round</param>
    /// <returns></returns>
    public static int RoundTowardsZeroInt(float n)
    {
        if (n > 0)
        {
            return Mathf.FloorToInt(n);
        }
        else
        {
            return Mathf.CeilToInt(n);
        }
    }
    /// <summary>
    /// Round a number towards zero, returns int
    /// </summary>
    /// <param name="n"> Number to round </param>
    /// <returns></returns>
    public static int RoundAwayFromZeroInt(float n)
    {
        if (n < 0)
        {
            return Mathf.FloorToInt(n);
        }
        else
        {
            return Mathf.CeilToInt(n);
        }
    }
    /// <summary>
    /// Increase the magnitude (absolute value) of a number
    /// </summary>
    /// <param name="n"> Number to increase </param>
    /// <param name="increment"> Increment </param>
    /// <returns></returns>
    public static float IncreaseMagnitude(float n, float increment)
    {
        if (n > 0)
        {
            return n += increment;
        }
        else
        {
            return n -= increment;
        }
    }
    public static int IncreaseMagnitudeInt(int n, int increment)
    {
        if (n > 0)
        {
            return n += increment;
        }
        else
        {
            return n -= increment;
        }

    }
    #endregion

    public class TileData
    {
        public Vector3Int tilePosition { get; private set; }
        public GameObject Machine { get; set; }
        public GameObject Food { get; set; }
        public GameObject Animal { get; set; }
        public bool HomeLocation { get; set; }

        public GameTile currentTile { get; private set; }
        public GameTile previousTile { get; private set; }
        public Color currentColor { get; private set; }
        public Color previousColor { get; private set; }
        public LiquidBody currentLiquidBody { get; set; }
        public LiquidBody previousLiquidBody { get; private set; }
        public bool isTileChanged { get; private set; } = false;
        public bool isLiquidBodyChanged { get; private set; } = false;
        public bool isColorChanged { get; private set; } = false;
        public bool isTilePlaceable { get; set; } = false;
        public TileData(Vector3Int tilePosition, GameTile tile = null)
        {
            this.Food = null;
            this.Animal = null;
            this.Machine = null;
            this.HomeLocation = false;

            this.tilePosition = tilePosition;
            this.currentTile = tile;
            this.currentColor = Color.white;
            this.currentLiquidBody = null;
            this.isTileChanged = false;
            this.isColorChanged = false;
        }
        public void Clear()
        {
            this.currentTile = null;
            this.previousTile = null;
            this.currentLiquidBody = null;
            this.previousLiquidBody = null;
        }
        public void PreviewReplacement(GameTile tile)
        {
            if (isTileChanged)
            {
                this.currentTile = tile;
                return;
            }
            this.previousTile = this.currentTile;
            //Debug.Log("previous:" + this.previousTile ?? this.previousTile.TileName + "current:" + this.currentTile ?? this.currentTile.TileName);
            this.currentTile = tile;
            this.isTileChanged = true;
        }
        public void PreviewColorChange(Color color)
        {
            if (isColorChanged)
            {
                this.currentColor = color;
                return;
            }
            this.previousColor = this.currentColor;
            this.currentColor = color;
            this.isColorChanged = true;
        }
        public void PreviewLiquidBody(LiquidBody newLiquidBody)
        {
            if (isLiquidBodyChanged)
            {
                if (newLiquidBody == this.currentLiquidBody)
                {
                    return;
                }
                this.currentLiquidBody = newLiquidBody;
                return;
            }
            this.previousLiquidBody = this.currentLiquidBody;
            this.currentLiquidBody = newLiquidBody;
            this.isLiquidBodyChanged = true;
        }
        public void ConfirmReplacement()
        {
            if (currentTile == null)
            {
                this.currentColor = Color.white;
                if (this.currentLiquidBody != null && currentLiquidBody.bodyID != 0)
                {
                    this.currentLiquidBody.RemoveTile(tilePosition); // Remove Tile from liquid body
                }
                return;
            }
            ClearHistory();
        }
        public void Revert()
        {
            if (isTileChanged)
            {
                this.currentTile = this.previousTile;
            }
            if (isLiquidBodyChanged)
            {
                this.currentLiquidBody = this.previousLiquidBody;
            }
            if (isColorChanged)
            {
                this.currentColor = this.previousColor;
            }
            ClearHistory();
        }
        private void ClearHistory()
        {
            this.previousColor = Color.white;
            this.previousLiquidBody = null;
            this.previousTile = null;
            this.isTileChanged = false;
            this.isColorChanged = false;
            this.isLiquidBodyChanged = false;
        }
    }
}
