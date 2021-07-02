﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO setup so can only be placed in liquid and only modifies the values in that liquid
public class LiquidMachine : Machine
{
    private TileSystem TileSystem = default;
    public override void Initialize()
    {
        this.TileSystem = FindObjectOfType<TileSystem>();
        base.Initialize();
    }
    // TODO Input handler
    private void OnMouseDown()
    {
        if (!this.machineHUDGO.activeSelf) this.OpenHUD();
    }

    public override void OpenHUD()
    {
        this.machineHUDGO.SetActive(!this.machineHUDGO.activeSelf);
        if (this.machineHUDGO.activeSelf)
        {
            GameTile tile = this.TileSystem.GetGameTileAt(this.position);
            this.machineHUD.Initialize(this);
        }
    }

    public void UpdateLiquid(float[] liquidComposition)
    {
        this.TileSystem.ChangeLiquidBodyComposition(this.position, liquidComposition);
    }
}
