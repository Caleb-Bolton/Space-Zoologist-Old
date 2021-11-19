﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelNavigator : MonoBehaviour
{
    [SerializeField] SceneNavigator SceneNavigator = default;
    [SerializeField] LevelDataLoader LevelLoader = default;
    [SerializeField] GameObject LevelUIPrefab = default;
    [SerializeField] GameObject LevelContent = default;
    public List<GameObject> DisplayedLevels = default;
    private LevelMenuSelector currentLevel = default;
    List<string> passedLevels = new List<string>();

    public void Start()
    {
        passedLevels = GameManager.LoadGame();
        this.DisplayedLevels = new List<GameObject>();
        this.InitializeLevelDisplay();
        currentLevel = FindObjectOfType<LevelMenuSelector>();
    }

    private void InitializeLevelDisplay()
    {
        foreach (LevelData level in this.LevelLoader.levelDatas)
        {
            GameObject newLevel = Instantiate(LevelUIPrefab, LevelContent.transform);
            newLevel.GetComponent<LevelUI>().InitializeLevelUI(level.Level);
            newLevel.GetComponent<Button>().onClick.AddListener(() => {
                currentLevel.levelName = level.Level.SceneName;
                SceneNavigator.LoadLevel("MainLevel");
            });
            if (!passedLevels.Contains(level.Level.SceneName)) {
                newLevel.GetComponent<Button>().interactable = false;
            }
            this.DisplayedLevels.Add(newLevel);
        }
    }
}
