﻿using System.Collections.Generic;

public enum GrowthStatus {declining, stagnate, growing}

/// <summary>
/// Determines the rate and status of growth for each population based off of their most severe need that isn't being met
/// </summary>
public class GrowthCalculator
{
    public GrowthStatus GrowthStatus { get; private set; }
    public float GrowthRate { get; private set; }

    public GrowthCalculator()
    {
        this.GrowthRate = 0;
        this.GrowthStatus = GrowthStatus.stagnate;
    }

    /*
        bad > 0 declining
        bad == 0 && good == 0 stagnate
        bad == 0 && good > 0 growing
        start with a rate of 60 and take off the rate of severity
    */
    public void CalculateGrowth(Population population)
    {
        int neutralConditionCount = 0;
        int badConditionModifier = 0;
        foreach(KeyValuePair<string, Need> need in population.Needs)
        {
            switch(need.Value.GetCondition(need.Value.NeedValue))
            {
                case NeedCondition.Bad:
                    badConditionModifier += need.Value.Severity;
                    break;
                case NeedCondition.Neutral:
                neutralConditionCount++;
                    break;
                case NeedCondition.Good:
                    break;
                default:
                    break;
            }
            if (neutralConditionCount == population.Needs.Count)
            {
                this.GrowthStatus = GrowthStatus.stagnate;
            }
            else if (badConditionModifier > 0)
            {
                this.GrowthStatus = GrowthStatus.declining;
                this.GrowthRate = population.Species.GrowthRate - badConditionModifier;
                if (this.GrowthRate < 10)
                {
                    this.GrowthRate = 10;
                }
            }
            else
            {
                this.GrowthStatus = GrowthStatus.growing;
                this.GrowthRate = population.Species.GrowthRate;
            }
        }

    }
}
