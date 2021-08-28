using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ObservationEntryEditor : NotebookUIChild
{
    [SerializeField]
    [Tooltip("Input field used to edit the title of the observations")]
    private TMP_InputField titleInput;
    [SerializeField]
    [Tooltip("Input field used to edit the text of the observations")]
    private TMP_InputField textInput;

    // Setup this editor with the entry that it will edit
    public void Setup(ObservationEntry entry, EnclosureID id, ScrollRect scrollTarget)
    {
        base.Setup();

        // Setup the text in the input with the initial values of the entry
        titleInput.text = entry.Title;
        textInput.text = entry.Text;

        // Cache the current enclosure
        EnclosureID current = EnclosureID.FromCurrentSceneName();
        // If the id setting up is the same as the current then add the listeners
        if(id == current)
        {
            titleInput.onEndEdit.AddListener(x => entry.Title = x);
            textInput.onEndEdit.AddListener(x => entry.Text = x);
        }

        // Input only interactable if the id for this editor is the same as the current id
        titleInput.readOnly = id != current;
        textInput.readOnly = id != current;

        // Add scroll interceptor to the title input
        OnScrollEventInterceptor interceptor = titleInput.gameObject.AddComponent<OnScrollEventInterceptor>();
        interceptor.InterceptTarget = scrollTarget;
        // Add scroll interceptor to the text input
        interceptor = textInput.gameObject.AddComponent<OnScrollEventInterceptor>();
        interceptor.InterceptTarget = scrollTarget;
    }
}
