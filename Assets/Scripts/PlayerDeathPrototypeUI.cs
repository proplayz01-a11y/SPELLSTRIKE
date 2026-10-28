using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Small, reusable YOU DIED prototype. Attached to the existing PlayerHealth
/// at runtime, so no production scene/prefab or StageSelect references change.
/// This UI does not award points, complete stages, or write progression.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerDeathPrototypeUI : MonoBehaviour
{
    [SerializeField, Min(0f)] private float deathAnimationDelay = 1.8f;
    [SerializeField] private string stageSelectSceneName = "StageSelectScene";

    private static readonly Color Gold = new Color(0.81f, 0.67f, 0.40f);
    private static readonly Color Ivory = new Color(0.94f, 0.90f, 0.82f);
    private static readonly Color Muted = new Color(0.65f, 0.70f, 0.73f);
    private static readonly Color Navy = new Color(0.065f, 0.105f, 0.165f);
    private static readonly Color DarkNavy = new Color(0.035f, 0.060f, 0.10f);
    private static readonly Color DeathRed = new Color(0.78f, 0.32f, 0.32f);

    private AttackController attack;
    private MovementofPlayer movement;
    private PlayerHealth playerHealth;
    private GameObject screen;
    private bool dying;
    private bool returning;
    private int wordsCast;
    private int longestWordLength;
    private float runStartedAt;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        attack = GetComponent<AttackController>();
        movement = GetComponent<MovementofPlayer>();
        ResetRunStats();
    }

    private void OnEnable()
    {
        if (attack != null)
            attack.WordSubmitted += RecordWord;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (attack != null)
            attack.WordSubmitted -= RecordWord;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The persistent player survives Stage Select and the combat scenes.
        // Do not let the previous attempt's words/time leak into a fresh stage.
        if (scene.name == "SampleScene" || scene.name == "Stage2Scene" ||
            scene.name == "Stage3Scene")
            ResetRunStats();
    }

    private void RecordWord(string word)
    {
        if (dying || string.IsNullOrWhiteSpace(word))
            return;
        wordsCast++;
        longestWordLength = Mathf.Max(longestWordLength, word.Trim().Length);
    }

    private void ResetRunStats()
    {
        wordsCast = 0;
        longestWordLength = 0;
        runStartedAt = Time.unscaledTime;
        dying = false;
        returning = false;
    }

    public void OnPlayerDied(PlayerHealth health)
    {
        BeginDeath(health, false);
    }

#if UNITY_EDITOR
    // Safe visual preview in combat Play Mode: F9. The preview does not
    // damage the player or mark any stage complete.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9) && !dying &&
            IsCombatScene(SceneManager.GetActiveScene().name))
            BeginDeath(playerHealth, true);
    }
#endif

    private static bool IsCombatScene(string name)
    {
        return name == "SampleScene" || name == "Stage2Scene" ||
               name == "Stage3Scene";
    }

    private void BeginDeath(PlayerHealth health, bool preview)
    {
        if (dying || health == null ||
            !IsCombatScene(SceneManager.GetActiveScene().name))
            return;

        dying = true;
        playerHealth = health;
        if (movement != null)
            movement.disableMovement = true;
        if (attack != null)
            attack.SetAttackInputLocked(true);
        StartCoroutine(ShowAfterAnimation(preview));
    }

    private IEnumerator ShowAfterAnimation(bool preview)
    {
        // Never pause Time.timeScale before Van Aert's death animation.
        if (!preview && deathAnimationDelay > 0f)
            yield return new WaitForSecondsRealtime(deathAnimationDelay);
        else
            yield return null;

        if (returning || !IsCombatScene(SceneManager.GetActiveScene().name))
            yield break;

        float survived = Mathf.Max(0f, Time.unscaledTime - runStartedAt);
        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
        string stage = StageDisplayName(SceneManager.GetActiveScene().name);
        TileGenerationDebugOverlay.Hide();
        PersistentHUDRoot.Instance?.SetGameplayVisible(false);
        Time.timeScale = 0f;
        CreateDeathScreen(stage, score, wordsCast, longestWordLength, survived);
    }

    private static string StageDisplayName(string sceneName)
    {
        switch (sceneName)
        {
            case "SampleScene": return "ENCHANTED KINGDOM";
            case "Stage2Scene": return "SUNKEN SEAS";
            case "Stage3Scene": return "MYTHIC RUINS";
            default: return sceneName.ToUpperInvariant();
        }
    }

    private void CreateDeathScreen(string stage, int score, int words,
        int longestWord, float survived)
    {
        if (screen != null)
            return;

        // The existing combat EventSystem is reused; create one only when
        // running this prototype in an older scene without one.
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("Death UI EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));

        screen = new GameObject("YOU DIED - Prototype",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = screen.GetComponent<RectTransform>();
        Image blackout = Box(canvasRect, "Blackout", Color.black * 0f, Vector2.zero,
            Vector2.zero);
        blackout.rectTransform.anchorMin = Vector2.zero;
        blackout.rectTransform.anchorMax = Vector2.one;
        blackout.rectTransform.offsetMin = Vector2.zero;
        blackout.rectTransform.offsetMax = Vector2.zero;
        blackout.color = new Color(0f, 0.008f, 0.02f, 0.89f);
        blackout.raycastTarget = true;

        RectTransform panel = Box(canvasRect, "DeathSummaryPanel", Navy,
            Vector2.zero, new Vector2(1120f, 720f)).rectTransform;
        Outline(panel, Gold, 3f);
        Box(panel, "Top Red Accent", DeathRed,
            new Vector2(0f, 353f), new Vector2(1080f, 4f)).raycastTarget = false;

        Label(panel, "Stage Name", stage, 29, Gold,
            new Vector2(0f, 310f), new Vector2(930f, 45f));
        Label(panel, "Death Title", "YOU DIED", 101, DeathRed,
            new Vector2(0f, 206f), new Vector2(940f, 118f), true);
        Label(panel, "Death Subtitle", "The journey is not over.", 29, Ivory,
            new Vector2(0f, 117f), new Vector2(940f, 48f));
        Box(panel, "Gold Divider", Gold,
            new Vector2(0f, 76f), new Vector2(924f, 2f)).raycastTarget = false;

        Label(panel, "Score Label", "RUN SCORE", 28, Muted,
            new Vector2(0f, 38f), new Vector2(750f, 42f));
        Label(panel, "Score Value", score.ToString("N0"), 77, Ivory,
            new Vector2(0f, -36f), new Vector2(750f, 89f), true);

        SummaryStat(panel, -325f, "WORDS CAST", words.ToString());
        SummaryStat(panel, 0f, "LONGEST WORD",
            longestWord > 0 ? longestWord + " LETTERS" : "NONE");
        int totalSeconds = Mathf.FloorToInt(survived);
        SummaryStat(panel, 325f, "TIME SURVIVED",
            (totalSeconds / 60).ToString("00") + ":" +
            (totalSeconds % 60).ToString("00"));

        RectTransform buttonRect = Box(panel, "ReturnToStageSelect",
            DarkNavy, new Vector2(0f, -260f),
            new Vector2(650f, 91f)).rectTransform;
        Outline(buttonRect, Gold, 2.5f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        Image buttonGraphic = buttonRect.GetComponent<Image>();
        buttonGraphic.raycastTarget = true;
        button.targetGraphic = buttonGraphic;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.70f, 0.86f, 1f);
        colors.pressedColor = new Color(0.49f, 0.71f, 0.94f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.18f;
        button.colors = colors;
        button.onClick.AddListener(ReturnToStageSelect);
        Label(buttonRect, "Button Label", "RETURN TO STAGE SELECT",
            36, Gold, Vector2.zero, new Vector2(608f, 78f), true);

        Label(panel, "Prototype Disclaimer",
            "Prototype results  •  Combat score awards are not connected yet.",
            21, Muted, new Vector2(0f, -328f), new Vector2(990f, 34f));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SummaryStat(Transform parent, float x, string title, string value)
    {
        RectTransform backing = Box(parent, title + " Box", DarkNavy,
            new Vector2(x, -139f), new Vector2(295f, 103f)).rectTransform;
        Outline(backing, new Color(0.36f, 0.34f, 0.27f, 1f), 1f);
        Label(backing, title + " Label", title, 23, Muted,
            new Vector2(0f, 20f), new Vector2(265f, 35f));
        Label(backing, title + " Value", value, 35, Ivory,
            new Vector2(0f, -24f), new Vector2(275f, 48f), true);
    }

    private static Image Box(Transform parent, string name, Color tint,
        Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = tint;
        image.raycastTarget = false;
        return image;
    }

    private static void Outline(RectTransform parent, Color color, float width)
    {
        Rect rect = parent.rect;
        // Geometry is proportional to each panel's authored RectTransform.
        Image top = Box(parent, "Top Gold Line", color,
            new Vector2(0f, rect.height * 0.5f - width * 0.5f),
            new Vector2(rect.width, width));
        Image bottom = Box(parent, "Bottom Gold Line", color,
            new Vector2(0f, -rect.height * 0.5f + width * 0.5f),
            new Vector2(rect.width, width));
        Image left = Box(parent, "Left Gold Line", color,
            new Vector2(-rect.width * 0.5f + width * 0.5f, 0f),
            new Vector2(width, rect.height));
        Image right = Box(parent, "Right Gold Line", color,
            new Vector2(rect.width * 0.5f - width * 0.5f, 0f),
            new Vector2(width, rect.height));
        top.raycastTarget = bottom.raycastTarget =
            left.raycastTarget = right.raycastTarget = false;
    }

    private static TextMeshProUGUI Label(Transform parent, string name,
        string value, float size, Color color, Vector2 position,
        Vector2 rectSize, bool bold = false)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Max(14f, size * 0.75f);
        tmp.fontSizeMax = size;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    public void ReturnToStageSelect()
    {
        if (!dying || returning || string.IsNullOrWhiteSpace(stageSelectSceneName))
            return;
        returning = true;
        Time.timeScale = 1f;
        if (attack != null)
            attack.SetAttackInputLocked(false);
        playerHealth?.RestoreFullHealth();
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Idle);
        if (screen != null)
            Destroy(screen);
        screen = null;
        Debug.Log("[Death UI Prototype] Returning to Stage Select. No stage completion or review triggered.");
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
