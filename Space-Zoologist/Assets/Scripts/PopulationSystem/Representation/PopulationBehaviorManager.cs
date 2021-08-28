using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public delegate void BehaviorCompleteCallback(GameObject animal);
public class PopulationBehaviorManager : MonoBehaviour
{
    public Dictionary<string, PopulationBehavior> ActiveBehaviors = new Dictionary<string, PopulationBehavior>();
    private Population population = default;
    [SerializeField] public Dictionary<GameObject, BehaviorExecutionData> animalsToExecutionData = new Dictionary<GameObject, BehaviorExecutionData>();
    [SerializeField] public List<PopulationBehavior> tempBehaviors = new List<PopulationBehavior>();
    [SerializeField] private PopulationBehavior defaultBehavior = default;
    private DequeueCoordinatedBehavior DequeueCoordinatedBehavior;
    private BehaviorCompleteCallback BehaviorCompleteCallback;

    public void Initialize()
    {
        DequeueCoordinatedBehavior = OnDequeue;
        BehaviorCompleteCallback = OnBehaviorComplete;
        foreach (PopulationBehavior behavior in tempBehaviors)
        {
            behavior.AssignCallback(BehaviorCompleteCallback);
        }
        this.population = this.gameObject.GetComponent<Population>();
        int j = -1;
        for (int i = 0; i < population.Count; i++)
        {
            j++;
            if (j >= tempBehaviors.Count)
            {
                j = 0;
            }
            animalsToExecutionData.Add(this.population.AnimalPopulation[i], new BehaviorExecutionData(j));
        }
        foreach (GameObject animal in this.population.AnimalPopulation)
        {
            Debug.Log(animalsToExecutionData[animal].currentBehaviorIndex);
            Debug.Log(tempBehaviors.Count);
            if (tempBehaviors[animalsToExecutionData[animal].currentBehaviorIndex].numberOfAnimalsRequired == 1)
            {
                List<GameObject> animals = new List<GameObject>();
                animals.Add(animal);
                tempBehaviors[animalsToExecutionData[animal].currentBehaviorIndex].EnterBehavior(animals);
                continue;
            }
            QueueGroupBehavior(animal, animalsToExecutionData[animal].currentBehaviorIndex, tempBehaviors[animalsToExecutionData[animal].currentBehaviorIndex].numberOfAnimalsRequired);
        }
    }
    private void QueueGroupBehavior(GameObject initiator, int behaviorIndex, int numToFind)
    {
        animalsToExecutionData[initiator].pendingBehavior = tempBehaviors[behaviorIndex];
        animalsToExecutionData[initiator].cooperatingAnimals.Add(initiator);// Add self to list
        int numFound = 1;
        int maxQueueLength = 0;
        while (numFound != numToFind || numToFind > animalsToExecutionData.Count)
        {
            if (maxQueueLength == 5) //Queue too long, skip Group behavior. The queue length actually stabilizes between 0 and 2, but just in case it exceeds for whatever reason
            {
                break;
            }
            for (int i = 1; i < tempBehaviors.Count; i++) //Prioritizes preceding behaviors to avoid clustering of behaviors
            {
                int currentIndex = behaviorIndex - i;
                if (currentIndex < 0)
                {
                    currentIndex += tempBehaviors.Count();
                }
                foreach (KeyValuePair<GameObject, BehaviorExecutionData> animalToData in animalsToExecutionData)
                {
                    if (animalToData.Key == initiator)//Skip self(It will not find itself anyways, but just in case)
                    {
                        continue;
                    }
                    if (animalToData.Value.currentBehaviorIndex == currentIndex)
                    {
                        if (animalToData.Value.QueuedCoordinatedBehaviorsToInitiators.Count <= maxQueueLength) //Queue is small enough to be added
                        {
                            animalsToExecutionData[initiator].cooperatingAnimals.Add(animalToData.Key);
                            numFound++;
                            if(numFound == numToFind) //When all found, queue behavior
                            {
                                foreach (GameObject animal in animalsToExecutionData[initiator].cooperatingAnimals)
                                {
                                    if (animal != initiator)
                                    {
                                        animalsToExecutionData[animal].QueueBehavior(DequeueCoordinatedBehavior, tempBehaviors[behaviorIndex], initiator, maxQueueLength);
                                    }
                                }
                                return;
                            }
                        }
                    }
                }
            }
            maxQueueLength++;
        }
        // When not enough animal or queue too long, go to next behavior
        animalsToExecutionData[initiator].pendingBehavior = null;
        animalsToExecutionData[initiator].cooperatingAnimals.Clear();
        OnBehaviorComplete(initiator);
    }
    private void OnDequeue(GameObject initiator)
    {
        animalsToExecutionData[initiator].avaliableAnimalCount++;
        if (animalsToExecutionData[initiator].avaliableAnimalCount == animalsToExecutionData[initiator].pendingBehavior.numberOfAnimalsRequired) //last animal ready will trigger the behavior
        {
            animalsToExecutionData[initiator].avaliableAnimalCount = 1;
            animalsToExecutionData[initiator].pendingBehavior.EnterBehavior(animalsToExecutionData[initiator].cooperatingAnimals);
            animalsToExecutionData[initiator].cooperatingAnimals.Clear();
        }
    }
    public void OnBehaviorComplete(GameObject animal)
    {
        if (!animalsToExecutionData.ContainsKey(animal))// Discriminate force exited callbacks from removing animals
        {
            return;
        }
        PopulationBehavior behavior = animalsToExecutionData[animal].NextBehavior(tempBehaviors, defaultBehavior);
        if (behavior == null)
        {
            return;
        }
        if (behavior.numberOfAnimalsRequired == 1)
        {
            List<GameObject> animals = new List<GameObject>();
            animals.Add(animal);
            behavior.EnterBehavior(animals);
            return;
        }
        QueueGroupBehavior(animal, animalsToExecutionData[animal].currentBehaviorIndex, behavior.numberOfAnimalsRequired);
    }
    public void RemoveAnimal(GameObject animal)
    {
        foreach (GameObject cooperatingAnimal in animalsToExecutionData[animal].cooperatingAnimals)
        {
            if (animal != cooperatingAnimal)
            {
                AnimalBehaviorManager behaviorManager = cooperatingAnimal.GetComponent<AnimalBehaviorManager>();
                if (behaviorManager.activeBehavior == null)
                {
                    OnBehaviorComplete(animal);
                    continue;
                }
            }
        }
        animalsToExecutionData.Remove(animal);
        animal.GetComponent<AnimalBehaviorManager>().ForceExit();
    }
    // If there's a bad condition behavior, initialize to that. Otherwise initialize to null.
    public void InitializeBehaviors(Dictionary<string, Need> _needs)
    {
        foreach (KeyValuePair<string, Need> needs in _needs)
        {
            this.ActiveBehaviors.Add(needs.Key, null);
        }
    }
}
