using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Notebook/ResearchModel")]
public class ResearchModel : ScriptableObject
{
    // Public accessor for the dictionary
    // Uses lazy loading to load the dictionary the first time it is requested
    public Dictionary<ResearchCategory, ResearchEntry> ResearchDictionary
    {
        get; private set;
    } = new Dictionary<ResearchCategory, ResearchEntry>();

    // Edit each research entry category seperately
    [SerializeField]
    [Tooltip("List of research data for all species")]
    private List<ResearchEntry> speciesResearch;
    [SerializeField]
    [Tooltip("List of research data for all foods")]
    private List<ResearchEntry> foodResearch;
    [SerializeField]
    [Tooltip("List of research data for all tiles")]
    private List<ResearchEntry> tileResearch;

    public void Setup()
    {
        InitResearchEntries(speciesResearch, ResearchCategoryType.Species);
        InitResearchEntries(foodResearch, ResearchCategoryType.Food);
        InitResearchEntries(tileResearch, ResearchCategoryType.Tile);
    }

    private void InitResearchEntries(List<ResearchEntry> entries, ResearchCategoryType type)
    {
        foreach (ResearchEntry entry in entries)
        {
            entry.Setup(type);
            if (!ResearchDictionary.ContainsKey(entry.Category)) ResearchDictionary.Add(entry.Category, entry);
        }
    }

    public ResearchEntry GetEntry(ResearchCategory category) => ResearchDictionary[category];
}
