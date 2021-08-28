using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A runtime instance of a food source
/// </summary>
public class FoodSource : MonoBehaviour, Life
{
    public FoodSourceSpecies Species => species;
    public float FoodOutput => CalculateOutput();
    public Vector2 Position { get; private set; } = Vector2.zero;
    public bool terrainNeedMet = false;
    public bool waterNeedMet = false;

    public float TerrainRating => terrainRating;
    public float WaterRating => waterRating;

    public Dictionary<string, Need> Needs => needs;
    private Dictionary<string, Need> needs = new Dictionary<string, Need>();

    // For runtime instances of a food source
    [Expandable][SerializeField] private FoodSourceSpecies species = default;
    [SerializeField] private GridSystem gridSystem = default;


    public bool isUnderConstruction = false;
    private float terrainRating = 0f;
    private float waterRating = 0f;

    private int[] accessibleTerrian = new int[(int)TileType.TypesOfTiles];
    private bool hasAccessibilityChanged = default;
    private bool hasAccessibilityChecked = default;

    // To figure out if the output has changed, in order to invoke vent
    private float prevOutput = 0;

    private void Awake()
    {
        if (species)
        {
            InitializeFoodSource(species, transform.position, this.gridSystem);
        }
    }

    // Subscribe to events here
    private void Start()
    {
        // If the food has atmospheric need then subscribe to atmosphere changed event
        foreach (AtmosphereComponent atmosphereComponent in Enum.GetValues(typeof(AtmosphereComponent)))
        {
            if (this.needs.ContainsKey(atmosphereComponent.ToString()))
            {
                EventManager.Instance.SubscribeToEvent(EventType.AtmosphereChange, () =>
                {
                    this.hasAccessibilityChecked = true;
                    this.hasAccessibilityChanged = true;
                });
            }
        }
    }

    public void InitializeFoodSource(FoodSourceSpecies species, Vector2 position, GridSystem gridSystem)
    {
        this.species = species;
        this.Position = position;
        this.GetComponent<SpriteRenderer>().sprite = species.FoodSourceItem.Icon;
        this.InitializeNeedValues();
        this.gridSystem = gridSystem;
        this.accessibleTerrian = this.gridSystem.CountOfTilesInRange(Vector3Int.FloorToInt(this.Position), this.Species.Size);
    }

    private void InitializeNeedValues()
    {
        this.needs = this.species.SetupNeeds();
    }

    private float CalculateOutput()
    {
        CalculateTerrainNeed();
        CalculateWaterNeed();

        float output;

        if(waterNeedMet && terrainNeedMet)
        {
            output = species.BaseOutput * (1 + (waterRating + terrainRating)/2);
        }
        else
        {
            output = species.BaseOutput * (1 + Mathf.Min(waterRating, 0)) * (1 + Mathf.Min(terrainRating, 0));
        }

        return output;
    }

    public void CalculateTerrainNeed()
    {
        float totalNeededTiles = Mathf.Pow(species.Size, 2);
        float availablePreferredTiles = 0f;
        float availableSurvivableTiles = 0f;
        float totalTilesAvailable = 0f;

        foreach (KeyValuePair<string, Need> need in this.needs)
        {
            if (need.Value.NeedType.Equals(NeedType.Terrain))
            {
                if (need.Value.IsPreferred)
                {
                    availablePreferredTiles += need.Value.NeedValue;
                }
                else
                {
                    availableSurvivableTiles += need.Value.NeedValue;
                }
            }
        }

        totalTilesAvailable = availablePreferredTiles + availableSurvivableTiles;
        if (totalTilesAvailable >= totalNeededTiles)
        {
            terrainNeedMet = true;
            terrainRating = availablePreferredTiles / totalNeededTiles;
        }
        else
        {
            terrainNeedMet = false;
            terrainRating = (totalTilesAvailable - totalNeededTiles) / totalNeededTiles;
        }

        //Debug.Log(gameObject.name + " terrain Rating: " + terrainRating + ", preferred tiles: " + availablePreferredTiles + ", survivable tiles: " + availableSurvivableTiles);
    }

    public void CalculateWaterNeed()
    {
        if(!needs.ContainsKey("LiquidTiles"))
        {
            waterRating = 0;
            return;
        }

        LiquidNeed tileNeed = (LiquidNeed)needs["LiquidTiles"];

        LiquidNeed waterNeed = null;
        if(needs.ContainsKey("Water"))
            waterNeed = (LiquidNeed)needs["Water"];

        LiquidNeed saltNeed = null;
        if(needs.ContainsKey("Salt"))
            saltNeed = (LiquidNeed)needs["Salt"];

        LiquidNeed bacteriaNeed = null;
        if(needs.ContainsKey("Bacteria"))
            bacteriaNeed = (LiquidNeed)needs["Bacteria"];

        float waterSourceSize = tileNeed.NeedValue;
        float totalNeedWaterTiles = tileNeed.GetThreshold();
        float waterTilesUsed = Mathf.Min(waterSourceSize, totalNeedWaterTiles);

        if (waterTilesUsed >= totalNeedWaterTiles)
        {
            Debug.Log("Water need met");
            waterNeedMet = true;
            waterRating = 0;

            if(waterNeed != null)
            {
                float percentPureWater = waterNeed.NeedValue;
                float neededPureWaterThreshold = waterNeed.GetThreshold();
                waterRating += (percentPureWater - neededPureWaterThreshold) / (GrowthCalculator.maxFreshWaterTilePercent - neededPureWaterThreshold);
                Debug.Log("Pure water received: " + percentPureWater + " out of " + neededPureWaterThreshold);
            }

            if(saltNeed != null)
            {
                float percentSalt = saltNeed.NeedValue;
                float neededSaltThreshold = saltNeed.GetThreshold();
                waterRating += (percentSalt - neededSaltThreshold) / (GrowthCalculator.maxSaltTilePercent - neededSaltThreshold);
                Debug.Log("Salt received: " + percentSalt + " out of " + neededSaltThreshold);
            }

            if(bacteriaNeed != null)
            {
                float percentBacteria = bacteriaNeed.NeedValue;
                float neededBacteriaThreshold = bacteriaNeed.GetThreshold();
                waterRating += (percentBacteria - neededBacteriaThreshold) / (GrowthCalculator.maxBacteriaTilePercent - neededBacteriaThreshold);
                Debug.Log("Bacteria received: " + percentBacteria + " out of " + neededBacteriaThreshold);
            }
        }
        else
        {
            waterNeedMet = false;
            waterRating = (waterTilesUsed - totalNeedWaterTiles) / totalNeedWaterTiles;
        }

        Debug.Log(gameObject.name + " water Rating: " + waterRating + ", water source size: " + waterTilesUsed);
    }

    /// <summary>
    /// Update the given need of the population with the given value.
    /// </summary>
    /// <param name="need">The need to update</param>Z
    /// <param name="value">The need's new value</param>
    public void UpdateNeed(string need, float value)
    {
        Debug.Assert(this.needs.ContainsKey(need), $"{ species.SpeciesName } food source has no need { need }");
        this.needs[need].UpdateNeedValue(value);
        // Debug.Log($"The { species.SpeciesName } population { need } need has new value: {NeedsValues[need]}");
    }

    public Dictionary<string, Need> GetNeedValues()
    {
        return this.Needs;
    }

    public Vector3 GetPosition()
    {
        return this.gameObject.transform.position;
    }

    /// <summary>
    /// Checks accessible terrain info, ie terrain tile composition
    /// </summary>
    /// <remarks>
    /// Actual checking will only be done once per universal NS update loop,
    /// since terrain will not change during that time
    /// </remarks>
    /// <returns>True is accessible terrain had changed, false otherwise</returns>
    public bool GetAccessibilityStatus()
    {
        // No need to check if terrain was not modified
        if (!this.gridSystem.HasTerrainChanged)
        {
            return false;
        }

        // Return result if have checked
        if (this.hasAccessibilityChecked)
        {
            return this.hasAccessibilityChanged;
        }

        var preTerrain = this.accessibleTerrian;
        var curTerrain = this.gridSystem.CountOfTilesInRange(Vector3Int.FloorToInt(this.Position), this.Species.Size);

        // Accessible terrain had changed
        this.hasAccessibilityChecked = true;
        if(!preTerrain.SequenceEqual(curTerrain))
        {
            this.hasAccessibilityChanged = true;
        }

        return this.hasAccessibilityChanged;
    }

    /// <summary>
    /// Updates the accessible terrain info
    /// </summary>
    public void UpdateAccessibleTerrainInfo()
    {
        if (this.hasAccessibilityChanged)
        {
            this.accessibleTerrian = this.gridSystem.CountOfTilesInRange(Vector3Int.FloorToInt(this.Position), this.Species.Size);
        }

        // Reset flags
        this.hasAccessibilityChecked = false;
        this.hasAccessibilityChanged = false;
    }
}
