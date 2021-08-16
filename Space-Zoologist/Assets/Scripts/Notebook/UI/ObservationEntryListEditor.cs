﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObservationEntryListEditor : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Script used to pick the enclosure we are currently taking notes on")]
    private EnclosureIDPicker enclosurePicker;
    [SerializeField]
    [Tooltip("Reference to the prefab used to edit a single observation entry")]
    private ObservationEntryEditor editorPrefab;
    [SerializeField]
    [Tooltip("Parent transform for the editor of the individual entries")]
    private LayoutGroup editorParent;

    private List<ObservationEntryEditor> currentEditors = new List<ObservationEntryEditor>();

    public override void Setup()
    {
        base.Setup();

        // Add listnener to enclosure id picked event and select the enclosure for the current scene
        enclosurePicker.OnEnclosureIDPicked.AddListener(OnEnclosureSelected);
        OnEnclosureSelected(EnclosureID.FromCurrentSceneName());
    }

    private void OnEnclosureSelected(EnclosureID id)
    {
        // Destroy all existing editors
        foreach(ObservationEntryEditor editor in currentEditors)
        {
            Destroy(editor.gameObject);
        }
        currentEditors.Clear();

        // Add editors for the current entry list
        foreach(ObservationEntry entry in UIParent.Notebook.GetObservationEntryList(id).Entries)
        {
            ObservationEntryEditor editor = Instantiate(editorPrefab, editorParent.transform);
            editor.Setup(entry, id);
            currentEditors.Add(editor);
        }
    }
}
