using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NeedTypeDropdown : NotebookUIChild
{
    [System.Serializable]
    public class NeedTypeEvent : UnityEvent<NeedType> { }

    public TMP_Dropdown Dropdown => dropdown;
    public NeedTypeEvent OnNeedTypeSelected => onNeedTypeSelected;
    public NeedType SelectedNeed => needs[dropdown.value];

    [SerializeField]
    [Tooltip("Reference to the dropdown used to select the need type")]
    private TMP_Dropdown dropdown;
    [SerializeField]
    [Tooltip("List of selectable needs in this dropdown")]
    private List<NeedType> needs;
    [SerializeField]
    [Tooltip("Event raised when the dropdown value changes")]
    private NeedTypeEvent onNeedTypeSelected;

    private const string suffix = " Need";

    public override void Setup()
    {
        // Clear any existing options
        dropdown.ClearOptions();

        // Create a dropdown option for each need
        foreach (NeedType need in needs)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(need.ToString() + suffix);
            dropdown.options.Add(option);
        }

        // Add a listener for the dropdown value change
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    public void Setup(NeedType[] needs)
    {
        // Setup the needs list
        this.needs = new List<NeedType>(needs);
        // Call the setup function
        Setup();
    }

    public void OnDropdownValueChanged(int value)
    {
        onNeedTypeSelected.Invoke(needs[value]);
    }
    public void SetDropdownValue(int value)
    {
        dropdown.value = value;
        dropdown.RefreshShownValue();
        OnDropdownValueChanged(value);
    }
    public void SetNeedTypeValue(NeedType need)
    {
        int index = dropdown.options.FindIndex(x => OptionDataToNeedType(x) == need);
        // If the need was found in the list that set it to that found index
        if(index >= 0)
        {
            dropdown.value = index;
            dropdown.RefreshShownValue();
            OnDropdownValueChanged(index);
        }
    }

    private NeedType OptionDataToNeedType(TMP_Dropdown.OptionData option)
    {
        int endIndex = option.text.IndexOf(suffix);
        string substring = option.text.Substring(0, endIndex);
        NeedType need = (NeedType)System.Enum.Parse(typeof(NeedType), substring);
        return need;
    }
}
