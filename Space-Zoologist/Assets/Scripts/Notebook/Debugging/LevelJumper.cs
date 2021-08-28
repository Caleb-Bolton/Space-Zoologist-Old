using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelJumper : MonoBehaviour
{
    public string levelPrefix = "NotebookTestLevel";
    public TMP_InputField levelInput;
    public TMP_InputField enclosureInput;
    public Button goButton;

    private void Awake()
    {
        goButton.onClick.AddListener(GoToLevel);
    }

    public void GoToLevel()
    {
        if (levelInput.text != string.Empty && enclosureInput.text != string.Empty)
        {
            int level = int.Parse(levelInput.text);
            int enclosure = int.Parse(enclosureInput.text);
            SceneManager.LoadScene(levelPrefix + level + "E" + enclosure);
        }
    }
}
