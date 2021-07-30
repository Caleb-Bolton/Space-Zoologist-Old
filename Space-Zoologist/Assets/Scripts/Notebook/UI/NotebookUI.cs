﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotebookUI : MonoBehaviour
{
    // Public accessors
    public Notebook NotebookModel => notebookModel;

    [SerializeField]
    [Expandable]
    [Tooltip("Reference to the serialized object that holds all info about the notebook")]
    private Notebook notebookModel;
    [SerializeField]
    [Tooltip("Reference to the script that selects the tabs in the notebook")]
    private NotebookTabPicker tabPicker;

    // Maps the names of the category pickers to the components for fast lookup
    // Used for navigating to a bookmark in the notebook
    private Dictionary<string, ResearchCategoryPicker> namePickerMap = new Dictionary<string, ResearchCategoryPicker>();

    private void Awake()
    {
        gameObject.SetActive(false);

        // Map all pickers to their corresponding name
        ResearchCategoryPicker[] allPickers = GetComponentsInChildren<ResearchCategoryPicker>(true);
        foreach (ResearchCategoryPicker picker in allPickers)
        {
            namePickerMap.Add(picker.name, picker);
        }
    }
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
    public void NavigateToBookmark(NotebookBookmark bookmark)
    {
        // Get the expected component in the children of the notebook somewhere
        Component component;

        // Set component based on if type is null
        if (bookmark.ExpectedComponentType != null) component = GetComponentInChildren(bookmark.ExpectedComponentType, true);
        else component = null;

        bookmark.NavigateTo(tabPicker, namePickerMap, component);
    }
}
