using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class NotebookBookmarkNavigateButton : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Button that navigates to the bookmark when clicked")]
    private Button navigateButton;
    [SerializeField]
    [Tooltip("Reference to the toggle that will delete the bookmark when navigating to another page")]
    private Toggle deleteToggle;
    [SerializeField]
    [Tooltip("Reference to the object used to display the text of the button")]
    private TextMeshProUGUI text;

    // Bookmark represented by this button
    private Bookmark bookmark;
    // The game object previously selected on the event system
    private GameObject previousSelectedGameObject;
    // The number of clicks this button has received while selected
    private int clicksWhileSelected = 0;

    private void Update()
    {
        GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;

        // If the current selected game object just changed, and it used to be the navigate button, reset clicks while selected
        if(previousSelectedGameObject != currentSelectedGameObject && previousSelectedGameObject == navigateButton.gameObject)
        {
            clicksWhileSelected = 0;
        }

        // Set the previous selected object at the end of the frame
        previousSelectedGameObject = currentSelectedGameObject;
    }

    public void Setup(Bookmark bookmark)
    {
        base.Setup();
        this.bookmark = bookmark;

        text.text = bookmark.Label;
        navigateButton.onClick.AddListener(OnNavigateButtonClicked);
        deleteToggle.onValueChanged.AddListener(OnDeleteToggleChanged);
    }

    // On click select the correct tab, and setup the category picker
    private void OnNavigateButtonClicked()
    {
        clicksWhileSelected++;

        // If this is the second click while selected then navigate to the bookmark
        if(clicksWhileSelected >= 2) UIParent.NavigateToBookmark(bookmark);
    }

    private void OnDeleteToggleChanged(bool isOn)
    {
        navigateButton.interactable = isOn;

        if (isOn) text.text = bookmark.Label;
        else text.text = "<marked for deletion>";
    }

    private void OnDisable()
    {
        if (!deleteToggle.isOn)
        {
            UIParent.Notebook.RemoveBookmark(bookmark);
            Destroy(gameObject);
        }
    }
}
