﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializedPopulation
{
    public MapItemSet population;
    public float populationIncreaseRate;
    public SerializedPopulation(AnimalSpecies animalSpecies, Vector3[] animals, float growthRate)
    {
        this.population = new MapItemSet(animalSpecies.name, animals);
        this.populationIncreaseRate = growthRate;
    }
}
