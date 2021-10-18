﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ReviewedResourceRequestDisplay : NotebookUIChild
{
    #region Public Properties
    public UnityEvent OnReviewConfirmed => onReviewConfirmed;
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Game object at the root of the display object")]
    private GameObject displayRoot;
    [SerializeField]
    [Tooltip("Reference to the text to display the status")]
    private TextMeshProUGUI statusText;
    [SerializeField]
    [Tooltip("Text that displays the reason for the current status")]
    private TextMeshProUGUI statusReasonText;
    [SerializeField]
    [Tooltip("Text that displays number of items granted")]
    private TextMeshProUGUI itemText;
    [SerializeField]
    [Tooltip("Test that displays the total cost of the request")]
    private TextMeshProUGUI costText;
    [SerializeField]
    [Tooltip("Button to accept the request")]
    private Button confirmButton;
    [SerializeField]
    [Tooltip("Button the cancel the request")]
    private Button cancelButton;

    [Space]

    [SerializeField]
    [Tooltip("Event invoked when the review is confirmed by the player")]
    private UnityEvent onReviewConfirmed;
    #endregion

    #region Private Fields
    // Review to display to the player
    private ReviewedResourceRequest review;
    #endregion

    #region Public Methods
    public override void Setup()
    {
        base.Setup();

        // Confirm button confirms the review given
        confirmButton.onClick.AddListener(() =>
        {
            displayRoot.SetActive(false);
            UIParent.Notebook.Concepts.ConfirmReviwedResourceRequest(LevelID.FromCurrentSceneName(), review);
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
        review = ReviewedResourceRequest.Review(request);

        // Set the status and reason text
        statusText.text = review.CurrentStatus.ToString();
        statusReasonText.text = review.StatusReason;

        // Change the text displayed based on whether the request was granted or denied
        if(review.CurrentStatus != ReviewedResourceRequest.Status.Denied)
        {
            itemText.text = review.QuantityGranted + " " + request.ItemRequested.Data.Name.Get(ItemName.Type.Colloquial);
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