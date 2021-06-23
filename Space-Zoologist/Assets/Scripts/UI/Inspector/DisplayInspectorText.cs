﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayInspectorText : MonoBehaviour
{
    [SerializeField] private Text inspectorWindowText = default;
    public InspectorText CurrentDisplay => currentDisplay;
    private InspectorText currentDisplay = InspectorText.Population;
    public enum InspectorText { Population, Food, Area, Liquid }

    public void DisplayPopulationStatus(Population population)
    {
        currentDisplay = InspectorText.Population;
        string displayText = $"{population.species.SpeciesName} Info: \n";

        displayText += $"Count: {population.Count}, {population.GrowthStatus}\n";
        if (population.GrowthStatus.Equals(GrowthStatus.stagnate))
        {
            displayText += $"Please wait 1 day for population to get accustomed to enclosure\n";
        }
        else if (population.GrowthStatus.Equals(GrowthStatus.growing))
        {
            displayText += $"{population.gameObject.name} population will increase in {population.DaysTillGrowth()} days\n";
        }
        else
        {
            if (population.IsStagnate())
            {
                displayText += $"{population.gameObject.name} is stagnate\n";
            }
            else
            {
                displayText += $"{population.gameObject.name} population will decrease in {population.DaysTillDeath()} days\n";
            }
            List<NeedType> unmetNeeds = population.GetUnmentNeeds();
            foreach (NeedType needType in unmetNeeds)
            {
                displayText += $"\n{needType.ToString()} need not being met"; 
            }
        }
        
        this.inspectorWindowText.text = displayText;
    }

    public void DisplayFoodSourceStatus(FoodSource foodSource)
    {
        currentDisplay = InspectorText.Food;
        string displayText = $"{foodSource.name} Info: \n";

        displayText += $"Output: {foodSource.FoodOutput}\n";
        if (!foodSource.terrainNeedMet)
        {
            displayText += $"\n Terrain need not being met" + foodSource.GetNeedValues().Values;
        }
        if (!foodSource.liquidNeedMet)
        {
            displayText += $"\n Liquid need not being met";
        }


        this.inspectorWindowText.text = displayText;
    }

    public void DislplayEnclosedArea(EnclosedArea enclosedArea)
    {
        currentDisplay = InspectorText.Area;
        // THe composition is a list of float value in the order of the AtmoshpereComponent Enum
        float[] atmosphericComposition = enclosedArea.atmosphericComposition.GetComposition();
        float[] terrainComposition = enclosedArea.terrainComposition;

        string displayText = $"Enclosed Area {enclosedArea.id} Info: \n";

        // Atmospheric info
        displayText += "Atmospheric composition: \n";
        foreach (var (value, index) in atmosphericComposition.WithIndex())
        {
            displayText += $"{((AtmosphereComponent)index).ToString()} : {value}\n";
        }

        displayText += "\nTerrain: \n";
        foreach (var (value, index) in terrainComposition.WithIndex())
        {
            displayText += $"{((TileType)index).ToString()} : {value}\n";
        }

        displayText += "\n";
        displayText += $"Population count: {enclosedArea.populations.Count}\n";
        displayText += $"Total animal count: {enclosedArea.animals.Count}\n";
        displayText += $"Food Source count: {enclosedArea.foodSources.Count}\n";

        this.inspectorWindowText.text = displayText;
    }

    public void DisplayLiquidCompisition(float[] compositions)
    {
        currentDisplay = InspectorText.Liquid;
        string displayText = "";
        if (compositions == null)
        {
            displayText = "Water : 0.00\n Salt : 0.00 \n Bacteria : 0.00\n";
        }
        else
        {
            string[] liquidName = new string[] { "Water", "Salt", "Bacteria" };
            for (int i = 0; i < 3; i++)
            {
                displayText += $"{liquidName[i]} : {compositions[i] * 100}%\n";
            }

        }
        this.inspectorWindowText.text = displayText;
    }
}
