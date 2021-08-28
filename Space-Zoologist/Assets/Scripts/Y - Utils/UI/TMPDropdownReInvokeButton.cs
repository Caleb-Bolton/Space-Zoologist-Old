using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class TMPDropdownReInvokeButton : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the dropdown to re-invoke the event for")]
    private TMP_Dropdown dropdown;
    [SerializeField]
    [Tooltip("Root object for this item")]
    private GameObject itemRoot;
    [SerializeField]
    [Tooltip("Toggle attached to this item")]
    private Toggle toggle;
    [SerializeField]
    [Tooltip("Button that forces dropdown to re-invoke OnValueChanged regardless of dropdown item clicked")]
    private Button button;

    private int index = 0;

    private void Start()
    {
        // Use the name of the item root to parse out the index of this item
        string indexString = itemRoot.name;
        string prefix = "Index ";
        int endSubstringIndex = indexString.IndexOf(':');
        indexString = indexString.Substring(prefix.Length - 1, endSubstringIndex - prefix.Length + 1);
        index = int.Parse(indexString);

        // Listen for button click, then re-invoke event
        button.onClick.AddListener(() =>
        {
            toggle.isOn = true;
            toggle.onValueChanged.Invoke(true);
            dropdown.onValueChanged.Invoke(index);
        });
    }
}
