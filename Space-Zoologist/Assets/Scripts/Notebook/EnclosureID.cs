using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The identity of a single enclosed area.
/// Holds the name of the scene it is in, as well as the id of the area
/// </summary>
[System.Serializable]
public struct EnclosureID : System.IComparable<EnclosureID>
{
    #region Public Properties
    public int LevelNumber => levelNumber;
    public int EnclosureNumber => enclosureNumber;
    #endregion

    #region Private Editor Data
    [SerializeField]
    [Tooltip("Name of the scene the enclosure was found in")]
    private int levelNumber;
    [SerializeField]
    [Tooltip("ID number of the enclosure")]
    private int enclosureNumber;
    #endregion

    #region Constructors
    public EnclosureID(int levelNumber, int enclosureNumber)
    {
        this.levelNumber = levelNumber;
        this.enclosureNumber = enclosureNumber;
    }
    #endregion

    #region Factory Methods
    public static EnclosureID FromSceneName(string name)
    {
        string levelPrefix = "Level";
        string enclosurePrefix = "E";

        // Search for the place in the string where the level number is
        int levelIndex = name.IndexOf(levelPrefix);
        if (levelIndex >= 0) levelIndex += levelPrefix.Length;
        // Search for the place in the string where the enclosure is identified
        int enclosureIndexSearchStart = levelIndex >= 0 && levelIndex < name.Length ? levelIndex : 0;
        int enclosureIndex = name.IndexOf(enclosurePrefix, enclosureIndexSearchStart);
        if (enclosureIndex >= 0) enclosureIndex += enclosurePrefix.Length;

        // Get the substrings where the numbers are
        string levelNumberString = string.Empty;
        string enclosureNumberString = string.Empty;

        if (levelIndex >= 0 && levelIndex < name.Length)
        {
            if (enclosureIndex >= 0 && enclosureIndex < name.Length)
            {
                levelNumberString = name.Substring(levelIndex, enclosureIndex - levelIndex - 1);
                enclosureNumberString = name.Substring(enclosureIndex);
            }
            else levelNumberString = name.Substring(levelIndex);
        }

        // Try to parse the level number string and enclosure number string
        if (!int.TryParse(levelNumberString, out int levelNumber))
        {
            levelNumber = -1;
        }
        if (!int.TryParse(enclosureNumberString, out int enclosureNumber))
        {
            enclosureNumber = 1;
        }

        // Return the enclosure ID
        return new EnclosureID(levelNumber, enclosureNumber);
    }

    public static EnclosureID FromSceneName(Scene scene)
    {
        return FromSceneName(scene.name);
    }

    // Parse the scene name to get the enclosure ID
    public static EnclosureID FromCurrentSceneName()
    {
        return FromSceneName(SceneManager.GetActiveScene().name);
    }
    #endregion

    #region Operators
    public static bool operator ==(EnclosureID a, EnclosureID b) => a.CompareTo(b) == 0;
    public static bool operator !=(EnclosureID a, EnclosureID b) => !(a == b);
    public static bool operator <(EnclosureID a, EnclosureID b) => a.CompareTo(b) < 0;
    public static bool operator >=(EnclosureID a, EnclosureID b) => !(a < b);
    public static bool operator >(EnclosureID a, EnclosureID b) => a.CompareTo(b) > 0;
    public static bool operator <=(EnclosureID a, EnclosureID b) => !(a > b);
    #endregion

    #region Object Overrides
    public override bool Equals(object other)
    {
        if (other == null) return false;
        else if (other.GetType() == typeof(EnclosureID)) return CompareTo((EnclosureID)other) == 0;
        else return false;
    }
    public override int GetHashCode()
    {
        return levelNumber.GetHashCode() + enclosureNumber.GetHashCode();
    }
    public override string ToString()
    {
        return "Enclosure ID: { " + levelNumber + ", " + enclosureNumber + " }";
    }
    #endregion

    #region Interface Implementation
    public int CompareTo(EnclosureID other)
    {
        int levelCompare = levelNumber.CompareTo(other.levelNumber);

        // If levels are equal, compare the enclosures
        if (levelCompare == 0) return enclosureNumber.CompareTo(other.enclosureNumber);
        // Otherwise return the result of the level compare
        else return levelCompare;
    }
    #endregion
}
