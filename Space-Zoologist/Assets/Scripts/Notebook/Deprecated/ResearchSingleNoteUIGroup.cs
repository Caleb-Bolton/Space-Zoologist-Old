using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResearchSingleNoteUIGroup
{
    private List<ResearchSingleNoteUI> notes = new List<ResearchSingleNoteUI>();

    // Add a note
    public void Add(ResearchSingleNoteUI ui)
    {
        notes.Add(ui);
    }

    // Enable/disable all notes
    public void SetActive(bool active)
    {
        foreach(ResearchSingleNoteUI ui in notes)
        {
            ui.gameObject.SetActive(active);
        }
    }
}
