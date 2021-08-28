using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RichTextTag
{
    // Public accessors
    public string Name => name;
    public string Value => value;
    public string OpeningTag
    {
        get
        {
            string tag = "<" + name;
            if (!string.IsNullOrEmpty(value)) tag += "=" + value;
            return tag + ">";
        }
    }
    public string ClosingTag => "</" + name + ">";
    // Total length of all text in the tag
    public int Length => OpeningTag.Length + ClosingTag.Length;

    // Private editor data
    [SerializeField]
    [Tooltip("Name of the tag")]
    private string name;
    [SerializeField]
    [Tooltip("Value of the tag")]
    private string value;

    public RichTextTag(string name, string value)
    {
        this.name = name;
        this.value = value;
    }
    public RichTextTag(string name) : this(name, "") { }

    // Apply a rich text tag to a substring in this string, 
    // and return the string with the rich text markups
    public string Apply(string baseString, int start, int length)
    {
        baseString = baseString.Insert(start, OpeningTag);
        baseString = baseString.Insert(start + length + OpeningTag.Length, ClosingTag);
        return baseString;
    }
    // Apply the rich text to the full string
    public string Apply(string baseString)
    {
        return Apply(baseString, 0, baseString.Length);
    }
    // Apply multiple tags to a string
    public static string ApplyMultiple(List<RichTextTag> tags, string baseString, int start, int length)
    {
        // Reset local adjuster to 0
        int indexAdjuster = 0;
        // Apply each of the tags used to highlight
        foreach (RichTextTag tag in tags)
        {
            baseString = tag.Apply(baseString, start + indexAdjuster, length);
            indexAdjuster += tag.OpeningTag.Length;
        }
        return baseString;
    }
    // Apply multiple tags to a string 
    public static string ApplyMultiple(List<RichTextTag> tags, string baseString)
    {
        return ApplyMultiple(tags, baseString, 0, baseString.Length);
    }
}
