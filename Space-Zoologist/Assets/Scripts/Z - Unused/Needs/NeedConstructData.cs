﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// Each NeedType holds a list of unique needs
public enum NeedType { Terrain, Atmosphere, Density, FoodSource, Liquid, Species, Temperature, Symbiosis };
public enum NeedCondition { Bad, Neutral, Good }

[System.Serializable]
public class NeedTypeConstructData
{

    public NeedType NeedType => needType;
    public List<NeedConstructData> Needs => needs;

    [SerializeField] private NeedType needType = default;
    [SerializeField] public List<NeedConstructData> needs = default;
}

/// <summary>
/// A data object that holds the information to create a Need object.
/// </summary>
[System.Serializable]
public class NeedConstructData
{
    public string NeedName => needName;
    public int Severity => severity;
    public List<NeedBehavior> Conditions => conditions;
    public List<float> Thresholds => thresholds;
    public bool IsPreferred => isPreferred;

    [SerializeField] private string needName = default;
    [Range(1.0f, 10.0f)]
    [SerializeField] private int severity = 1;
    [SerializeField] private bool isPreferred = false;
    [SerializeField] private List<NeedBehavior> conditions = default;
    [SerializeField] private List<float> thresholds = default;

    public NeedConstructData(string name, int severity, List<string> conditions, List<float> thresholds)
    {
        this.conditions = new List<NeedBehavior>();
        this.thresholds = new List<float>();
        this.needName = name;
        this.severity = severity;
        foreach(string condition in conditions)
        {
            if (condition.Equals("Good", StringComparison.OrdinalIgnoreCase))
            {
                this.conditions.Add(new NeedBehavior(NeedCondition.Good));
            }
            if (condition.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
            {
                this.conditions.Add(new NeedBehavior(NeedCondition.Neutral));
            }
            if (condition.Equals("Bad", StringComparison.OrdinalIgnoreCase))
            {
                this.conditions.Add(new NeedBehavior(NeedCondition.Bad));
            }
        }
        this.thresholds = thresholds;
    }
}