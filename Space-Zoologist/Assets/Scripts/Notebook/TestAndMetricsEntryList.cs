using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of test and metric enries
/// </summary>
[System.Serializable]
public class TestAndMetricsEntryList
{
    // Access the list of entries
    public List<TestAndMetricsEntry> Entries => entries;

    [SerializeField]
    [Tooltip("List of entries in this data set")]
    private List<TestAndMetricsEntry> entries = new List<TestAndMetricsEntry>();
}
