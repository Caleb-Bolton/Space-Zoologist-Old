﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Notebook/Notebook")]
public class NotebookModel : ScriptableObject
{
    public string Acronym => acronym;
    public ResearchModel Research => research;
    public List<NotebookBookmark> Bookmarks { get; private set; } = new List<NotebookBookmark>();

    [SerializeField]
    [Tooltip("Acronym that the player gets to spell out on the home page")]
    private string acronym = "ROCTM";
    [SerializeField]
    [Expandable]
    [Tooltip("Reference to the model holding all the player's research and info" +
        "about the different species, foods, and tiles")]
    private ResearchModel research;

    private Dictionary<char, string> acronymNotes = new Dictionary<char, string>();

    public void Setup()
    {
        // Foreach letter in the acronym, add an empty string to the dictionary
        foreach(char c in acronym)
        {
            acronymNotes.Add(c, "");
        }
        research.Setup();
    }

    public string ReadAcronymNote(char c) => acronymNotes[c];
    public void WriteAcronymNote(char c, string note) => acronymNotes[c] = note;

    // Return true/false if the notebook already has this bookmark
    public bool HasBookmark(NotebookBookmark bookmark) => Bookmarks.Contains(bookmark);
    // Add the bookmark if the notebook doesn't already have it in the list
    public bool TryAddBookmark(NotebookBookmark bookmark)
    {
        if (!Bookmarks.Contains(bookmark))
        {
            AddBookmark(bookmark);
            return true;
        }
        else return false;
    }
    private void AddBookmark(NotebookBookmark bookmark)
    {
        Bookmarks.Add(bookmark);
    }
}
