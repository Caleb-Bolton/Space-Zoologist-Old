using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script to create custome NPC dialogue
/// </summary>
public class NPCDialogueGenerator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogueTextMeshPro = default;
    [SerializeField] private ObjectiveManager objectiveManager = default;
    [SerializeField] private Selector selector = default;

    // Loader script to read in dialogue ouuputs
    [SerializeField] private DialogueSheetLoader dialgoueSheetLoader = default;

    private Dictionary<string, List<string>> dialogueTemplates = new Dictionary<string, List<string>>();

    private Dictionary<string, DialogueOptionData> specieseNeedDialogoues = new Dictionary<string, DialogueOptionData>();
    private Dictionary<string, DialogueOptionData> objectiveStatusDialogue = new Dictionary<string, DialogueOptionData>();

    private System.Random random = new System.Random();

    // Call the dialogue sheet loader to load the dialogues
    private void Start()
    {
        this.loadDialogueOutputDatas();
        this.loadDialogueTemplates();
    }

    private void loadDialogueOutputDatas()
    {
        this.specieseNeedDialogoues = this.dialgoueSheetLoader.LoadSpeciesNeedDialogue();
    }

    private void loadDialogueTemplates()
    {
        this.dialogueTemplates = new Dictionary<string, List<string>>();

        // Loading template about population status
        this.dialogueTemplates.Add("Population", new List<string>());
        this.dialogueTemplates["Population"] = this.dialgoueSheetLoader.LoadDialogueTemplates("Population");

        // Load template about objective status
        this.dialogueTemplates.Add("Objective", new List<string>());
        this.dialogueTemplates["Objective"] = this.dialgoueSheetLoader.LoadDialogueTemplates("Objective");
    }

    public void NPCTalksAboutObjectiveStatus()
    {
        // Pick a template
        string template = this.dialogueTemplates["Objectve"][this.random.Next(this.dialogueTemplates["Population"].Count)];

        this.dialogueTextMeshPro.text = this.fillObjectiveTemplate(template);
    }

    public void GetPopulationToTalkAbout()
    {
        this.selector.EnableSelection();
    }

    public void NPCTalksAboutPopulationStatus()
    {  
        string dialogue = "";

        if (this.selector.SelectedPopulation)
        {
            this.selector.UnenableSelection();

            // Pick a template
            string template = this.dialogueTemplates["Population"][this.random.Next(this.dialogueTemplates["Population"].Count)];

            dialogue = this.fillPopulationStatusTemplate(template, this.selector.SelectedPopulation);

            this.selector.UnenableSelection();

            //dialogue = $"{this.selector.SelectedPopulation} was selected";
        }
        else
        {
            dialogue = "You did NOT select a population!";
        }

        this.dialogueTextMeshPro.text = dialogue;
        //Debug.Log(dialogue);
    }

    public void NPCTalksAboutJournalProgress()
    {
        this.dialogueTextMeshPro.text = $"Displaying something about the journal progress";
    }

    // TODO: re-think on how to talk about objective
    // TODO: redo logic to match fillSpeciseNeedTemplate
    private string fillObjectiveTemplate(string template)
    {
        string dialogue = template;

        if (this.objectiveManager.IsGameOver)
        {
            List<string> parsed = new List<string>(dialogue.Split(' '));

            // Replace text with dialogue when seen keyworkds, remove '{' and '}' used for syntax.
            for (int i = 0; i < parsed.Count; i++)
            {
                if (parsed[i] == "MAIN-GOOD/BAD")
                {
                    
                }
                else if (parsed[i] == "MAIN-HINT")
                {

                }
                else if (parsed[i] == "{" || parsed[i] == "}")
                {
                    parsed.RemoveAt(i);
                }
            }

            return string.Join(" ", parsed);
        }

        return null;
    }

    private string fillPopulationStatusTemplate(string template, Population population)
    {
        string dialogue = template;

        List<string> parsed = new List<string>(dialogue.Split(' '));
        List<string> goodNeeds = new List<string>();
        List<string> badNeeds = new List<string>();

        // Compute a list of good needs and bad needs
        foreach (Need need in population.Needs.Values)
        {
            if (need.IsThresholdMet(need.NeedValue))
            {
                goodNeeds.Add(need.NeedName);
            }
            else
            {
                badNeeds.Add(need.NeedName);
            }
        }

        // Replace text with dialogue when seen keyworkds, remove '{' and '}' used for syntax.
        for (int i = 0; i < parsed.Count; i++)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(parsed[i], "^{POPULATION}"))
            {
                parsed[i] = parsed[i].Replace("{POPULATION}", population.species.SpeciesName);
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(parsed[i], "^{GOOD}"))
            {
                string text = "";

                if (goodNeeds.Count > 0)
                {
                    int index = goodNeeds.Count >= 1 ? this.random.Next(goodNeeds.Count) - 1 : 0;
                    int count = 0;

                    // Pick at least two randow good dialogue
                    // Unless there is only one 
                    while (index >= 0 && count < 2)
                    {
                        text += this.specieseNeedDialogoues[$"{population.species.SpeciesName}-{goodNeeds[index]}"].GetDialogue(DialogueOutputOption.Good);
                        index--;
                        count++;
                    }
                }
                
                // In case no dialogue found
                if (text == "")
                {
                    text = "I have nothing to say about this...";
                }

                parsed[i] = parsed[i].Replace("{GOOD}", text);
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(parsed[i], "^{BAD}"))
            {
                string text = "";

                if (badNeeds.Count > 0)
                {
                    int index = badNeeds.Count > 1 ? this.random.Next(badNeeds.Count) : 0;
                    int count = 0;

                    // Pick at least two randow good dialogue
                    // Unless there is only one
                    while (index >= 0 && count < 2)
                    {
                        text += this.specieseNeedDialogoues[$"{population.species.SpeciesName}-{badNeeds[index]}"].GetDialogue(DialogueOutputOption.Bad);
                        index--;
                        count++;
                    }
                }

                // In case no dialogue found
                if (text == "")
                {
                    text = "I have nothing to say about this...";
                }

                parsed[i] = parsed[i].Replace("{BAD}", text);
            }
            else if(badNeeds.Count > 0 && System.Text.RegularExpressions.Regex.IsMatch(parsed[i], "^{HINT}"))
            {
                string text = "";

                int index = badNeeds.Count > 1 ? this.random.Next(badNeeds.Count) : 0;
                int count = 0;

                // Pick at least two randow good dialogue
                // Unless there is only one
                while (index >= 0 && count < 2)
                {
                    text += this.specieseNeedDialogoues[$"{population.species.SpeciesName}-{badNeeds[index]}"].GetDialogue(DialogueOutputOption.Hint);
                    index--;
                    count++;
                }

                // In case no dialogue found
                if (text == "")
                {
                    text = "I have nothing to say about this...";
                }

                parsed[i] = parsed[i].Replace("{HINT}", text);
            }
        }

        return string.Join(" ", parsed);
    }
}
