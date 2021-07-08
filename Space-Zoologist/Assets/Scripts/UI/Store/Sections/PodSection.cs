﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PodSection : StoreSection
{
    [Header("Handled by Prefab")]
    [SerializeField] Transform PodItemContainer = default;
    [Header("Dependencies")]
    [SerializeField] PopulationManager populationManager = default;

    AnimalSpecies selectedSpecies = null;

    public override void Initialize()
    {
        base.itemType = ItemType.Pod;
        base.Initialize();
    }

    public override void OnCursorPointerUp(PointerEventData eventData)
    {
        // If in CursorItem mode and the cursor is clicked while over the menu
        if (IsCursorOverUI(eventData))
        {
            Debug.Log("Clicked over UI");
            base.OnItemSelectionCanceled();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            Vector2 position = Camera.main.ScreenToWorldPoint(eventData.position);
            selectedSpecies = base.GridSystem.PlacementValidation.GetAnimalSpecies(selectedItem);
            if (!this.GridSystem.PlacementValidation.IsPodPlacementValid(position, selectedSpecies))
            {
                Debug.Log("Can't place species there");
                return;
            }
            if (base.ResourceManager.CheckRemainingResource(selectedSpecies) <= 0)
            {
                base.OnItemSelectionCanceled();
                return;
            }
            populationManager.UpdatePopulation(selectedSpecies, position);
            base.ResourceManager.Placed(selectedSpecies, 1);
        }
        if (!base.CanBuy(selectedItem))
        {
            base.OnItemSelectionCanceled();
        }
    }
}