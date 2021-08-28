using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// Each NeedType holds a list of unique needs
public enum NeedType { Terrain, Atmosphere, Density, FoodSource, Liquid, Species, Temperature, Symbiosis, Prey };
public enum NeedCondition { Bad, Neutral, Good }

[System.Serializable]
public class TerrainNeedConstructData : NeedConstructData
{
    [SerializeField] private bool isPreferred;

    public TerrainNeedConstructData(string name) 
        : base(name)
    {
    }

    protected override bool getIsPreferred()
    {
        return isPreferred;
    }

    public override float GetSurvivableThreshold() { return -1f; }
}

[System.Serializable]
public class FoodNeedConstructData : NeedConstructData
{
    [SerializeField] private bool isPreferred;

    public FoodNeedConstructData(string name) 
        : base(name)
    {
    }

    public override float GetSurvivableThreshold()
    {
        return -1;
    }

    protected override bool getIsPreferred()
    {
        return isPreferred;
    }
}

[System.Serializable]
public class LiquidNeedConstructData : NeedConstructData
{
    public float TileNeedThreshold => tileNeedThreshold;
    public float FreshWaterMinThreshold => freshWaterMinThreshold;
    public float FreshWaterMaxThreshold => freshWaterMaxThreshold;
    public float SaltMinThreshold => saltMinThreshold;
    public float SaltMaxThreshold => saltMaxThreshold;
    public float BacteriaMinThreshold => bacteriaMinThreshold;
    public float BacteriaMaxThreshold => bacteriaMaxThreshold;

    [SerializeField] private float tileNeedThreshold;
    [Range(0,1)] [SerializeField] private float freshWaterMinThreshold;
    [Range(0,1)] [SerializeField] private float freshWaterMaxThreshold = 1;
    [Range(0,1)] [SerializeField] private float saltMinThreshold;
    [Range(0,1)] [SerializeField] private float saltMaxThreshold = 1;
    [Range(0,1)] [SerializeField] private float bacteriaMinThreshold;
    [Range(0,1)] [SerializeField] private float bacteriaMaxThreshold = 1;

    public LiquidNeedConstructData(string name) 
        : base(name)
    {
    }

    public override float GetSurvivableThreshold()
    {
        return tileNeedThreshold;
    }

    protected override bool getIsPreferred()
    {
        return false;
    }
}

[System.Serializable]
public class PreyNeedConstructData : NeedConstructData
{
    public PreyNeedConstructData(string name) : base(name) {}

    public override float GetSurvivableThreshold() { return 0; }
    protected override bool getIsPreferred() { return false; }
}

/// <summary>
/// A data object that holds the information to create a Need object.
/// </summary>
[System.Serializable]
public abstract class NeedConstructData
{
    public string NeedName => needName;
    public bool IsPreferred => getIsPreferred();

    [SerializeField] private string needName = default;

    public NeedConstructData(string name)
    {
        this.needName = name;
    }

    public abstract float GetSurvivableThreshold();

    protected abstract bool getIsPreferred();
}