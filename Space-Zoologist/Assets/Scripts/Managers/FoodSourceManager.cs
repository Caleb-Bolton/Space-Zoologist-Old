using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void CreateFoodCallback(FoodSource food);
/// <summary>
/// Manager of all the FoodSource instance
/// </summary>
public class FoodSourceManager : GridObjectManager
{
    public List<FoodSource> FoodSources => foodSources;
    private List<FoodSource> foodSources = new List<FoodSource>();
    private Dictionary<FoodSourceSpecies, List<FoodSource>> foodSourcesBySpecies = new Dictionary<FoodSourceSpecies, List<FoodSource>>();

    // FoodSourceSpecies to string name
    [SerializeField] private GameObject foodSourcePrefab = default;
    public Color constructionColor = new Color(0.5f, 1f, 0.5f, 1f);//Green

    public void Initialize()
    {
        // Get all FoodSource at start of level
        // TODO make use of saved tile
        GameObject[] foods = GameObject.FindGameObjectsWithTag("FoodSource");

        foreach (GameObject food in foods)
        {
            foodSources.Add(food.GetComponent<FoodSource>());
            Vector3Int GridPosition = GameManager.Instance.m_gridSystem.WorldToCell(food.transform.position);

            GameManager.Instance.m_gridSystem.GetTileData(GridPosition).Food = food;
        }

        // Register Foodsource with NeedSystem via NeedSystemManager
        foreach (FoodSource foodSource in foodSources)
        {
            if (!foodSourcesBySpecies.ContainsKey(foodSource.Species))
            {
                foodSourcesBySpecies.Add(foodSource.Species, new List<FoodSource>());
                foodSourcesBySpecies[foodSource.Species].Add(foodSource);
            }
            else {
                foodSourcesBySpecies[foodSource.Species].Add(foodSource);
            }
            
            ((FoodSourceNeedSystem)GameManager.Instance.NeedSystems[NeedType.FoodSource]).AddFoodSource(foodSource);
            GameManager.Instance.RegisterWithNeedSystems(foodSource);
            EventManager.Instance.InvokeEvent(EventType.NewFoodSource, foodSource);
        }
        //FoodPlacer.PlaceFood();
        this.Parse();
    }
    // TODO: combine two version into one
    public GameObject CreateFoodSource(FoodSourceSpecies species, Vector2 position, int ttb = -1)
    {
        GameObject newFoodSourceGameObject = Instantiate(foodSourcePrefab, position, Quaternion.identity, this.transform);
        newFoodSourceGameObject.name = species.SpeciesName;
        FoodSource foodSource = newFoodSourceGameObject.GetComponent<FoodSource>();
        foodSource.InitializeFoodSource(species, position);
        foodSources.Add(foodSource);
        Vector2 pos = position;
        if (species.Size % 2 == 0)
        {
            pos.x -= 1;
            pos.y -= 1;
        }
        GameManager.Instance.m_gridSystem.AddFood(GameManager.Instance.m_gridSystem.WorldToCell(pos), species.Size, newFoodSourceGameObject);
        GameManager.Instance.m_buildBufferManager.CreateSquareBuffer(new Vector2Int((int)pos.x, (int)pos.y), ttb, species.Size, this.constructionColor);
        if (ttb > 0)
        {
            foodSource.isUnderConstruction = true;
            GameManager.Instance.m_buildBufferManager.ConstructionFinishedCallback(() =>
            {
                foodSource.isUnderConstruction = false;
            });
        }

        if (!foodSourcesBySpecies.ContainsKey(foodSource.Species))
        {
            foodSourcesBySpecies.Add(foodSource.Species, new List<FoodSource>());
            foodSourcesBySpecies[foodSource.Species].Add(foodSource);
        }
        else
        {
            foodSourcesBySpecies[foodSource.Species].Add(foodSource);
        }

        //Debug.Log("Food source being added: " + foodSource.Species.SpeciesName);
        ((FoodSourceNeedSystem)GameManager.Instance.NeedSystems[NeedType.FoodSource]).AddFoodSource(foodSource);

        // Register with NeedSystemManager
        GameManager.Instance.RegisterWithNeedSystems(foodSource);

        EventManager.Instance.InvokeEvent(EventType.NewFoodSource, newFoodSourceGameObject.GetComponent<FoodSource>());

        return newFoodSourceGameObject;
    }

    public GameObject CreateFoodSource(string foodsourceSpeciesID, Vector2 position)
    {
        return CreateFoodSource(GameManager.Instance.FoodSources[foodsourceSpeciesID], position);
    }

    public void DestroyFoodSource(FoodSource foodSource) {
        foodSources.Remove(foodSource);
        ((FoodSourceNeedSystem)GameManager.Instance.NeedSystems[NeedType.FoodSource]).RemoveFoodSource(foodSource);
        foodSourcesBySpecies[foodSource.Species].Remove(foodSource);
        GameManager.Instance.UnregisterWithNeedSystems(foodSource);
        GameManager.Instance.m_gridSystem.RemoveFood(GameManager.Instance.m_gridSystem.WorldToCell(foodSource.gameObject.transform.position));
        Destroy(foodSource.gameObject);
    }

    /// <summary>
    /// Update accessible terrain info for all food sources,
    /// called when all NS updates are done
    /// </summary>
    public void UpdateAccessibleTerrainInfoForAll()
    {
        foreach (FoodSource foodSource in this.foodSources)
        {
            foodSource.UpdateAccessibleTerrainInfo();
        }
    }

    public string GetSpeciesID(FoodSourceSpecies species) {
        if (GameManager.Instance.FoodSources.ContainsValue(species)) {
            for (var pair = GameManager.Instance.FoodSources.GetEnumerator(); pair.MoveNext() != false;) {
                if (pair.Current.Value.Equals(species)) {
                    return pair.Current.Key;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get a list of Food Source with the given species name.
    /// </summary>
    /// <param name="speciesName">Same as FoodSourceSpecies.SpeciesName</param>
    /// <returns>An list of Food Source with the given species name</returns>
    public List<FoodSource> GetFoodSourcesWithSpecies(string speciesName) {
        // Given species doesn't exist in the level
        if (!GameManager.Instance.FoodSources.ContainsKey(speciesName))
        {
            Debug.Log("Food source not in level data");
            return null;
        } 
        FoodSourceSpecies species = GameManager.Instance.FoodSources[speciesName];

        // No food source of given species exist
        if (!foodSourcesBySpecies.ContainsKey(species))
        {
            return null;
        }
        else {
            return foodSourcesBySpecies[species];
        }
    }

    /// <summary>
    /// Get an array of tile positions of Food Source with the given species name.Used to bypass having access to TileSystem.
    /// </summary>
    /// <param name="speciesName">Same as FoodSourceSpecies.SpeciesName</param>
    /// <returns>An array of tile positions of Food Source with the given species name</returns>
    public Vector3Int[] GetFoodSourcesLocationWithSpecies(string speciesName) {
        List<FoodSource> foods = GetFoodSourcesWithSpecies(speciesName);
        if (foods == null) return null;
        Vector3Int[] locations = new Vector3Int[foods.Count];
        for (int i = 0; i < foods.Count; i++) {
            locations[i] = GameManager.Instance.m_gridSystem.WorldToCell(foods[i].transform.position);
        }
        //Debug.Log("Returned locations");
        return locations;
    }
    public Vector3[] GetFoodSourcesWorldLocationWithSpecies(string speciesName)
    {
        List<FoodSource> foods = GetFoodSourcesWithSpecies(speciesName);
        if (foods == null)
        {
            //Debug.Log("returned null");
            return null;
        }
        
        Vector3[] locations = new Vector3[foods.Count];
        for (int i = 0; i < foods.Count; i++)
        {
            locations[i] = foods[i].transform.position;
        }
        return locations;
    }
    public override void Serialize(SerializedMapObjects serializedMapObjects)
    {
        foreach (string speciesName in GameManager.Instance.FoodSources.Keys)
        {
            serializedMapObjects.AddType(this.MapObjectName, new GridItemSet(this.GetSpeciesID(GameManager.Instance.FoodSources[speciesName]), this.GetFoodSourcesWorldLocationWithSpecies(speciesName)));
        }
    }
    public override void Parse()
    {
        foreach (KeyValuePair<string, GridItemSet> keyValuePair in SerializedMapObjects)
        {
            if (keyValuePair.Key.Equals(this.MapObjectName))
            {
                foreach (Vector3 position in SerializationUtils.ParseVector3(keyValuePair.Value.coords))
                {
                    this.CreateFoodSource(keyValuePair.Value.name, position);
                }
            }
        }
    }
    protected override string GetMapObjectName()
    {
        // String used to identify serialized map objects being handled by this manager
        return "FoodSource";
    }

    /// <summary>
    /// Debug function to remove all food sources
    /// </summary>
    public void DestroyAll()
    {
        while (foodSources.Count > 0)
        {
            this.DestroyFoodSource(foodSources[foodSources.Count - 1]);
        }
    }

    public void placeFood(Vector3Int mouseGridPosition, FoodSourceSpecies species, int ttb = -1)
    {
        Vector3 FoodLocation = GameManager.Instance.m_gridSystem.Grid.CellToWorld(mouseGridPosition);
        FoodLocation.x += 1;
        FoodLocation.y += 1;
        if (species.Size % 2 == 1)
        {
            FoodLocation.x -= 0.5f;
            FoodLocation.y -= 0.5f;
        }
        CreateFoodSource(species, FoodLocation, ttb);
    }
}