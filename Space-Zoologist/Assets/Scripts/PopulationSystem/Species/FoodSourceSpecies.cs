using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO have this create the food source item and hold it, then have the store display that info
[CreateAssetMenu]
public class FoodSourceSpecies : ScriptableObject
{
    public string SpeciesName => speciesName;
    public int RootRadius => rootRadius;
    public int BaseOutput => baseOutput;
    public Item FoodSourceItem => FoodSource;
    public List<TileType> AccessibleTerrain => accessibleTerrain;
    public int Size => size;
    public int MoveCost => moveCost;
    public int SellBackPrice => sellBackPrice;

    [SerializeField] private int size = 1; // default to 1 tile big
    [SerializeField] private List<TileType> accessibleTerrain = default;
    [SerializeField] private string speciesName = default;
    [SerializeField] private int rootRadius = default;
    [SerializeField] private int baseOutput = default;
    [SerializeField] private int moveCost = default;
    [SerializeField] private int sellBackPrice = default;
    [SerializeField] private List<TerrainNeedConstructData> terrainNeeds = default;
    [SerializeField] private List<LiquidNeedConstructData> liquidNeeds = default;
    [SerializeField] private Item FoodSource = default;


    public Dictionary<string, Need> SetupNeeds()
    {
        Dictionary<string, Need> needs = new Dictionary<string, Need>();

        //Terrain Needs
        foreach (TerrainNeedConstructData need in terrainNeeds)
        {
            needs.Add(need.NeedName, new TerrainNeed(need, this));
        }

        //Water Needs
        foreach (LiquidNeedConstructData need in liquidNeeds)
        {
            if(need.TileNeedThreshold <= 0)
                continue;

            needs.Add("LiquidTiles", new LiquidNeed("LiquidTiles", need));

            if(need.FreshWaterMinThreshold != 0)
                needs.Add("Water", new LiquidNeed("Water", need));

            if(need.SaltMinThreshold != 0)
                needs.Add("Salt", new LiquidNeed("Salt", need));

            if(need.BacteriaMinThreshold != 0)
                needs.Add("Bacteria", new LiquidNeed("Bacteria", need));

            //Food sources do not have liquid poisons so no need to worry about those here
        }


        return needs;
    }

    public void SetupData(string name, int rootRadius, int output, List<List<NeedConstructData>> needs)
    {
        this.speciesName = name;
        this.rootRadius = rootRadius;
        this.baseOutput = output;

        
        for(int i = 0; i < needs.Count; ++i)
        {
            switch(i)
            {
                case 0:
                    terrainNeeds = new List<TerrainNeedConstructData>();
                    foreach(NeedConstructData data in needs[i])
                    {
                        if(!(data is TerrainNeedConstructData))
                        {
                            Debug.LogError("Invalid needs data: NeedConstructData was not a TerrainNeedConstructData");
                            return;
                        }

                        terrainNeeds.Add((TerrainNeedConstructData)data);
                    }
                    break;
                case 1:
                    liquidNeeds = new List<LiquidNeedConstructData>();
                    foreach(NeedConstructData data in needs[i])
                    {
                        if(!(data is LiquidNeedConstructData))
                        {
                            Debug.LogError("Invalid needs data: NeedConstructData was not a LiquidNeedConstructData");
                            return;
                        }

                        liquidNeeds.Add((LiquidNeedConstructData)data);
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
