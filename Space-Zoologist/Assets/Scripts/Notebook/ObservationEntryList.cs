using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObservationEntryList
{
    public List<ObservationEntry> Entries => entries;

    [SerializeField]
    [Tooltip("List of the observation entries")]
    private List<ObservationEntry> entries;

    public ObservationEntryList(ObservationEntryList other)
    {
        entries = new List<ObservationEntry>(other.entries);
    }
    public static ObservationEntryList Default()
    {
        throw new System.NotImplementedException("This factory method is deprecated and no longer usable");
    }
}
