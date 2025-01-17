﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ReviewedResourceRequestDisplay : NotebookUIChild
{
    #region Public Typedefs
    [System.Serializable]
    public class ColorArray
    {
        public Color[] colors = new Color[3] 
        {
            Color.green, Color.yellow, Color.red
        };
    }
    #endregion

    #region Public Properties
    public ReviewedResourceRequest Review => review;
    public ReviewedResourceRequest LastReviewConfirmed => lastReviewConfirmed;
    public UnityEvent OnReviewConfirmed => onReviewConfirmed;
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Game object at the root of the display object")]
    private GameObject displayRoot = null;
    [SerializeField]
    [Tooltip("Reference to the image that displays the border")]
    private Image border = null;
    [SerializeField]
    [Tooltip("List of colors displayed depending on the status of the review")]
    [EditArrayWrapperOnEnum("colors", typeof(ReviewedResourceRequest.Status))]
    private ColorArray statusColors = new ColorArray();
    [SerializeField]
    [Tooltip("Reference to the text to display the status")]
    private TextMeshProUGUI statusText = null;
    [SerializeField]
    [Tooltip("Text that displays the reason for the current status")]
    private TextMeshProUGUI statusReasonText = null;
    [SerializeField]
    [Tooltip("Text that displays number of items granted")]
    private TextMeshProUGUI itemText = null;
    [SerializeField]
    [Tooltip("Test that displays the total cost of the request")]
    private TextMeshProUGUI costText = null;
    [SerializeField]
    [Tooltip("Button to accept the request")]
    private Button confirmButton = null;
    [SerializeField]
    [Tooltip("Button the cancel the request")]
    private Button cancelButton = null;

    [Space]

    [SerializeField]
    [Tooltip("Event invoked when the review is confirmed by the player")]
    private UnityEvent onReviewConfirmed = null;
    #endregion

    #region Private Fields
    // Review to display to the player
    private ReviewedResourceRequest review;
    // Last review confirmed by the display
    private ReviewedResourceRequest lastReviewConfirmed;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        // Confirm button confirms the review given
        confirmButton.onClick.AddListener(() =>
        {
            displayRoot.SetActive(false);
            lastReviewConfirmed = review;
            UIParent.Data.Concepts.ConfirmReviwedResourceRequest(LevelID.Current(), review);
            onReviewConfirmed.Invoke();
        });
        // Cancel button disables the display
        cancelButton.onClick.AddListener(() =>
        {
            displayRoot.SetActive(false);
        });

        // At the setup, make sure the display is inactive
        displayRoot.SetActive(false);
    }
    /// <summary>
    /// Ask the display to review a resource request and display the result
    /// </summary>
    /// <param name="request"></param>
    public void DisplayReview(ResourceRequest request)
    {
        // Create the review
        review = ReviewedResourceRequest.Review(new ResourceRequest(request));

        Color statusColor = Color.white;

        // Set the border color based on the review status
        if(review.CurrentStatus == ReviewedResourceRequest.Status.Invalid)
            statusColor = statusColors.colors[(int)ReviewedResourceRequest.Status.Denied];
        else
            statusColor = statusColors.colors[(int)review.CurrentStatus];

        border.color = statusColor;
        statusText.color = statusColor;
        // Set the status and reason text


        statusText.text = review.StatusLabel;
        statusReasonText.text = review.StatusReason;

        // Change the text displayed based on whether the request was granted or denied
        if((int)review.CurrentStatus < 2)
        {
            itemText.text = review.QuantityGranted + " " + request.ItemRequested.Data.Name.GetCombinedName();
            costText.text = "$" + review.TotalCost;
        }
        else
        {
            itemText.text = "<no items granted>";
            costText.text = "<no cost>";
        }

        // Can only cancel a request that was not denied
        cancelButton.gameObject.SetActive(review.CurrentStatus != ReviewedResourceRequest.Status.Denied);

        // Enable the object for the display
        displayRoot.SetActive(true);
    }
    #endregion
}
