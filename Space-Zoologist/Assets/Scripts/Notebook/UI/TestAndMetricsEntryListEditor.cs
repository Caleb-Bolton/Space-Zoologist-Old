﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestAndMetricsEntryListEditor : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Script used to pick the enclosure we are currently taking notes on")]
    private LevelIDPicker enclosurePicker;
    [SerializeField]
    [Tooltip("Reference to the prefab used to edit a single test and metrics entry")]
    private TestAndMetricsEntryEditor editorPrefab;
    [SerializeField]
    [Tooltip("Parent transform for the editor of the individual entries")]
    private LayoutGroup editorParent;
    [SerializeField]
    [Tooltip("Reference to the scroll rect that the editors will fit into")]
    private ScrollRect editorScroller;

    private List<TestAndMetricsEntryEditor> currentEditors = new List<TestAndMetricsEntryEditor>();

    public override void Setup()
    {
        base.Setup();

        // Add listener for the enclosure picked event
        enclosurePicker.OnLevelIDPicked.AddListener(OnEnclosureSelected);
        OnEnclosureSelected(LevelID.Current());
    }

    private void OnEnclosureSelected(LevelID id)
    {
        // Destroy all existing editors
        foreach(TestAndMetricsEntryEditor editor in currentEditors)
        {
            Destroy(editor.gameObject);
        }
        // Clear out the list
        currentEditors.Clear();

        // Foreach entry in the selected list, add an editor
        foreach(TestAndMetricsEntryData entry in UIParent.Data.TestAndMetrics.GetEntryList(id).Entries)
        {
            TestAndMetricsEntryEditor editor = Instantiate(editorPrefab, editorParent.transform);
            editor.Setup(id, entry, editorScroller);
            currentEditors.Add(editor);
        }

        // If the enclosure selected is the current enclosure, then add a new editor
        // that we can use to add more entries
        if(id == LevelID.Current())
        {
            CreateAddingEntry();
        }
    }

    private void OnNewEntryCreated()
    {
        CreateAddingEntry();
    }

    private void CreateAddingEntry()
    {
        TestAndMetricsEntryEditor editor = Instantiate(editorPrefab, editorParent.transform);
        editor.Setup(LevelID.Current(), null, editorScroller);
        editor.OnNewEntryCreated.AddListener(OnNewEntryCreated);
        currentEditors.Add(editor);
    }
}
