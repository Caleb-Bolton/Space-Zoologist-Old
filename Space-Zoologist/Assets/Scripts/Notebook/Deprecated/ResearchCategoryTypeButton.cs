using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

public class ResearchCategoryTypeButton : MonoBehaviour
{
    // Typedef so it will appear in the editor
    [System.Serializable]
    public class ResearchCategoryTypeEvent : UnityEvent<ResearchCategoryType> { }

    // Public accessors
    public Toggle MyToggle => myToggle;

    [SerializeField]
    [Tooltip("The toggle that manages this button")]
    private Toggle myToggle;
    [SerializeField]
    [Tooltip("Text displayed in the button")]
    private TextMeshProUGUI text;
    [SerializeField]
    [Tooltip("Event invoked when this button is selected")]
    private ResearchCategoryTypeEvent onSelected;

    [Tooltip("This button's type")]
    private ResearchCategoryType type;

    public void Setup(ToggleGroup group, ResearchCategoryType type, UnityAction<ResearchCategoryType> callback, bool isOn)
    {
        // Set the toggle group for this toggle
        myToggle.group = group;
        // Set the type for this button
        this.type = type;
        // Add the callback to the on selected listener
        onSelected.AddListener(callback);

        // Set the text on the button
        text.text = type.ToString();
        // Add function to callback when the toggle is switched to on
        myToggle.onValueChanged.AddListener(OnToggleStateChanged);

        // Set the desired initial toggle state
        myToggle.isOn = isOn;
    }

    // If the toggle is toggling on, invoke on selected event
    private void OnToggleStateChanged(bool state)
    {
        if (state)
        {
            onSelected.Invoke(type);
        }
    }
}
