﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using TMPro;

public class ResearchEncyclopediaArticleInputField : NotebookUIChild, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    // So that the event appears in the editor
    [System.Serializable]
    public class IntIntEvent : UnityEvent<int, int> { }

    // Public accessors of private data
    public IntIntEvent OnHighlightConfirm => onHighlightConfirm;

    // Private editor data
    [SerializeField]
    [Tooltip("Text field used to display the encyclopedia article")]
    private TMP_InputField textField;
    [SerializeField]
    [Tooltip("Layout group used to layout the article text and image")]
    private LayoutGroup articleLayout;
    [SerializeField]
    [Tooltip("Image component used to render the image for the encyclopedia article")]
    private Image image;
    [SerializeField]
    [Tooltip("Empty sprite to display if the article doesn't have one for us")]
    private Sprite noneSprite;
    [SerializeField]
    [Tooltip("Toggle used to determine if highlights are being added or removed")]
    private Toggle highlightToggle;
    [SerializeField]
    [Tooltip("Texture of the cursor while highlighting")]
    private CursorTexture highlightAddTexture;
    [SerializeField]
    [Tooltip("Texture of the cursor while removing highlights")]
    private CursorTexture highlightRemoveTexture;
    [SerializeField]
    [Tooltip("List of tags used to render highlighted encyclopedia article text")]
    private List<RichTextTag> highlightTags;
    [SerializeField]
    [Tooltip("Event invoked when the user finishes dragging")]
    private IntIntEvent onHighlightConfirm;

    // Reference to the encyclopedia article that is currently being rendered,
    // if "null" no article is rendered
    private ResearchEncyclopediaArticle currentArticle;

    public void OnEndDrag(PointerEventData data)
    {
        if(currentArticle != null)
        {
            // Use selection position on the input field to determine position of highlights
            int start = textField.selectionAnchorPosition;
            int end = textField.selectionFocusPosition;

            // If selection has no length, exit the function
            if (start == end) return;

            // If start is bigger than end, swap them
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            // Add/remove highlight depending on the state of the toggle
            if (highlightToggle.isOn) currentArticle.RequestHighlightAdd(start, end);
            else currentArticle.RequestHighlightRemove(start, end);

            // Udpate the text for this article
            UpdateArticleDisplay();

            // Deactivate the input field
            textField.DeactivateInputField(true);
        }
    }
    // On pointer enter, set the correct cursor
    public void OnPointerEnter(PointerEventData data)
    {
        if (highlightToggle.isOn) highlightAddTexture.SetCursor();
        else highlightRemoveTexture.SetCursor();
    }
    // On pointer exit, restore the default cursor
    public void OnPointerExit(PointerEventData data)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    public void UpdateArticle(ResearchEncyclopediaArticle article)
    {
        currentArticle = article;
        UpdateArticleDisplay();
    }

    public void UpdateArticleDisplay()
    {
        // If an article was given, set the text with the highlights
        if (currentArticle != null) 
        { 
            textField.text = RichEncyclopediaArticleText(currentArticle, highlightTags);

            // Set the correct sprite
            if (currentArticle.Image) image.sprite = currentArticle.Image;
            else image.sprite = noneSprite;
        }
        // No article given implies this encyclopedia has no entries
        else textField.text = "<color=#aaa>This encyclopedia has no entries</color>";

        // Update the layout component since the text amount just changed
        articleLayout.SetLayoutHorizontal();
        articleLayout.SetLayoutVertical();
    }

    public static string RichEncyclopediaArticleText(ResearchEncyclopediaArticle article, List<RichTextTag> tags)
    {
        string richText = article.Text;
        int indexAdjuster = 0;    // Adjust the index for each highlight
        int indexIncrementer = 0; // Length of all the tags used in each highlight

        // Compute the index incrementer by incrementing tag lengths
        foreach (RichTextTag tag in tags)
        {
            indexIncrementer += tag.Length;
        }
        // Go through all highlights
        foreach (ResearchEncyclopediaArticleHighlight highlight in article.Highlights)
        {
            richText = RichTextTag.ApplyMultiple(tags, richText, highlight.Start + indexAdjuster, highlight.Length);
            // Increase the global index adjuster
            indexAdjuster += indexIncrementer;
        }

        return richText;
    }
}
