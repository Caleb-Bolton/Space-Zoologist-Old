using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

public class ResearchSingleNoteUI : MonoBehaviour
{
    // So that it appears in the editor
    [System.Serializable] public class StringStringEvent : UnityEvent<string, string> { }

    [SerializeField]
    [Tooltip("Input field used to write the note")]
    private TMP_InputField myInputField;
    [SerializeField]
    [Tooltip("Event invoked when the note value is changed")]
    private StringStringEvent onNoteChanged;

    // The label associated with this note
    private string label;

    public void Setup(string label, string initialNote, UnityAction<string, string> callback)
    {
        this.label = label;

        // Set the placeholder text to the label
        TextMeshProUGUI placeholderText = (TextMeshProUGUI)myInputField.placeholder;
        placeholderText.text = label;

        // Setup the initial note on the input field
        myInputField.text = initialNote;

        // Add the callback to the note changed event
        onNoteChanged.AddListener(callback);
        // Invoke the note changed event when the input field finishes editing
        myInputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    private void OnInputEndEdit(string newText)
    {
        onNoteChanged.Invoke(label, newText);
    }
}
