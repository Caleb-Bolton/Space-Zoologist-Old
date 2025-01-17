using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager of miscellaneous events that occur during tutorials in the dialogue
/// It manages the freezing of the conversation until a directive is finished,
/// as well as assists in highlighting buttons that the player needs to press
/// to complete the tutorial
/// </summary>
public class TutorialPrompter : MonoBehaviour
{
    #region Private Properties
    private ConversationFreezingScheduler FreezingScheduler
    {
        get
        {
            if(!freezingScheduler)
            {
                GameObject schedulerObject = new GameObject("Conversation Freezing Scheduler");
                freezingScheduler = schedulerObject.AddComponent<ConversationFreezingScheduler>();
            }
            return freezingScheduler;
        }
    }
    private TutorialHighlightingScheduler HighlightingScheduler
    {
        get
        {
            if(!highlightingScheduler)
            {
                GameObject schedulerObject = new GameObject("Tutorial Highlighting Scheduler");
                highlightingScheduler = schedulerObject.AddComponent<TutorialHighlightingScheduler>();
                highlightingScheduler.Setup(highlightPrefab);
            }
            return highlightingScheduler;
        }
    }
    #endregion

    #region Private Editor Fields
    [SerializeField]
    [Tooltip("Prefab used to highlight different parts of the tutorial")]
    private TutorialHighlight highlightPrefab = null;
    #endregion

    #region Private Fields
    private ConversationFreezingScheduler freezingScheduler;
    private TutorialHighlightingScheduler highlightingScheduler;
    #endregion

    #region Public Methods
    public void ToggleUI(bool active)
    {
        GameManager.Instance.m_menuManager.ToggleUI(active);
    }

    public void ToggleUISingleButton(string buttonName)
    {
        GameManager.Instance.m_menuManager.ToggleUISingleButton(buttonName);
    }
    #endregion

    #region Prompt Callbacks
    public void FreezeUntilNotebookOpen()
    {
        HighlightingScheduler.SetHighlights(HighlightNotebookButton());
        FreezingScheduler.FreezeUntilConditionIsMet(() => GameManager.Instance.NotebookUI.IsOpen);
    }
    public void FreezeUntilResearchTabOpen()
    {
        FreezeUntilNotebookTabOpen(NotebookTab.Research);
    }
    public void FreezeUntilObserveTabOpen()
    {
        FreezeUntilNotebookTabOpen(NotebookTab.Observe);
    }
    public void FreezeUntilConceptTabOpen()
    {
        FreezeUntilNotebookTabOpen(NotebookTab.Concepts);
    }
    public void FreezeUntilTestAndMetricsTabOpen()
    {
        FreezeUntilNotebookTabOpen(NotebookTab.TestAndMetrics);
    }
    public void FreezeUntilReportsTabOpen()
    {
        FreezeUntilNotebookTabOpen(NotebookTab.Reports);
    }
    public void HighlightNotesNoFreeze () {
        HighlightingScheduler.SetHighlights (HighlightNotes ());
    }
    public void HighlightAnimalDropdownNoFreeze (string pickerNameFilter) {
        HighlightingScheduler.SetHighlights (HighlightItemPickerCategory (NotebookTab.Research, ItemRegistry.Category.Species, pickerNameFilter));
    }
    public void HighlightDropdownNoFreeze (string pickerNameFilter) {
        HighlightingScheduler.SetHighlights (HighlightDropdownUtility (GetDropdown (NotebookTab.Research, pickerNameFilter)));
    }
    // TODO: Fix this, it's just technical debt :P
    public void HighlightTestAndMetricsDropdownNoFreeze (string pickerNameFilter) {
        Transform entryEditor = GetEntryEditor (NotebookTab.TestAndMetrics).transform;
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => entryEditor.transform.GetChild (0).GetComponent<RectTransform> ()
        });
    }
    // Ick more tech debt
    public void HighlightConceptsDropdownNoFreeze (string pickerNameFilter) {
        RectTransform entryEditor = GetDropdown (NotebookTab.Concepts, pickerNameFilter).GetComponent<RectTransform> ();
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => entryEditor
        });
    }
    // More technical debt
    public void HighlightTestAndMetricContentNoFreeze() {
        Transform entryEditor = GetEntryEditor (NotebookTab.TestAndMetrics).transform;
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => entryEditor.transform.GetChild (1).GetComponent<RectTransform> ()
        });
    }
    // Even more
    public void HighlightConceptsToggle (string toggleNameFilter) {
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => GetToggle (NotebookTab.Concepts, toggleNameFilter).GetComponent<RectTransform> ()
        }); 
    }
    // MOre
    public void HighlightConceptsInputField (string fieldNameFilter) {
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => GetInputField (NotebookTab.Concepts, fieldNameFilter).GetComponent<RectTransform> ()
        }); 
    }
    // eVen MorE
    public void HighlightConceptsButton (string fieldNameFilter) {
        HighlightingScheduler.SetHighlights (new ConditionalHighlight () {
            predicate = () => true,
            target = () => GetButton (NotebookTab.Concepts, fieldNameFilter).GetComponent<RectTransform> ()
        });
    }
    public void FreezeUntilGoatTerrainHighlightAdd()
    {
        FreezeUntilHighlightPresent(
            new ItemID(ItemRegistry.Category.Species, 0), 
            0,
            new TextHighlight(195, 336));
    }
    public void FreezeUntilGoatFirstHighlightRemove()
    {
        FreezeUntilHighlightAbsent(
            new ItemID(ItemRegistry.Category.Species, 0), 
            0,
            new TextHighlight(0, 127));
    }
    public void FreezeUntilCowPicked(string pickerNameFilter)
    {
        FreezeUntilNotebookItemPicked(NotebookTab.Research, new ItemID(ItemRegistry.Category.Species, 1), pickerNameFilter);
    }
    public void FreezeUntilSlugPicked(string pickerNameFilter)
    {
        FreezeUntilNotebookItemPicked(NotebookTab.Research, new ItemID(ItemRegistry.Category.Species, 4), pickerNameFilter);
    }
    public void FreezeUntilTreePicked (string pickerNameFilter) {
        FreezeUntilNotebookItemPicked (NotebookTab.Research, new ItemID (ItemRegistry.Category.Food, 0), pickerNameFilter);
    }
    public void FreezeUntilSecondArticlePicked (string articleDropdownNameFilter) {
        FreezeUntilArticlePicked (NotebookTab.Research, 1, articleDropdownNameFilter);
    }
    public void FreezeUntilInputFieldHas4CharactersTyped (string inputFieldNameFilter) {
        ConditionalHighlight highlight = HighlightInputField (NotebookTab.Research, inputFieldNameFilter, 4);
        FreezingScheduler.FreezeUntilConditionIsMet (() => !highlight.predicate());
        HighlightingScheduler.SetHighlights (highlight);
    }
    public void FreezeUntilBuildUIOpen()
    {
        FreezingScheduler.FreezeUntilConditionIsMet(() => GameManager.Instance.m_menuManager.IsInStore);
        HighlightingScheduler.SetHighlights(HighlightBuildButton(true));
    }
    public void FreezeUntilBuildUIClosed()
    {
        FreezingScheduler.FreezeUntilConditionIsMet(() => !GameManager.Instance.m_menuManager.IsInStore);
        HighlightingScheduler.SetHighlights(HighlightBuildButton(false));
    }
    public void FreezeUntilZeigPickedForPlacement()
    {
        FreezeUntilBuildItemPicked<PodStoreSection>(new ItemID(ItemRegistry.Category.Species, 0), 2);
    }
    public void FreezeUntilZeigsExist(int numZeigs)
    {
        ItemID goatID = ItemRegistry.FindHasName("Goat");
        FreezingScheduler.FreezeUntilConditionIsMet(() => PopulationExists(goatID, numZeigs));
    }
    public void FreezeUntilZeigPopulationIncrease()
    {
        // Find the next day button
        RectTransform nextDay = FindRectTransform("NextDay");

        // Get the current population
        PopulationManager populationManager = GameManager.Instance.m_populationManager;
        ItemID goatID = ItemRegistry.FindHasName("Goat");
        int currentGoats = populationManager.TotalPopulationSize(goatID.Data.Species as AnimalSpecies);

        // Population increased if current population exceeds population computed just now
        bool PopulationIncrease()
        {
            return populationManager.TotalPopulationSize(goatID.Data.Species as AnimalSpecies) > currentGoats;
        }

        FreezingScheduler.FreezeUntilConditionIsMet(PopulationIncrease);
        HighlightingScheduler.SetHighlights(new ConditionalHighlight()
        {
            predicate = PopulationIncrease,
            invert = true,
            target = () => nextDay
        });
    }
    public void FreezeUntilInspectorOpened()
    {
        FreezingScheduler.FreezeUntilConditionIsMet(() => GameManager.Instance.m_inspector.IsInInspectorMode);
        HighlightingScheduler.SetHighlights(HighlightInspectorButton());
    }
    public void FreezeUntilGoatIsInspected()
    {
        FreezeUntilPopulationIsInspected(new ItemID (ItemRegistry.Category.Species, 0));
    }
    public void FreezeUntilMapleIsInspected () {
        FreezeUntilFoodIsInspected (new ItemID (ItemRegistry.Category.Food, 0));
    }
    public void FreezeUntilWaterIsInspected () {
        FreezingScheduler.FreezeUntilConditionIsMet (() => WaterIsInspected ());
    }
    public void FreezeUntilSpaceMapleRequested(int quantity)
    {
        FreezeUntilResourceRequestSubmitted(new ItemID(ItemRegistry.Category.Food, 0), quantity);
    }
    public void FreezeUntilGoatPlacedMaplePlacedAndDaysAdvanced()
    {
        ItemID goatID = ItemRegistry.FindHasName("Goat");
        bool GoatsExist() => PopulationExists(goatID, 3);
        bool GoatsDontExist() => !GoatsExist();
        bool MaplesExist() => FoodSourceExists("Space Maple", 2);
        bool MaplesDontExist() => !MaplesExist();

        FreezingScheduler.FreezeUntilConditionIsMet(() =>
        {
            return GoatsExist() && 
                MaplesExist() && 
                GameManager.Instance.CurrentDay >= 3 && 
                PopulationIsInspected(goatID);
        });
        HighlightingScheduler.SetHighlights(
            // Prompt player to put down some goats
            HighlightBuildButton(true),
            HighlightBuildSectionPicker(2),
            HighlightBuildItem<PodStoreSection>(new ItemID(ItemRegistry.Category.Species, 0)),
            ConditionalHighlight.NoTarget(GoatsDontExist),

            // Prompt player to put down some maples
            HighlightBuildSectionPicker(0),
            HighlightBuildItem<FoodSourceStoreSection>(new ItemID(ItemRegistry.Category.Food, 0)),
            ConditionalHighlight.NoTarget(MaplesDontExist),
           
            // Prompt player to advance a few days
            HighlightBuildButton(false),
            HighlightNextDayButton(() => GameManager.Instance.CurrentDay >= 3),
            
            // Prompt player to use the inspector to inspect the goats
            HighlightInspectorButton());
    }
    public void FreezeUntilConceptCanvasExpanded(bool expanded)
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ConceptsCanvasUI canvas = notebook.GetComponentInChildren<ConceptsCanvasUI>(true);

        FreezingScheduler.FreezeUntilConditionIsMet(() => NotebookTabIsOpen(NotebookTab.Concepts) && canvas.IsExpanded == expanded);
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(),
            HighlightNotebookTabButton(NotebookTab.Concepts),
            new ConditionalHighlight()
            {
                predicate = () => canvas.IsExpanded != expanded,
                target = () => canvas.FoldoutToggle.transform as RectTransform
            });
    }
    public void HighlightDropdownConceptsTutorial()
    {
        // Doesn't freeze
        // Grab a bunch of references to various scripts in the Notebook
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ConceptsUI concepts = notebook.GetComponentInChildren<ConceptsUI>(true);
        ResourceRequestEditor requestEditor = notebook.ResourceRequestEditor;
        ReviewedResourceRequestDisplay reviewDisplay = notebook.GetComponentInChildren<ReviewedResourceRequestDisplay>(true);
        ItemDropdown itemReqDropdown = requestEditor.ItemRequestedDropdown;
        RectTransform itemDropdownTransform = itemReqDropdown.GetComponent<RectTransform>();

        HighlightingScheduler.SetHighlights(
            new ConditionalHighlight()
            {
                predicate = () => !itemReqDropdown.Dropdown.IsExpanded,
                target = () => itemDropdownTransform
            });
    }
    public void ClearHighlights()
    {
        HighlightingScheduler.ClearHighlights();
    }
    #endregion

    #region FreezeUntil Helpers
    private void FreezeUntilNotebookTabOpen(NotebookTab tab)
    {
        // Freeze until notebook tab is open
        FreezingScheduler.FreezeUntilConditionIsMet(() => NotebookTabIsOpen(tab));

        // Highlight notebook button if not open, then highlight correct tab
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(), HighlightNotebookTabButton(tab));
    }
    private void FreezeUntilHighlightPresent(ItemID item, int articleIndex, TextHighlight targetHighlight)
    {
        // Cache some useful values
        NotebookUI notebook = GameManager.Instance.NotebookUI;

        // Get the list of all highlights in this encyclopedia article
        List<TextHighlight> highlights = notebook
            .Data.Research.GetEntry(item)
            .GetArticleData(articleIndex).Highlights;

        FreezingScheduler.FreezeUntilConditionIsMet(() =>
        {
            // Get index of a highlight that this highlight contains
            int indexOfMatch = highlights.FindIndex(current => current.Contains(targetHighlight));
            // Freeze until highlight is found and notebook is open to research tab
            return indexOfMatch >= 0 && notebook.IsOpen && notebook.TabPicker.CurrentTab == NotebookTab.Research;
        });
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(), 
            HighlightNotebookTabButton(NotebookTab.Research), 
            HighlightHighlightButton());
    }
    private void FreezeUntilHighlightAbsent(ItemID item, int articleIndex, TextHighlight targetHighlight)
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ResearchEncyclopediaArticleInputField inputField = notebook.GetComponentInChildren<ResearchEncyclopediaArticleInputField>(true);

        // Get the list of all highlights in this encyclopedia article
        List<TextHighlight> highlights = notebook
            .Data.Research.GetEntry(item)
            .GetArticleData(articleIndex).Highlights;

        FreezingScheduler.FreezeUntilConditionIsMet(() =>
        {
            // Get an index of any highlight that overlaps the target highlight
            int indexOfMatch = highlights.FindIndex(current => targetHighlight.Overlap(current));
            // Freeze until no overlapping highlights found and notebook is open to research tab
            return indexOfMatch < 0 && notebook.IsOpen && notebook.TabPicker.CurrentTab == NotebookTab.Research;
        });
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(), 
            HighlightNotebookTabButton(NotebookTab.Research), 
            HighlightEraseButton());
    }
    private void FreezeUntilNotebookItemPicked(NotebookTab targetTab, ItemID targetItem, string nameFilter)
    {
        FreezingScheduler.FreezeUntilConditionIsMet(() => NotebookItemPicked(targetTab, targetItem, nameFilter));

        ConditionalHighlight [] itemPickerHighlights = HighlightItemPicker (GetItemPicker (targetTab, nameFilter), targetItem);
        // Highlight notebook button if not open, then highlight correct tab
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(),
            // Highlight the correct tab
            HighlightNotebookTabButton(targetTab),
            // Highlight the dropdown in the picker
            itemPickerHighlights [0],
            itemPickerHighlights [1]);
    }
    private void FreezeUntilArticlePicked (NotebookTab targetTab, int targetArticleIndex, string nameFilter) {
        FreezingScheduler.FreezeUntilConditionIsMet (() => ArticlePicked (targetTab, targetArticleIndex, nameFilter));

        ConditionalHighlight [] itemPickerHighlights = HighlightArticle (GetDropdown (targetTab, nameFilter), targetArticleIndex);
        // Highlight notebook button if not open, then highlight correct tab
        HighlightingScheduler.SetHighlights (HighlightNotebookButton (),
            // Highlight the correct tab
            HighlightNotebookTabButton (targetTab),
            // Highlight the dropdown in the picker
            itemPickerHighlights [0],
            itemPickerHighlights [1]);
    }
    private void FreezeUntilBuildItemPicked<StoreSectionType>(ItemID targetItem, int storeSectionIndex) 
        where StoreSectionType : StoreSection
    {
        // Freeze until the menu is in store, the store section is picked and the item selected is the target item
        FreezingScheduler.FreezeUntilConditionIsMet(() => BuildItemIsPicked<StoreSectionType>(targetItem, storeSectionIndex));

        // Set the highlights for the build button, the section picker, and the item in that section
        HighlightingScheduler.SetHighlights(HighlightBuildButton(true),
            HighlightBuildSectionPicker(storeSectionIndex),
            HighlightBuildItem<StoreSectionType>(targetItem));
    }
    private void FreezeUntilPopulationIsInspected(ItemID targetSpecies)
    {
        FreezingScheduler.FreezeUntilConditionIsMet(() => PopulationIsInspected(targetSpecies));
        HighlightingScheduler.SetHighlights(HighlightInspectorButton());
    }
    private void FreezeUntilFoodIsInspected (ItemID targetFood) {
        FreezingScheduler.FreezeUntilConditionIsMet (() => FoodIsInspected (targetFood));
    }
    private void FreezeUntilResourceRequestSubmitted(ItemID requestedItem, int requestQuantity)
    {
        //ConditionalHighlight[] resourceRequestDropdownHighlights = HighlightResourceRequestDropdown(requestedItem, requestQuantity);
        // Grab a bunch of references to various scripts in the Notebook
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ConceptsUI concepts = notebook.GetComponentInChildren<ConceptsUI>(true);
        ResourceRequestEditor requestEditor = notebook.ResourceRequestEditor;
        ReviewedResourceRequestDisplay reviewDisplay = notebook.GetComponentInChildren<ReviewedResourceRequestDisplay>(true);
        TMP_InputField quantityInput = requestEditor.QuantityInput;
        ItemDropdown itemReqDropdown = requestEditor.ItemRequestedDropdown;

        // Freeze conversation until correct review was confirmed
        FreezingScheduler.FreezeUntilConditionIsMet(() => ResourceRequestWasSubmitted(requestedItem, requestQuantity), HighlightingScheduler.ClearHighlights);
        HighlightingScheduler.SetHighlights(HighlightNotebookButton(),
            HighlightNotebookTabButton(NotebookTab.Concepts),
            new ConditionalHighlight()
            {
                predicate = () => !itemReqDropdown.Dropdown.IsExpanded && quantityInput.text != requestQuantity.ToString(),
                target = () => quantityInput.transform as RectTransform
            },
            // Highlight the request button only after the correct quantity is entered
            new ConditionalHighlight()
            {
                predicate = () => quantityInput.text == requestQuantity.ToString() && !reviewDisplay.gameObject.activeInHierarchy,
                target = () => concepts.RequestButton.transform as RectTransform
            }
            // COMMENTED OUT CODE IS RELEVANT TO HIGHLIGHTING RESOURCES THAT ARE REQUESTED THAT AREN'T CURRENTLY VISIBLE IN THE DROPDOWN
            //resourceRequestDropdownHighlights[0],
            //resourceRequestDropdownHighlights[1],
            //resourceRequestDropdownHighlights[2],
            //resourceRequestDropdownHighlights[3]
            // HighlightInputField(quantityInput, requestQuantity.ToString()),
            //new ConditionalHighlight()
            //{
            //    predicate = () => !reviewDisplay.gameObject.activeInHierarchy,
            //    target = () => concepts.RequestButton.transform as RectTransform
            //}
            );
    }
    #endregion

    #region Freezing Conditions
    private bool NotebookItemPicked(NotebookTab targetTab, ItemID targetItem, string nameFilter)
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ItemPicker picker = GetItemPicker (targetTab, nameFilter);

        return picker.SelectedItem == targetItem && notebook.IsOpen && notebook.TabPicker.CurrentTab == targetTab;
    }
    private bool ArticlePicked (NotebookTab targetTab, int targetArticleIndex, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        TMP_Dropdown selector = GetDropdown (targetTab, nameFilter);

        return selector.value == targetArticleIndex && notebook.IsOpen && notebook.TabPicker.CurrentTab == targetTab;
    }
    private bool BuildItemIsPicked<StoreSectionType>(ItemID targetItem, int storeSectionIndex)
        where StoreSectionType : StoreSection
    {
        MenuManager menuManager = GameManager.Instance.m_menuManager;
        BuildUI buildUI = GameManager.Instance.BuildUI;
        // Get the store section that manages the placement of the target item
        StoreSectionType storeSection = buildUI.GetComponentInChildren<StoreSectionType>(true);

        // Freeze until the menu is in store, the store section is picked and the item selected is the target item
        if (storeSection.SelectedItem != null)
        {
            return menuManager.IsInStore && buildUI.StoreSectionIndexPicker.FirstValuePicked == storeSectionIndex && storeSection.SelectedItem.ID == targetItem;
        }
        else return false;
    }
    private bool PopulationIsInspected(ItemID species)
    {
        Inspector inspector = GameManager.Instance.m_inspector;

        // Get the population highlighted
        if (!inspector.PopulationHighlighted) return false;

        // Get the population script on the object highlighted
        Population population = inspector.PopulationHighlighted.GetComponent<Population>();

        // If you got the script then check the species type highlighted
        if (!population) return false;

        return population.Species.ID == species;
    }
    private bool FoodIsInspected (ItemID food) {
        Inspector inspector = GameManager.Instance.m_inspector;

        TileData tileData = GameManager.Instance.m_tileDataController.GetTileData (inspector.selectedPosition);

        if (!tileData.Food) return false;

        return tileData.Food.GetComponent<FoodSource>().Species.ID == food;
    }
    private bool WaterIsInspected () {
        Inspector inspector = GameManager.Instance.m_inspector;

        TileData tileData = GameManager.Instance.m_tileDataController.GetTileData (inspector.selectedPosition);

        if (!tileData.currentTile) return false;

        return tileData.currentTile.type == TileType.Liquid;
    }
    private bool PopulationExists(ItemID species, int amount)
    {
        PopulationManager populationManager = GameManager.Instance.m_populationManager;
        return populationManager.TotalPopulationSize(species.Data.Species as AnimalSpecies) >= amount;
    }
    private bool FoodSourceExists(string speciesName, int amount)
    {
        FoodSourceManager foodManager = GameManager.Instance.m_foodSourceManager;
        int totalFood = foodManager.GetFoodSourcesWithSpecies(speciesName).Count;
        return totalFood >= amount;
    }
    private bool ResourceRequestWasSubmitted(ItemID requestedItem, int requestQuantity)
    {
        // Grab a bunch of references to various scripts in the Notebook
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ReviewedResourceRequestDisplay reviewDisplay = notebook.GetComponentInChildren<ReviewedResourceRequestDisplay>(true);
        ReviewedResourceRequest review = reviewDisplay.LastReviewConfirmed;

        // If there is a review that was just confirmed then check if it was the correct request
        if (review != null)
        {
            ResourceRequest request = review.Request;
            return request.ItemRequested == requestedItem && request.QuantityRequested == requestQuantity;
        }
        else return false;
    }
    private bool NotebookTabIsOpen(NotebookTab tab)
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        NotebookTabPicker picker = notebook.TabPicker;
        return notebook.IsOpen && picker.CurrentTab == tab;
    }
    #endregion

    #region Highlighting Helpers
    private ConditionalHighlight HighlightInspectorButton()
    {
        InspectorObjectiveUI ui = GameManager.Instance.InspectorObjectUI;
        RectTransform target = ui.InspectorToggle.transform as RectTransform;

        // Return a conditional highlight that highlights the inspector toggle while not in inspector mode
        return new ConditionalHighlight()
        {
            predicate = () => !GameManager.Instance.m_inspector.IsInInspectorMode,
            target = () => target
        };
    }
    private ConditionalHighlight HighlightBuildButton(bool targetOpenState)
    {
        RectTransform buildButton = FindRectTransform("DraftButton");

        // Target the build button while not in the store
        return new ConditionalHighlight()
        {
            predicate = () => GameManager.Instance.m_menuManager.IsInStore,
            invert = targetOpenState,
            target = () => buildButton
        };
    }
    private ConditionalHighlight HighlightBuildSectionPicker(int sectionIndex)
    {
        GenericToggleGroupPicker<int> storeSectionGroup = GameManager.Instance.BuildUI.StoreSectionIndexPicker;
        GenericTogglePicker<int> storeSectionPicker = storeSectionGroup.GetTogglePicker(sectionIndex);
        RectTransform pickerTransform = storeSectionPicker.transform as RectTransform;

        // Setup a highlight to target the picker that picks the particular store section
        return new ConditionalHighlight()
        {
            predicate = () => storeSectionGroup.FirstValuePicked != sectionIndex,
            target = () => pickerTransform
        };
    }
    private ConditionalHighlight HighlightBuildItem<StoreSectionType>(ItemID item)
        where StoreSectionType : StoreSection
    {
        // Get the store section targetted
        StoreSectionType storeSection = GameManager.Instance.BuildUI.GetComponentInChildren<StoreSectionType>(true);
        // Get a list of all store item cells in the store section
        StoreItemCell[] cells = storeSection.GetComponentsInChildren<StoreItemCell>(true);
        // Find a cell in the list of all the cells that will be our target
        StoreItemCell target = Array.Find(cells, cell => cell.item.ID == item);
        // Convert the target transform to a rect transform
        RectTransform targetTransform = target.transform as RectTransform;

        // Return a highlight for the item cell with the given id
        return new ConditionalHighlight()
        {
            predicate = () => storeSection.SelectedItem == null || storeSection.SelectedItem.ID != item,
            target = () => targetTransform
        };
    }
    private ItemPicker GetItemPicker (NotebookTab targetTab, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        ItemPicker [] pickers = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<ItemPicker> (true);
        // Find a picker whose name contains the filter
        return Array.Find (pickers, p => p.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    private TMP_Dropdown GetDropdown (NotebookTab targetTab, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        TMP_Dropdown [] selectors = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<TMP_Dropdown> (true);
        // Find a picker whose name contains the filter
        return Array.Find (selectors, s => s.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    // TODO: Fix, it's technical debt-y
    private TestAndMetricsEntryEditor GetEntryEditor (NotebookTab targetTab) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        TestAndMetricsEntryEditor [] editors = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<TestAndMetricsEntryEditor> (true);
        // Find a picker whose name contains the filter
        return editors [0];
    }
    // Also fix
    private TMP_InputField GetInputField (NotebookTab targetTab, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        TMP_InputField [] fields = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<TMP_InputField> (true);
        // Find a picker whose name contains the filter
        return Array.Find (fields, f => f.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    // Nasty, fix
    private Button GetButton (NotebookTab targetTab, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        Button [] fields = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<Button> (true);
        // Find a picker whose name contains the filter
        return Array.Find (fields, f => f.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    // Fix fix fix
    private Toggle GetToggle (NotebookTab targetTab, string nameFilter) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        Toggle [] toggles = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<Toggle> (true);
        // Find a picker whose name contains the filter
        return Array.Find (toggles, t => t.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    // Highlights a dropdown category without highlighting items in the dropdown
    private ConditionalHighlight HighlightItemPickerCategory (NotebookTab targetTab, ItemRegistry.Category itemCategory, string nameFilter) {
        // Get the item dropdown for this category
        ItemDropdown categoryDropdown = GetItemPicker (targetTab, nameFilter).GetDropdown(itemCategory);
        return new ConditionalHighlight () 
        { 
            predicate = () => true,
            target = () => categoryDropdown.GetComponent<RectTransform> ()
        };
    }
    // Should only be used when an item is in a dropdown contained in an item picker
    private ConditionalHighlight [] HighlightItemPicker(ItemPicker itemPicker, ItemID targetItem)
    {
        ItemDropdown itemDropdown = itemPicker.GetDropdown (targetItem.Category);
        return HighlightItemDropdownUtility (itemDropdown, targetItem, () => itemPicker.SelectedItem);
    }
    // Should only be used when a dropdown is independent of an item picker
    private ConditionalHighlight [] HighlightItemDropdown (ItemDropdown itemDropdown, ItemID targetItem) {
        return HighlightItemDropdownUtility (itemDropdown, targetItem, () => itemDropdown.SelectedItem);
    }
    // Used for specific behaviors relating to the dropdown used in resource requests
    private ConditionalHighlight [] HighlightResourceRequestDropdown (ItemID targetItem, int requestQuantity) {
        return HighlightResourceRequestDropdownUtility (targetItem, requestQuantity);
    }
    private ConditionalHighlight [] HighlightResourceRequestDropdownUtility (ItemID targetItem, int requestQuantity) {
        // Grab a bunch of references to various scripts in the Notebook
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ConceptsUI concepts = notebook.GetComponentInChildren<ConceptsUI> (true);
        ResourceRequestEditor requestEditor = notebook.ResourceRequestEditor;
        ReviewedResourceRequestDisplay reviewDisplay = notebook.GetComponentInChildren<ReviewedResourceRequestDisplay> (true);
        TMP_InputField quantityInput = requestEditor.QuantityInput;
        ItemDropdown itemReqDropdown = requestEditor.ItemRequestedDropdown;

        // Get the dropdown of the target category
        RectTransform itemDropdownTransform = itemReqDropdown.GetComponent<RectTransform> ();
        int itemIndex = itemReqDropdown.DropdownIndex (targetItem);

        // Local function used to get the rect transform of the particular item in the dropdown
        RectTransform DropdownItemGetter () {
            Transform dropdownList = itemDropdownTransform.Find ("Dropdown List").Find ("Viewport").Find ("Content");

            foreach (Transform child in dropdownList) {
                if (child.name.Contains (itemIndex.ToString ())) {
                    return child as RectTransform;
                }
            }

            return null;
        }

        // Local function used to determine if the target item is visible in the Viewport of the dropdown
        bool TargetItemVisible () {
            RectTransform viewport = itemDropdownTransform.Find ("Dropdown List").Find ("Viewport") as RectTransform;
            RectTransform targetItemTransform = viewport.Find ("Content").GetChild (itemIndex + 1) as RectTransform; // +1 to account for disabled Item child in Viewport

            if (viewport != null && targetItemTransform != null) {
                // Corners are stored starting from bottom left of rect, going clockwise. Corners are positions in world space
                Vector3 [] viewportCorners = new Vector3 [4];
                Vector3 [] targetItemCorners = new Vector3 [4];
                viewport.GetWorldCorners (viewportCorners);
                targetItemTransform.GetWorldCorners (targetItemCorners);

                // We only care about comparing the y-values of the bottom-left and top-left corners, since this is a dropdown list and x-values are locked
                // Viewport of dropdown list should be guaranteed to be at least as small as a single entry, so only checking if target item entry is within viewport
                if ((targetItemCorners [0].y >= viewportCorners [0].y) && (targetItemCorners [1].y <= viewportCorners [1].y)) {
                    return true;
                }
            }

            return false;
        }

        return new ConditionalHighlight []
        {
            // Highlight the dropdown in the picker
            new ConditionalHighlight()
            {
                predicate = () => !(itemReqDropdown.Dropdown.IsExpanded || itemReqDropdown.SelectedItem == targetItem),
                target = () => itemDropdownTransform
            },
            // Highlight the single option button in the dropdown list
            new ConditionalHighlight()
            {
                predicate = () => itemReqDropdown.Dropdown.IsExpanded && TargetItemVisible() && itemReqDropdown.SelectedItem != targetItem,
                target = () => DropdownItemGetter()
            },
            // Highlight the input field only after the dropdown is closed
            new ConditionalHighlight()
            {
                predicate = () => !itemReqDropdown.Dropdown.IsExpanded && quantityInput.text != requestQuantity.ToString(),
                target = () => quantityInput.transform as RectTransform
            },
            // Highlight the request button only after the correct quantity is entered
            new ConditionalHighlight()
            {
                predicate = () => quantityInput.text == requestQuantity.ToString() && !reviewDisplay.gameObject.activeInHierarchy,
                target = () => concepts.RequestButton.transform as RectTransform
            }
        };
    }
    private ConditionalHighlight[] HighlightItemDropdownUtility(ItemDropdown itemDropdown, ItemID targetItem, Func<ItemID> selectedItem)
    {
        // Get the dropdown of the target category
        RectTransform itemDropdownTransform = itemDropdown.Dropdown.GetComponent<RectTransform> ();

        // Local function used to get the rect transform of the particular item in the dropdown
        RectTransform DropdownItemGetter () {
            Transform dropdownList = itemDropdownTransform.Find ("Dropdown List");
            /*Toggle templateItem = dropdownList.GetComponentInChildren<Toggle>(true);
            // Search all the template item's children for the item with the same index in the name
            Transform itemParent = templateItem.transform.parent;*/
            foreach (Transform child in dropdownList) {
                if (child.name.Contains (targetItem.Index.ToString ())) {
                    return child as RectTransform;
                }
            }
            return null;
        }

        return new ConditionalHighlight [] {
            // Highlight the dropdown in the picker
            new ConditionalHighlight()
            {
                predicate = () => !(itemDropdown.Dropdown.IsExpanded || selectedItem.Invoke () == targetItem),
                target = () => itemDropdownTransform
            },
            // Highlight the single option button in the dropdown list
            new ConditionalHighlight()
            {
                predicate = () => itemDropdown.Dropdown.IsExpanded && selectedItem.Invoke () != targetItem,
                target = () => DropdownItemGetter()
            },
        };
    }
    private ConditionalHighlight [] HighlightArticle (TMP_Dropdown articleDropdown, int targetArticleIndex) {
        return HighlightDropdownItemUtility (articleDropdown, targetArticleIndex, () => articleDropdown.value);
    }
    private ConditionalHighlight [] HighlightDropdownItemUtility (TMP_Dropdown dropdown, int targetItemIndex, Func<int> selectedItem) {
        // Get the dropdown of the target category
        RectTransform itemDropdownTransform = dropdown.GetComponent<RectTransform> ();

        // Local function used to get the rect transform of the particular item in the dropdown
        RectTransform DropdownItemGetter () {
            Transform dropdownList = itemDropdownTransform.Find ("Dropdown List");
            /*Toggle templateItem = dropdownList.GetComponentInChildren<Toggle>(true);
            // Search all the template item's children for the item with the same index in the name
            Transform itemParent = templateItem.transform.parent;*/
            foreach (Transform child in dropdownList) 
            {
                if (child.name.Contains (targetItemIndex.ToString ())) 
                {
                    return child as RectTransform;
                }
            }
            return null;
        }

        return new ConditionalHighlight[] {
            // Highlight the dropdown in the picker
            new ConditionalHighlight()
            {
                predicate = () => !(dropdown.IsExpanded || selectedItem.Invoke () == targetItemIndex),
                target = () => itemDropdownTransform
            },
            // Highlight the single option button in the dropdown list
            new ConditionalHighlight()
            {
                predicate = () => dropdown.IsExpanded && selectedItem.Invoke () != targetItemIndex,
                target = () => DropdownItemGetter()
            },
        };
    }
    private ConditionalHighlight HighlightDropdownUtility (TMP_Dropdown dropdown) {
        RectTransform itemDropdownTransform = dropdown.GetComponent<RectTransform> ();

        return new ConditionalHighlight () {
            predicate = () => true,
            target = () => itemDropdownTransform
        };
    }
    private ConditionalHighlight HighlightEraseButton()
    {
        // Cache some useful values
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ResearchEncyclopediaArticleInputField inputField = notebook.GetComponentInChildren<ResearchEncyclopediaArticleInputField>(true);
        RectTransform erasePicker = FindRectTransformInChildren(notebook.TabPicker.GetTabRoot(NotebookTab.Research), "ErasePicker");

        return new ConditionalHighlight()
        {
            predicate = () => !inputField.IsHighlighting,
            target = () => erasePicker
        };
    }
    private ConditionalHighlight HighlightHighlightButton()
    {
        // Cache some useful values
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        ResearchEncyclopediaArticleInputField inputField = notebook.GetComponentInChildren<ResearchEncyclopediaArticleInputField>(true);
        RectTransform highlightPicker = FindRectTransformInChildren(notebook.TabPicker.GetTabRoot(NotebookTab.Research), "HighlightPicker");

        return new ConditionalHighlight()
        {
            predicate = () => !inputField.IsHighlighting,
            target = () => highlightPicker
        };
    }
    private ConditionalHighlight HighlightNotebookTabButton(NotebookTab tab)
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        RectTransform tabButton = notebook.TabPicker.GetTabSelectButton(tab).GetComponent<RectTransform>();

        // Return a highlight that will be active while the tab is not picked
        return new ConditionalHighlight()
        {
            predicate = () => notebook.TabPicker.CurrentTab != tab,
            target = () => tabButton
        };
    }
    private ConditionalHighlight HighlightNotebookButton()
    {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        RectTransform notebookButton = FindRectTransform("NotebookButton");
        
        // Highlight the notebook button while notebook is not open
        return new ConditionalHighlight()
        {
            predicate = () => !notebook.IsOpen,
            target = () => notebookButton
        };
    }
    private ConditionalHighlight HighlightNotes () {
        RectTransform notesTitle = FindRectTransform ("ResearchPageNotesTitle");

        // Highlight the notes until the next dialogue entry
        return new ConditionalHighlight () {
            predicate = () => true,
            target = () => notesTitle
        };
    }
    private ConditionalHighlight HighlightInputField(TMP_InputField inputField, string targetInput)
    {
        RectTransform rectTransform = inputField.transform as RectTransform;
        return new ConditionalHighlight()
        {
            predicate = () => inputField.text != targetInput,
            target = () => rectTransform
        };
    }
    private ConditionalHighlight HighlightInputField (NotebookTab targetTab, string nameFilter, int targetInputLength) {
        NotebookUI notebook = GameManager.Instance.NotebookUI;
        // Get all pickers in the given tab
        TMP_InputField [] inputFields = notebook.TabPicker.GetTabRoot (targetTab).GetComponentsInChildren<TMP_InputField> (true);
        // Find a picker whose name contains the filter
        TMP_InputField inputField = Array.Find (inputFields, p => p.name.IndexOf (nameFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        return new ConditionalHighlight () {
            predicate = () => inputField.text.Length < targetInputLength,
            target = () => inputField.transform.parent.GetComponent<RectTransform> ()
        };
    }
    private ConditionalHighlight HighlightNextDayButton(Func<bool> predicate, bool invert = false)
    {
        RectTransform nextDay = FindRectTransform("NextDay");
        return new ConditionalHighlight()
        {
            predicate = predicate,
            invert = invert,
            target = () => nextDay
        };
    }
    // Find rect transform in children of the parent with the given name
    private RectTransform FindRectTransformInChildren(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if(child)
        {
            RectTransform childRect = child as RectTransform;

            if (childRect) return childRect;
            else
            {
                Debug.Log($"{nameof(TutorialPrompter)}: child '{child}' is not a rect transform");
                return null;
            }
        }
        else
        {
            Debug.Log($"{nameof(TutorialPrompter)}: parent '{parent}' has no child named '{child}'");
            return null;
        }
    }
    private RectTransform FindRectTransform(string name)
    {
        GameObject targetGameObject = GameObject.Find(name);

        if(targetGameObject)
        {
            RectTransform target = targetGameObject.transform as RectTransform;

            // Return the rect transform you got
            if (target) return target;
            // No rect transform found on the object so log the result and return null
            else
            {
                Debug.Log($"{nameof(TutorialPrompter)}: Game object {targetGameObject} " +
                    $"has no component of type {nameof(RectTransform)} attached");
                return null;
            }
        }
        // No game object found so log the result and return null
        else
        {
            Debug.Log($"{nameof(TutorialPrompter)}: Unable to find an object named {name} in the scene");
            return null;
        }
    }
    #endregion
}
