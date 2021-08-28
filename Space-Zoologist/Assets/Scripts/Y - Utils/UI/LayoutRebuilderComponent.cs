using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LayoutRebuilderComponent : UIBehaviour
{
    public void ForceRebuildThisLayoutImmediate() => LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    public void MarkThisLayoutForRebuild() => LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
}
