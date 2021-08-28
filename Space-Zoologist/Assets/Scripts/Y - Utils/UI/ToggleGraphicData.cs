using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ToggleGraphicData
{
    [SerializeField]
    [Tooltip("Reference to the graphic to change color for")]
    private Graphic graphic;
    [SerializeField]
    [Tooltip("Color of the graphic while the toggle is on")]
    private Color isOnColor = Color.white;
    [SerializeField]
    [Tooltip("Color of the graphic while the toggle is off")]
    private Color isOffColor = Color.white;

    public void SetColor(bool isOn)
    {
        if (isOn) graphic.color = isOnColor;
        else graphic.color = isOffColor;
    }
}
