using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum LiquidComposition { Water, Salt, Bacteria };

/// <summary>
/// Handles liquid need value updates
/// </summary>
public class LiquidNeedSystem : NeedSystem
{
    // TODO: Find the right helper system
    private readonly GridSystem gridSystem = default;
    private readonly ReservePartitionManager rpm = default;

    public LiquidNeedSystem(ReservePartitionManager rpm, GridSystem gridSystem, NeedType needType = NeedType.Liquid) : base(needType)
    {
        this.rpm = rpm;
        this.gridSystem = gridSystem;
    }

    public override void UpdateSystem()
    {
        if (this.Consumers.Count == 0)
        {
            this.isDirty = false;
            return;
        }

        Dictionary<Population, float> liquidTilesPerPopulation = new Dictionary<Population, float>();
        Dictionary<Population, HashSet<LiquidBody>> liquidBodiesPerPopulation = new Dictionary<Population, HashSet<LiquidBody>>();

        foreach(Population population in Consumers.OfType<Population>())
        {
            if(!liquidTilesPerPopulation.ContainsKey(population) && population.GetNeedValues().ContainsKey("LiquidTiles"))
                liquidTilesPerPopulation.Add(population, 0);
        }

        //Go through each body of water in the level and divide it between all populations that can access it
        foreach(LiquidBody liquidBody in gridSystem.liquidBodies)
        {
            HashSet<Population> accessiblePopulations = new HashSet<Population>();
            foreach(Population population in Consumers.OfType<Population>())
            {
                Dictionary<string, Need> popNeeds = population.GetNeedValues();

                if(!popNeeds.ContainsKey("LiquidTiles"))
                    continue;

                if ((!popNeeds.ContainsKey("Water") || popNeeds["Water"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Water])) && //If the population either doesn't need fresh water or the fresh water threshold is met
                    (!popNeeds.ContainsKey("Salt") || popNeeds["Salt"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Salt])) &&  //and it either doesn't need salt or the salt threshold is met
                    (!popNeeds.ContainsKey("Bacteria") || popNeeds["Bacteria"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Bacteria])) ) //and it either doesn't need bacteria or the bacteria threshold is met
                {
                    bool populationCanAccess = false;
                    foreach(Vector3Int location in rpm.GetLiquidLocations(population)) //check if any of the liquidbody's tiles are accessible to this population
                    {
                        if(liquidBody.tiles.Contains(location))
                        {
                            populationCanAccess = true;
                            break;
                        }
                    }

                    if(populationCanAccess) //if the population can access this water source, add it to the set
                        accessiblePopulations.Add(population);
                }
            }

            //split this water source equally between all populations that have access to it, regardless of that population's size
            float waterSplit = liquidBody.tiles.Count / (float)accessiblePopulations.Count;
            foreach(Population population in accessiblePopulations)
            {
                Dictionary<string, Need> popNeeds = population.GetNeedValues();
                if((popNeeds.ContainsKey("WaterPoison") && popNeeds["WaterPoison"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Water])) || //If the food source has a fresh water poison threshold and that threshold is surpassed
                   (popNeeds.ContainsKey("SaltPoison") && popNeeds["SaltPoison"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Salt])) ||  //or it has a salt poison threshold and that threshold is surpassed
                   (popNeeds.ContainsKey("BacteriaPoison") && popNeeds["BacteriaPoison"].IsThresholdMet(liquidBody.contents[(int)LiquidComposition.Bacteria])) ) //or it has a bacteria poison threshold and that threshold is surpassed
                {
                    liquidTilesPerPopulation[population] -= waterSplit;
                }
                else
                {
                    liquidTilesPerPopulation[population] += waterSplit;
                }

                if(!liquidBodiesPerPopulation.ContainsKey(population))
                    liquidBodiesPerPopulation.Add(population, new HashSet<LiquidBody>());
                
                liquidBodiesPerPopulation[population].Add(liquidBody);
            }
        }

        float[] liquidCompositionToUpdate = default; 
        foreach (Life life in Consumers)
        {
            if (life is Population)
            {
                Population population = (Population)life;
                
                population.UpdateNeed("LiquidTiles", liquidTilesPerPopulation[population]);
                //Debug.Log(population.name + " updates LiquidTiles with value: " + liquidTilesPerPopulation[population]);

                // Check is there is found composition
                if (liquidBodiesPerPopulation.ContainsKey(population))
                {
                    int totalTiles = 0;

                    //Average out all the liquid compositions
                    liquidCompositionToUpdate = new float[]{0, 0, 0};

                    foreach (LiquidBody liquidBody in liquidBodiesPerPopulation[population])
                    {
                        //Weight each composition based on the number of tiles in the liquidbody
                        liquidCompositionToUpdate[0] += liquidBody.contents[0] * liquidBody.tiles.Count;
                        liquidCompositionToUpdate[1] += liquidBody.contents[1] * liquidBody.tiles.Count;
                        liquidCompositionToUpdate[2] += liquidBody.contents[2] * liquidBody.tiles.Count;

                        totalTiles += liquidBody.tiles.Count;
                    }

                    for(int i = 0; i <= 2; ++i)
                    {
                        liquidCompositionToUpdate[i] /= totalTiles;
                    }
                }
                else
                {
                    this.isDirty = false;
                    continue;
                }
            }
            else if (life is FoodSource)
            {
                FoodSource foodSource = (FoodSource)life;
                Dictionary<string, Need> foodNeeds = foodSource.GetNeedValues();

                float liquidCount = 0;
                List<float[]> liquidCompositions = new List<float[]>();

                for(int i = 1; i <= foodSource.Species.RootRadius; ++i)
                {
                    List<float[]> compositions = gridSystem.GetLiquidCompositionWithinRange(gridSystem.WorldToCell(life.GetPosition()), i, true);
                    foreach(float[] composition in compositions)
                    {
                        if ((!foodNeeds.ContainsKey("Water") || foodNeeds["Water"].IsThresholdMet(composition[(int)LiquidComposition.Water])) && //If the food source either doesn't need fresh water or the fresh water threshold is met
                            (!foodNeeds.ContainsKey("Salt") || foodNeeds["Salt"].IsThresholdMet(composition[(int)LiquidComposition.Salt])) &&  //and it either doesn't need salt or the salt threshold is met
                            (!foodNeeds.ContainsKey("Bacteria") || foodNeeds["Bacteria"].IsThresholdMet(composition[(int)LiquidComposition.Bacteria])) ) //and it either doesn't need bacteria or the bacteria threshold is met
                        {
                            liquidCount += 1f/i;
                            liquidCompositions.Add(composition);
                        }
                    }
                }

                foodSource.UpdateNeed("LiquidTiles", liquidCount);
                //Debug.Log(foodSource.name + " updated LiquidTiles with value: " + liquidCount);

                // Check is there is found composition
                if (liquidCompositions.Count > 0)
                {
                    float[] sumComposition = new float[liquidCompositions[0].Count()];

                    foreach (float[] composition in liquidCompositions)
                    {
                        foreach (var (value, index) in composition.WithIndex())
                        {
                            sumComposition[index] += value;
                        }
                    }

                    // Use to avergae composition to update
                    liquidCompositionToUpdate = sumComposition.Select(v => v / liquidCompositions.Count).ToArray();
                }
                else
                {
                    this.isDirty = false;
                    continue;
                }
            }
            else
            {
                Debug.Assert(true, "Consumer type error!");
            }

            foreach (var (value, index) in liquidCompositionToUpdate.WithIndex())
            {
                string needName = ((LiquidComposition)index).ToString();
                if (life.GetNeedValues().ContainsKey(needName))
                {
                    life.UpdateNeed(needName, value);
                    //Debug.Log("Life: " + ((MonoBehaviour)life).gameObject.name + " updates need of type: " + needName + " with value " + value);
                }
            }
        }

        this.isDirty = false;
    }
}