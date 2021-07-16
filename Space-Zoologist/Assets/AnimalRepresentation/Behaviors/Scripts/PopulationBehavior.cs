﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For creating more behaviors, inherit like how IdleRandomTrigger is setup
public delegate void StepCompletedCallBack(GameObject gameObject, List<GameObject> gameObjects);
[CreateAssetMenu(fileName = "PopulationBehavior", menuName = "SpeciesBehavior/PopulationBehavior")]
public class PopulationBehavior : ScriptableObject
{
    public BehaviorData behaviorData;
    public int numberOfAnimalsRequired = 1;
    public List<BehaviorPattern> behaviorPatterns = default;
    protected Dictionary<GameObject, int> animalsToSteps = new Dictionary<GameObject, int>();
    protected StepCompletedCallBack stepCompletedCallback;
    protected StepCompletedCallBack alternativeCallback;
    protected BehaviorCompleteCallback behaviorCompleteCallback;
    public void AssignCallback(BehaviorCompleteCallback callback)
    {
        behaviorCompleteCallback = callback;
    }
    /// <summary>
    /// Called when animal enters behavior
    /// </summary>
    /// <param name="avalabilityToAnimals"></param>
    public void EnterBehavior(List<GameObject> animals)
    {
        behaviorData.behaviorName = this.name;
        behaviorData.ForceExitCallback = OnForceExit;
        stepCompletedCallback = OnStepCompleted; // Setup Callback
        alternativeCallback = OnAlternativeExit;

        if (animals.Count >= numberOfAnimalsRequired)
        {
            foreach (GameObject animal in animals)
            {
                Initialize(animal, animals);
            }
        }
    }
    /// <summary>
    /// Initialization, mainly giving animals information of animals cooperating with them to perform a behavior
    /// </summary>
    /// <param name="animal">The animal is being processed</param>
    /// <param name="selectedAnimals">All selected animals</param>
    protected virtual void Initialize(GameObject animal, List<GameObject> selectedAnimals)
    {
        List<GameObject> otherAnimals = new List<GameObject>(selectedAnimals);
        otherAnimals.Remove(animal);
        animal.GetComponent<AnimalBehaviorManager>().activeBehavior = behaviorData;
        animalsToSteps.Add(animal, 0);
        ProceedToNext(animal, otherAnimals);
    }
    /// <summary>
    /// Called every "step", decides what to do after a step is completed. By default, it proceeds to next step.
    /// Can be modified so that when all animals are ready, then move on. Specifically, when not every collaboratingAnimal is on the same step, do not proceed. 
    /// Then when the last animal is ready, it calls all collaboratingAnimals with isDriven set to true (See ProceedWhenAllAnimalsReady)
    /// Can also set to loop
    /// </summary>
    /// <param name="animal"></param>
    /// <param name="collaboratingAnimals">When defaults to null, the animal is driven by the action of another animal</param>
    /// <param name="isDriven">Set to true </param>
    protected virtual void ProceedToNext(GameObject animal, List<GameObject> collaboratingAnimals, bool isDriven = false) //Looping can be achieved by overriding this function, as well as synchronization among all animals
    {
        if (animalsToSteps[animal] < behaviorPatterns.Count) // exit behavior when all steps are completed
        {
            //Debug.Log("Next");
            animal.GetComponent<AnimalBehaviorManager>().AddBehaviorPattern(behaviorPatterns[animalsToSteps[animal]], stepCompletedCallback, alternativeCallback, collaboratingAnimals);
        }
        else
        {
            RemoveBehavior(animal);
        }
    }
    /// <summary>
    /// Defines how the behavior exits
    /// </summary>
    /// <param name="animal"></param>
    protected virtual void RemoveBehavior(GameObject animal)
    {
        if (animalsToSteps.ContainsKey(animal))
        {
            animalsToSteps.Remove(animal);
            animal.GetComponent<AnimalBehaviorManager>().activeBehavior = null;
            behaviorCompleteCallback.Invoke(animal);
        }
    }
    /// <summary>
    /// Callback function which increases the step count and calls to proceeds to next step
    /// </summary>
    /// <param name="animal"></param>
    /// <param name="collaboratingAnimals"></param>
    protected void OnStepCompleted(GameObject animal, List<GameObject> collaboratingAnimals)
    {
        animalsToSteps[animal]++;
        ProceedToNext(animal, collaboratingAnimals);
    }
    protected virtual void OnAlternativeExit(GameObject animal, List<GameObject> collaboratingAnimals)
    {
        this.RemoveBehavior(animal);
        foreach(GameObject collaboratingAnimal in collaboratingAnimals)
        {
            this.RemoveBehavior(collaboratingAnimal);
        }
    }
    /// <summary>
    /// Define what needs to be done when this behavior is force exiting, usually just call remove behavior
    /// </summary>
    /// <param name="animal"></param>
    protected virtual void OnForceExit(GameObject animal)
    {
        RemoveBehavior(animal);
    }


    // Functions below are sample functions that can either to be used as references or called
    //
    //
    //
    /// <summary>
    /// Example alternative to ProceedToNext() which only proceeds when all animals are at same step
    /// Typical usage: all animals move to a location, when all of them arrived, do something
    /// </summary>
    /// <param name="animal"></param>
    /// <param name="collaboratingAnimals"></param>
    /// <param name="isDriven"></param>
    protected void ProceedWhenAllAnimalsReady(GameObject animal, List<GameObject> collaboratingAnimals, bool isDriven = false)
    {
        if (animalsToSteps[animal] < behaviorPatterns.Count) // exit behavior when all steps are completed
        {
            if (!isDriven)// Avoids infinite loop
            {
                foreach (GameObject otherAnimal in collaboratingAnimals)
                {

                    if (!animalsToSteps.ContainsKey(otherAnimal) || animalsToSteps[otherAnimal] != animalsToSteps[animal])
                    {
                        return;
                    }
                }
                // When the above loop completes without returning, it means that all animals are ready, then all animals should proceed to next step
                foreach (GameObject otherAnimal in collaboratingAnimals) //Calls other animals to proceed without checking completion, which collaborating animals defaults to null
                {
                    List<GameObject> otherAnimalCollabs = new List<GameObject>(collaboratingAnimals);
                    otherAnimalCollabs.Add(animal);
                    otherAnimalCollabs.Remove(otherAnimal);
                    ProceedToNext(otherAnimal, otherAnimalCollabs, true);
                }
            }
            animal.GetComponent<AnimalBehaviorManager>().AddBehaviorPattern(behaviorPatterns[animalsToSteps[animal]], stepCompletedCallback, alternativeCallback, collaboratingAnimals);
        }
        else
        {
            RemoveBehavior(animal);
        }
    }
}
