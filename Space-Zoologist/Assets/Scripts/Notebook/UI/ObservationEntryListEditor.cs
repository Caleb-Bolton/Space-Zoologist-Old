﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObservationEntryListEditor : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Script used to pick the enclosure we are currently taking notes on")]
    private LevelIDPicker enclosurePicker = null;
    [SerializeField]
    [Tooltip("Reference to the prefab used to edit a single observation entry")]
    private ObservationEntryEditor editorPrefab = null;
    [SerializeField]
    [Tooltip("Parent transform for the editor of the individual entries")]
    private LayoutGroup editorParent = null;
    [SerializeField]
    [Tooltip("Reference to the scroll rect that the editors will fit into")]
    private ScrollRect editorScroller = null;

    private List<ObservationEntryEditor> currentEditors = new List<ObservationEntryEditor>();

    public override void Setup()
    {
        base.Setup();

        // Add listnener to enclosure id picked event and select the enclosure for the current scene
        enclosurePicker.OnLevelIDPicked.AddListener(OnEnclosureSelected);
        OnEnclosureSelected(LevelID.Current());
    }

    private void OnEnclosureSelected(LevelID id)
    {
        // Destroy all existing editors
        foreach(ObservationEntryEditor editor in currentEditors)
        {
            Destroy(editor.gameObject);
        }
        currentEditors.Clear();

        // Add editors for the current entry list
        foreach(ObservationsEntryData entry in UIParent.Data.Observations.GetEntryList(id).Entries)
        {
            ObservationEntryEditor editor = Instantiate(editorPrefab, editorParent.transform);
            editor.Setup(entry, id, editorScroller);
            currentEditors.Add(editor);
        }
    }
}
