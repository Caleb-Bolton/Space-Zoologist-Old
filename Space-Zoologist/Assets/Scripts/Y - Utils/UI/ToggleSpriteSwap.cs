using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToggleSpriteSwap : UIBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the toggle")]
    private Toggle toggle;
    [SerializeField]
    [Tooltip("Graphic to change sprites on")]
    private Image image;
    [SerializeField]
    [Tooltip("Sprite while the toggle is on")]
    private Sprite onSprite;
    [SerializeField]
    [Tooltip("Color of the image while the toggle is on")]
    private Color onColor = Color.white;
    [SerializeField]
    [Tooltip("Sprite while the toggle is off")]
    private Sprite offSprite;
    [SerializeField]
    [Tooltip("Color of the image while the toggle is off")]
    private Color offColor = Color.white;

    protected override void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleStateChanged);
        OnToggleStateChanged(toggle.isOn);
    }

    private void OnToggleStateChanged(bool isOn)
    {
        if (isOn)
        {
            image.sprite = onSprite;
            image.color = onColor;
        }
        else
        {
            image.sprite = offSprite;
            image.color = offColor;
        }
    }
}
