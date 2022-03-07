using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO have this create the food source item and hold it, then have the store display that info
[CreateAssetMenu]
public class FoodSourceSpecies : ScriptableObject
{
    public ItemID ID => ItemRegistry.FindSpecies(this);
    public int RootRadius => rootRadius;
    public int BaseOutput => baseOutput;
    public Item FoodSourceItem => ID.Data.ShopItem;
    public List<TileType> AccessibleTerrain => accessibleTerrain;
    public int WaterTilesRequired => waterTilesRequired;
    public Vector2Int Size => size;
    public List<TerrainNeedConstructData> TerrainNeeds => terrainNeeds;
    public List<LiquidNeedConstructData> LiquidNeeds => liquidNeeds;
    public NeedRegistry Needs => needs;
    public int TerrainTilesNeeded => size.x * size.y;

    [SerializeField] private Vector2Int size = new Vector2Int(1, 1); // default to 1 tile big
    [SerializeField] private List<TileType> accessibleTerrain = default;
    [SerializeField] private int waterTilesRequired = default;
    [SerializeField] private int rootRadius = default;
    [SerializeField] private int baseOutput = default;
    [SerializeField] private List<TerrainNeedConstructData> terrainNeeds = default;
    [SerializeField] private List<LiquidNeedConstructData> liquidNeeds = default;
    [SerializeField]
    [Tooltip("Registry of everything that the food source needs")]
    private NeedRegistry needs;

    public Dictionary<ItemID, Need> SetupNeeds()
    {
        Dictionary<ItemID, Need> needs = new Dictionary<ItemID, Need>();

        //Terrain Needs
        foreach (TerrainNeedConstructData need in terrainNeeds)
        {
            // Only add non water terrain needs to the dictionary
            if (!need.ID.IsWater)
            {
                needs.Add(need.ID, new TerrainNeed(need, this));
            }
        }

        //Water Needs
        foreach (LiquidNeedConstructData need in liquidNeeds)
        {
            needs.Add(need.ID, new LiquidNeed(need));

            //Food sources do not have liquid poisons so no need to worry about those here
        }

        return needs;
    }

    public Need GetTerrainWaterNeed()
    {
        TerrainNeedConstructData terrainWaterNeed = terrainNeeds.Find(need => need.ID.IsWater);

        if (terrainWaterNeed != null)
        {
            return new TerrainNeed(terrainWaterNeed, this);
        }
        else return null;
    }

    public void SetupData(int rootRadius, int output, List<List<NeedConstructData>> needs)
    {
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
