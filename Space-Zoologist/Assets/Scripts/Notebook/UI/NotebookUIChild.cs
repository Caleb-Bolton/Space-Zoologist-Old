using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The base class for all UI scripts that are children of the notebook UI
public class NotebookUIChild : MonoBehaviour
{
    public NotebookUI UIParent { get; private set; }
    public bool IsSetUp => UIParent;

    public virtual void Setup()
    {
        UIParent = GetComponentInParent<NotebookUI>();

        // Log a warning if no UI parent is found
        if (UIParent == null)
        {
            Debug.LogWarning(GetType() + ": expecting a component of type 'NotebookUI' attached to one of its parents, " +
                "but no such component was found.  Did you place the object in the heirarchy correctly?", gameObject);
        }
    }
}
