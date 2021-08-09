﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using TMPro;

public class TypeFilteredResearchCategoryDropdown : ResearchCategoryDropdown
{
    public List<ResearchCategoryType> TypeFilter => typeFilter;

    [SerializeField]
    [Tooltip("Research category type that this dropdown represents")]
    private List<ResearchCategoryType> typeFilter;

    public void SetTypeFilter(params ResearchCategoryType[] types)
    {
        typeFilter.Clear();
        foreach(ResearchCategoryType type in types)
        {
            typeFilter.Add(type);
        }
        Awake();
    }

    protected override ResearchCategory[] GetResearchCategories()
    {
        return UIParent.Notebook.Research.ResearchDictionary
            .Where(kvp => typeFilter.Contains(kvp.Key.Type))
            .Select(kvp => kvp.Key)
            .ToArray();
    }
}
