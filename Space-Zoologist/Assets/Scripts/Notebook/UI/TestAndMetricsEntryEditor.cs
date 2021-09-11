﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class TestAndMetricsEntryEditor : NotebookUIChild
{
    #region Public Properties
    public TestAndMetricsEntry Entry
    {
        get
        {
            if (entry == null)
            {
                // Create a new entry with the current information
                entry = new TestAndMetricsEntry
                {
                    Category = researchCategoryDropdown.SelectedCategory,
                    Need = needDropdown.SelectedNeed,
                    Improved = differenceDropdown.options[differenceDropdown.value].text == "Improved",
                    Notes = inputField.text
                };
                // Add the new entry to the list on the notebook object
                TestAndMetricsEntryList list = UIParent.Notebook.TestAndMetrics.GetEntryList(enclosureID);
                list.Entries.Add(entry);

                // Editor is no longer faded
                group.alpha = 1f;

                // Invoke the new entry created event
                onNewEntryCreated.Invoke();
            }
            return entry;
        }
    }
    public UnityEvent OnNewEntryCreated => onNewEntryCreated;
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Reference to the canvas group that handles all child elements")]
    private CanvasGroup group;
    [SerializeField]
    [Tooltip("Reference to the dropdown to select the research category")]
    private TypeFilteredResearchCategoryDropdown researchCategoryDropdown;
    [SerializeField]
    [Tooltip("Reference to the dropdown that selects the needs analyzed in the metric")]
    private NeedTypeDropdown needDropdown;
    [SerializeField]
    [Tooltip("Reference to the dropdown that selects if the need increased or decreased")]
    private TMP_Dropdown differenceDropdown;
    [SerializeField]
    [Tooltip("Reference to the input field used to write notes")]
    private TMP_InputField inputField;
    [SerializeField]
    [Tooltip("Event invoked when this editor creates a new entry")]
    private UnityEvent onNewEntryCreated;
    #endregion

    #region Private Fields
    // Enclosure ID for the entry we are editing
    private EnclosureID enclosureID;
    // The entry that is edited by this UI
    private TestAndMetricsEntry entry;
    #endregion

    #region Public Methods
    public void Setup(EnclosureID enclosureID, TestAndMetricsEntry entry, ScrollRect scrollTarget)
    {
        base.Setup();

        // Setup private fields
        this.enclosureID = enclosureID;
        this.entry = entry;

        // Setup each dropdown
        researchCategoryDropdown.Setup(ResearchCategoryType.Food, ResearchCategoryType.Species);
        needDropdown.Setup(new NeedType[]{ NeedType.FoodSource, NeedType.Terrain, NeedType.Liquid });
        // Reset the difference options
        differenceDropdown.ClearOptions();
        differenceDropdown.options.Add(new TMP_Dropdown.OptionData("Deteriorated"));
        differenceDropdown.options.Add(new TMP_Dropdown.OptionData("Improved"));

        // Set the initial values of the elements
        if(entry != null)
        {
            researchCategoryDropdown.SetResearchCategory(entry.Category);
            needDropdown.SetNeedTypeValue(entry.Need);
            differenceDropdown.value = entry.Improved ? 1 : 0;
            inputField.text = entry.Notes;
        }
        // If the entry is null, set the values to the first in the dropdown lists
        else
        {
            researchCategoryDropdown.SetDropdownValueWithoutNotify(0);
            needDropdown.SetDropdownValue(0);
            differenceDropdown.value = 0;
            inputField.text = UIParent.Notebook.TestAndMetrics.GetInitialEntryText(enclosureID);
        }

        // Cache the current id
        EnclosureID current = EnclosureID.FromCurrentSceneName();
        // Only add the listeners if this editor is in the current scene
        if (enclosureID == current)
        {
            // Add event listeners for everything
            researchCategoryDropdown.OnResearchCategorySelected.AddListener(x => Entry.Category = x);
            needDropdown.OnNeedTypeSelected.AddListener(x => Entry.Need = x);
            differenceDropdown.onValueChanged.AddListener(x => Entry.Improved = x == 1);
            inputField.onValueChanged.AddListener(x => Entry.Notes = x);
        }

        // Elements are only interactable if id is the same as the current scene
        group.interactable = enclosureID == current;

        // Make elements faded if the entry is null - meaning editing this will add a new entry
        if (entry != null) group.alpha = 1f;
        else group.alpha = 0.5f;

        // Make sure the scroll event is taken away from the input field
        OnScrollEventInterceptor interceptor = inputField.gameObject.AddComponent<OnScrollEventInterceptor>();
        interceptor.InterceptTarget = scrollTarget;
    }
    #endregion
}
