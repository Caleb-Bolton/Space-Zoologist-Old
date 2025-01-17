using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Store section for tile items.
/// </summary>
/// Figure out how to handle case when more than one machine present after changes
public class TileStoreSection : StoreSection
{
    private EnclosureSystem EnclosureSystem = default;

    private float startingBalance;
    private int initialAmt;
    private bool isPlacing = false;
    private int numTilesPlaced = 0;
    private int prevTilesPlaced = 0;
    public override void Initialize()
    {
        EnclosureSystem = GameManager.Instance.m_enclosureSystem;
        base.itemType = ItemRegistry.Category.Tile;
        base.Initialize();
        //Debug.Assert(tilePlacementController != null);
    }

    /// <summary>
    /// Start tile placement preview.
    /// </summary>
    private void StartPlacing()
    {
        Debug.Log("Start placing");
        numTilesPlaced = 0;
        initialAmt = ResourceManager.CheckRemainingResource(selectedItem);
        isPlacing = true;
        startingBalance = GameManager.Instance.Balance;

        float[] contents = null;
        if(selectedItem is LiquidItem)
        {
            Vector3 liquidVector = ( (LiquidItem)selectedItem ).LiquidContents;
            contents = new float[] {liquidVector.x, liquidVector.y, liquidVector.z};
        }

        tilePlacementController.StartPreview(selectedItem.ID.Data.Tile, liquidContents: contents);
    }

    /// <summary>
    /// Stop tile placement preview and finalize changes to the grid.
    /// </summary>
    private void FinishPlacing()
    {
        //Debug.Log("Finish placing");
        isPlacing = false;
        foreach (Vector3Int pos in this.tilePlacementController.addedTiles)
        {
            if (tilePlacementController.godMode)
                GameManager.Instance.m_reservePartitionManager.UpdateAccessMapChangedAt(tilePlacementController.addedTiles.ToList<Vector3Int>());
            else
            {
                GridSystem.CreateUnitBuffer(new Vector2Int(pos.x, pos.y), this.selectedItem.buildTime, TileDataController.ConstructionCluster.ConstructionType.TILE);
            }
        }
        this.EnclosureSystem.UpdateEnclosedAreas();
        tilePlacementController.StopPreview();
        // NOTE: placing tiles no longer costs money, only requesting them does
        // GameManager.Instance.SetBalance(startingBalance - numTilesPlaced * selectedItem.Price);
        base.ResourceManager.Placed(selectedItem, numTilesPlaced);
    }

    /// <summary>
    /// Triggered by mouse down on the cursor item.
    /// </summary>
    public override void OnCursorPointerDown(PointerEventData eventData)
    {/*
        base.OnCursorPointerDown(eventData);
        if (!UIBlockerSettings.OperationIsAvailable("Build"))
        {
            base.OnItemSelectionCanceled();
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Left && !isPlacing)
        {
            this.StartPlacing();
        }*/
    }

    /// <summary>
    /// Triggered by mouse up on the cursor item.
    /// </summary>
    public override void OnCursorPointerUp(PointerEventData eventData)
    {/*
        base.OnCursorPointerUp(eventData);
        Debug.Log("IsPlacing " + isPlacing);
        if (eventData.button == PointerEventData.InputButton.Left && isPlacing)
        {
            FinishPlacing();
        }
        if (!base.CanBuy(selectedItem))
        {
            base.OnItemSelectionCanceled();
        }*/
    }

    public override void HandleCursor () {
        base.HandleCursor ();

        bool operationUnobstructed = UIBlockerSettings.OperationIsAvailable("Build");
        //print ("Place tiles...");
        if (Input.GetMouseButtonDown (0) && !isPlacing && operationUnobstructed) {
            this.StartPlacing ();
        }
        if (Input.GetMouseButtonUp (0) && isPlacing) {
            FinishPlacing ();
        }
    }

    /// <summary>
    /// Event when the item selection is canceled.
    /// </summary>
    public override void OnItemSelectionCanceled()
    {
        base.OnItemSelectionCanceled();
    }

    // For tile placement undoing the preview changes is tricky so just finish placement instead of cancelling
    protected override void OnManualCancel()
    {
        if(isPlacing)
            FinishPlacing();
        OnItemSelectionCanceled();
    }

    public override void Update()
    {
        base.Update();
        if (isPlacing)
        {
            numTilesPlaced = tilePlacementController.PlacedTileCount();
            if (prevTilesPlaced != numTilesPlaced)
            {
                base.HandleAudio();
                prevTilesPlaced = numTilesPlaced;
            }
            // NOTE: placing tiles no longer costs money
            // GameManager.Instance.SetBalance(startingBalance - numTilesPlaced * selectedItem.Price);
            if (/* GameManager.Instance.Balance < selectedItem.Price ||*/ initialAmt - numTilesPlaced == 0)
            {
                FinishPlacing();
                base.OnItemSelectionCanceled();
            }
        }
    }
}
