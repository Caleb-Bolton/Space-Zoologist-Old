using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine the level/enclosure numbers where the scaffolding levels change
/// </summary>
[CreateAssetMenu(fileName = "Enclosure Scaffold", menuName = "Notebook/Enclosure Scaffold")]
public class EnclosureScaffold : ScriptableObject
{
    public int TotalLevels => scaffoldLevelSwitches.Count + 1;

    [SerializeField]
    [Tooltip("Scaffold level switches at each enclosure id in this list")]
    private List<EnclosureID> scaffoldLevelSwitches;

    // Get the scaffold level of the given id
    public int ScaffoldLevel(EnclosureID id)
    {
        // Create a sorted list with the lowest possible id at the front
        List<EnclosureID> levels = new List<EnclosureID>(scaffoldLevelSwitches);
        levels.Insert(0, new EnclosureID(int.MinValue, int.MinValue));
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
