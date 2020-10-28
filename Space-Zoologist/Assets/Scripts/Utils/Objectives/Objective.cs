﻿using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveStatus { Completed, InProgress, Failed }

public abstract class Objective
{
    public abstract ObjectiveStatus Status { get; }
    public abstract ObjectiveStatus UpdateStatus();
    public abstract string GetObjectiveText();
}

public class SurvivalObjective : Objective
{
    public List<Population> Populations = default;
    public AnimalSpecies AnimalSpecies { get; private set; }
    public byte TargetPopulationCount { get; private set; }
    public byte TargetPopulationSize { get; private set; }
    public float TargetTime { get; private set; }

    public float timer { get; private set; }
    private ObjectiveStatus status;

    public override ObjectiveStatus Status => this.status;

    public SurvivalObjective(AnimalSpecies animalSpecies, byte targetPopulationCount, byte targetPopulationSize, float targetTime)
    {
        this.Populations = new List<Population>();
        this.AnimalSpecies = animalSpecies;
        this.TargetPopulationCount = targetPopulationCount;
        this.TargetPopulationSize = targetPopulationSize;
        this.TargetTime = targetTime;
        this.status = ObjectiveStatus.InProgress;
    }

    public override ObjectiveStatus UpdateStatus()
    {
        byte satisfiedPopulationCount = 0;

        foreach (Population population in this.Populations)
        {
            // Found a population that has enough pop count
            if (population.Count >= this.TargetPopulationSize)
            {
                satisfiedPopulationCount++;
            }

            // Have met the population number requirement
            if (satisfiedPopulationCount >= this.TargetPopulationCount)
            {

                if (this.timer >= this.TargetTime)
                {
                    this.status = ObjectiveStatus.Completed;
                    return ObjectiveStatus.Completed;
                }
                else
                {
                    this.timer += Time.deltaTime;
                }

                break;
            }
            // reset timer if requirement not met
            else
            {
                this.timer = 0f;
            }
        }
        this.status = ObjectiveStatus.InProgress;
        return ObjectiveStatus.InProgress;
    }

    public override string GetObjectiveText()
    {
        string displayText = "";
        string population = "population";
        string timeLabel = "minute";
        float targetTime = this.TargetTime / 60f;
        if (this.TargetPopulationCount > 1)
        {
            population += "s";
        }
        if (!(this.TargetTime <= 120f))
        {
            timeLabel += "s";
        }
        if (this.TargetTime < 60f)
        {
            targetTime = this.TargetTime;
            timeLabel = "seconds";
        }
        displayText += $"Maintain at least {this.TargetPopulationCount} ";
        displayText += $"{this.AnimalSpecies.SpeciesName} {population} with a count of {this.TargetPopulationSize}";
        displayText += $" for {targetTime} {timeLabel} ";
        displayText += $"[{this.Status.ToString()}] [{Math.Round(this.timer, 0)}/{this.TargetTime}]\n";

        return displayText;
    }
}

public class ResourceObjective : Objective
{
    private PlayerBalance playerBalance;
    public int amountToKeep { get; private set; }

    public override ObjectiveStatus Status => this.status;

    private ObjectiveStatus status;

    public ResourceObjective(PlayerBalance playerBalance, int amountToKeep)
    {
        this.playerBalance = playerBalance;
        this.amountToKeep = amountToKeep;
        this.status = ObjectiveStatus.InProgress;
    }

    public override ObjectiveStatus UpdateStatus()
    {
        if (this.playerBalance.Balance >= this.amountToKeep)
        {
            this.status = ObjectiveStatus.Completed;
        }
        else
        {
            this.status = ObjectiveStatus.Failed;
        }

        return this.status;
    }

    public override string GetObjectiveText()
    {
        return $"Have at least ${this.amountToKeep} left when level is complete [{this.status}]\n";
    }
}