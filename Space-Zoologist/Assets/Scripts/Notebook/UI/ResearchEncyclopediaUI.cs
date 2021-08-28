using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchEncyclopediaUI : NotebookUIChild
{
    public ResearchEncyclopedia CurrentEncyclopedia => UIParent.Notebook.Research.GetEntry(currentCategory).Encyclopedia;
    public ResearchEncyclopediaArticle CurrentArticle => CurrentEncyclopedia.GetArticle(currentArticleID);

    public ResearchEncyclopediaArticleID CurrentArticleID
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

    [SerializeField]
    [Tooltip("Reference to the widget that selects the category for the encyclopedia")]
    private ResearchCategoryPicker categoryPicker;
    [SerializeField]
    [Tooltip("Dropdown used to select available encyclopedia articles")]
    private TMP_Dropdown dropdown;
    [SerializeField]
    [Tooltip("Input field used to display the encyclopedia article")]
    private ResearchEncyclopediaArticleInputField articleBody;
    [SerializeField]
    [Tooltip("Script that is targetted by the bookmarking system")]
    private BookmarkTarget bookmarkTarget;

    // Maps the research category to the index of the article previously selected
    private Dictionary<ResearchCategory, int> previousSelected = new Dictionary<ResearchCategory, int>();
    // Current research category selected
    private ResearchCategory currentCategory;
    // Current research article selected
    private ResearchEncyclopediaArticleID currentArticleID;

    public override void Setup()
    {
        base.Setup();

        // Add listener for change of dropdown value
        // (is "on value changed" invoked at the start?)
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // If the category picker is already initialized, we need to update our UI
        if(categoryPicker.HasBeenInitialized)
        {
            OnResearchCategoryChanged(categoryPicker.SelectedCategory);
        }

        // Add listener for changes in the research category selected
        categoryPicker.OnResearchCategoryChanged.AddListener(OnResearchCategoryChanged);

        // Setup the bookmark target to get/set the article id
        bookmarkTarget.Setup(() => CurrentArticleID, x => CurrentArticleID = (ResearchEncyclopediaArticleID)x);
    }

    private void OnResearchCategoryChanged(ResearchCategory category)
    {
        // If the current category exists, save the value previously selected in the dictionary
        if(previousSelected.ContainsKey(currentCategory)) previousSelected[currentCategory] = dropdown.value;

        // Set currently selected category
        currentCategory = category;
        // Clear the options of the dropdown
        dropdown.ClearOptions();

        // Loop through all articles in the current encyclopedia and add their title-author pairs to the dropdown list
        foreach(KeyValuePair<ResearchEncyclopediaArticleID, ResearchEncyclopediaArticle> article in CurrentEncyclopedia.Articles)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(ArticleIDToDropdownLabel(article.Key)));
        }
        // Select the first article in the list
        if (previousSelected.ContainsKey(category)) dropdown.value = previousSelected[category];
        else
        {
            previousSelected[category] = 0;
            dropdown.value = 0;
        }

        // Refresh the shown value since we just changed it
        dropdown.RefreshShownValue();
        OnDropdownValueChanged(dropdown.value);
    }

    private void OnDropdownValueChanged(int value)
    {
        if (dropdown.options.Count > 0)
        {
            // Create the id object
            currentArticleID = DropdownLabelToArticleID(dropdown.options[value].text);
            // Update the article on the script
            articleBody.UpdateArticle(CurrentArticle);
        }
        else articleBody.UpdateArticle(null);
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
        if(titleAndAuthor.Length > 1)
        {
            return new ResearchEncyclopediaArticleID(titleAndAuthor[0], titleAndAuthor[1]);
        }
        // If there was only one item, we know that there was not author
        else return new ResearchEncyclopediaArticleID(titleAndAuthor[0], "");
    }
}
