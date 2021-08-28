using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;
using DG.Tweening;

public class NotebookTabSelectButton : MonoBehaviour
{
    [System.Serializable]
    public class NotebookTabEvent : UnityEvent<NotebookTab> { }

    [SerializeField]
    [Tooltip("Reference to the rect transform to change size for")]
    private RectTransform rectTransform;
    [SerializeField]
    [Tooltip("Toggle that selects the notebook tab")]
    private Toggle myToggle;
    [SerializeField]
    [Tooltip("Text that displays the name of the notebook tab this button navigates to")]
    private TextMeshProUGUI tabName;
    [SerializeField]
    [Tooltip("Amount that the notebook tab select button grows when it is selected")]
    private float selectedGrowthSize = 0.3f;
    [SerializeField]
    [Tooltip("Time it takes for the button to grow/shrink when selected/deselected")]
    private float sizeChangeTime = 0.5f;
    [SerializeField]
    [Tooltip("Event invoked when this button is selected")]
    private NotebookTabEvent selectedEvent;

    // Notebook tab that this button navigates to
    private NotebookTab tab;

    public void Setup(NotebookTab tab, ToggleGroup parent, UnityAction<NotebookTab> callback, bool isOn)
    {
        this.tab = tab;

        // Setup the toggle 
        myToggle.group = parent;
        myToggle.onValueChanged.AddListener(OnToggleStateChanged);
        // Listen for the callback when selected event is raised
        selectedEvent.AddListener(callback);

        // Set the text on the GUI element
        tabName.text = tab.ToString();

        // Set state of the toggle.  NOTE: this invokes OnToggleStateChanged immediately
        myToggle.isOn = isOn;
    }

    private void OnToggleStateChanged(bool state)
    {
        if (state) 
        {
            // Slightly grow the rect transform when selected
            rectTransform.DOScale(1f + selectedGrowthSize, sizeChangeTime);
            // Invoke selection event
            selectedEvent.Invoke(tab); 
        }
        // If deselected, shrink back to normal size
        else
        {
            rectTransform.DOScale(1f, sizeChangeTime);
        }
    }
    // Select this button by setting toggle to be "on"
    // NOTE: Immediately invokes OnToggleStateChanged
    public void Select()
    {
        myToggle.isOn = true;
    }
}
