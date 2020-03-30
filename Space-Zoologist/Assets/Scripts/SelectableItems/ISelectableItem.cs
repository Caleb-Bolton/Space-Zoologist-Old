﻿using UnityEngine;
using System.Collections.Generic;

public interface ISelectableItem
{
    /// <summary>
    /// Initialize SelectableItem by passing in a ScriptableObject and the event with the delegated methods
    /// </summary>
    /// <param name="item"></param>
    void InitializeItem(ScriptableObject item);
    /// <summary>
    /// Callback exectuted when an item is selected
    /// </summary>
    /// <param name="itemSelected"></param>
    void OnItemSelected(GameObject itemSelected);
}
