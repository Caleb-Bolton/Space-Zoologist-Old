using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct LayoutData
{
    public int layoutPriority;
    // Width fields
    public float minWidth;
    public float preferredWidth;
    public float flexibleWidth;
    // Height fields
    public float minHeight;
    public float preferredHeight;
    public float flexibleHeight;

    public LayoutData(int layoutPriority, float minWidth, float preferredWidth, float flexibleWidth, float minHeight, float preferredHeight, float flexibleHeight)
    {
        this.layoutPriority = layoutPriority;
        this.minWidth = minWidth;
        this.preferredWidth = preferredWidth;
        this.flexibleWidth = flexibleWidth;
        this.minHeight = minHeight;
        this.preferredHeight = preferredHeight;
        this.flexibleHeight = flexibleHeight;
    }
}
