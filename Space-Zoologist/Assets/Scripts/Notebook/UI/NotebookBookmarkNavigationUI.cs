using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotebookBookmarkNavigationUI : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Prefab of the buttons used to navigate to a bookmark")]
    private NotebookBookmarkNavigateButton buttonPrefab;
    [SerializeField]
    [Tooltip("Parent object for all navigation buttons")]
    private Transform buttonParent;

    public override void Setup()
    {
        base.Setup();

        // Create a bookmark for each bookmark currently in the notebook
        // NOTE: doesn't work because the bookmarks can't be serialized - they lose their base type
        // Probably wouldn't work on the client't end anyways, there must be some way to associate
        // it with the player's account or something
        for (int i = 0; i < UIParent.Notebook.Bookmarks.Count; i++)
        {
            CreateBookmarkButton(UIParent.Notebook.Bookmarks[i]);
        }
    }

    // When a new bookmark is created, then instantiate a new button for it
    public void CreateBookmarkButton(Bookmark newBookmark)
    {
        NotebookBookmarkNavigateButton clone = Instantiate(buttonPrefab, buttonParent);
        clone.Setup(newBookmark);
    }
}
