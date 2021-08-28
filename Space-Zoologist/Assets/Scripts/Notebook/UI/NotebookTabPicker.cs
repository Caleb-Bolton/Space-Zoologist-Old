using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class NotebookTabPicker : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Root object where all of the pages will be found")]
    private Transform pagesRoot;
    [SerializeField]
    [Tooltip("Prefab of the button used to select notebook tabs")]
    private NotebookTabSelectButton buttonPrefab;
    [SerializeField]
    [Tooltip("Toggle group used to make only one button selected")]
    private ToggleGroup parent;
    [SerializeField]
    [Tooltip("Reference to the bookmark target to use")]
    private BookmarkTarget bookmarkTarget;

    // Current tab of the picker
    private NotebookTab currentTab;
    // List of the buttons used to select a tab
    private List<NotebookTabSelectButton> buttons = new List<NotebookTabSelectButton>();

    public override void Setup()
    {
        base.Setup();

        // Setup the bookmark target
        bookmarkTarget.Setup(() => currentTab, t => SelectTab((NotebookTab)t));

        // Get all notebook tabs
        NotebookTab[] tabs = (NotebookTab[])System.Enum.GetValues(typeof(NotebookTab));
        
        // Set all pages false
        for(int i = 0; i < tabs.Length; i++)
        {
            pagesRoot.GetChild(i).gameObject.SetActive(false);
        }
        
        for(int i = 0; i < tabs.Length; i++)
        {
            NotebookTabSelectButton button = Instantiate(buttonPrefab, parent.transform);
            // Only the first selector will be on. NOTE: this invokes "OnTabSelected" immediately
            button.Setup(tabs[i], parent, OnTagSelected, i == 0);
            // Add this button to the list
            buttons.Add(button);
        }
    }

    private void OnTagSelected(NotebookTab tab)
    {
        // Disable the current page and enable the new page
        pagesRoot.GetChild((int)currentTab).gameObject.SetActive(false);
        pagesRoot.GetChild((int)tab).gameObject.SetActive(true);
        currentTab = tab;
    }
    // Select a specific notebook tab by selecting one of the buttons
    public void SelectTab(NotebookTab tab)
    {
        buttons[(int)tab].Select();
    }
}
