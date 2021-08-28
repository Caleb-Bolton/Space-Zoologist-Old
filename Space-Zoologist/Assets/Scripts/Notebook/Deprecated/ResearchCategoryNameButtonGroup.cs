using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResearchCategoryNameButtonGroup
{
    private Dictionary<string, ResearchCategoryNameButton> buttons = new Dictionary<string, ResearchCategoryNameButton>();

    // Index of the button previously selected before disabling
    private string previousSelection = null;

    public void AddButton(ResearchCategoryNameButton button)
    {
        buttons.Add(button.ResearchCategoryName, button);
    }
    public void SelectButton(string categoryName)
    {
        buttons[categoryName].MyToggle.isOn = true;
    }
    public void SetActive(bool active)
    {
        foreach(KeyValuePair<string, ResearchCategoryNameButton> kvp in buttons)
        {
            kvp.Value.gameObject.SetActive(active);
            // If this button is on, set previous selection
            if (kvp.Value.MyToggle.isOn) previousSelection = kvp.Value.ResearchCategoryName;
        }
        // The button that was previously selected will be on when active, and off when deactivating
        if(previousSelection != null) buttons[previousSelection].MyToggle.isOn = active;
    }
}
