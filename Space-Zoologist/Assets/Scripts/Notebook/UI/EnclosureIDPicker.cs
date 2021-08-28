using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// UI element used to pick an enclosure ID
/// </summary>
public class EnclosureIDPicker : NotebookUIChild
{
    #region Typedefs
    [System.Serializable] public class EnclosureIDEvent : UnityEvent<EnclosureID> { } 
    #endregion

    #region Constants
    private const string levelPrefix = "Level ";
    private const string enclosurePrefix = "Enclosure ";
    #endregion

    // Public accessors
    #region Public Properties
    public EnclosureIDEvent OnEnclosureIDPicked => onEnclosureIDPicked;
    public EnclosureID CurrentEnclosureID
    {
        get
        {
            return new EnclosureID(
                OptionDataToLevelNumber(levelDropdown.options[levelDropdown.value]),
                OptionDataToEnclosureNumber(enclosureDropdown.options[enclosureDropdown.value]));
        }

        set
        {
            // Get the option in the level dropdown we are trying to select
            TMP_Dropdown.OptionData targetOption = LevelNumberToOptionData(value.LevelNumber);
            int optionIndex = levelDropdown.options.FindIndex(x => x.text == targetOption.text);

            if (optionIndex >= 0)
            {
                // Set the value of the level dropdown
                levelDropdown.SetValueWithoutNotify(optionIndex);
                levelDropdown.RefreshShownValue();

                // Get the target option in the list of enclosure options
                targetOption = EnclosureNumberToOptionData(value.EnclosureNumber);
                optionIndex = enclosureDropdown.options.FindIndex(x => x.text == targetOption.text);

                // If an option is found, invoke the event to change the enclosure dropdown index value
                if (optionIndex >= 0) OnEnclosureDropdownValueChanged(optionIndex);
            }
        }
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Reference to the dropdown used to select the level number")]
    private TMP_Dropdown levelDropdown;
    [SerializeField]
    [Tooltip("Reference to the dropdown used to select the enclosure number")]
    private TMP_Dropdown enclosureDropdown;
    [SerializeField]
    [Tooltip("Reference to the script targetted by the bookmarking system")]
    private BookmarkTarget bookmarkTarget;
    [SerializeField]
    [Tooltip("Event invoked when an enclosure ID is picked")]
    private EnclosureIDEvent onEnclosureIDPicked;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        // Setup the bookmark target to get-set the enclosure id
        bookmarkTarget.Setup(() => CurrentEnclosureID, x => CurrentEnclosureID = (EnclosureID)x);

        // Clear out any existing options
        levelDropdown.ClearOptions();
        enclosureDropdown.ClearOptions();

        // Loop through all enclosure id's and add them to the list
        foreach (EnclosureID id in UIParent.Notebook.EnclosureIDs)
        {
            TMP_Dropdown.OptionData option = LevelNumberToOptionData(id.LevelNumber);
            // If no option with the same text yet exists, then add it to the dropdown
            if (levelDropdown.options.FindIndex(x => x.text == option.text) < 0)
            {
                levelDropdown.options.Add(option);
            }
        }

        // Update the level dropdown to reflect the current level
        EnclosureID currentEnclosure = EnclosureID.FromCurrentSceneName();
        levelDropdown.value = currentEnclosure.LevelNumber;
        levelDropdown.RefreshShownValue();
        OnLevelDropdownValueChanged(levelDropdown.value);

        // Add listeners for the value changed events
        levelDropdown.onValueChanged.AddListener(OnLevelDropdownValueChanged);
        enclosureDropdown.onValueChanged.AddListener(OnEnclosureDropdownValueChanged);
    }
    #endregion

    #region Private Methods
    private void OnLevelDropdownValueChanged(int value)
    {
        int selectedLevel = OptionDataToLevelNumber(levelDropdown.options[value]);

        // Clear out the options in the enclosure dropdown
        enclosureDropdown.ClearOptions();
        foreach (EnclosureID id in UIParent.Notebook.EnclosureIDs)
        {
            if (id.LevelNumber == selectedLevel) enclosureDropdown.options.Add(EnclosureNumberToOptionData(id.EnclosureNumber));
        }

        // Get the enclosure represented in the current scene
        EnclosureID currentEnclosure = EnclosureID.FromCurrentSceneName();
        // If we selected the current level, then select the current enclosure number
        if (currentEnclosure.LevelNumber == selectedLevel)
        {
            enclosureDropdown.value = currentEnclosure.EnclosureNumber - 1;
        }
        else enclosureDropdown.value = 0;

        // Set the enclosure to the first one selected
        enclosureDropdown.RefreshShownValue();
        OnEnclosureDropdownValueChanged(enclosureDropdown.value);
    }
    private void OnEnclosureDropdownValueChanged(int value)
    {
        TMP_Dropdown.OptionData levelOptionSelected = levelDropdown.options[levelDropdown.value];
        TMP_Dropdown.OptionData enclosureOptionSelected = enclosureDropdown.options[value];
        EnclosureID enclosureSelected = new EnclosureID(OptionDataToLevelNumber(levelOptionSelected), OptionDataToEnclosureNumber(enclosureOptionSelected));
        onEnclosureIDPicked.Invoke(enclosureSelected);
        UIParent.OnContentChanged.Invoke();
    }

    // Private conversions from option data to numbers and back
    private int OptionDataToLevelNumber(TMP_Dropdown.OptionData option)
    {
        return int.Parse(option.text.Substring(levelPrefix.Length));
    }
    private int OptionDataToEnclosureNumber(TMP_Dropdown.OptionData option)
    {
        return int.Parse(option.text.Substring(enclosurePrefix.Length));
    } 
    private TMP_Dropdown.OptionData LevelNumberToOptionData(int levelNumber)
    {
        return new TMP_Dropdown.OptionData(levelPrefix + levelNumber.ToString());
    }
    private TMP_Dropdown.OptionData EnclosureNumberToOptionData(int enclosureNumber)
    {
        return new TMP_Dropdown.OptionData(enclosurePrefix + enclosureNumber.ToString());
    }
    #endregion
}
