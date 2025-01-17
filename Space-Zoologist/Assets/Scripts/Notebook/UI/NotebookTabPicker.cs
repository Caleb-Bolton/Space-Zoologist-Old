﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class NotebookTabPicker : NotebookUIChild
{
    #region Public Typedefs
    [System.Serializable]
    public class NotebookTabObjects
    {
        [SerializeField]
        [Tooltip("List of objects mapped to each of the available notebook objects")]
        private GameObject[] tabObjects;

        public NotebookTabObjects()
        {
            string[] tabNames = System.Enum.GetNames(typeof(NotebookTab));
            tabObjects = new GameObject[tabNames.Length];
        }
        public GameObject Get(NotebookTab tab) => tabObjects[(int)tab];
        public void Set(NotebookTab tab, GameObject tabObject) => tabObjects[(int)tab] = tabObject;
    }
    #endregion

    #region Public Properties
    public NotebookTab CurrentTab => currentTab;
    public System.Action OnTabSelect { get => onTabSelect; set => onTabSelect = value; }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Root object where all of the pages will be found")]
    private Transform pagesRoot = null;
    [SerializeField]
    [EditArrayWrapperOnEnum("tabObjects", typeof(NotebookTab))]
    [Tooltip("List of prefabs to instantiate for each notebook page")]
    private NotebookTabObjects tabPrefabs = null;
    [SerializeField]
    [Tooltip("Prefab of the button used to select notebook tabs")]
    private NotebookTabSelectButton buttonPrefab = null;
    [SerializeField]
    [Tooltip("Toggle group used to make only one button selected")]
    private ToggleGroup parent = null;
    [SerializeField]
    [Tooltip("Reference to the bookmark target to use")]
    private BookmarkTarget bookmarkTarget = null;
    #endregion

    #region Private Fields
    // Current tab of the picker
    private NotebookTab currentTab;
    // List of the buttons used to select a tab
    private List<NotebookTabSelectButton> buttons = new List<NotebookTabSelectButton>();
    private NotebookTabObjects tabObjects = new NotebookTabObjects();
    private System.Action onTabSelect;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        if(!IsSetUp)
        {
            base.Setup();

            // Setup the bookmark target
            bookmarkTarget.Setup(() => currentTab, t => SelectTab((NotebookTab)t));

            // Get all notebook tabs
            NotebookTab[] tabs = (NotebookTab[])System.Enum.GetValues(typeof(NotebookTab));

            foreach (NotebookTab tab in tabs)
            {
                // Instantiate the prefab for this tab
                GameObject tabObject = Instantiate(tabPrefabs.Get(tab), pagesRoot);
                tabObjects.Set(tab, tabObject);

                // Create a notebook tab select button for this tab
                NotebookTabSelectButton button = Instantiate(buttonPrefab, parent.transform);
                button.Setup(tab, parent, OnTabSelected);
                buttons.Add(button);
            }

            // Select the home tab by default
            SelectTab(NotebookTab.Home);
        }
    }
    // Select a specific notebook tab by selecting one of the buttons
    public void SelectTab(NotebookTab tab)
    {
        GetTabSelectButton(tab).Select();
    }
    /// <summary>
    /// Get the root transform for the given notebook tab
    /// </summary>
    /// <param name="tab"></param>
    /// <returns></returns>
    public Transform GetTabRoot(NotebookTab tab)
    {
        return pagesRoot.GetChild((int)tab);
    }
    public NotebookTabSelectButton GetTabSelectButton(NotebookTab tab)
    {
        int index = (int)tab;

        if (index >= 0 && index < buttons.Count) return buttons[index];
        else throw new System.IndexOutOfRangeException($"{nameof(NotebookTabPicker)}: " +
            $"No notebook tab button defined for tab '{tab}'. Total buttons: {buttons.Count}");
    }
    #endregion

    #region Private Methods
    private void OnTabSelected(NotebookTab tab)
    {
        // Set the current tab to this tab
        // This order is critical!  The current tab has to be set before
        // the game objects activate because some of them read the current tab
        // when they are enabled
        currentTab = tab;

        AudioManager.instance.PlayOneShot(SFXType.NotebookTabSwitch);
        // Enable / Disable the correct objects
        NotebookTab[] tabs = (NotebookTab[])System.Enum.GetValues(typeof(NotebookTab));
        foreach(NotebookTab t in tabs)
        {
            tabObjects.Get(t).SetActive(t == tab);
        }
        // For backend: Event invocation for tab switch.
        EventManager.Instance.InvokeEvent(EventType.OnTabChanged, null);
        onTabSelect?.Invoke();
    }
    #endregion
}
