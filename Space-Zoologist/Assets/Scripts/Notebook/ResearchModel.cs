using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResearchModel
{
    #region Public Typedefs
    [System.Serializable]
    public class ResearchEntryData
    {
        public ResearchEntryList[] entryLists;
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("List of research entry lists - used to make the entries parallel to the item registry")]
    [ParallelItemRegistry("entryLists", "entries")]
    private ResearchEntryData researchEntryData;
    #endregion

    #region Public Methods
    public void Setup()
    {
        // Get a list of all categories
        ItemRegistry.Category[] categories = (ItemRegistry.Category[])System.Enum.GetValues(typeof(ItemRegistry.Category));

        // Loop over all categories
        foreach(ItemRegistry.Category category in categories)
        {
            ResearchEntry[] entries = researchEntryData.entryLists[(int)category].Entries;

            // Loop through all research entries 
            for(int i = 0; i < entries.Length; i++)
            {
                entries[i].Setup();
            }
        }
    }
    public ResearchEntry GetEntry(ItemID id)
    {
        ResearchEntry[] entries = researchEntryData.entryLists[(int)id.Category].Entries;
        if (id.Index >= 0 && id.Index < entries.Length) return entries[id.Index];
        else return null;
    }
    #endregion
}
