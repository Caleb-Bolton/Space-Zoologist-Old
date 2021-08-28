using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using DG.Tweening;

public class ResearchCategoryPicker : NotebookUIChild
{
    // So that the event appears in the editor
    [System.Serializable] public class ResearchCategoryEvent : UnityEvent<ResearchCategory> { }

    // Public accessors
    #region Public Properties
    public ResearchCategory SelectedCategory
    {
        get => selectedCategory;
        set
        {
            // Set the research category on each dropdown (those that can't select this category ignore it)
            foreach(ResearchCategoryDropdown dropdown in dropdowns)
            {
                dropdown.SetResearchCategory(value);
            }
        }
    }
    public ResearchCategoryEvent OnResearchCategoryChanged => onResearchCategoryChanged;
    public bool HasBeenInitialized => !string.IsNullOrEmpty(selectedCategory.Name);
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Reference to the prefab used to select a research category")]
    private TypeFilteredResearchCategoryDropdown dropdown;
    [SerializeField]
    [Tooltip("Parent transform that the dropdowns are instantiated into")]
    private Transform dropdownParent;
    [SerializeField]
    [Tooltip("Sprite of the dropdown when it is selected")]
    private Sprite selectedSprite;
    [SerializeField]
    [Tooltip("Sprite of the dropdown when it is not selected")]
    private Sprite notSelectedSprite;
    [SerializeField]
    [Tooltip("Scale of the button when it is selected")]
    private float selectedScale = 0.2f;
    [SerializeField]
    [Tooltip("Time it takes for the button to grow/shrink when selected/deselected")]
    private float sizeChangeTime = 0.5f;
    [SerializeField]
    [Tooltip("Reference to the script targetted by the bookmarking system")]
    private BookmarkTarget bookmarkTarget;
    [SerializeField]
    [Tooltip("Event invoked when the research category picker changes category picked")]
    private ResearchCategoryEvent onResearchCategoryChanged;
    #endregion

    #region Private Fields
    // List of the dropdowns used by the category picker
    private List<ResearchCategoryDropdown> dropdowns = new List<ResearchCategoryDropdown>();
    // The category currently selected
    private ResearchCategory selectedCategory;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        // Setup the bookmark target
        bookmarkTarget.Setup(() => SelectedCategory, c => SelectedCategory = (ResearchCategory)c);

        ResearchCategoryType[] types = (ResearchCategoryType[])System.Enum.GetValues(typeof(ResearchCategoryType));

        // Instantiate a dropdown for each type and set it up
        foreach(ResearchCategoryType type in types)
        {
            TypeFilteredResearchCategoryDropdown clone = Instantiate(dropdown, dropdownParent);
            // Setup the clone's type and add a listener for the category change
            clone.Setup(type);
            clone.OnResearchCategorySelected.AddListener(ResearchCategoryChanged);
            // Add to the list of our dropdowns
            dropdowns.Add(clone);
        }

        // Set the first value of the first dropdown
        // NOTE: this should invoke ResearchCategoryChanged immediately
        dropdowns[0].SetDropdownValue(0);
    }
    #endregion

    #region Private Methods
    private void ResearchCategoryChanged(ResearchCategory category)
    {
        selectedCategory = category;

        // Set the sprites of the backgrounds of the dropdown images
        foreach(TypeFilteredResearchCategoryDropdown dropdown in dropdowns)
        {
            // Get this dropdown rect transform. Make sure it finishes any scale animations
            RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
            //dropdownRect.DOComplete(true);

            if (dropdown.TypeFilter[0] == category.Type)
            {
                // Make this dropdown a little bigger
                dropdownRect.DOScale(1f + selectedScale, sizeChangeTime);
                // Set the correct sprite
                dropdown.Dropdown.image.sprite = selectedSprite;
            }
            else
            {
                // Make this dropdown the default size
                dropdownRect.DOScale(1f, sizeChangeTime);
                // Set the correct sprite
                dropdown.Dropdown.image.sprite = notSelectedSprite;
            }
        }

        onResearchCategoryChanged.Invoke(category);
        UIParent.OnContentChanged.Invoke();
    }
    #endregion
}
