﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveObject : MonoBehaviour
{
    private GridSystem gridSystem = default;
    private FoodSourceManager foodSourceManager = default;
    [SerializeField] CursorItem cursorItem = default;
    [SerializeField] GameObject MoveButtonPrefab = default;
    [SerializeField] GameObject DeleteButtonPrefab = default;
    [SerializeField] FoodSourceStoreSection FoodSourceStoreSection = default;
    [SerializeField] TileStoreSection TileStoreSection = default;
    // do this better
    [SerializeField] Sprite LiquidSprite = default;

    Dictionary<string, Item> itemByID = new Dictionary<string, Item>();
    Item tempItem;
    private enum ItemType { NONE, FOOD, ANIMAL, TILE }

    GameObject objectToMove = null;
    GameObject MoveButton = null;
    GameObject DeleteButton = null;
    private ItemType movingItemType;
    Vector3Int previousLocation = default; // previous grid location
    Vector3 initialPos;
    Vector3 curPos;
    bool moving;
    int moveCost = 0;
    int sellBackCost = 0;

    const float MoveCost = 0.5f;
    const float SellBackRefund = 0.25f;
    const float FixedCost = 0;
    const float CostPerUnitSizeAnimal = 10;
    const float CostPerUnitSizeFood = 10;

    private GameObject tileToDelete;
    private Vector3Int initialTilePosition;
    private GameTile initialTile;
    private float[] initialTileContents;

    private void Start()
    {
        gridSystem = GameManager.Instance.m_gridSystem;
        foodSourceManager = GameManager.Instance.m_foodSourceManager;
        foreach (var itemData in GameManager.Instance.LevelData.itemQuantities) {
            // Primarily checks for liquids, which may have the same id. Liquids are handled by a separate function
            if(!itemByID.ContainsKey(itemData.itemObject.ID))
                itemByID.Add(itemData.itemObject.ID, itemData.itemObject);
        }

        tempItem = (Item)ScriptableObject.CreateInstance("Item");
        MoveButton = Instantiate(MoveButtonPrefab, this.transform);
        DeleteButton = Instantiate(DeleteButtonPrefab, this.transform);
        MoveButton.GetComponent<Button>().onClick.AddListener(StartMovement);
        DeleteButton.GetComponent<Button>().onClick.AddListener(RemoveSelectedGameObject);
        MoveButton.SetActive(false);
        DeleteButton.SetActive(false);
        Reset();

        movingItemType = ItemType.NONE;

        tileToDelete = GameObject.FindGameObjectWithTag("tiletodelete");
        if (!tileToDelete)
        {
            tileToDelete = new GameObject();
            tileToDelete.AddComponent<SpriteRenderer>();
            tileToDelete.tag = "tiletodelete";
        }
    }

    public void StartMovement()
    {
        moving = true;
        MoveButton.SetActive(false);
        DeleteButton.SetActive(false);

        if (movingItemType == ItemType.TILE)
            tileToDelete.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (gridSystem.IsDrafting)
        {
            if (!moving && Input.GetMouseButtonDown(0))
            {
                // currently has no cursorItem
                bool notPlacingItem = !cursorItem.IsOn;

                if (notPlacingItem)
                {
                    // Imported from Inspector.cs -- prevents selecting UI element
                    if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.layer == 5)
                    {
                        return;
                    }
                    else if (DeleteButton.activeSelf)
                    {
                        // The UI is initialized: reset it
                        Reset();
                    }

                    // Select the food or animal at mouse position
                    GameObject SelectedObject = SelectGameObjectAtMousePosition();
                    if (SelectedObject != null)
                        objectToMove = SelectedObject;
                }
            }

            if (objectToMove != null && !moving)
            {
                // Initialize UI
                if (!DeleteButton.activeSelf)
                {
                    if (objectToMove.name == "tile")
                    {
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(objectToMove.transform.position);
                        DeleteButton.GetComponentInChildren<Text>().text = $"${sellBackCost}";
                        DeleteButton.SetActive(true);
                        DeleteButton.transform.position = screenPos + new Vector3(50, 100, 0);
                    }
                    else
                    {
                        SetMoveUI();
                    }
                }
                else {
                    UpdateMoveUIPosition();
                }
            }

            if (objectToMove != null && moving)
            {
                // Preview placement
                GameObjectFollowMouse(objectToMove);
                HighlightGrid();

                // If trying to place
                if (Input.GetMouseButtonDown(0))
                {
                    // Imported from Inspector.cs -- prevents selecting UI element
                    if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.layer == 5)
                    {
                        return;
                    }

                    // Update animal location reference
                    this.gridSystem.UpdateAnimalCellGrid();
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    worldPos.z = 0;


                    switch (movingItemType)
                    {
                        case ItemType.ANIMAL:
                            TryPlaceAnimal(worldPos, objectToMove);
                            break;
                        case ItemType.FOOD:
                            TryPlaceFood(worldPos, objectToMove);
                            break;
                        case ItemType.TILE:
                            TryPlaceTile(worldPos, objectToMove);
                            break;
                        default:
                            break;
                    }

                    Reset();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (objectToMove != null) objectToMove.transform.position = initialPos;
                Reset();
            }
        }
        else if (DeleteButton.activeSelf)
        {
            Reset();
        }

    }

    private void Reset()
    {
        if (objectToMove?.name == "tile")
        {
            Destroy(objectToMove);
        }
        objectToMove = null;
        moving = false;
        MoveButton.SetActive(false);
        DeleteButton.SetActive(false);
        moveCost = 0;
        sellBackCost = 0;
        gridSystem.ClearHighlights();
    }
    // Set up UI for move and delete
    private void SetMoveUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(objectToMove.transform.position);
        MoveButton.SetActive(true);
        DeleteButton.SetActive(true);
        MoveButton.transform.position = screenPos + new Vector3(-50, 100, 0);
        DeleteButton.transform.position = screenPos + new Vector3(50, 100, 0);

        switch (movingItemType)
        {
            case ItemType.ANIMAL:
                int price = itemByID[objectToMove.GetComponent<Animal>().PopulationInfo.species.SpeciesName].Price;
                moveCost = Mathf.RoundToInt(MoveCost * price);
                sellBackCost = Mathf.RoundToInt(SellBackRefund * price);
                break;
            case ItemType.FOOD:
                FoodSourceSpecies species = objectToMove.GetComponent<FoodSource>().Species;
                price = itemByID[species.SpeciesName].Price;
                moveCost = Mathf.RoundToInt(MoveCost * price);
                sellBackCost = Mathf.RoundToInt(SellBackRefund * price);
                break;
            case ItemType.TILE:
                LevelData.ItemData tileItemData = GameManager.Instance.LevelData.itemQuantities.Find(x => x.itemObject.ID.ToLower().Equals(objectToMove.name));
                sellBackCost = Mathf.RoundToInt(SellBackRefund * tileItemData.itemObject.Price);
                break;
            default:
                break;
        }
        MoveButton.GetComponentInChildren<Text>().text = $"${moveCost}";
        DeleteButton.GetComponentInChildren<Text>().text = $"${sellBackCost}";
    }

    private void UpdateMoveUIPosition()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(objectToMove.transform.position);
        MoveButton.transform.position = screenPos + new Vector3(-50, 100, 0);
        DeleteButton.transform.position = screenPos + new Vector3(50, 100, 0);
    }

    // Find what the mouse clicked on
    private GameObject SelectGameObjectAtMousePosition()
    {
        // Update animal location reference
        this.gridSystem.UpdateAnimalCellGrid();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int pos = this.gridSystem.WorldToCell(worldPos);
        GridSystem.TileData tileData = gridSystem.GetTileData(pos);
        GameObject toMove = null;

        if (tileData == null)
        {
            return null;
        }

        if (tileData.Animal)
        {
            toMove = tileData.Animal;
            movingItemType = ItemType.ANIMAL;
            string ID = toMove.GetComponent<Animal>().PopulationInfo.Species.SpeciesName;
            tempItem.SetupData(ID, "Pod", ID, 0);
        }
        else if (tileData.Food)
        {
            toMove = tileData.Food;
            movingItemType = ItemType.FOOD;
            string ID = toMove.GetComponent<FoodSource>().Species.SpeciesName;
            tempItem.SetupData(ID, "Food", ID, 0);
        }
        else if (gridSystem.IsWithinGridBounds(pos))
        {
            if (gridSystem.IsConstructing(pos.x, pos.y))
            {
                tileToDelete.name = gridSystem.GetGameTileAt(pos).TileName;

                if (tileToDelete.name.Equals("liquid"))
                {
                    tileToDelete.GetComponent<SpriteRenderer>().sprite = LiquidSprite;
                    initialTileContents = gridSystem.GetTileData(pos).contents;
                }
                else
                {
                    tileToDelete.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.LevelData.itemQuantities.Find(x => x.itemObject.ItemName.ToLower().Equals(tileToDelete.name.ToLower())).itemObject.Icon;
                }

                movingItemType = ItemType.TILE;
                tileToDelete.transform.position = (Vector3)pos + new Vector3(0.5f, 0.5f, 0);
                toMove = tileToDelete;
                string ID = tileToDelete.name;
                tempItem.SetupData(ID, "Tile", ID, 0);

                initialTilePosition = pos;
                initialTile = gridSystem.GetTileData(pos).currentTile;
                initialTile.defaultContents = initialTileContents;
            }
        }

        if (toMove != null) initialPos = toMove.transform.position;
        return toMove;
    }

    public void RemoveSelectedGameObject()
    {
        switch (movingItemType)
        {
            case ItemType.ANIMAL:
                GameManager.Instance.AddToBalance(sellBackCost);
                objectToMove.GetComponent<Animal>().PopulationInfo.RemoveAnimal(objectToMove);
                break;
            case ItemType.FOOD:
                GameManager.Instance.AddToBalance(sellBackCost);
                FoodSource foodSource = objectToMove.GetComponent<FoodSource>();
                removeOriginalFood(foodSource);

	            // Selling items no longer return them to inventory
	            //Item foodItem = GameManager.Instance.LevelData.itemQuantities.Find(x => x.itemObject.ID.Equals(foodSource.Species.SpeciesName)).itemObject;

	            //if (!foodItem)
	            //    return;

	            //FoodSourceStoreSection.AddItemQuantity(foodItem);
                break;
            case ItemType.TILE:
                GameManager.Instance.AddToBalance(sellBackCost);
                GridSystem.TileData tileData = gridSystem.GetTileData(gridSystem.WorldToCell(objectToMove.transform.position));
                tileData.Revert();
                gridSystem.ApplyChangesToTilemapTexture(gridSystem.WorldToCell(objectToMove.transform.position));
                if (tileData.currentTile == null)
                    tileData.Clear();
                //gridSystem.RemoveTile(gridSystem.WorldToCell(objectToMove.transform.position));
                gridSystem.RemoveBuffer((Vector2Int)gridSystem.WorldToCell(objectToMove.transform.position));

                tileToDelete.SetActive(false);
                // Selling items no longer return them to inventory
                //LevelData.ItemData tileItemData = GameManager.Instance.LevelData.itemQuantities.Find(x => x.itemObject.ID.ToLower().Equals(objectToMove.name));

                //if (tileItemData == null)
                //    return;

                //TileStoreSection.AddItemQuantity(tileItemData.itemObject);
                break;
            default:
                break;
        }
        Reset();
    }

    private void GameObjectFollowMouse(GameObject toMove)
    {
        float z = toMove.transform.position.z;
        curPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        curPos.z = z;
        toMove.transform.position = curPos;
    }

    private void HighlightGrid()
    {
        curPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (!gridSystem.IsWithinGridBounds(curPos)) return;

        Vector3Int gridLocation = gridSystem.WorldToCell(curPos);
        if (this.gridSystem.IsOnWall(gridLocation)) return;

        // Different position: need to repaint
        if (gridLocation.x != previousLocation.x || gridLocation.y != previousLocation.y)
        {
            previousLocation = gridLocation;
            gridSystem.ClearHighlights();
            gridSystem.updateVisualPlacement(gridLocation, tempItem);
        }
    }

    private void TryPlaceAnimal(Vector3 worldPos, GameObject toMove)
    {
        Population population = toMove.GetComponent<Animal>().PopulationInfo;
        AnimalSpecies species = population.Species;

        float cost = moveCost;
        bool valid = gridSystem.IsPodPlacementValid(worldPos, species) && GameManager.Instance.Balance >= cost;

        // placement is valid and population did not already reach here
        if (valid && !GameManager.Instance.m_reservePartitionManager.CanAccess(population, worldPos) && gridSystem.IsPodPlacementValid(worldPos, species))
        {
            GameManager.Instance.m_populationManager.UpdatePopulation(species, worldPos);
            GameManager.Instance.SubtractFromBalance(cost);
            population.RemoveAnimal(toMove);
        }
        toMove.transform.position = worldPos; // always place animal back because animal movement will be handled by pop manager
    }

    private void TryPlaceFood(Vector3 worldPos, GameObject toMove)
    {
        FoodSource foodSource = toMove.GetComponent<FoodSource>();
        FoodSourceSpecies species = foodSource.Species;
        Vector3Int pos = this.gridSystem.WorldToCell(worldPos);
        Vector3Int initialGridPos = gridSystem.WorldToCell(initialPos) - new Vector3Int(foodSource.Species.Size.x / 2, foodSource.Species.Size.x / 2, 0);

        //If the player clicks on the food source's original position, don't bother with the mess below
        if(pos == initialGridPos)
        {
            toMove.transform.position = initialPos;
            return;
        }

        //Check if the food source is under construction and if so, grab its build progress
        GridSystem.ConstructionCluster cluster = this.gridSystem.GetConstructionClusterAtPosition(initialGridPos);
        int buildProgress = this.GetStoreItem(species).buildTime;
        if(cluster != null)
            buildProgress = cluster.currentDays;

        //Remove the food so it doesn't interfere with its own placement
        removeOriginalFood(foodSource);
        float cost = moveCost;
        bool valid = gridSystem.IsFoodPlacementValid(worldPos, null, species) && GameManager.Instance.Balance >= cost;
        
        if (valid) //If valid, place the food at the mouse destination
        {
            placeFood(pos, species);
            GameManager.Instance.SubtractFromBalance(cost);
        }
        else //Otherwise, place it back at the original position with the correct build progress
        {
            placeFood(initialGridPos, species, buildProgress);
            toMove.transform.position = initialPos;
        }
    }

    private void TryPlaceTile(Vector3 worldPos, GameObject toMove)
    {
        Vector3Int tilePos = gridSystem.WorldToCell(worldPos);

        if (gridSystem.GetTileData(tilePos).currentTile.type != initialTile.type)
        {
            // undo current progress on existing tile
            gridSystem.GetTileData(initialTilePosition).Revert();
            gridSystem.RemoveBuffer((Vector2Int)initialTilePosition);
            gridSystem.ApplyChangesToTilemapTexture(initialTilePosition);
            
            tileToDelete.SetActive(false);

            // add the new tile
            gridSystem.AddTile(tilePos, initialTile);
            gridSystem.CreateUnitBuffer((Vector2Int)tilePos, 1, GridSystem.ConstructionCluster.ConstructionType.TILE);
            gridSystem.ApplyChangesToTilemapTexture(tilePos);
        }
    }

    // placing food is more complicated due to grid
    public void placeFood(Vector3Int mouseGridPosition, FoodSourceSpecies species, int buildProgress = 0)
    {
        Vector3 FoodLocation = gridSystem.CellToWorld(mouseGridPosition); //equivalent since cell and world is 1:1, but in Vector3
        FoodLocation += new Vector3((float)species.Size.x / 2, (float)species.Size.y / 2, 0);

        GameObject Food = foodSourceManager.CreateFoodSource(species.SpeciesName, FoodLocation);
        FoodSource foodSource = Food.GetComponent<FoodSource>();

        gridSystem.AddFoodReferenceToTile(mouseGridPosition, species.Size, Food);
        
        if(buildProgress < this.GetStoreItem(species).buildTime) //If the food source has yet to fully construct, add a build buffer
        {
            foodSource.isUnderConstruction = true;
            GameManager.Instance.m_gridSystem.ConstructionFinishedCallback(() =>
            {
                foodSource.isUnderConstruction = false;
            });
            gridSystem.CreateRectangleBuffer(new Vector2Int(mouseGridPosition.x, mouseGridPosition.y), this.GetStoreItem(species).buildTime, species.Size,
                species.SpeciesName.Equals("Gold Space Maple") || species.SpeciesName.Equals("Space Maple") ? GridSystem.ConstructionCluster.ConstructionType.TREE : GridSystem.ConstructionCluster.ConstructionType.ONEFOOD, buildProgress);
        }
        else //Otherwise, make sure its needs are up to date
        {
            GameManager.Instance.UpdateAllNeedSystems();
            foodSource.CalculateTerrainNeed();
            foodSource.CalculateWaterNeed();
            GameManager.Instance.m_inspector.UpdateCurrentDisplay();
        }

    }
    private Item GetStoreItem(FoodSourceSpecies foodSourceSpecies)
    {
        string itemID = "";
        foreach (KeyValuePair<string, FoodSourceSpecies> nameToFoodSpecies in GameManager.Instance.FoodSources)
        {
            if (nameToFoodSpecies.Value == foodSourceSpecies)
            {
                itemID = nameToFoodSpecies.Key;
            }
        }
        return this.FoodSourceStoreSection.GetItemByID(itemID);
    }
    public void removeOriginalFood(FoodSource foodSource)
    {
        Vector3Int FoodLocation = gridSystem.WorldToCell(initialPos);
        GridSystem.TileData data = gridSystem.GetTileData(FoodLocation);
        gridSystem.RemoveFood(FoodLocation);
        foodSourceManager.DestroyFoodSource(foodSource); // Finds the lower left cell the food occupies
        Vector2Int shiftedPos = new Vector2Int(FoodLocation.x, FoodLocation.y) - foodSource.Species.Size / 2;
        gridSystem.RemoveBuffer(shiftedPos, foodSource.Species.Size.x, foodSource.Species.Size.y);
    }
}
