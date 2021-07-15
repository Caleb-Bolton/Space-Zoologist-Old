﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO create QueueChangeClass which gets invoked by Initialize and nextDay to calculate any changes
public class TimeSystem : MonoBehaviour
{
    [SerializeField] ReserveDraft ReserveDraft = default;
    [SerializeField] NeedSystemManager NeedSystemManager = default;
    [SerializeField] PopulationManager PopulationManager = default;
    [SerializeField] Inspector Inspector = default;
    [SerializeField] Text CurrentDayText = default;
    [SerializeField] BuildBufferManager buildBufferManager = default;
    private int currentDay = 1;
    private int maxDay = 20; //TODO implement max day?
    // Initialize next day event to listen to.
    public static NextDayEvent onNextDay = new NextDayEvent();

    private void Start()
    {
        UpdateDayText(currentDay);
    }

    public void nextDay()
    {
        this.ReserveDraft.loadDraft();
        // Recalculates need system values and should updates all populations needs
        foreach (Population population in this.PopulationManager.Populations)
        {
            population.HandleGrowth();
        }
        this.NeedSystemManager.UpdateAllSystems();
        this.PopulationManager.UpdateAccessibleLocations();
        foreach (Population population in this.PopulationManager.Populations)
        {
            population.UpdateGrowthConditions();
        }
        this.Inspector.UpdateCurrentDisplay();
        this.buildBufferManager.CountDown();
        UpdateDayText(++currentDay);

        // Fire next day event.
        onNextDay.Invoke();
    }

    private void UpdateDayText(int day)
    {
        CurrentDayText.text = "DAY " + day;
        if (maxDay > 0)
        {
            CurrentDayText.text += " / " + maxDay;
        }
    }

    // PUBLIC GETTER
    public int CurrentDay
    {
        get { return currentDay; }
    }
}
