﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemRegistry : ScriptableObjectSingleton<ItemRegistry>
{
    #region Public Typedefs
    public enum Category { Species, Food, Tile }
    // So that the attributes work correctly in the editor
    [System.Serializable]
    public class ItemRegistryData
    {
        [Tooltip("List of item data lists - parallel to the 'Category' enum")]
        [WrappedProperty("items")]
        public ItemDataList[] itemDataLists;
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("List of item data lists - parallel to the 'Category' enum")]
    [EditArrayWrapperOnEnum("itemDataLists", typeof(Category))]
    private ItemRegistryData itemData;
    #endregion

    #region Public Methods
    public static bool ValidID(ItemID id)
    {
        ItemData[] datas = GetItemsWithCategory(id.Category);
        return id.Index >= 0 && id.Index < datas.Length;
    }
    public static ItemData Get(ItemID id)
    {
        ItemData[] datas = GetItemsWithCategory(id.Category);
        if (ValidID(id)) return datas[id.Index];
        else throw new System.IndexOutOfRangeException($"{nameof(ItemRegistry)}: " +
            $"No item exists at index {id.Index} for category {id.Category}. " +
            $"Total items in category: {datas.Length}");
    }
    public static ItemData[] GetItemsWithCategory(Category category) => Instance.itemData.itemDataLists[(int)category].Items;
    public static ItemData[] GetItemsWithCategoryName(string categoryName)
    {
        if (System.Enum.TryParse(categoryName, true, out Category category))
        {
            return GetItemsWithCategory(category);
        }
        else throw new System.ArgumentException($"{nameof(ItemRegistry)}: " +
            $"attempted to get items with category '{categoryName}', " +
            $"but no such category exists");
    }
    public static int CountItemsWithCategory(Category category) => GetItemsWithCategory(category).Length;
    public static int CountAllItems()
    {
        int count = 0;
        Category[] categories = (Category[])System.Enum.GetValues(typeof(Category));

        // Add the lengths of each array to the total count
        foreach(Category category in categories)
        {
            count += CountItemsWithCategory(category);
        }

        return count;
    }
    public static ItemID[] GetAllItemIDs()
    {
        // Create an array as big as all the items in the registry
        ItemID[] ids = new ItemID[CountAllItems()];
        int index = 0;

        // Get a list of categories
        Category[] categories = (Category[])System.Enum.GetValues(typeof(Category));

        // Loop through all categories
        foreach(Category category in categories)
        {
            // Add an id with each index for each item in this category
            for(int i = 0; i < CountItemsWithCategory(category); i++, index++)
            {
                ids[index] = new ItemID(category, i);
            }
        }

        return ids;
    }
    #endregion
}
