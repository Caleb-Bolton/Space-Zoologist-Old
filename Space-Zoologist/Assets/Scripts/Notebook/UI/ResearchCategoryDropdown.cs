using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using TMPro;

public class ResearchCategoryDropdown : NotebookUIChild
{
    [System.Serializable]
    public class ResearchCategoryEvent : UnityEvent<ResearchCategory> { }

    // Public accessors
    public TMP_Dropdown Dropdown => dropdown;
    public ResearchCategoryEvent OnResearchCategorySelected => onResearchCategorySelected;
    public ResearchCategory SelectedCategory => optionCategoryMap[dropdown.options[dropdown.value]];

    // Private editor data

    [SerializeField]
    [Tooltip("Reference to the dropdown used to select the research category")]
    protected TMP_Dropdown dropdown;
    [SerializeField]
    [Tooltip("True if text and image should display simultaneously")]
    protected bool textAndImage = false;
    [SerializeField]
    [Tooltip("Event invoked when this dropdown selects a research category")]
    protected ResearchCategoryEvent onResearchCategorySelected;

    // Maps a selected item in the dropdown to a research category
    // NOTE: why don't we just change this to two conversion functions to change betweeen types?
    protected Dictionary<TMP_Dropdown.OptionData, ResearchCategory> optionCategoryMap = new Dictionary<TMP_Dropdown.OptionData, ResearchCategory>();

    public override void Setup()
    {
        base.Setup();

        // Clear any existing data
        dropdown.ClearOptions();
        optionCategoryMap.Clear();

        ResearchCategory[] allCategories = GetResearchCategories();

        foreach(ResearchCategory category in GetResearchCategories())
        {
            // Get the current option
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(category.Name, category.Image);
            // Add the option to the dropdown and the dictionary
            dropdown.options.Add(option);
            optionCategoryMap.Add(option, category);
        }

        // Setup the value changed callback
        dropdown.onValueChanged.AddListener(SetDropdownValue);
        // Setup the value to the first one
        if (dropdown.options.Count > 0) SetDropdownValueWithoutNotify(0);
    }

    // Set the value of the dropdown
    public void SetDropdownValue(int value) => SetDropdownValueHelper(value, true);
    public void SetDropdownValueWithoutNotify(int value) => SetDropdownValueHelper(value, false);

    public bool SetResearchCategory(ResearchCategory category) => SetResearchCategoryHelper(category, v => SetDropdownValue(v));
    public bool SetResearchCategoryWithoutNotify(ResearchCategory category) => SetResearchCategoryHelper(category, v => SetDropdownValueWithoutNotify(v));

    private bool SetResearchCategoryHelper(ResearchCategory category, UnityAction<int> valueSetter)
    {
        // Find the first value in the list that matches
        TMP_Dropdown.OptionData selection = optionCategoryMap.FirstOrDefault(kvp => kvp.Value == category).Key;

        if (selection != null)
        {
            int value = dropdown.options.FindIndex(option => option == selection);

            // If the option was found, then invoke the event
            if (value >= 0)
            {
                valueSetter.Invoke(value);
                return true;
            }
            else return false;
        }
        else return false;
    }
    private void SetDropdownValueHelper(int value, bool notify)
    {
        // Get the selected option
        TMP_Dropdown.OptionData selection = dropdown.options[value];

        // Display image if it is not null, otherwise display text
        dropdown.captionImage.enabled = textAndImage || selection.image != null;
        dropdown.captionText.enabled = textAndImage || selection.image == null;

        // Set the value and refresh the shown value
        dropdown.value = value;
        dropdown.RefreshShownValue();

        // If we are notifying then raise the event
        if (notify) onResearchCategorySelected.Invoke(optionCategoryMap[selection]);
    }
    protected virtual ResearchCategory[] GetResearchCategories()
    {
        return UIParent.Notebook.Research.ResearchDictionary
            .Select(kvp => kvp.Key)
            .ToArray();
    }
}
