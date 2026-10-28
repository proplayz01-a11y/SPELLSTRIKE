using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSpellbookUI : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private Button openButton;
    [SerializeField] private TMP_Text openButtonSubtitle;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private bool unlockWithoutTutorialForTesting;

    [Header("Content")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private MainMenuSpellbookEntryView entryTemplate;
    [SerializeField] private TMP_Text discoveredCountText;
    [SerializeField] private TMP_Text emptyStateText;

    private readonly List<MainMenuSpellbookEntryView> spawnedEntries = new();
    private readonly List<SpellbookEntry> cachedEntries = new();

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void ConfigureReferences(
        Button open,
        TMP_Text buttonSubtitle,
        Button close,
        GameObject panel,
        TMP_InputField search,
        RectTransform content,
        MainMenuSpellbookEntryView template,
        TMP_Text countLabel,
        TMP_Text emptyLabel)
    {
        openButton = open;
        openButtonSubtitle = buttonSubtitle;
        closeButton = close;
        panelRoot = panel;
        searchInput = search;
        contentRoot = content;
        entryTemplate = template;
        discoveredCountText = countLabel;
        emptyStateText = emptyLabel;
    }

    private void Awake()
    {
        if (entryTemplate != null)
            entryTemplate.gameObject.SetActive(false);
        if (panelRoot != null)
            panelRoot.SetActive(false);

        RefreshUnlockState();
    }

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(HandleSearchChanged);
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(HandleSearchChanged);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (!RefreshUnlockState())
            return;

        DictionaryManager.EnsureInstance();
        SpellbookManager spellbook = SpellbookManager.Instance;
        spellbook.RefreshFromPersistentHistory();

        cachedEntries.Clear();
        cachedEntries.AddRange(spellbook.GetAllEntries());
        cachedEntries.Sort(
            (left, right) => string.Compare(
                left.word,
                right.word,
                StringComparison.OrdinalIgnoreCase));

        if (searchInput != null)
            searchInput.SetTextWithoutNotify(string.Empty);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RebuildEntries(string.Empty);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void HandleSearchChanged(string query)
    {
        RebuildEntries(query);
    }

    private void RebuildEntries(string query)
    {
        ClearSpawnedEntries();

        string normalizedQuery = string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : query.Trim();
        int visibleCount = 0;

        foreach (SpellbookEntry entry in cachedEntries)
        {
            if (!MatchesSearch(entry, normalizedQuery))
                continue;

            MainMenuSpellbookEntryView view =
                Instantiate(entryTemplate, contentRoot);
            view.gameObject.SetActive(true);
            view.SetEntry(entry);
            spawnedEntries.Add(view);
            visibleCount++;
        }

        if (discoveredCountText != null)
        {
            int total = cachedEntries.Count;
            discoveredCountText.text =
                $"{total} {(total == 1 ? "WORD" : "WORDS")} DISCOVERED";
        }

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(visibleCount == 0);
            emptyStateText.text = cachedEntries.Count == 0
                ? "No words recorded yet.\nCast valid words in combat to fill your Spellbook."
                : "No recorded words match your search.";
        }
    }

    private void ClearSpawnedEntries()
    {
        foreach (MainMenuSpellbookEntryView view in spawnedEntries)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        spawnedEntries.Clear();
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

    private bool RefreshUnlockState()
    {
        GameDatabaseManager database = GameDatabaseManager.EnsureInstance();
        bool unlocked =
            unlockWithoutTutorialForTesting
            || PlayerPrefs.GetInt("SpellStrike_TutorialCompleted", 0) == 1
            || (database.Data != null && database.Data.tutorialCompleted);

        if (openButton != null)
            openButton.interactable = unlocked;

        if (openButtonSubtitle != null)
        {
            openButtonSubtitle.text = unlocked
                ? "VIEW DISCOVERED WORDS"
                : "COMPLETE TUTORIAL TO UNLOCK";
            openButtonSubtitle.color = unlocked
                ? new Color(0.37f, 0.88f, 1f, 1f)
                : new Color(0.66f, 0.68f, 0.72f, 1f);
        }

        return unlocked;
    }
}
