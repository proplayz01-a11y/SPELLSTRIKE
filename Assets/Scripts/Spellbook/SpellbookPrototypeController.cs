using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the isolated UI Toolkit Spellbook prototype to the existing
/// SpellbookManager. This component does not record words or own persistence.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class SpellbookPrototypeController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private bool startHidden;

    private readonly List<SpellbookEntry> entries = new List<SpellbookEntry>();
    private readonly List<SpellbookEntry> filteredEntries = new List<SpellbookEntry>();
    private readonly Dictionary<string, Button> entryButtons =
        new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

    private SpellbookManager spellbookManager;
    private VisualElement root;
    private ScrollView wordList;
    private Label listEmptyState;
    private TextField searchField;
    private TextField registeredSearchField;
    private Button closeButton;
    private Button registeredCloseButton;
    private Label uniqueWordsValue;
    private Label combatCastsValue;
    private Label longestWordValue;
    private Label mostUsedWordValue;
    private Label mostUsedWordCaption;
    private Label entryCounter;
    private Label wordTitle;
    private Label wordSubtitle;
    private Label rarityText;
    private Label definitionText;
    private Label timesUsedValue;
    private Label usageCaption;
    private Label wordLengthValue;
    private Label wordRarityValue;
    private Label wordUsesValue;
    private SpellbookEntry selectedEntry;
    private EventCallback<ChangeEvent<string>> searchChangedCallback;
    private bool initialized;
    private bool subscribed;

    private IEnumerator Start()
    {
        // Let UIDocument finish creating its runtime visual tree before caching
        // named elements. UI Toolkit can replace the root during scene startup.
        yield return null;
        Initialize();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            BindVisualTree();
            SubscribeToChanges();
            RefreshFromManager();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromChanges();
    }

    private void OnDestroy()
    {
        UnsubscribeFromChanges();

        if (registeredSearchField != null && searchChangedCallback != null)
            registeredSearchField.UnregisterValueChangedCallback(searchChangedCallback);

        if (registeredCloseButton != null)
            registeredCloseButton.clicked -= CloseOverlay;
    }

    private void Initialize()
    {
        if (initialized && root != null)
            return;

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[SpellbookPrototype] UIDocument or root visual element is missing.", this);
            return;
        }

        BindVisualTree();

        if (wordList == null || searchField == null)
        {
            Debug.LogError("[SpellbookPrototype] Required word-list or search element is missing from UXML.", this);
            return;
        }

        spellbookManager = SpellbookManager.Instance;
        SubscribeToChanges();
        initialized = true;

        // Rebuild the in-memory catalogue from the authoritative saved history.
        spellbookManager.RefreshFromPersistentHistory();
        RefreshFromManager();
    }

    private void BindVisualTree()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            return;

        VisualElement currentRoot = uiDocument.rootVisualElement;
        if (currentRoot == null || ReferenceEquals(root, currentRoot))
            return;

        if (registeredSearchField != null && searchChangedCallback != null)
            registeredSearchField.UnregisterValueChangedCallback(searchChangedCallback);

        if (registeredCloseButton != null)
            registeredCloseButton.clicked -= CloseOverlay;

        root = currentRoot;
        CacheVisualElements();

        if (searchField != null)
        {
            searchChangedCallback = OnSearchChanged;
            searchField.RegisterValueChangedCallback(searchChangedCallback);
            registeredSearchField = searchField;
        }

        if (closeButton != null)
        {
            closeButton.clicked += CloseOverlay;
            registeredCloseButton = closeButton;
        }

        if (startHidden)
            root.style.display = DisplayStyle.None;
    }

    private void CacheVisualElements()
    {
        wordList = root.Q<ScrollView>("word-list");
        listEmptyState = root.Q<Label>("list-empty-state");
        searchField = root.Q<TextField>("word-search");
        closeButton = root.Q<Button>("close-button");
        uniqueWordsValue = root.Q<Label>("unique-words-value");
        combatCastsValue = root.Q<Label>("combat-casts-value");
        longestWordValue = root.Q<Label>("longest-word-value");
        mostUsedWordValue = root.Q<Label>("most-used-word-value");
        mostUsedWordCaption = root.Q<Label>("most-used-word-caption");
        entryCounter = root.Q<Label>("entry-counter");
        wordTitle = root.Q<Label>("word-title");
        wordSubtitle = root.Q<Label>("word-subtitle");
        rarityText = root.Q<Label>("rarity-text");
        definitionText = root.Q<Label>("definition-text");
        timesUsedValue = root.Q<Label>("times-used-value");
        usageCaption = root.Q<Label>("usage-caption");
        wordLengthValue = root.Q<Label>("word-length-value");
        wordRarityValue = root.Q<Label>("word-rarity-value");
        wordUsesValue = root.Q<Label>("word-uses-value");
    }

    private void SubscribeToChanges()
    {
        if (spellbookManager == null || subscribed)
            return;

        spellbookManager.EntriesChanged += HandleEntriesChanged;
        subscribed = true;
    }

    private void UnsubscribeFromChanges()
    {
        if (spellbookManager == null || !subscribed)
            return;

        spellbookManager.EntriesChanged -= HandleEntriesChanged;
        subscribed = false;
    }

    private void HandleEntriesChanged()
    {
        RefreshFromManager();
    }

    private void RefreshFromManager()
    {
        BindVisualTree();

        if (spellbookManager == null || root == null)
            return;

        entries.Clear();
        entries.AddRange(spellbookManager.GetAllEntries());
        entries.Sort((left, right) => string.Compare(
            left.word,
            right.word,
            StringComparison.OrdinalIgnoreCase));

        UpdateSummaryMetrics();
        RebuildWordList(searchField != null ? searchField.value : string.Empty);
    }

    /// <summary>Shows the live Spellbook over the current menu without changing scenes.</summary>
    public void OpenOverlay()
    {
        if (!initialized)
            Initialize();

        if (!initialized || root == null || spellbookManager == null)
            return;

        if (searchField != null)
            searchField.value = string.Empty;

        root.style.display = DisplayStyle.Flex;
        spellbookManager.RefreshFromPersistentHistory();
        RefreshFromManager();
    }

    /// <summary>Hides the Spellbook and leaves the existing menu scene untouched.</summary>
    public void CloseOverlay()
    {
        if (root != null)
            root.style.display = DisplayStyle.None;
    }

    private void UpdateSummaryMetrics()
    {
        int totalCombatCasts = 0;
        SpellbookEntry mostUsed = null;
        int longestLength = 0;

        foreach (SpellbookEntry entry in entries)
        {
            if (entry == null)
                continue;

            int uses = Mathf.Max(0, entry.timesUsed);
            totalCombatCasts += uses;

            if (mostUsed == null || uses > mostUsed.timesUsed)
                mostUsed = entry;

            longestLength = Mathf.Max(longestLength, entry.length);
        }

        SetLabel(uniqueWordsValue, entries.Count.ToString());
        SetLabel(combatCastsValue, totalCombatCasts.ToString());
        SetLabel(longestWordValue, entries.Count > 0 ? longestLength.ToString() : "—");
        SetLabel(mostUsedWordValue, mostUsed != null ? mostUsed.word : "—");
        SetLabel(
            mostUsedWordCaption,
            mostUsed != null
                ? $"USED {mostUsed.timesUsed} {(mostUsed.timesUsed == 1 ? "TIME" : "TIMES")}"
                : "No combat history yet");
    }

    private void OnSearchChanged(ChangeEvent<string> change)
    {
        RebuildWordList(change.newValue);
    }

    private void RebuildWordList(string query)
    {
        if (wordList == null)
            return;

        string normalizedQuery = string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : query.Trim();

        filteredEntries.Clear();
        wordList.Clear();
        entryButtons.Clear();

        foreach (SpellbookEntry entry in entries)
        {
            if (entry != null && MatchesSearch(entry, normalizedQuery))
                filteredEntries.Add(entry);
        }

        foreach (SpellbookEntry entry in filteredEntries)
        {
            string rarity = string.IsNullOrWhiteSpace(entry.rarity)
                ? "UNCLASSIFIED"
                : entry.rarity.Trim().ToUpperInvariant();
            int uses = Mathf.Max(0, entry.timesUsed);
            Button button = new Button
            {
                text = $"{entry.word}     •     {rarity}     •     {uses} USES",
                userData = entry
            };
            button.AddToClassList("word-entry");

            SpellbookEntry capturedEntry = entry;
            button.clicked += () => SelectEntry(capturedEntry);

            wordList.Add(button);
            entryButtons[entry.word] = button;
        }

        if (listEmptyState != null)
        {
            listEmptyState.style.display = filteredEntries.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            listEmptyState.text = entries.Count == 0
                ? "No valid combat words have been recorded yet. Form and cast a valid word in battle to add it here."
                : "No recorded words match your search.";
        }

        SpellbookEntry nextSelection = null;
        if (selectedEntry != null)
        {
            foreach (SpellbookEntry entry in filteredEntries)
            {
                if (string.Equals(entry.word, selectedEntry.word, StringComparison.OrdinalIgnoreCase))
                {
                    nextSelection = entry;
                    break;
                }
            }
        }

        if (nextSelection == null && filteredEntries.Count > 0)
            nextSelection = filteredEntries[0];

        SelectEntry(nextSelection);

        // Dynamic entries are added before UI Toolkit finishes the next layout
        // pass. Reset after that pass so a newly opened or filtered catalogue
        // always starts at the first alphabetical result, not its last item.
        ScrollView currentWordList = wordList;
        if (currentWordList != null)
        {
            currentWordList.schedule.Execute(() =>
            {
                if (currentWordList != null)
                    currentWordList.scrollOffset = Vector2.zero;
            }).ExecuteLater(0);
        }
    }

    private static bool MatchesSearch(SpellbookEntry entry, string query)
    {
        if (string.IsNullOrEmpty(query))
            return true;

        return Contains(entry.word, query)
               || Contains(entry.definition, query)
               || Contains(entry.rarity, query);
    }

    private static bool Contains(string value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SelectEntry(SpellbookEntry entry)
    {
        selectedEntry = entry;

        foreach (KeyValuePair<string, Button> pair in entryButtons)
        {
            pair.Value.RemoveFromClassList("word-entry-selected");
            if (entry != null && string.Equals(pair.Key, entry.word, StringComparison.OrdinalIgnoreCase))
                pair.Value.AddToClassList("word-entry-selected");
        }

        if (entry == null)
        {
            SetLabel(entryCounter, "NO MATCHING ENTRY");
            SetLabel(wordTitle, "NO WORD SELECTED");
            SetLabel(wordSubtitle, "Search or select a recorded combat word");
            SetLabel(rarityText, "UNCLASSIFIED");
            SetLabel(definitionText, entries.Count == 0
                ? "Your Spellbook will fill with valid words recorded during combat."
                : "No recorded word matches the current search.");
            SetLabel(timesUsedValue, "0");
            SetLabel(usageCaption, "TIMES RECORDED IN COMBAT");
            SetLabel(wordLengthValue, "—");
            SetLabel(wordRarityValue, "—");
            SetLabel(wordUsesValue, "—");
            return;
        }

        int index = entries.FindIndex(candidate =>
            string.Equals(candidate.word, entry.word, StringComparison.OrdinalIgnoreCase));
        int uses = Mathf.Max(0, entry.timesUsed);
        string rarity = string.IsNullOrWhiteSpace(entry.rarity)
            ? "UNCLASSIFIED"
            : entry.rarity.Trim().ToUpperInvariant();

        SetLabel(entryCounter, $"ENTRY {index + 1:000} / {entries.Count:000}");
        SetLabel(wordTitle, entry.word);
        SetLabel(wordSubtitle, "RECORDED IN SPELLSTRIKE COMBAT");
        SetLabel(rarityText, $"✦  {rarity}");
        SetLabel(definitionText, string.IsNullOrWhiteSpace(entry.definition)
            ? "Definition unavailable in the gameplay dictionary."
            : entry.definition);
        SetLabel(timesUsedValue, uses.ToString());
        SetLabel(usageCaption, "TIMES RECORDED IN COMBAT");
        SetLabel(wordLengthValue, $"{entry.length} LETTERS");
        SetLabel(wordRarityValue, rarity);
        SetLabel(wordUsesValue, uses.ToString());
    }

    private static void SetLabel(Label label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
