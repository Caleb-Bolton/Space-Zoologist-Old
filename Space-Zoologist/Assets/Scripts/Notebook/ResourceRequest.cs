using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player's request for additional resources
/// they can use to sustain the enclosure
/// </summary>
[System.Serializable]
public class ResourceRequest
{
    public enum Status
    {
        NotReviewed, Granted, PartiallyGranted, Denied
    }

    public int Priority
    {
        get => priority;
        set => priority = value;
    }
    public ResearchCategory Target
    {
        get => target;
        set => target = value;
    }
    public NeedType ImprovedNeed
    {
        get => improvedNeed;
        set => improvedNeed = value;
    }
    public int Quantity
    {
        get => quantity;
        set => quantity = value;
    }
    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    [SerializeField]
    [Tooltip("Priority of the request relative to other requests being made")]
    private int priority;
    [SerializeField]
    [Tooltip("Target of the resource request")]
    private ResearchCategory target;
    [SerializeField]
    [Tooltip("Need of the target that this resource request is supposed to improve")]
    private NeedType improvedNeed;
    [SerializeField]
    [Tooltip("Quantity of the resource requested")]
    private int quantity;
    [SerializeField]
    [Tooltip("Name of the item requested")]
    private string itemName;
    [SerializeField]
    [Tooltip("Current status of the resource request")]
    private Status status;
    [SerializeField]
    [Tooltip("Written reason why the request was given the status it currently has")]
    private string statusReason;
}
