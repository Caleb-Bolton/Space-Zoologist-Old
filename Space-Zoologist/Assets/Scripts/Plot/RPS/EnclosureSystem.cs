﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This system finds and manages enclose areas
/// </summary>
public class EnclosureSystem : MonoBehaviour
{
    public Dictionary<Vector3Int, byte> positionToEnclosedArea { get; private set; }

    public List<EnclosedArea> EnclosedAreas;
    private List<EnclosedArea> internalEnclosedAreas;

    [Tooltip("Leave this empty if using TileSystem's default starting position")]
    [SerializeField] private List<Vector3Int> startingPositions = default;

    // The global atmosphere
    private Vector3Int startPos = default;
    private byte enclosedAreaCount = 0;




    /// <summary>
    /// Variable initialization on awake.
    /// </summary>
    private void Awake()
    {
        startingPositions = GameManager.Instance.LevelData.StartinPositions;
        positionToEnclosedArea = new Dictionary<Vector3Int, byte>();
        this.internalEnclosedAreas = new List<EnclosedArea>();
        this.EnclosedAreas = new List<EnclosedArea>();
    }

    private void Start()
    {
        startPos = GameManager.Instance.m_tileDataController.startTile;

        if (startingPositions.Count == 0)
        {
            startingPositions.Add(startPos);
        }

        this.UpdateEnclosedAreas(false);
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
            foreach (EnclosedArea enclosedArea in this.EnclosedAreas)
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
        if (positionToEnclosedArea == null) {
            Debug.LogError("Enclosure System hasn't been initialized yet");
            return null;
        }
        Vector3Int position = GameManager.Instance.m_tileDataController.WorldToCell(cellPos);
        positionToEnclosedArea.TryGetValue(position, out byte val);
        return this.GetEnclosedAreaById(val);
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

    /// <summary>
    /// This deletes enclosed areas that has nothing in it.
    /// To fix issues with creating enclosed area for areas outside of the border walls
    /// </summary>
    private void UpdatePublicEnclosedAreas()
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
    private void FloodFill(Vector3Int start, HashSet<Vector3Int> accessed, HashSet<Vector3Int> unaccessible, Stack<Vector3Int> walls, byte atmosphereCount, EnclosedArea enclosedArea, bool isUpdate)
    {
        if (accessed.Contains(start) || unaccessible.Contains(start))
        {
            // checked before, move on
            return;
        }

        // Using iterative approach due to stack overflow with large maps
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(start);

        Vector3Int cur;

        while (stack.Count > 0) {

            cur = stack.Pop();
            

            if (accessed.Contains(cur) || unaccessible.Contains(cur))
            {
                // checked before, move on
                continue;
            }

            // check if tilemap has tile
            GameTile tile = GameManager.Instance.m_tileDataController.GetGameTileAt(cur);
            if (tile != null)
            {
                if (tile.type != TileType.Wall)
                {
                    // Mark the cell
                    accessed.Add(cur);

                    // Updating enclosed area
                    if (isUpdate && this.positionToEnclosedArea.ContainsKey(cur) && this.GetEnclosedAreaById(this.positionToEnclosedArea[cur]) != null)
                    {
                        // Add the tile and tell the enclosed area what the previous area is
                        enclosedArea.AddCoordinate(new EnclosedArea.Coordinate(cur.x, cur.y), (int)tile.type, this.GetEnclosedAreaById(this.positionToEnclosedArea[cur]));
                    }
                    // Initial round
                    else
                    {
                        enclosedArea.AddCoordinate(new EnclosedArea.Coordinate(cur.x, cur.y), (int)tile.type, null);
                    }

                    this.positionToEnclosedArea[cur] = atmosphereCount;
                    stack.Push(cur + Vector3Int.left);
                    stack.Push(cur + Vector3Int.up);
                    stack.Push(cur + Vector3Int.right);
                    stack.Push(cur + Vector3Int.down);
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
    }

    /// <summary>
    /// Call this to update all the enclosed areas and create an EnclosedArea data structure to hold its information.
    /// </summary>
    /// <remarks>
    /// This is using a flood fill (https://en.wikipedia.org/wiki/Flood_fill) to find enclosed areas.
    /// Assumptions: the reserve is bordered by walls
    /// NOTE: this assumption does not hold up for some reserves, such as Level2E2 and Level2E3
    /// </remarks>
    public void UpdateEnclosedAreas(bool isUpdate = true)
    {
        // non-wall tiles
        HashSet<Vector3Int> accessed = new HashSet<Vector3Int>();
        // wall or null tiles
        HashSet<Vector3Int> unaccessible = new HashSet<Vector3Int>();
        // walls
        Stack<Vector3Int> walls = new Stack<Vector3Int>();

        List<EnclosedArea> newEnclosedAreas = new List<EnclosedArea>();

        // Initial flood fill
        this.enclosedAreaCount = 0;
        EnclosedArea area = new EnclosedArea(this.enclosedAreaCount);
        newEnclosedAreas.Add(area);

        // If startingPositions is empty on start, startingPositions will contain gridSystem.startTile by default.
        foreach (var startingPos in startingPositions)
        {
            if (area.coordinates.Count > 0)
            {
                this.enclosedAreaCount++;
                area = new EnclosedArea(this.enclosedAreaCount);
                newEnclosedAreas.Add(area);
            }
            this.FloodFill(startingPos, accessed, unaccessible, walls, enclosedAreaCount, area, isUpdate);

            Vector3Int curPos = startingPos;
            while (walls.Count > 0)
            {
                if (area.coordinates.Count != 0)
                {
                    this.enclosedAreaCount++;
                    area = new EnclosedArea(this.enclosedAreaCount);
                    newEnclosedAreas.Add(area);
                }

                curPos = walls.Pop();

                this.FloodFill(curPos + Vector3Int.left, accessed, unaccessible, walls, this.enclosedAreaCount, area, isUpdate);
                this.FloodFill(curPos + Vector3Int.up, accessed, unaccessible, walls, this.enclosedAreaCount, area, isUpdate);
                this.FloodFill(curPos + Vector3Int.right, accessed, unaccessible, walls, this.enclosedAreaCount, area, isUpdate);
                this.FloodFill(curPos + Vector3Int.down, accessed, unaccessible, walls, this.enclosedAreaCount, area, isUpdate);
            }
        }

        // Not initializing: update the areas based on the previous ones
        if (isUpdate)
        {
        }

        this.internalEnclosedAreas = newEnclosedAreas;
        this.UpdatePublicEnclosedAreas();
    }
}
