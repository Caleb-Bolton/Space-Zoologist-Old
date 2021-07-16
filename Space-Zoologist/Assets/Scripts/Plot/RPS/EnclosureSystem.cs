﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This system finds and manages enclose areas
/// </summary>
public class EnclosureSystem : MonoBehaviour
{
    public Dictionary<Vector3Int, byte> positionToEnclosedArea { get; private set; }
    public List<AtmosphericComposition> Atmospheres { get; private set; }

    public List<EnclosedArea> EnclosedAreas;
    private List<EnclosedArea> internalEnclosedAreas;

    [SerializeField] private LevelDataReference LevelDataReference = default;
    [SerializeField] private TileSystem TileSystem = default;
    [SerializeField] private NeedSystemManager needSystemManager = default;
    [SerializeField] private GridSystem gridSystem = default;

    // The global atmosphere
    private AtmosphericComposition GlobalAtmosphere;
    private Vector3Int startPos = default;
    private byte enclosedAreaCount = 0;

    /// <summary>
    /// Variable initialization on awake.
    /// </summary>
    private void Awake()
    {
        positionToEnclosedArea = new Dictionary<Vector3Int, byte>();
        Atmospheres = new List<AtmosphericComposition>();
        this.internalEnclosedAreas = new List<EnclosedArea>();
        this.EnclosedAreas = new List<EnclosedArea>();
        this.GlobalAtmosphere = this.LevelDataReference.LevelData.GlobalAtmosphere;
        // TODO Hard fix to reference issue
        this.TileSystem = FindObjectOfType<TileSystem>();
    }

    private void Start()
    {
        // TODO When this is called GridSystem might not be initlized,
        // ie, cannot read from CellData
        startPos = gridSystem.startTile;
        this.FindEnclosedAreas();
    }

    /// <summary>
    /// Gets the atmospheric composition at a given position.
    /// </summary>
    /// <param name="position">The position at which to get the atmopheric conditions</param>
    /// <returns></returns>
    public AtmosphericComposition GetAtmosphericComposition(Vector3 worldPosition)
    {
        Vector3Int position = this.TileSystem.WorldToCell(worldPosition);
        if (positionToEnclosedArea.ContainsKey(position) && this.GetEnclosedAreaById(positionToEnclosedArea[position]) != null)
        {
            return this.GetEnclosedAreaById(positionToEnclosedArea[position]).atmosphericComposition;
        }
        else
        {
            throw new System.Exception("Unable to find atmosphere at position (" + position.x + " , " + position.y + ")");
        }
    }

    /// <summary>
    /// Find which enclosed area a population/food source is in and returns it
    /// </summary>
    /// <param name="obj">The object to be find in enclosed area</param>
    /// <returns>
    /// The enclosed area this object is in, null if not found.
    /// This is a helper function from the log system.
    /// </returns>
    public EnclosedArea GetEnclosedAreaThisIsIn(object obj)
    {
        if (obj.GetType() == typeof(Population))
        {
            foreach(EnclosedArea enclosedArea in this.EnclosedAreas)
            {
                if (enclosedArea.populations.Contains((Population)obj))
                {
                    return enclosedArea;
                }
           }
        }
        else if (obj.GetType() == typeof(FoodSource))
        {
            foreach (EnclosedArea enclosedArea in this.EnclosedAreas)
            {
                if (enclosedArea.foodSources.Contains((FoodSource)obj))
                {
                    return enclosedArea;
                }
            }
        }

        return null;
    }

    public EnclosedArea GetEnclosedAreaByCellPosition(Vector3Int cellPos)
    {
        Vector3Int position = this.TileSystem.WorldToCell(cellPos);

        return this.GetEnclosedAreaById(positionToEnclosedArea[position]);
    }

    public EnclosedArea GetEnclosedAreaById(byte id)
    {
        foreach (EnclosedArea enclosedArea in this.internalEnclosedAreas)
        {
            if (enclosedArea.id == id)
            {
                return enclosedArea;
            }
        }

        return null;
    }

    public void UpdateAtmosphereComposition(Vector3 worldPosition, AtmosphericComposition atmosphericComposition)
    {
        Vector3Int position = this.TileSystem.WorldToCell(worldPosition);
        if (positionToEnclosedArea.ContainsKey(position))
        {
            this.GetEnclosedAreaById(positionToEnclosedArea[position]).UpdateAtmosphericComposition(atmosphericComposition);

            // Mark Atmosphere NS dirty
            this.needSystemManager.Systems[NeedType.Atmosphere].MarkAsDirty();

            // Invoke event
            EventManager.Instance.InvokeEvent(EventType.AtmosphereChange, this.GetEnclosedAreaById(positionToEnclosedArea[position]));
        }
        else
        {
            throw new System.Exception("Unable to find atmosphere at position (" + position.x + " , " + position.y + ")");
        }
    }

    /// <summary>
    /// This deletes enclosed areas that has nothing in it.
    /// To fix issues with creating enclosed area for areas outside of the border walls
    /// </summary>
    private void updatePublicEnlcosedAreas()
    {
        this.EnclosedAreas.Clear();

        foreach (EnclosedArea enclosedArea in this.internalEnclosedAreas)
        {
            if (enclosedArea.coordinates.Count != 0)
            {
                this.EnclosedAreas.Add(enclosedArea);
            }
        }
    }

    /// <summary>
    /// Private function for determining if a position is within the area
    /// </summary>
    bool WithinRange(Vector3Int pos, int minx, int miny, int maxx, int maxy)
    {
        if (pos.x >= minx && pos.y >= miny && pos.x <= maxx && pos.y <= maxy)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Recursive flood fill. 
    /// </summary>
    /// <param name="cur">Start location</param>
    /// <param name="accessed">Accessed cells</param>
    /// <param name="unaccessible">Unaccessible cells</param>
    /// <param name="walls">wall cells</param>
    /// <param name="atmosphereCount">index of the enclosed area</param>
    private void FloodFill(Vector3Int cur, HashSet<Vector3Int> accessed, HashSet<Vector3Int> unaccessible, Stack<Vector3Int> walls, byte atmosphereCount, EnclosedArea enclosedArea, bool isUpdate)
    {

        if (accessed.Contains(cur) || unaccessible.Contains(cur))
        {
            // checked before, move on
            return;
        }

        // check if tilemap has tile
        GameTile tile = this.TileSystem.GetGameTileAt(cur);
        if (tile != null)
        {
            if (tile.type != TileType.Wall)
            {
                // Mark the cell
                accessed.Add(cur);

                // Updating enclosed area
                if (isUpdate && this.positionToEnclosedArea.ContainsKey(cur) && this.GetEnclosedAreaById(this.positionToEnclosedArea[cur]) != null)
                {
                    enclosedArea.AddCoordinate(new EnclosedArea.Coordinate(cur.x, cur.y), (int)tile.type, this.GetEnclosedAreaById(this.positionToEnclosedArea[cur]).atmosphericComposition);
                }
                // Initial round
                else
                {
                    enclosedArea.AddCoordinate(new EnclosedArea.Coordinate(cur.x, cur.y), (int)tile.type, null);
                }

                this.positionToEnclosedArea[cur] = atmosphereCount;

                FloodFill(cur + Vector3Int.left, accessed, unaccessible, walls, atmosphereCount, enclosedArea, isUpdate);
                FloodFill(cur + Vector3Int.up, accessed, unaccessible, walls, atmosphereCount, enclosedArea, isUpdate);
                FloodFill(cur + Vector3Int.right, accessed, unaccessible, walls, atmosphereCount, enclosedArea, isUpdate);
                FloodFill(cur + Vector3Int.down, accessed, unaccessible, walls, atmosphereCount, enclosedArea, isUpdate);
            }
            else
            {
                walls.Push(cur);
                unaccessible.Add(cur);
            }
        }
        else
        {
            unaccessible.Add(cur);
        }
    }

    /// <summary>
    /// Call this to find all the enclosed areas and create a EnclosedArea data structure to hold its information.
    /// </summary>
    /// <remarks>
    /// This is using a flood fill (https://en.wikipedia.org/wiki/Flood_fill) to find enclosed areas.
    /// Assumptions: the reserve is bordered by walls
    /// </remarks>
    public void FindEnclosedAreas()
    {
        // tiles to-process
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        // non-wall tiles
        HashSet<Vector3Int> accessed = new HashSet<Vector3Int>();
        // wall or null tiles
        HashSet<Vector3Int> unaccessible = new HashSet<Vector3Int>();
        // walls
        Stack<Vector3Int> walls = new Stack<Vector3Int>();

        List<EnclosedArea> newEnclosedAreas = new List<EnclosedArea>();

        // Initial flood fill
        this.enclosedAreaCount = 0;
        newEnclosedAreas.Add(new EnclosedArea(new AtmosphericComposition(this.GlobalAtmosphere), this.gridSystem, enclosedAreaCount));
        this.FloodFill(startPos, accessed, unaccessible, walls, enclosedAreaCount, newEnclosedAreas[enclosedAreaCount], false);

        Vector3Int currPos = startPos;
        while (walls.Count > 0)
        {
            // this.enclosedAreaCount++;
            // newEnclosedAreas.Add(new EnclosedArea(new AtmosphericComposition(this.GlobalAtmosphere), this.gridSystem, this.enclosedAreaCount));

            currPos = walls.Pop();

            this.FloodFill(currPos + Vector3Int.left, accessed, unaccessible, walls, this.enclosedAreaCount, newEnclosedAreas[this.enclosedAreaCount], false);
            this.FloodFill(currPos + Vector3Int.up, accessed, unaccessible, walls, this.enclosedAreaCount, newEnclosedAreas[this.enclosedAreaCount], false);
            this.FloodFill(currPos + Vector3Int.right, accessed, unaccessible, walls, this.enclosedAreaCount, newEnclosedAreas[this.enclosedAreaCount], false);
            this.FloodFill(currPos + Vector3Int.down, accessed, unaccessible, walls, this.enclosedAreaCount, newEnclosedAreas[this.enclosedAreaCount], false);
        }

        this.internalEnclosedAreas = newEnclosedAreas;
        this.updatePublicEnlcosedAreas();
    }

    public void UpdateEnclosedAreas()
    {
        // tiles to-process
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        // non-wall tiles
        HashSet<Vector3Int> accessed = new HashSet<Vector3Int>();
        // wall or null tiles
        HashSet<Vector3Int> unaccessible = new HashSet<Vector3Int>();
        // walls
        Stack<Vector3Int> walls = new Stack<Vector3Int>();

        List<EnclosedArea> newEnclosedAreas = new List<EnclosedArea>();

        // Stores the ids of the enclosed areas that has been updated
        HashSet<byte> updatedEnclosedArea = new HashSet<byte>();

        bool createdNewEclosedArea = false;

        // Initial flood fill
        // TODO Replace this with a better way to determine the first tile to start with
        // If the map DOES NOT contain a tile at (1,1,0), this code causes an ERROR! -> tile will not get placed in store
        byte curEnclosedAreaIndex = this.positionToEnclosedArea[startPos];
        newEnclosedAreas.Add(new EnclosedArea(new AtmosphericComposition(this.GlobalAtmosphere), this.gridSystem, curEnclosedAreaIndex));
        this.FloodFill(startPos, accessed, unaccessible, walls, curEnclosedAreaIndex, newEnclosedAreas[curEnclosedAreaIndex], true);
        updatedEnclosedArea.Add(curEnclosedAreaIndex);
        Vector3Int currPos = startPos;
        while (walls.Count > 0)
        {
            currPos = walls.Pop();

            foreach (Vector3Int pos in new List<Vector3Int>() { currPos + Vector3Int.left, currPos + Vector3Int.up, currPos + Vector3Int.right, currPos + Vector3Int.down } )
            {
                if (!this.positionToEnclosedArea.ContainsKey(pos) || accessed.Contains(pos) || unaccessible.Contains(pos))
                {
                    continue;
                }
                else
                {
                    if (updatedEnclosedArea.Contains(this.positionToEnclosedArea[pos]))
                    {
                        curEnclosedAreaIndex = ++this.enclosedAreaCount;
                        createdNewEclosedArea = true;
                    }
                    else
                    {
                        curEnclosedAreaIndex = this.positionToEnclosedArea[pos];
                    }
                }
               

                newEnclosedAreas.Add(new EnclosedArea(new AtmosphericComposition(this.GlobalAtmosphere), this.gridSystem, curEnclosedAreaIndex));
                this.FloodFill(pos, accessed, unaccessible, walls, curEnclosedAreaIndex, newEnclosedAreas[newEnclosedAreas.Count-1], true);
                updatedEnclosedArea.Add(curEnclosedAreaIndex);


                if (createdNewEclosedArea)
                {
                    EnclosedArea newlyCreatedEnclosedArea = newEnclosedAreas[newEnclosedAreas.Count - 1];
                    EventManager.Instance.InvokeEvent(EventType.NewEnclosedArea, newlyCreatedEnclosedArea);
                    createdNewEclosedArea = false;
                }
            }
        }

        this.internalEnclosedAreas = newEnclosedAreas;
        this.updatePublicEnlcosedAreas();
    }
}