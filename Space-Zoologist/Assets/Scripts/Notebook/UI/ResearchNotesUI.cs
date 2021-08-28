using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ResearchNotesUI : NotebookUIChild
{
    // Current notes to edit
    public ResearchNotes CurrentNotes => UIParent.Notebook.Research.GetEntry(categoryPicker.SelectedCategory).Notes;

    [SerializeField]
    [Tooltip("Reference to the picker object that selects the research category")]
    private ResearchCategoryPicker categoryPicker;
    [SerializeField]
    [Tooltip("Reference to the input field used to edit the notes")]
    private TMP_InputField notesInputField;
    [SerializeField]
    [Tooltip("Text that displays the name of the current category taking notes on")]
    private TextMeshProUGUI titleText;

    // Start is called before the first frame update
    public override void Setup()
    {
        base.Setup();

        // Call the method when the input field ends edit
        notesInputField.onEndEdit.AddListener(OnNoteValueChanged);

        // If category picker already has a selected category,
        // then we know it initialized before us, so we need to update our UI
        if (categoryPicker.HasBeenInitialized) OnResearchCategoryChanged(categoryPicker.SelectedCategory);

        // Add listener for the research category change
        categoryPicker.OnResearchCategoryChanged.AddListener(OnResearchCategoryChanged);
    }

    private void OnResearchCategoryChanged(ResearchCategory newCategory)
    {
        // Set the title text to the name of the category
        titleText.text = categoryPicker.SelectedCategory.Name + ": Target Specifications";

        // Set the text on the input field to the text in the notes of the current entry
        notesInputField.SetTextWithoutNotify(CurrentNotes.Notes);
    }

    // Set the note with the given label on the research notes
    private void OnNoteValueChanged(string newNote)
    {
        CurrentNotes.Notes = newNote;
    }
}
