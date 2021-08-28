using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// A generic dropdown button
/// The button is designed to pull up some object when clicked
/// If the user clicks anywhere else, the dropdown goes away
/// </summary>
public class GeneralDropdown : UIBehaviour
{
    #region Public Accessors
    public bool Interactable
    {
        get => button.interactable;
        set => button.interactable = value;
    }
    public UnityEvent OnDropdownEnabled => onDropdownEnabled;
    public UnityEvent OnDropdownDisabled => onDropdownDisabled;
    #endregion

    #region Private Editor Data

    [SerializeField]
    [Tooltip("Reference to the button that enables the dropdown")]
    private Button button;
    [SerializeField]
    [Tooltip("Panel that is activated-deactivated for the dropdown")]
    private GameObject dropdownPanel;
    [SerializeField]
    [Tooltip("List of buttons on the dropdown panel that make it deactivate")]
    private List<Button> dropdownDisableButtons;
    [SerializeField]
    [Tooltip("Event invoked when the dropdown is activated")]
    private UnityEvent onDropdownEnabled;
    [SerializeField]
    [Tooltip("Event invoked when the dropdown is deactivated")]
    private UnityEvent onDropdownDisabled;

    #endregion

    #region Private Data

    // Reference to the object to detect when the user clicks away from the dropdown
    private GameObject blocker;

    #endregion

    #region Methods

    protected override void Start()
    {
        base.Start();

        // Make the dropdown invisible invisible
        dropdownPanel.SetActive(false);

        foreach(Button disableButton in dropdownDisableButtons)
        {
            disableButton.onClick.AddListener(DisableDropdown);
        }

        // When the button is clicked create the dropdown
        button.onClick.AddListener(EnableDropdown);
    }

    public void EnableDropdown()
    {
        // Enable the dropdown panel
        dropdownPanel.SetActive(true);

        // Add a child canvas to the panel
        Canvas dropdownCanvas = dropdownPanel.gameObject.GetOrAddComponent<Canvas>();
        dropdownCanvas.overrideSorting = true;
        dropdownCanvas.sortingLayerName = "Default";
        dropdownCanvas.sortingOrder = 30000;
        dropdownCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

        // Add a graphic raycaster to the panel
        GraphicRaycaster dropdownRaycaster = dropdownPanel.gameObject.GetOrAddComponent<GraphicRaycaster>();
        dropdownRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        // Add a canvas group to the panel
        CanvasGroup dropdownGroup = dropdownPanel.gameObject.GetOrAddComponent<CanvasGroup>();
        dropdownGroup.alpha = 1;
        dropdownGroup.interactable = true;
        dropdownGroup.blocksRaycasts = true;
        dropdownGroup.ignoreParentGroups = false;

        // Get the canvas in the parents of this object
        Canvas parent = GetComponentInParent<Canvas>();

        if(parent && !blocker)
        {
            blocker = new GameObject("Blocker");
            RectTransform blockerTransform = blocker.AddComponent<RectTransform>();

            // Set the blocker as the last sibling of the parent canvas
            blockerTransform.SetParent(parent.transform);
            blockerTransform.SetAsLastSibling();
            
            // Anchor the blocker to stretch over the full canvas
            blockerTransform.anchorMin = Vector2.zero;
            blockerTransform.anchorMax = Vector2.one;
            blockerTransform.offsetMin = blockerTransform.offsetMax = Vector2.zero;

            // Setup the canvas of the blocking object
            Canvas blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerName = "Default";
            blockerCanvas.sortingOrder = 29999;
            blockerCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            // Add a raycaster to the blocker
            GraphicRaycaster blockerRaycaster = blocker.AddComponent<GraphicRaycaster>();
            blockerRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            // Add a canvas renderer and an image so that this object can process events from the event system
            blocker.AddComponent<CanvasRenderer>();
            Image blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;

            Button blockerButton = blocker.AddComponent<Button>();
            blockerButton.onClick.AddListener(DisableDropdown);
        }

        onDropdownEnabled.Invoke();
    }

    public void DisableDropdown()
    {
        dropdownPanel.SetActive(false);
        if (blocker != null) Destroy(blocker);
        onDropdownDisabled.Invoke();
    }

    #endregion
}
