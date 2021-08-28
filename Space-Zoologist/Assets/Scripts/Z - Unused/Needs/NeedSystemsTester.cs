using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NeedSystemsTester : MonoBehaviour
{

    [SerializeField] private Text populationStats = default;
    [SerializeField] private Text foodSourceStats = default;
    [SerializeField] PopulationManager PopulationManager = default;
    [SerializeField] FoodSourceManager FoodSourceManager = default;

    public void Update()
    {
        string populationStatsText = "";
        foreach (Population population in PopulationManager.Populations)
        {
            populationStatsText += $"***{ population.Species.SpeciesName } {population.GetInstanceID()}; Count: {population.Count}; Dominance: {population.FoodDominance}***\n";
            foreach (KeyValuePair<string, Need> needValue in population.Needs)
            {
                populationStatsText += $"- {needValue.Key}: { needValue.Value.NeedValue } -- Condition: {population.Needs[needValue.Key].IsThresholdMet(needValue.Value.NeedValue)}\n";
            }
            populationStatsText += "\n";
        }
        populationStats.text = populationStatsText;

        string foodSourceStatsText = "";
        foreach (FoodSource foodSource in FoodSourceManager.FoodSources)
        {
            foodSourceStatsText += $"***{ foodSource.Species.SpeciesName }; Food output: {foodSource.FoodOutput}***\n";
            foreach (KeyValuePair<string, Need> needValue in foodSource.Needs)
            {
                foodSourceStatsText += $"- {needValue.Key}: { needValue.Value.NeedValue } -- Condition: {foodSource.Needs[needValue.Key].IsThresholdMet(needValue.Value.NeedValue)}\n";
            }
            foodSourceStatsText += "\n";
        }
        foodSourceStats.text = foodSourceStatsText;
    }
}