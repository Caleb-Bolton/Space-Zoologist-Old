using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotebookAcronymEditor : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Reference to the prefab of acronym note editors to instantiate")]
    private NotebookAcronymSingleNoteEditor noteEditorPrefab;

    public override void Setup()
    {
        base.Setup();

        // Create all UI elements that edit each character in the acronym
        foreach(char c in UIParent.Notebook.Acronym)
        {
            NotebookAcronymSingleNoteEditor clone = Instantiate(noteEditorPrefab, transform);
            clone.Setup(c);
        }
    }
}
