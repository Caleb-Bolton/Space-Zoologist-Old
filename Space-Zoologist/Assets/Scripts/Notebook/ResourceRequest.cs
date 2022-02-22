﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player's request for additional resources
/// they can use to sustain the enclosure
/// </summary>
[System.Serializable]
public class ResourceRequest
{
    #region Public Properties
    public ItemID ItemAddressed
    {
        get => itemAddressed;
        set => itemAddressed = value;
    }
    public NeedType NeedAddressed
    {
        get
        {
            // If the item requested is food, the need type addressed is food
            if (ItemRequested.Category == ItemRegistry.Category.Food) return NeedType.FoodSource;
            // If it's not food we assume it is a tile. Check if it is a water tile
            else if (ItemRequested.Data.Name.AnyNameContains("Water")) return NeedType.Liquid;
            // If it is not water it must be a tile to address terrain needs
            else return NeedType.Terrain;
        }
    }
    public int QuantityRequested
    {
        get => quantityRequested;
        set => quantityRequested = value;
    }
    public ItemID ItemRequested
    {
        get => itemRequested;
        set => itemRequested = value;
    }
    // This total mess of a property can't really be computed in any easier way
    // because animal species and food source species have no base class, and their
    // NeedConstructData only has the name of the need whereas we need whether it is
    // preferred too
    public int Usefulness
    {
        get
        {
            List<NeedConstructData> ConvertNeeds<T>(List<T> needs)
            {
                return needs.Cast<NeedConstructData>().ToList();
            }

            // Try to get the animal and food species
            ItemData itemAddressedData = ItemAddressed.Data;
            ItemData itemRequestedData = ItemRequested.Data;
            AnimalSpecies animalSpeciesAddressed = itemAddressedData.Species as AnimalSpecies;
            FoodSourceSpecies foodSpeciesAddressed = itemAddressedData.Species as FoodSourceSpecies;

            // Check if the addressed item is an animal or food species
            if (animalSpeciesAddressed != null || foodSpeciesAddressed != null)
            {
                List<NeedConstructData> speciesNeeds;

                // If the item addressed is an animal species,
                // convert their needs
                if (animalSpeciesAddressed)
                {
                    if (NeedAddressed == NeedType.Terrain)
                    {
                        speciesNeeds = ConvertNeeds(animalSpeciesAddressed.TerrainNeeds);
                    }
                    else if (NeedAddressed == NeedType.FoodSource)
                    {
                        speciesNeeds = ConvertNeeds(animalSpeciesAddressed.FoodNeeds);
                    }
                    else throw new System.NotImplementedException($"{nameof(QuizConversation)}: " +
                        $"Usefulness cannot be computed for need {NeedAddressed}");
                }
                // If the item addressed is a food species,
                // convert their needs
                else
                {
                    if (NeedAddressed == NeedType.Terrain)
                    {
                        speciesNeeds = ConvertNeeds(foodSpeciesAddressed.TerrainNeeds);
                    }
                    else throw new System.NotImplementedException($"{nameof(QuizConversation)}: " +
                        $"Usefulness cannot be computed for need {NeedAddressed}");
                }

                // Find a need with the same name as the item addressed
                NeedConstructData need = speciesNeeds
                    .Find(n => n.ID == itemRequested);

                if (need != null)
                {
                    // Preferred need is more useful than non preferred
                    if (need.IsPreferred) return 2;
                    else return 1;
                }
                // If no need was found then this item is not edible/traversible,
                // it is not useful at all
                else return 0;
            }
            else throw new System.ArgumentException($"{nameof(QuizConversation)}: " +
                $"Item addressed '{itemAddressedData.Name}' has no valid species associated with it, " +
                $"so we cannot compute the usefulness of a requested item for it");
        }
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Item that the player is trying to address by making the resource request")]
    private ItemID itemAddressed;
    [SerializeField]
    [Tooltip("Quantity of the resource requested")]
    private int quantityRequested;
    [SerializeField]
    [Tooltip("The item that is requested")]
    private ItemID itemRequested;
    #endregion

    #region Constructors
    public ResourceRequest() : this(new ItemID(ItemRegistry.Category.Species, 0), 1, new ItemID(ItemRegistry.Category.Food, 0)) { }
    public ResourceRequest(ResourceRequest other) : this(other.itemAddressed, other.quantityRequested, other.itemRequested) { }
    public ResourceRequest(ItemID itemAddressed, int quantityRequested, ItemID itemRequested)
    {
        this.itemAddressed = itemAddressed;
        this.quantityRequested = quantityRequested;
        this.itemRequested = itemRequested;
    }
    #endregion
}
