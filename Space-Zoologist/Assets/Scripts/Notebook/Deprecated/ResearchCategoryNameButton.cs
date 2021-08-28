using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using TMPro;

public class ResearchCategoryNameButton : MonoBehaviour
{
    // Typedef so it will appear in the editor
    [System.Serializable]
    public class StringSpriteEvent : UnityEvent<string, Sprite> { }

    // Public accessors
    public Toggle MyToggle => myToggle;
    public string ResearchCategoryName => researchCategoryName;

    [SerializeField]
    [Tooltip("The toggle that manages this button")]
    private Toggle myToggle;
    [SerializeField]
    [Tooltip("Text displayed in the button")]
    private TextMeshProUGUI text;
    [SerializeField]
    [Tooltip("Reference to the image that will render the item")]
    private Image image;
    [SerializeField]
    [Tooltip("Event invoked when this button is selected")]
    private StringSpriteEvent onSelected;

    [Tooltip("This button's name")]
    private string researchCategoryName;

    public void Setup(ToggleGroup group, string researchCategoryName, Sprite sprite, UnityAction<string, Sprite> callback)
    {
        // Set the toggle group for this toggle
        myToggle.group = group;
        // Set the name for this button
        this.researchCategoryName = researchCategoryName;
        // Add the callback to the on selected listener
        onSelected.AddListener(callback);

        // Set button image and text
        image.sprite = sprite;
        text.text = researchCategoryName;

        // If sprite is null, display text, otherwise display image
        image.enabled = sprite != null;
        text.enabled = sprite == null;

        // Add function to callback when the toggle is switched to on
        myToggle.onValueChanged.AddListener(OnToggleStateChanged);
    }

    // If the toggle is toggling on, invoke on selected event
    private void OnToggleStateChanged(bool state)
    {
        if (state) onSelected.Invoke(researchCategoryName, image.sprite);
    }
}
