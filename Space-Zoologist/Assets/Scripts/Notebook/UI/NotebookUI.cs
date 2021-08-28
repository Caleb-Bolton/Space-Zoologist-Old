using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class NotebookUI : MonoBehaviour
{
    // Public accessors
    public NotebookModel Notebook => notebook;
    public UnityEvent OnContentChanged => onContentChanged;

    [SerializeField]
    [Expandable]
    [Tooltip("Reference to the serialized object that holds all info about the notebook")]
    private NotebookModel notebook;
    [SerializeField]
    [Tooltip("Reference to the script that selects the tabs in the notebook")]
    private NotebookTabPicker tabPicker;
    [SerializeField]
    [Tooltip("Event invoked when the content on the notebook changes")]
    private UnityEvent onContentChanged;

    // Maps the names of the category pickers to the components for fast lookup
    // Used for navigating to a bookmark in the notebook
    private Dictionary<string, BookmarkTarget> nameTargetMap = new Dictionary<string, BookmarkTarget>();
    private bool isOpen = false;

    // I thought that this was called when the game object is inactive but apparently it is not
    private void Awake()
    {
        // Setup the notebook at the start
        notebook.Setup();

        // Update the enclosure IDs
        notebook.TryAddEnclosureID(EnclosureID.FromCurrentSceneName());

        // Map all pickers to their corresponding name
        BookmarkTarget[] allBookmarkTargets = GetComponentsInChildren<BookmarkTarget>(true);
        foreach (BookmarkTarget bookmarkTarget in allBookmarkTargets)
        {
            nameTargetMap.Add(bookmarkTarget.name, bookmarkTarget);
        }

        // Setup all children, ensuring correct initialization order
        NotebookUIChild[] children = GetComponentsInChildren<NotebookUIChild>(true);
        foreach (NotebookUIChild child in children) child.Setup();

        // This line of code prevents the notebook from turning off the first time that it is turned on,
        // while also making sure it is turned off at the start
        if (!isOpen) SetIsOpen(false);
    }
    public void Toggle()
    {
        SetIsOpen(!isOpen);
    }
    public void SetIsOpen(bool isOpen)
    {
        this.isOpen = isOpen;
        gameObject.SetActive(isOpen);
    }

    public void NavigateToBookmark(Bookmark bookmark)
    {
        bookmark.Navigate(nameTargetMap);
    }
}
