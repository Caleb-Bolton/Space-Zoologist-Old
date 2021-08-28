using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Notebook/Notebook")]
public class NotebookModel : ScriptableObject
{
    public string Acronym => acronym;
    public ResearchModel Research => research;
    public TestAndMetricsModel TestAndMetrics => testAndMetrics;
    public ObservationModel Observations => observations;
    public List<Bookmark> Bookmarks { get; private set; } = new List<Bookmark>();
    public List<EnclosureID> EnclosureIDs => new List<EnclosureID>(requestData.Keys);

    [SerializeField]
    [Tooltip("Acronym that the player gets to spell out on the home page")]
    private string acronym = "ROCTM";
    [SerializeField]
    [Expandable]
    [Tooltip("Reference to the model holding all the player's research and info" +
        "about the different species, foods, and tiles")]
    private ResearchModel research;
    [SerializeField]
    [Tooltip("Player observation notes")]
    private ObservationModel observations;
    [SerializeField]
    [Tooltip("Test and metrics that the player has taken")]
    private TestAndMetricsModel testAndMetrics;

    // Notes on each character in the acronym
    private Dictionary<char, string> acronymNotes = new Dictionary<char, string>();
    // Map the resource requests to the enclosure it applies to
    private Dictionary<EnclosureID, ResourceRequestList> requestData = new Dictionary<EnclosureID, ResourceRequestList>();

    public void Setup()
    {
        // Foreach letter in the acronym, add an empty string to the dictionary
        foreach(char c in acronym)
        {
            if (!acronymNotes.ContainsKey(c)) acronymNotes.Add(c, "");
        }
        research.Setup();
    }

    public string ReadAcronymNote(char c) => acronymNotes[c];
    public void WriteAcronymNote(char c, string note) => acronymNotes[c] = note;

    // Return true/false if the notebook already has this bookmark
    public bool HasBookmark(Bookmark bookmark) => Bookmarks.Contains(bookmark);
    // Add the bookmark if the notebook doesn't already have it in the list
    public bool TryAddBookmark(Bookmark bookmark)
    {
        if (!Bookmarks.Contains(bookmark))
        {
            AddBookmark(bookmark);
            return true;
        }
        else return false;
    }
    private void AddBookmark(Bookmark bookmark)
    {
        Bookmarks.Add(bookmark);
    }
    public void RemoveBookmark(Bookmark bookmark)
    {
        Bookmarks.Remove(bookmark);
    }
    public void TryAddEnclosureID(EnclosureID id)
    {
        if (!requestData.ContainsKey(id))
        {
            requestData.Add(id, new ResourceRequestList());
        }
        observations.TryAddEnclosureID(id);
        testAndMetrics.TryAddEnclosureID(id);
    }
    public ResourceRequestList GetResourceRequestList(EnclosureID id) => requestData[id];
}
