﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CategoryFilteredItemDropdown : ItemDropdown
{
    #region Public Properties
    public List<ItemRegistry.Category> CategoryFilter => categoryFilter;
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Research category type that this dropdown represents")]
    protected List<ItemRegistry.Category> categoryFilter;
    #endregion

    #region Public Methods
    public void Setup(params ItemRegistry.Category[] categoryFilter)
    {
        this.categoryFilter = new List<ItemRegistry.Category>(categoryFilter);
        
        // Now that type filter is set we will setup the base class
        base.Setup();
    }
    #endregion

    #region Private/Protected Methods
    protected override ItemID[] GetItemIDs(ItemID[] source)
    {
        return base.GetItemIDs(source)
            .Where(id => categoryFilter.Contains(id.Category) && UIParent.Data.ItemIsUnlocked(id))
            .ToArray();
    }
    #endregion
}