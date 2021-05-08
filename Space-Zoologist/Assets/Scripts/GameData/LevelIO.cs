﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
public class LevelIO : MonoBehaviour
{
    private TilePlacementController tilePlacementController;
    [SerializeField] private string directory = "Assets/SaveData/";
    private string sceneName;
    private TileLayerManager[] tileLayerManagers;
    private PlotIO plotIO;
    private PopulationManager populationManager;
    // Start is called before the first frame update
    public void Awake()
    {
        this.tilePlacementController = this.gameObject.GetComponent<TilePlacementController>();
        this.sceneName = SceneManager.GetActiveScene().name;
        this.tileLayerManagers = GetComponentsInChildren<TileLayerManager>();
        this.plotIO = FindObjectOfType<PlotIO>();
        this.populationManager = FindObjectOfType<PopulationManager>();
        this.Load();
    }
    public void Save(string name = null)
    {
        name = name ?? this.sceneName;
        name = name + ".json";
        string fullPath = Path.Combine(Application.persistentDataPath, name);
        this.generateSaveFile(fullPath);
    }
    public void SaveAsPreset(string name = null)
    {
        name = name ?? this.sceneName;
        name = name + ".json";
        string fullPath = this.directory + name; // preset map
        this.generateSaveFile(fullPath);
    }
    private void generateSaveFile(string fullPath)
    {
        Debug.Log("Saving Grid to " + fullPath);
        if (File.Exists(fullPath))
        {
            Debug.Log("Overwriting file at " + fullPath);
        }

        SerializedLevel level = new SerializedLevel();
        try
        {
            // Serialize plot
            level.SetPlot(this.plotIO.SavePlot());
            // Serialize Animals
            level.SetPopulations(this.populationManager);
        }
        catch
        {
            Debug.LogError("Serialization error, NOT saved to protect existing saves");
            return;
        }
        this.WriteToFile(fullPath, level);
    }
    private void WriteToFile(string path, SerializedLevel level)
    {
        using (StreamWriter streamWriter = new StreamWriter(path))
        {
            streamWriter.Write(JsonUtility.ToJson(level));
        }
        Debug.Log("Grid Saved to: " + path);
    }
    public void Load(string name = null)
    {
        name = name ?? this.sceneName;
        string fullPath = Path.Combine(Application.persistentDataPath, name);
        this.LoadFromFile(fullPath);
    }
    public void LoadPreset(string name = null)
    {
        name = name ?? this.sceneName;
        string fullPath = this.directory + name + ".json";
        this.LoadFromFile(fullPath);
    }
    private void LoadFromFile(string fullPath)
    {
        SerializedLevel serializedLevel;
        try
        {
            serializedLevel = JsonUtility.FromJson<SerializedLevel>(File.ReadAllText(fullPath));
        }
        catch
        {
            Debug.LogWarning("No map save found for this scene, create a map using map designer or check your spelling");
            Debug.Log("Creating Empty level");
            serializedLevel = new SerializedLevel();
            serializedLevel.SetPlot(new SerializedPlot(new SerializedMapObjects(), this.CreateDefaultGrid()));
        }
        this.plotIO.LoadPlot(serializedLevel.serializedPlot);
        Debug.Log(serializedLevel.serializedPlot.serializedGrid);
        //Animals loaded after map to avoid path finding issues
        // TODO make dependencies clearer
        this.populationManager.Parse(serializedLevel.serializedPopulations);
    }
    public void Reload()
    {
        this.plotIO.Initialize();
    }
    private SerializedGrid CreateDefaultGrid()
    {
        // Reads a grid with a dirt tile at 1,1,0 to avoid flood fill bug (flood fill assumes a tile at 1,1,0)
        return JsonUtility.FromJson<SerializedGrid>(File.ReadAllText(this.directory+"_defaultGrid.json"));
    }
}
