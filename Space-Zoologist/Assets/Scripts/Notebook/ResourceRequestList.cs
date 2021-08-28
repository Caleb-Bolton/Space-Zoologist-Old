using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourceRequestList
{
    public List<ResourceRequest> Requests => requests;

    [SerializeField]
    [Tooltip("List of requests")]
    private List<ResourceRequest> requests = new List<ResourceRequest>();
}
