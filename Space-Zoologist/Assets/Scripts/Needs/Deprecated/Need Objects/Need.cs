using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class TerrainNeed : Need
{
    private AnimalSpecies animalSpecies;
    private FoodSourceSpecies foodSourceSpecies;

    public TerrainNeed(TerrainNeedConstructData needConstructData, AnimalSpecies species) : base(needConstructData) 
    { 
        animalSpecies = species;
    }

    public TerrainNeed(TerrainNeedConstructData needConstructData, FoodSourceSpecies species) : base(needConstructData) 
    {
        foodSourceSpecies = species;
    }

    protected override NeedType GetNeedType()
    {
        return NeedType.Terrain;
    }

    public override float GetThreshold()
    {
        if(animalSpecies)
            return animalSpecies.TerrainTilesRequired;
        
        if(foodSourceSpecies)
            return foodSourceSpecies.Size.sqrMagnitude;
        
        return needConstructData.GetSurvivableThreshold();
    }
}

[System.Serializable]
public class FoodNeed : Need
{
    private int foodThreshold;

    public FoodNeed(FoodNeedConstructData needConstructData, int minFoodThreshold) : base(needConstructData) 
    {
        foodThreshold = minFoodThreshold;
    }

    protected override NeedType GetNeedType()
    {
        return NeedType.FoodSource;
    }

    public override float GetThreshold()
    {
        return foodThreshold;
    }
}

[System.Serializable]
public class LiquidNeed : Need
{
    private new LiquidNeedConstructData needConstructData;

    public LiquidNeed(LiquidNeedConstructData needConstructData) : base(needConstructData) 
    { 
        this.needConstructData = needConstructData; 
    }

    protected override NeedType GetNeedType()
    {
        return NeedType.Liquid;
    }
}

public class PreyNeed : Need
{
    public PreyNeed(NeedConstructData needConstructData) : base(needConstructData) { }

    protected override NeedType GetNeedType()
    {
        return NeedType.Prey;
    }
}

[System.Serializable]
public abstract class Need
{
    public ItemID ID => needConstructData.ID;
    public NeedType NeedType => GetNeedType();
    public Sprite Sprite => sprite;
    public float NeedValue => this.needValue;
    public float Severity => severity;
    public bool IsPreferred => needConstructData.IsPreferred;

    [SerializeField] private float needValue = default;
    [Range(1.0f, 10.0f)]
    [SerializeField] private int severity = 1;
    [SerializeField] private Sprite sprite = default;

    protected NeedConstructData needConstructData;

    protected abstract NeedType GetNeedType();

    protected Need(NeedConstructData needConstructData)
    {
        this.needConstructData = needConstructData;
    }

    /// <summary>
    /// Returns what condition the need is in based on the given need value.
    /// </summary>
    /// <param name="value">The value to compare to the need thresholds</param>
    /// <returns></returns>
    public virtual bool IsThresholdMet(float value)
    {
        return value >= needConstructData.GetSurvivableThreshold();
    }

    public virtual float GetThreshold()
    {
        return needConstructData.GetSurvivableThreshold();
    }

    public virtual void UpdateNeedValue(float value)
    {
        this.needValue = value;
    }
}