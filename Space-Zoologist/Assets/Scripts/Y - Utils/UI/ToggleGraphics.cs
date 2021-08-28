using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleGraphics : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the toggle with changing graphics")]
    private Toggle toggle;
    [SerializeField]
    [Tooltip("List of graphics to change colors for when the toggle changes")]
    private List<ToggleGraphicData> graphicData;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        OnToggleValueChanged(toggle.isOn);
    }

    public void OnToggleValueChanged(bool isOn)
    {
        foreach(ToggleGraphicData data in graphicData)
        {
            data.SetColor(isOn);
        }
    }
}
