﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class BookmarkAddButton : NotebookUIChild
{
    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Suggested title of the bookmark")]
    private string suggestedTitle = "New Bookmark";
    [SerializeField]
    [Tooltip("Reference to the script to assist with the dropdown functionality")]
    private GeneralDropdown dropdown;
    [SerializeField]
    [Tooltip("Game object that is enabled if the current page has a bookmark")]
    private GameObject hasBookmarkGraphic;
    [SerializeField]
    [Tooltip("Text used to input the name of the new bookmark")]
    private TMP_InputField bookmarkTitle;
    [SerializeField]
    [Tooltip("Reference to the button that adds the bookmark when clicked")]
    private Button confirmButton;
    [SerializeField]
    [Tooltip("Reference to the script that manages the UI for the bookmarks")]
    private NotebookBookmarkNavigationUI bookmarkUI;
    [SerializeField]
    [Tooltip("List of components to target when navigating to the newly added bookmark")]
    protected BookmarkTarget[] bookmarkTargets;
    #endregion

    #region Monobehaviour Messages
    private void OnEnable()
    {
        if (IsSetUp) UpdateInteractable();
    }
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        dropdown.OnDropdownEnabled.AddListener(OnDropdownActivated);
        confirmButton.onClick.AddListener(TryAddBookmark);
        bookmarkTitle.onSubmit.AddListener(s => TryAddBookmark());

        UIParent.OnContentChanged.AddListener(UpdateInteractable);
    }
    public void UpdateInteractable()
    {
        Bookmark bookmark = new Bookmark(suggestedTitle, bookmarkTargets.Select(x => BookmarkData.Create(x)).ToArray());
        dropdown.Interactable = !UIParent.Notebook.HasBookmark(bookmark);
        hasBookmarkGraphic.SetActive(!dropdown.Interactable);
    }
    #endregion

    #region Private / Protected Methods
    private void OnDropdownActivated()
    {
        // When the dropdown is activated then set the suggested bookmark title
        bookmarkTitle.text = suggestedTitle;
    }

    // On click, try to add the bookmark
    // If adding the bookmark succeeds, then make the bookmark UI create a new bookmark
    protected virtual void TryAddBookmark()
    {
        Bookmark bookmark = new Bookmark(bookmarkTitle.text, bookmarkTargets.Select(x => BookmarkData.Create(x)).ToArray());
        if (UIParent.Notebook.TryAddBookmark(bookmark))
        {
            bookmarkUI.CreateBookmarkButton(bookmark);
            dropdown.DisableDropdown();

            // Update interactable state of the button
            UpdateInteractable();
        }

        Debug.Log($"Tried to add a bookmark:" +
            $"\n\tGameObject: {name}" +
            $"\n\tDropdown interactable: {dropdown.Interactable}" +
            $"\n\tHas bookmark graphic active: {hasBookmarkGraphic.activeInHierarchy}" +
            $"\n\tThis bookmark: {bookmark.Label}" +
            $"\n\tExisting bookmarks: \n\t\t{string.Join("\n\t\t", UIParent.Notebook.Bookmarks.Select(b => b.Label))}");
    }
    #endregion
}
