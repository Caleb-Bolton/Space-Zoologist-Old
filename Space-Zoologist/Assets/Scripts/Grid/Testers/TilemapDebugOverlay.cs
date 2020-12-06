﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDebugOverlay : MonoBehaviour
{
    private bool DisplayLiquidBodyInfo;
    private bool DisplayPreviewBodies;
    private Tilemap[] tilemaps;
    private TileSystem tileSystem;
    private Vector2 scrollPosition1;
    private Vector2 scrollPosition2;
    private Dictionary<TileLayerManager, Dictionary<LiquidBody, bool>> ManagersToToggles = new Dictionary<TileLayerManager, Dictionary<LiquidBody, bool>>();
    private void Awake()
    {
        this.tilemaps = FindObjectsOfType<Tilemap>();
        this.tileSystem = FindObjectOfType<TileSystem>();
    }
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 200, 0, 200, 900));
        GUILayout.BeginVertical();
        GUILayout.Box("Grid Debugger");
        this.DisplayLiquidBodyInfo = GUILayout.Toggle(this.DisplayLiquidBodyInfo, "Liquid Body Info Display");
        if (this.DisplayLiquidBodyInfo)
        {
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.TryGetComponent(out TileLayerManager tileLayerManager))
                {
                    if (!tileLayerManager.holdsContent)
                    {
                        continue;
                    }
                    if (!this.ManagersToToggles.ContainsKey(tileLayerManager))
                    {
                        this.ManagersToToggles.Add(tileLayerManager, new Dictionary<LiquidBody, bool>());
                    }
                    GUILayout.Box("Tilemap: " + tilemap.name);
                    GUILayout.Box("Active Liquid Bodies: " + tileLayerManager.liquidBodies.Count.ToString());
                    this.scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1, GUILayout.Width(200), GUILayout.Height(300));
                    foreach (LiquidBody liquidBody in tileLayerManager.liquidBodies)
                    {
                        if (!this.ManagersToToggles[tileLayerManager].ContainsKey(liquidBody))
                        {
                            this.ManagersToToggles[tileLayerManager].Add(liquidBody, false);
                        }
                        GUILayout.Box("LiquidBodyID: " + liquidBody.bodyID);
                        GUILayout.Box("Composition");
                        for (int i = 0; i < liquidBody.contents.Length; i++)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Box(liquidBody.contents[i].ToString("n3"));
                            liquidBody.contents[i] = GUILayout.HorizontalSlider(liquidBody.contents[i], 0.0f, 1.0f);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Box("Tile Count: " + liquidBody.tiles.Count);
                        bool validCrossReference = true;
                        foreach (Vector3Int tile in liquidBody.tiles)
                        {
                            if (tileLayerManager.positionsToTileData[tile].currentLiquidBody != liquidBody)
                            {
                                validCrossReference = false;
                                break;
                            }
                        }
                        GUILayout.Box("Valid Cross Reference: " + validCrossReference.ToString());
                        if (GUILayout.Toggle(ManagersToToggles[tileLayerManager][liquidBody],"View Area"))
                        {
                            // View Area
                        }
                    }
                    GUILayout.EndScrollView();
                    int bodyCount = 0;
                    this.scrollPosition2 = GUILayout.BeginScrollView(scrollPosition1, GUILayout.Width(200), GUILayout.Height(300));
                    GUILayout.Box("Preview Bodies");
                    GUILayout.Toggle(this.DisplayPreviewBodies, "Display Preview Bodies");
                    foreach (LiquidBody liquidBody in tileLayerManager.previewBodies)
                    {
                        bodyCount++;
                        GUILayout.Box("Body No. " + bodyCount);
                        GUILayout.Box("Composition");
                        for (int i = 0; i < liquidBody.contents.Length; i++)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Box(liquidBody.contents[i].ToString("n3"));
                            liquidBody.contents[i] = GUILayout.HorizontalSlider(liquidBody.contents[i], 0.0f, 1.0f);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Box("Tile Count: " + liquidBody.tiles.Count);
                        bool validCrossReference = true;
                        foreach (Vector3Int tile in liquidBody.tiles)
                        {
                            if (tileLayerManager.positionsToTileData[tile].currentLiquidBody != liquidBody)
                            {
                                validCrossReference = false;
                                break;
                            }
                        }
                        GUILayout.Box("Valid Cross Reference" + validCrossReference.ToString());
                        if (this.DisplayPreviewBodies)
                        {
                            // View Area
                        }
                    }
                    GUILayout.EndScrollView();
                }
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
