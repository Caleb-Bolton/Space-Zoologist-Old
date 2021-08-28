using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Handles neeed value updates of all the `FoodSource` type need
/// </summary>
public class FoodSourceNeedSystem : NeedSystem
{
    //private List<FoodSource> foodSources = new List<FoodSource>();
    private readonly ReservePartitionManager rpm = null;

    public static readonly Dictionary<SpeciesType, float> foodDominanceRatios = new Dictionary<SpeciesType, float>() 
    {
        {SpeciesType.Cow, 0.3f}, 
        {SpeciesType.Anteater, 0.25f}, 
        {SpeciesType.Goat, 0.20f}, 
        {SpeciesType.Slug, 0.15f}, 
        {SpeciesType.Spider, 0.10f}
    };

    // Food name to food calculators
    private Dictionary<string, FoodSourceCalculator> foodSourceCalculators = new Dictionary<string, FoodSourceCalculator>();

    public FoodSourceNeedSystem(ReservePartitionManager rpm, NeedType needType = NeedType.FoodSource) : base(needType)
    {
        this.rpm = rpm;
    }

    public override bool CheckState()
    {
        bool needUpdate = false;
        foreach (FoodSourceCalculator foodSourceCalculator in this.foodSourceCalculators.Values)
        {
            // Check if consumer is dirty
            foreach (Population consumer in foodSourceCalculator.Consumers)
            {
                if (consumer.GetAccessibilityStatus() || consumer.Count != consumer.PrePopulationCount)
                {
                    foodSourceCalculator.MarkDirty();
                    needUpdate = true;
                    break;
                }
            }
            // If consumer is already dirty check next food source calculator, otherwise check the terrain
            if (needUpdate)
            {
                continue;
            }
            else
            {
                needUpdate = this.CheckFoodSourcesTerrain(foodSourceCalculator);
            }
        }
        return needUpdate;
    }

    // foodSource.GetAccessibilityStatus() is Expensive!
    private bool CheckFoodSourcesTerrain(FoodSourceCalculator foodSourceCalculator)
    {
        foreach (FoodSource foodSource in foodSourceCalculator.FoodSources)
        {
            if (foodSource.GetAccessibilityStatus())
            {
                foodSourceCalculator.MarkDirty();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Updates how much all the registered populations take from all of the FoodSourceNeedSystem's food sources and updates the associated need in the Population's needs
    /// </summary>
    public override void UpdateSystem()
    {
        // 1. Reset all calculators remaining food
        foreach (FoodSourceCalculator foodSourceCalculator in this.foodSourceCalculators.Values)
        {
            foodSourceCalculator.ResetCalculator();
        }

        // 2. Iterate through populations based on most dominant
        foreach (Population population in new SortedSet<Population>(rpm.Populations, new DominanceComparer()))
        {
            float preferredAmount = 0;
            float compatibleAmount = 0;

            // 3. Iterate through needs starting with preferred (inefficient, could be refactored to first calculate list of ordered needs)
            for (int j = 0; j <= 1; j++)
            {
                foreach (KeyValuePair<string, Need> need in population.Needs)
                {
                    // 4. Calculate preferred and available food, skipping if need already met
                    if (!need.Value.NeedType.Equals(NeedType.FoodSource) || !foodSourceCalculators.ContainsKey(need.Key))
                    {
                        continue;
                    }

                    if (j == 0 && need.Value.IsPreferred)
                    {
                        preferredAmount += foodSourceCalculators[need.Key].CalculateDistribution(population);
                        continue;
                    }

                    if (j == 1 && !need.Value.IsPreferred)
                    {
                        compatibleAmount += foodSourceCalculators[need.Key].CalculateDistribution(population);
                    }
                }
            }
            population.UpdateFoodNeed(preferredAmount, compatibleAmount);
        }
    }

    public void AddFoodSource(FoodSource foodSource)
    {
        if (!this.foodSourceCalculators.ContainsKey(foodSource.Species.SpeciesName))
        {
            this.foodSourceCalculators.Add(foodSource.Species.SpeciesName, new FoodSourceCalculator(rpm, foodSource.Species.SpeciesName));
        }

        this.foodSourceCalculators[foodSource.Species.SpeciesName].AddSource(foodSource);

        this.isDirty = true;
    }

    public void RemoveFoodSource(FoodSource foodSource) {
        if (this.foodSourceCalculators.ContainsKey(foodSource.Species.SpeciesName))
        {
            this.foodSourceCalculators[foodSource.Species.SpeciesName].RemoveSource(foodSource);

            this.isDirty = true;
        }
    }

    public override void AddConsumer(Life life)
    {
        foreach (Need need in life.GetNeedValues().Values)
        {
            // Check if the need is a 'FoodSource' type
            if (need.NeedType == NeedType.FoodSource)
            {
                // Create a food source calculator for this food source,
                // if not already exist
                if (!this.foodSourceCalculators.ContainsKey(need.NeedName))
                {
                    this.foodSourceCalculators.Add(need.NeedName, new FoodSourceCalculator(rpm, need.NeedName));
                }

                // Add consumer to food source calculator
                this.foodSourceCalculators[need.NeedName].AddConsumer((Population)life);
            }
        }

        this.isDirty = true;
    }

    public override bool RemoveConsumer(Life life)
    {
        foreach (Need need in life.GetNeedValues().Values)
        {
            // Check if the need is a 'FoodSource' type
            if (need.NeedType == NeedType.FoodSource)
            {
                Debug.Assert(this.foodSourceCalculators[need.NeedName].RemoveConsumer((Population)life), "Remove conumer failed!");
            }
        }

        this.isDirty = true;

        return true;
    }

    public override void MarkAsDirty()
    {
        base.MarkAsDirty();

        foreach (FoodSourceCalculator foodSourceCalculator in this.foodSourceCalculators.Values)
        {
            foodSourceCalculator.MarkDirty();
        }
    }

    private class DominanceComparer : IComparer<Population>
    {
        public int Compare(Population a, Population b)
        {
            return (int)(100*(b.FoodDominance - a.FoodDominance));
        }
    }
}