﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine the level/enclosure numbers where the scaffolding levels change
/// </summary>
[CreateAssetMenu]
public class LevelScaffold : ScriptableObject
{
    public int TotalLevels => scaffoldLevelSwitches.Count + 1;

    [SerializeField]
    [Tooltip("Scaffold level switches at each enclosure id in this list")]
    private List<LevelID> scaffoldLevelSwitches = null;

    // Get the scaffold level of the given id
    public int ScaffoldLevel(LevelID id)
    {
        // Create a sorted list with the lowest possible id at the front
        List<LevelID> levels = new List<LevelID>(scaffoldLevelSwitches);
        levels.Insert(0, new LevelID(int.MinValue, int.MinValue));
        levels.Sort();

        // Loop over all ids not including the last one
        for(int i = 0; i < levels.Count - 1; i++)
        {
            // If the id is bigger than this id and less than the next, we've found the scaffold level
            if (id >= levels[i] && id < levels[i + 1]) return i;
        }

        // If we get to this point, we know we are in the last scaffold
        return levels.Count - 1;
    }
}
