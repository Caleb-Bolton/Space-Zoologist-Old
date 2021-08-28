using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotebookDebugging : MonoBehaviour
{
    public RectTransform imageHolder;
    public Sprite sprite;
        
    private void Start()
    {
        // Create the image object
        GameObject imageObject = new GameObject(sprite.name);

        // Add a rect transform 
        RectTransform imageTransform = imageObject.AddComponent<RectTransform>();

        // Add the image component that renders the sprite
        imageObject.AddComponent<CanvasRenderer>();
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;

        // Set the parent, size, and anchors of the image rect transform
        imageTransform.SetParent(imageHolder);
        // So that it renders below the example
        imageTransform.SetAsFirstSibling();
        // Anchor/pivot in the center
        imageTransform.anchorMin = imageTransform.anchorMax = imageTransform.pivot = Vector2.one * 0.5f;
        // Put the position in the center
        imageTransform.anchoredPosition = Vector2.zero;
        // Set the size to (150, 150)
        imageTransform.sizeDelta = Vector2.one * 150f;
    }
}
