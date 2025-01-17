﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchEncyclopediaUI : NotebookUIChild
{
    #region Public Properties
    public ResearchEncyclopediaConfig EncyclopediaConfig => UIParent.Config.Research.GetEntry(currentItem).Encyclopedia;
    public ResearchEncyclopediaArticleConfig ArticleConfig => EncyclopediaConfig != null ? EncyclopediaConfig.GetArticle(currentArticleID) : null;
    public ResearchEncyclopediaArticleData ArticleData => UIParent.Data.Research.GetEntry(currentItem).GetArticleData(currentArticleID);
    public ResearchEncyclopediaArticleID ArticleID
    {
        get => currentArticleID;
        set
        {
            List<ResearchEncyclopediaArticleID> ids = GetDropdownIDs();
            int index = ids.FindIndex(x => x == value);

            if(index >= 0 && index < dropdown.options.Count)
            {
                // NOTE: this invokes "OnDropdownValueChanged" immediately
                dropdown.value = index;
                dropdown.RefreshShownValue();
            }
            else
            {
                Debug.LogWarning("Encyclopedia article " + value.ToString() + " was not found in the dropdown, so the new value will be ignored");
            }
        }
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Reference to the widget that selects the category for the encyclopedia")]
    private ItemPicker itemPicker = null;
    [SerializeField]
    [Tooltip("Dropdown used to select available encyclopedia articles")]
    private TMP_Dropdown dropdown = null;
    [SerializeField]
    [Tooltip("Input field used to display the encyclopedia article")]
    private ResearchEncyclopediaArticleInputField articleBody = null;
    [SerializeField]
    [Tooltip("Script that is targetted by the bookmarking system")]
    private BookmarkTarget bookmarkTarget = null;
    [SerializeField]
    [Tooltip("Scrolling")]
    private Scrollbar contentScrollbar = null;
    #endregion

    #region Private Fields
    // Maps the research category to the index of the article previously selected
    private Dictionary<ItemID, int> previousSelected = new Dictionary<ItemID, int>();
    // Current research category selected
    private ItemID currentItem = new ItemID(ItemRegistry.Category.Species, -1);
    // Current research article selected
    private ResearchEncyclopediaArticleID currentArticleID;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        // Add listener for change of dropdown value
        // (is "on value changed" invoked at the start?)
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // If the category picker is already initialized, we need to update our UI
        if(itemPicker.HasBeenInitialized)
        {
            OnItemIDChanged(itemPicker.SelectedItem);
        }

        // Add listener for changes in the research category selected
        itemPicker.OnItemPicked.AddListener(OnItemIDChanged);

        // Setup the bookmark target to get/set the article id
        bookmarkTarget.Setup(() => ArticleID, x => ArticleID = (ResearchEncyclopediaArticleID)x);
    }
    // Get a list of the research article IDs currently in the dropdown
    public List<ResearchEncyclopediaArticleID> GetDropdownIDs()
    {
        return dropdown.options
            .Select(o => DropdownLabelToArticleID(o.text))
            .ToList();
    }
    public static string ArticleIDToDropdownLabel(ResearchEncyclopediaArticleID id)
    {
        string label = id.Title;
        // Only include the author if it has an author
        if (id.Author != "") label += "\n" + id.Author;
        return label;
    }
    public static ResearchEncyclopediaArticleID DropdownLabelToArticleID(string label)
    {
        string[] titleAndAuthor = Regex.Split(label, "\n");

        // If there are two items in the split string, use them both
        if (titleAndAuthor.Length > 1)
        {
            return new ResearchEncyclopediaArticleID(titleAndAuthor[0], titleAndAuthor[1]);
        }
        // If there was only one item, we know that there was not author
        else return new ResearchEncyclopediaArticleID(titleAndAuthor[0], "");
    }
    #endregion

    #region Private Methods
    private void OnItemIDChanged(ItemID id)
    {
        // If a current item is selected that save the dropdown value that was previously selected
        if (currentItem.Index >= 0) previousSelected[currentItem] = dropdown.value;

        // Set currently selected category
        currentItem = id;
        // Clear the options of the dropdown
        dropdown.ClearOptions();

        // Check if encyclopedia is null before trying to use it
        if(EncyclopediaConfig != null)
        {
            // Loop through all articles in the current encyclopedia and add their title-author pairs to the dropdown list
            foreach (ResearchEncyclopediaArticleConfig article in EncyclopediaConfig.Articles)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(ArticleIDToDropdownLabel(article.ID)));
            }
            // If this item has been selected before, open to the article that was selected previously
            if (previousSelected.ContainsKey(id)) dropdown.value = previousSelected[id];
            // If this item has not been selected before, then select the first article
            else
            {
                previousSelected[id] = 0;
                dropdown.value = 0;
            }
        }
        // If the current encyclopedia is null then there are no articles to pick
        else
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData("<No articles>"));
            dropdown.value = 0;
        }

        // Refresh the shown value since we just changed it
        dropdown.RefreshShownValue();
        OnDropdownValueChanged(dropdown.value);
    }

    private void OnDropdownValueChanged(int value)
    {
        if (EncyclopediaConfig != null)
        {
            // Create the id object
            currentArticleID = DropdownLabelToArticleID(dropdown.options[value].text);
            // Update the article on the script
            articleBody.UpdateArticle(ArticleConfig, ArticleData);
            // articleBody.UpdateArticle(CurrentArticle);
            // For backend: event invocation for tracking number of articles read.
            EventManager.Instance.InvokeEvent(EventType.OnArticleChanged, null);
            contentScrollbar.value = 1;
        }
        else articleBody.UpdateArticle(null, null);
    }
    #endregion
}
