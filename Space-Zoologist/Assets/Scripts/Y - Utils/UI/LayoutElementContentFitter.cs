using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LayoutElementContentFitter : UIBehaviour, ILayoutElement
{
    [System.Serializable]
    public struct SpanningContent 
    {
        public List<RectTransform> span;
        public float extraSpace;
    }

    [SerializeField]
    [Tooltip("Priority of this layout element")]
    private int m_layoutPriority;
    [SerializeField]
    [Tooltip("Objects that span this object horizontally")]
    private SpanningContent horizontalSpan;
    [SerializeField]
    [Tooltip("Objects that span this object vertically")]
    private SpanningContent verticalSpan;

    private float m_minWidth;
    private float m_minHeight;
    private float m_preferredWidth;
    private float m_preferredHeight;
    private float m_flexibleWidth;
    private float m_flexibleHeight;

    public int layoutPriority => m_layoutPriority;
    public float minWidth => m_minWidth;
    public float minHeight => m_minHeight;
    public float preferredWidth => m_preferredWidth;
    public float preferredHeight => m_preferredHeight;
    public float flexibleWidth => m_flexibleWidth;
    public float flexibleHeight => m_flexibleHeight;

    public void CalculateLayoutInputHorizontal()
    {
        m_minWidth = horizontalSpan.extraSpace;
        m_preferredWidth = horizontalSpan.extraSpace;
        m_flexibleWidth = horizontalSpan.extraSpace;

        foreach (RectTransform rect in horizontalSpan.span)
        {
            m_minWidth += LayoutUtility.GetMinWidth(rect);
            m_preferredWidth += LayoutUtility.GetPreferredWidth(rect);
            m_flexibleWidth += LayoutUtility.GetFlexibleWidth(rect);
        }
    }

    public void CalculateLayoutInputVertical()
    {
        m_minHeight = verticalSpan.extraSpace;
        m_preferredHeight = verticalSpan.extraSpace;
        m_flexibleHeight = verticalSpan.extraSpace;

        foreach (RectTransform rect in verticalSpan.span)
        {
            m_minHeight += LayoutUtility.GetMinHeight(rect);
            m_preferredHeight += LayoutUtility.GetPreferredHeight(rect);
            m_flexibleHeight += LayoutUtility.GetFlexibleHeight(rect);
        }
    }
}
