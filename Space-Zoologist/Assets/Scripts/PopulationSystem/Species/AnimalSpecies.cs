using System.Collections.Generic;
using UnityEngine;
using System;

public enum SpeciesType { Goat, Cow, Anteater, Spider, Slug, Momo }

[CreateAssetMenu]
public class AnimalSpecies : ScriptableObject
{
    // Getters
    public string SpeciesName => speciesName;
    public SpeciesType Species => species;
    public int TerrainTilesRequired => terrainTilesRequired;
    public int MinFoodRequired => minFoodRequired;
    public int MaxFoodRequired => maxFoodRequired;
    public int GrowthRate => growthRate;
    public int DecayRate => decayRate;
    public float Size => size;
    public List<TileType> AccessibleTerrain => accessibleTerrain;
    public Sprite Icon => icon;
    public Sprite Sprite => icon;
    public int MoveCost => moveCost;
    public Sprite Representation => representation;
    // TODO setup tile weights for species
    public Dictionary<TileType, byte> TilePreference = default;
    public RuntimeAnimatorController AnimatorController => animatorController;

    // Values
    [SerializeField] private RuntimeAnimatorController animatorController = default;
    [SerializeField] private string speciesName = default;
    [SerializeField] private SpeciesType species = default;
    [SerializeField] private int terrainTilesRequired = default;
    [SerializeField] private int minFoodRequired = default;
    [SerializeField] private int maxFoodRequired = default;
    [SerializeField] private int growthRate = 3;
    [SerializeField] private int decayRate = 3;
    [SerializeField] private int moveCost = default;

    [SerializeField] private float size = default;
    [SerializeField] private List<TileType> accessibleTerrain = default;
    [SerializeField] private Sprite icon = default;

    [SerializeField] private List<TerrainNeedConstructData> terrainNeeds = default;
    [SerializeField] private List<FoodNeedConstructData> foodNeeds = default;
    [SerializeField] private List<LiquidNeedConstructData> liquidNeeds = default;
    [SerializeField] private List<PreyNeedConstructData> preyNeeds = default;

    // Replace later with actual representation/animations/behaviors
    [SerializeField] private Sprite representation = default;

    public Dictionary<string, Need> SetupNeeds()
    {
        Dictionary<string, Need> needs = new Dictionary<string, Need>();
        
        //Terrain Needs
        foreach (TerrainNeedConstructData need in terrainNeeds)
        {
            needs.Add(need.NeedName, new TerrainNeed(need, this));
        }

        //Food Needs
        foreach (FoodNeedConstructData need in foodNeeds)
        {
            needs.Add(need.NeedName, new FoodNeed(need, minFoodRequired));
        }

        //Water Needs
        foreach (LiquidNeedConstructData need in liquidNeeds)
        {
            if(need.TileNeedThreshold <= 0)
                continue;

            needs.Add("LiquidTiles", new LiquidNeed("LiquidTiles", need));

            if(need.FreshWaterMinThreshold != 0)
                needs.Add("Water", new LiquidNeed("Water", need));

            if(need.FreshWaterMaxThreshold != 1)
                needs.Add("WaterPoison", new LiquidNeed("WaterPoison", need));

            if(need.SaltMinThreshold != 0)
                needs.Add("Salt", new LiquidNeed("Salt", need));

            if(need.SaltMaxThreshold != 1)
                needs.Add("SaltPoison", new LiquidNeed("SaltPoison", need));

            if(need.BacteriaMinThreshold != 0)
                needs.Add("Bacteria", new LiquidNeed("Bacteria", need));

            if(need.BacteriaMaxThreshold != 1)
                needs.Add("BacteriaPoison", new LiquidNeed("BacteriaPoison", need));
        }

        //Prey Needs
        foreach (PreyNeedConstructData need in preyNeeds)
        {
            needs.Add(need.NeedName, new PreyNeed(need));
        }

        return needs;
    }

    public List<PopulationBehavior> GetBehaviors()
    {
        return new List<PopulationBehavior>();
    }

    public Dictionary<Need, Dictionary<NeedCondition, PopulationBehavior>> SetupBehaviors(Dictionary<string, Need> needs)
    {
        return new Dictionary<Need, Dictionary<NeedCondition, PopulationBehavior>>();
    }

    public void SetupData(string name, int growthRate, List<string> accessibleTerrain, List<List<NeedConstructData>> needsLists)
    {
        // TODO setup behaviors and accessible terrain
        this.speciesName = name;
        this.growthRate = growthRate;
        this.accessibleTerrain = new List<TileType>();
        foreach (string tileType in accessibleTerrain)
        {
            if (tileType.Equals("Sand", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Sand);
            }
            if (tileType.Equals("Grass", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Grass);
            }
            if (tileType.Equals("Dirt", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Dirt);
            }
            if (tileType.Equals("Liquid", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Liquid);
            }
            if (tileType.Equals("Rock", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Stone);
            }
            if (tileType.Equals("Wall", StringComparison.OrdinalIgnoreCase))
            {
                this.accessibleTerrain.Add(TileType.Wall);
            }
        }
        //this.accessibleTerrain = accessibleTerrain;

        for(int i = 0; i < needsLists.Count; ++i)
        {
            switch(i)
            {
                case 0:
                    terrainNeeds = new List<TerrainNeedConstructData>();
                    foreach(NeedConstructData data in needsLists[i])
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
                    foodNeeds = new List<FoodNeedConstructData>();
                    foreach(NeedConstructData data in needsLists[i])
                    {
                        if(!(data is FoodNeedConstructData))
                        {
                            Debug.LogError("Invalid needs data: NeedConstructData was not a FoodNeedConstructData");
                            return;
                        }

                        foodNeeds.Add((FoodNeedConstructData)data);
                    }
                    break;
                case 2:
                    liquidNeeds = new List<LiquidNeedConstructData>();
                    foreach(NeedConstructData data in needsLists[i])
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