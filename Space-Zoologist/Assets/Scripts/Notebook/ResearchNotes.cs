using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResearchNotes
{
    // Public accessors

    public ResearchNoteLabels Labels => labels;
    public string Notes { get; set; } = string.Empty;

    // Private editor data

    [SerializeField]
    [Expandable]
    [Tooltip("Lables attached to the notes in the research notes")]
    private ResearchNoteLabels labels;

    public void Setup()
    {
        // Later, we need to load the existing data
        Notes = string.Empty;

        if(Notes == string.Empty)
        {
            // Tags used to decorate the labels in the notes
            List<RichTextTag> labelTags = new List<RichTextTag>()
            {
                new RichTextTag("color", "white"),
                new RichTextTag("font", "\"times new roman SDF\"")
            };
            // Add the labels to the notes if they are not empty
            foreach (string label in labels.Labels)
            {
                string richLabel = RichTextTag.ApplyMultiple(labelTags, label + ":");
                Notes += richLabel + " \n\n";
            }
        }
    }
}
