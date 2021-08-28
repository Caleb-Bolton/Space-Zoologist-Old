using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calculates food distribution of a certain food type
/// </summary>
public class FoodSourceCalculator : NeedCalculator
{
    public string FoodSourceName => this.foodSourceName;
    public List<FoodSource> FoodSources => this.foodSources;
    public List<Population> Consumers => this.consumers;
    public bool IsDirty => this.isDirty;

    private string foodSourceName = default;
    private readonly ReservePartitionManager rpm = null;
    private List<FoodSource> foodSources = new List<FoodSource>();
    private List<Population> consumers = new List<Population>();
    private bool isDirty = default;

    // Holds which FoodSources each population has access to.
    private Dictionary<Population, HashSet<FoodSource>> accessibleFoodSources = new Dictionary<Population, HashSet<FoodSource>>();
    // Holds which populations have access to each FoodSource, the opposite of accessibleFoodSources.
    private Dictionary<FoodSource, HashSet<Population>> populationsWithAccess = new Dictionary<FoodSource, HashSet<Population>>();

    private Dictionary<FoodSource, float> foodRemaining = new Dictionary<FoodSource, float>();
    Dictionary<FoodSource, float> localDominanceRemaining = new Dictionary<FoodSource, float>();

    public FoodSourceCalculator(ReservePartitionManager rpm, string foodSourceName)
    {
        this.foodSourceName = foodSourceName;
        this.rpm = rpm;
    }

    public void MarkDirty()
    {
        this.isDirty = true;
    }


    public void AddConsumer(Population consumer)
    {
        if (this.consumers.Contains(consumer))
        {
            return;
        }

        this.consumers.Add(consumer);

        //Population population = (Population)life;
        accessibleFoodSources.Add(consumer, new HashSet<FoodSource>());
        foreach (FoodSource foodSource in foodSources)
        {
            if (rpm.CanAccess(consumer, foodSource.Position))
            {
                accessibleFoodSources[consumer].Add(foodSource);
                populationsWithAccess[foodSource].Add(consumer);
            }
        }

        this.isDirty = true;
    }

    public void AddSource(Life source)
    {
        FoodSource foodSource = (FoodSource)source;

        this.foodSources.Add(foodSource);
        populationsWithAccess.Add(foodSource, new HashSet<Population>());
        foreach (Population population in Consumers)
        {
            if (rpm.CanAccess(population, foodSource.Position))
            {
                accessibleFoodSources[population].Add(foodSource);
                populationsWithAccess[foodSource].Add(population);
            }
        }

        this.isDirty = true;
    }

    public bool RemoveConsumer(Population consumer)
    {
        this.isDirty = true;
 
        this.consumers.Remove(consumer);
        this.accessibleFoodSources.Remove(consumer);
        return true;
    }

    public bool RemoveSource(Life source)
    {
        Debug.Log(this.FoodSourceName);
        FoodSource foodSource = (FoodSource)source;
        Debug.Log(foodSources.Contains(foodSource));
        this.isDirty = true;

        Debug.Assert(this.foodSources.Remove(foodSource), "FoodSource removal failure");
        foreach (Population pop in populationsWithAccess[foodSource])
        {
            Debug.Assert(accessibleFoodSources[pop].Remove(foodSource), "Accessible FoodSource removal failure");
        }
        Debug.Assert(this.populationsWithAccess.Remove(foodSource), "Removal of foodsource in populationsWithAccess failed!");

        return true;
    }

    public float CalculateDistribution(Population population)
    {
        float foodAcquired = 0.0f;
        List<FoodSource> leastToMostContested = accessibleFoodSources[population].OrderByDescending(foodSource => localDominanceRemaining[foodSource]).ToList();
        foreach (FoodSource foodSource in leastToMostContested)
        {
            if (foodSource.isUnderConstruction)
            {
                //Debug.Log(foodSource.name + " at " + foodSource.gameObject.transform.position + " under construction");
                continue;
            }
            // 1. Calculate how much food each population can receive from available food, accounting for no food or dominance
            float dominanceRatio = population.FoodDominance / localDominanceRemaining[foodSource];
            if (localDominanceRemaining[foodSource] <= 0 || dominanceRatio <= 0)
            {
                dominanceRatio = 1;
            }
            float foodToAcquire = foodRemaining[foodSource] * (dominanceRatio);
            // 2. Handle case for if they take all remaining food
            if (foodToAcquire > foodRemaining[foodSource])
            {
                foodAcquired += foodRemaining[foodSource];
                foodRemaining[foodSource] = 0;
            }
            else
            {
                foodAcquired += foodToAcquire;
                foodRemaining[foodSource] -= foodToAcquire;
            }
        }
        UpdateLocalDominance(population);
        population.UpdateNeed(foodSourceName, foodAcquired);
        //Debug.Log(population.species.SpeciesName + " receieved " + foodAcquired + " from " + this.FoodSourceName);
        return foodAcquired;
    }

    private void UpdateLocalDominance(Population population)
    {
        foreach (FoodSource foodSource in accessibleFoodSources[population])
        {
            localDominanceRemaining[foodSource] -= population.FoodDominance;
        }
    }

    // Single sum to keep track of total food output
    public void ResetCalculator()
    {
        foreach (FoodSource foodSource in foodSources)
        {
            if (!foodRemaining.ContainsKey(foodSource))
            {
                foodRemaining[foodSource] = 0f;
            }
            if (!localDominanceRemaining.ContainsKey(foodSource))
            {
                localDominanceRemaining[foodSource] = 0f;
            }
            float total = populationsWithAccess[foodSource].Sum(p => p.FoodDominance);
            localDominanceRemaining[foodSource] = total;
            foodRemaining[foodSource] = foodSource.FoodOutput;
        }
    }
}
