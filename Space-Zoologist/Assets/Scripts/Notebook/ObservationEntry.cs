using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObservationEntry
{
    public string Title
    {
        get => title;
        set => title = value;
    }
    public string Text
    {
        get => text;
        set => text = value;
    }

    [SerializeField]
    [Tooltip("The title applied to this entry")]
    private string title;
    [SerializeField]
    [TextArea(3, 20)]
    [Tooltip("Text in the entry")]
    private string text;
}
