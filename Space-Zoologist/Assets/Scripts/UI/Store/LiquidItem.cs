using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/LiquidItem")]
public class LiquidItem : Item
{
    public Vector3 LiquidContents => liquidContents;

    [SerializeField] private Vector3 liquidContents = default;

    public new void SetupData(string name, int price)
    {
        base.SetupData(name, price);
        this.liquidContents = new Vector3(0.98f, 0, 0.02f);
    }

    public void SetupData(string name, int price, Vector3 liquidContents)
    {
        base.SetupData(name, price);
        this.liquidContents = liquidContents;
    }
}
