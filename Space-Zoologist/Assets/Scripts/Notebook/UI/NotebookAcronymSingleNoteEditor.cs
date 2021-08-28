using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using TMPro;

public class NotebookAcronymSingleNoteEditor : NotebookUIChild
{
    // So that the event appears in the editor
    [System.Serializable]
    public class CharStringEvent : UnityEvent<char, string> { }

    [SerializeField]
    [Tooltip("Reference to the text that will display the character in the acronym that this note edits")]
    private TextMeshProUGUI label;
    [SerializeField]
    [Tooltip("Reference to the input field used to write the note")]
    private TMP_InputField inputField;
    [SerializeField]
    [Tooltip("Event invoked when the note is finished editing")]
    private CharStringEvent onNoteEdited;

    // Character in the acronym that this character edits
    private char acronymChar;

    public void Setup(char acronymChar)
    {
        // Call the base awake method
        base.Setup();

        // Set the acronym character
        this.acronymChar = acronymChar;
        label.text = acronymChar.ToString();

        // Read the note for this acronym into the 
        inputField.text = UIParent.Notebook.ReadAcronymNote(acronymChar);
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    private void OnInputFieldEndEdit(string text)
    {
        UIParent.Notebook.WriteAcronymNote(acronymChar, text);
        onNoteEdited.Invoke(acronymChar, text);
    }
}
