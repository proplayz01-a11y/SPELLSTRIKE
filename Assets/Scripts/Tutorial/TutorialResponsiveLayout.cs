using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Keeps the shared gameplay HUD visually consistent across landscape aspect ratios.
///
/// The legacy combat UI was authored against an 800x600 Canvas and several panels used
/// stretch anchors. On wider phones that made those panels grow horizontally, while the
/// player health Canvas scaled from screen width. This component preserves the approved
/// 16:9 composition, anchors edge controls to Screen.safeArea, and lets wider displays use
/// their extra horizontal space instead of stretching the center HUD.
///
/// Tutorial-specific overlay references are resolved independently (including inactive objects),
/// while the combat HUD rules are shared by Tutorial, Stage 1, and Stage 2.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialResponsiveLayout : MonoBehaviour
{
    private const string TutorialSceneName = "TutorialScene";
    private const string Stage1SceneName = "SampleScene";
    private const string Stage1OptimizationTestSceneName = "Stage1_OptimizedMap_Test";
    private const string Stage2SceneName = "Stage2Scene";
    private const string TutorialHudResourcePath = "Tutorial/TutorialHUD_Preview";

    private static readonly Vector2 TutorialReferenceResolution = new(1920f, 1080f);
    private static readonly Vector2 LegacyReferenceResolution = new(800f, 600f);

    // 800x600 Canvas + Match Height produces a 1066.6667x600 logical Canvas at 16:9.
    // These dimensions preserve that approved 16:9 layout instead of allowing the legacy
    // stretch anchors to become wider on 18:9, 19.5:9, 20:9, etc.
    private const float DesignCanvasWidth = 1066.6667f;
    private const float DesignCanvasHeight = 600f;

    // Canonical mobile combat-HUD baseline approved in Play Mode at 2400x1080
    // (Samsung Galaxy A05s landscape). Because the combat Canvas matches height,
    // these logical dimensions remain physically stable across wider/narrower
    // landscape phones while safe-area anchoring handles the horizontal edges.
    private const float MobilePoolWidth = 720f;
    private const float MobilePoolHeight = 190f;
    private const float MobileWordBarWidth = 733.3334f;
    private const float MobileWordBarHeight = 86f;

    private static readonly Vector3 MobilePoolScale = new(0.83f, 0.83f, 0.83f);
    private static readonly Vector3 MobilePoolFrameScale = new(0.89f, 0.83f, 0.83f);
    private static readonly Vector3 MobilePoolBackdropScale = new(0.82f, 0.67f, 0.7946805f);
    private static readonly Vector3 MobileWordBarScale = new(0.97f, 0.86f, 0.77f);
    private static readonly Vector3 MobileAttackScale = new(1.08f, 1.067727f, 1.067727f);

    private RectTransform tutorialSafeArea;
    private RectTransform objectivePanel;
    private RectTransform guideDialogue;
    private RectTransform mobilePrompt;
    private CanvasScaler tutorialOverlayScaler;

    private RectTransform playerHealthBar;
    private RectTransform enemyHealthBar;
    private RectTransform tilePoolPanel;
    private RectTransform tilePoolPanelDesign;
    private RectTransform tilePoolPanelDesignOverlay;
    private RectTransform wordBarPanel;
    private RectTransform uiButtons;
    private RectTransform attackButton;
    private RectTransform mobileMovementJoystick;
    private RectTransform mobileCollectButton;

    private GridLayoutGroup tilePoolGrid;
    private GridLayoutGroup wordBarGrid;

    private CanvasScaler playerHealthScaler;
    private CanvasScaler enemyHealthScaler;
    private CanvasScaler combatScaler;
    private CombatCamera combatCamera;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private bool resolved;

    public static bool IsMobilePresentation
    {
        get
        {
            if (Application.isMobilePlatform)
                return true;

#if UNITY_EDITOR
            // Lets the active Android Build Profile preview the real mobile HUD in Play Mode
            // without pretending every ultrawide desktop monitor is a phone.
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
#else
            return false;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (IsSupportedGameplayScene(scene) && PersistentHUDRoot.Instance != null)
        {
            PersistentHUDRoot.Instance.RefreshPersistentPresentation();
            return;
        }

        EnsureForScene(scene);
    }

    public static void EnsureForScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!IsSupportedGameplayScene(scene))
            return;

        if (PersistentHUDRoot.Instance != null)
        {
            PersistentHUDRoot.Instance.RefreshPersistentPresentation();
            return;
        }

        BootstrapTutorialScene(scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapTutorialLayout()
    {
        for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);

            if (!IsSupportedGameplayScene(scene))
                continue;

            BootstrapTutorialScene(scene);
        }
    }

    private static void BootstrapTutorialScene(UnityEngine.SceneManagement.Scene scene)
    {
        Transform gameplayRoot = FindNamedTransformInScene(scene, "GameplayCanvas");
        if (gameplayRoot == null)
        {
            RectTransform attackButton = FindNamedRectInScene(scene, "AttackButton");
            Canvas attackCanvas = attackButton != null ? attackButton.GetComponentInParent<Canvas>() : null;
            gameplayRoot = attackCanvas != null ? attackCanvas.transform : null;
        }

        if (gameplayRoot == null)
        {
            Debug.LogWarning(
                $"[TutorialResponsiveLayout] {scene.name} loaded without a usable gameplay UI host; " +
                "canonical combat HUD layout could not bootstrap.");
            return;
        }

        // The tutorial overlay is optional from the combat-HUD point of view. The saved
        // TutorialScene historically did not contain it, so create the approved overlay
        // from a Resources prefab when necessary. Combat responsiveness must still work
        // even when this prefab is absent, disabled, or intentionally hidden.
        Transform tutorialRoot = scene.name == TutorialSceneName
            ? FindNamedTransformInScene(scene, "TutorialHUD_Preview")
            : null;
        if (scene.name == TutorialSceneName && tutorialRoot == null)
        {
            GameObject tutorialHudPrefab =
                Resources.Load<GameObject>(TutorialHudResourcePath);
            if (tutorialHudPrefab != null)
            {
                GameObject tutorialHud = Object.Instantiate(tutorialHudPrefab);
                tutorialHud.name = "TutorialHUD_Preview";

                // Move while still a root object; MoveGameObjectToScene only accepts roots.
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tutorialHud, scene);

                Transform tutorialCanvas = FindNamedTransformInScene(scene, "TutorialCanvas");
                if (tutorialCanvas != null)
                    tutorialHud.transform.SetParent(tutorialCanvas, false);

                tutorialRoot = tutorialHud.transform;
                tutorialRoot.localScale = Vector3.one;
                Debug.Log(
                    "[TutorialResponsiveLayout] Instantiated TutorialHUD_Preview from " +
                    $"Resources/{TutorialHudResourcePath}.");
            }
            else
            {
                Debug.LogWarning(
                    "[TutorialResponsiveLayout] TutorialHUD_Preview is absent and its " +
                    $"Resources prefab '{TutorialHudResourcePath}' was not found. " +
                    "Combat HUD responsiveness will continue without the overlay.");
            }
        }

        // The approved Tutorial HUD was temporarily hidden during mobile combat-HUD
        // iteration by zeroing its root transform scale. A serialized/prefab copy can
        // therefore be active yet mathematically invisible on device. Normalize only
        // this optional overlay root here; child layout remains controlled below.
        if (tutorialRoot != null)
            tutorialRoot.localScale = Vector3.one;

        // One authority only. Hosting it on GameplayCanvas keeps Update/LateUpdate alive
        // regardless of TutorialHUD_Preview visibility or existence.
        TutorialResponsiveLayout layout = gameplayRoot.GetComponent<TutorialResponsiveLayout>();
        if (layout == null)
            layout = gameplayRoot.gameObject.AddComponent<TutorialResponsiveLayout>();

        TutorialResponsiveLayout[] layouts = Object.FindObjectsByType<TutorialResponsiveLayout>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < layouts.Length; i++)
        {
            TutorialResponsiveLayout duplicate = layouts[i];
            if (duplicate == null
                || duplicate == layout
                || duplicate.gameObject.scene != scene)
            {
                continue;
            }

            duplicate.enabled = false;
            Object.Destroy(duplicate);
        }

        layout.ForceRefresh();
        layout.LogRuntimeDiagnostics(tutorialRoot != null);
    }
    private void OnEnable()
    {
        ResolveReferences();
        ApplyLayout(force: true);
    }

    private void Start()
    {
        // Run again after other Awake/OnEnable work so the final layout is authoritative
        // before TileManager's deferred UI initialization settles.
        ResolveReferences();
        ApplyLayout(force: true);
    }

    private void Update()
    {
        ApplyLayout(force: false);
    }

    private void LateUpdate()
    {
        // TileManager recalculates its legacy grids after a deferred startup frame and again
        // whenever the word changes. Re-assert only the mobile grid geometry here so the
        // responsive layout remains authoritative without touching desktop behavior.
        if (IsMobilePresentation)
            ApplyMobileGridLayout();
    }

    public void ForceRefresh()
    {
        resolved = false;
        ResolveReferences();
        ApplyLayout(force: true);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Reuses the exact mobile combat layout used in Play Mode to author a
    /// WYSIWYG Android Game View in Edit Mode. Invoked explicitly via the
    /// Editor menu, never by Update/OnValidate. Does not touch prefabs or
    /// gameplay state and keeps this script as the one layout authority.
    /// </summary>
    public static bool SyncAndroidEditModeCombatHud(
        UnityEngine.SceneManagement.Scene scene, out string message)
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
            || !scene.IsValid() || !scene.isLoaded
            || (scene.path != "Assets/Scenes/SampleScene.unity"
                && scene.path != "Assets/Scenes/Stage2Scene/Stage2Scene.unity"
                && scene.path != "Assets/Scenes/Stage3Scene/Stage3Scene.unity"
                && scene.path != "Assets/Scenes/Stage4Scene/Stage4Scene.unity")
            || !IsMobilePresentation)
        {
            message = "Requires Edit Mode, a saved production Stage 1-4 scene, and Android Build Target.";
            return false;
        }

        GameObject temporaryHost = null;
        try
        {
            // An inactive transient host lets us reuse the actual private
            // runtime methods without installing another persistent layout
            // component or invoking any MonoBehaviour lifecycle hooks.
            temporaryHost = new GameObject("__CombatHUD_EditPreviewSync_Temporary");
            temporaryHost.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                temporaryHost, scene);
            var layout = temporaryHost.AddComponent<TutorialResponsiveLayout>();
            layout.ResolveReferences();
            if (!layout.resolved || layout.wordBarPanel == null)
            {
                message = "HUD references could not be resolved; no layout was applied.";
                return false;
            }

            var objectsToRecord = new System.Collections.Generic.List<UnityEngine.Object>();
            void Record(Component item)
            {
                if (item != null && !objectsToRecord.Contains(item))
                    objectsToRecord.Add(item);
            }

            Record(layout.playerHealthBar);
            Record(layout.enemyHealthBar);
            Record(layout.tilePoolPanel);
            Record(layout.tilePoolPanelDesign);
            Record(layout.tilePoolPanelDesignOverlay);
            Record(layout.wordBarPanel);
            Record(layout.uiButtons);
            Record(layout.attackButton);
            Record(layout.tilePoolGrid);
            Record(layout.wordBarGrid);
            Record(layout.playerHealthScaler);
            Record(layout.enemyHealthScaler);
            Record(layout.combatScaler);
            if (layout.tilePoolPanelDesign != null)
                Record(layout.tilePoolPanelDesign.GetComponent<Image>());
            if (layout.tilePoolPanelDesignOverlay != null)
                Record(layout.tilePoolPanelDesignOverlay.GetComponent<Image>());

            // Mobile combat layout changes sibling order as well as fields.
            if (layout.wordBarPanel != null)
                UnityEditor.Undo.RegisterFullObjectHierarchyUndo(
                    layout.wordBarPanel.parent.gameObject,
                    "Sync Android HUD Edit Preview");
            if (objectsToRecord.Count > 0)
                UnityEditor.Undo.RecordObjects(objectsToRecord.ToArray(),
                    "Sync Android HUD Edit Preview");

            // Same safe-area anchor positions as a fullscreen Game View.
            // MatchHeight 800x600 on 2400x1080 produces scale 1.8, not 3.0.
            layout.ApplyLegacyCombatLayout(Vector2.zero, Vector2.one, 0.5f);
            layout.ApplyMobileGridLayout();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Canvas.ForceUpdateCanvases();
            message = "Android combat HUD Edit Preview synced from runtime layout " +
                "(Match Height, WordBar, TilePool, Attack, HP and EXP canvases). " +
                "Save the scene after reviewing.";
            return true;
        }
        finally
        {
            if (temporaryHost != null)
                UnityEngine.Object.DestroyImmediate(temporaryHost);
        }
    }
#endif

    private void LogRuntimeDiagnostics(bool tutorialOverlayPresent)
    {
        Debug.Log(
            $"[TutorialResponsiveLayout] Runtime diagnostic scene={gameObject.scene.name} " +
            $"host={gameObject.name} screen={Screen.width}x{Screen.height} safe={Screen.safeArea} " +
            $"mobile={IsMobilePresentation} overlay={tutorialOverlayPresent} " +
            $"playerHP={playerHealthBar != null} enemyHP={enemyHealthBar != null} " +
            $"tilePool={tilePoolPanel != null} tileGrid={tilePoolGrid != null} " +
            $"wordBar={wordBarPanel != null} attack={attackButton != null}.",
            this);
    }

    private void ResolveReferences()
    {
        if (resolved)
            return;

        Transform tutorialRoot = FindTutorialHudRootInScene();
        tutorialSafeArea = FindNamedRect(tutorialRoot, "SafeArea");
        objectivePanel = FindNamedRect(tutorialRoot, "ObjectivePanel");
        guideDialogue = FindNamedRect(tutorialRoot, "GuideDialogue");
        mobilePrompt = FindNamedRect(tutorialRoot, "MobilePrompt");
        tutorialOverlayScaler = tutorialRoot != null
            ? tutorialRoot.GetComponent<CanvasScaler>()
            : null;

        playerHealthBar = FindNamedRectInScene(gameObject.scene, "HealthBar");
        enemyHealthBar = FindNamedRectInScene(gameObject.scene, "EnemyHealthBar");
        tilePoolPanel = FindNamedRectInScene(gameObject.scene, "TilePoolPanel");
        tilePoolPanelDesign = FindNamedRectInScene(gameObject.scene, "TilePoolPanelDesign");
        tilePoolPanelDesignOverlay = FindNamedRectInScene(gameObject.scene, "TilePoolPanelDesign (1)");
        wordBarPanel = FindNamedRectInScene(gameObject.scene, "WordBarPanel");
        uiButtons = FindNamedRectInScene(gameObject.scene, "UI_Buttons");
        attackButton = FindNamedRectInScene(gameObject.scene, "AttackButton");
        mobileCollectButton = FindNamedRectInScene(gameObject.scene, "MobileCollectButton");
        mobileMovementJoystick = FindNamedRectInScene(gameObject.scene, "MobileMovementJoystick");

        tilePoolGrid = tilePoolPanel != null
            ? tilePoolPanel.GetComponent<GridLayoutGroup>()
            : null;
        wordBarGrid = wordBarPanel != null
            ? wordBarPanel.GetComponent<GridLayoutGroup>()
            : null;

        playerHealthScaler = GetParentCanvasScaler(playerHealthBar);
        enemyHealthScaler = GetParentCanvasScaler(enemyHealthBar);
        combatScaler = GetParentCanvasScaler(tilePoolPanel);

        combatCamera = FindComponentInScene<CombatCamera>(gameObject.scene);

        // Combat layout is allowed to resolve even while the optional Tutorial overlay is
        // inactive or temporarily absent. Overlay references will be used whenever available.
        resolved = tilePoolPanel != null
                   && uiButtons != null
                   && attackButton != null;
    }

    private void ApplyLayout(bool force)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new(Screen.width, Screen.height);

        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        if (!resolved)
            ResolveReferences();

        Vector2 safeMin = new(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        Vector2 safeMax = new(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
        float safeCenterX = (safeMin.x + safeMax.x) * 0.5f;

        ApplyTutorialOverlayLayout(safeMin, safeMax);
        ApplyLegacyCombatLayout(safeMin, safeMax, safeCenterX);
        ApplyMobileMovementJoystick(safeMin);
        ApplyMobileCollectButtonLayout(safeMin, safeMax);
        ApplyMobileGridLayout();

        // Shared gameplay scenes use the same mobile camera-orbit input contract.
        if (combatCamera != null)
            combatCamera.SetMobileTouchOrbitEnabled(IsMobilePresentation);

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        Debug.Log(
            $"[TutorialResponsiveLayout] Applied {Screen.width}x{Screen.height}, " +
            $"safe={safeArea}, mobilePresentation={IsMobilePresentation}.",
            this);
    }

    private void ApplyTutorialOverlayLayout(Vector2 safeMin, Vector2 safeMax)
    {
        // Mobile landscape phones primarily vary in width, not height. Match Height keeps
        // the tutorial/guide cards a stable physical size from 16:9 through 20:9 instead of
        // making them grow just because a device is wider. Desktop keeps the approved blend.
        ConfigureScaler(
            tutorialOverlayScaler,
            TutorialReferenceResolution,
            IsMobilePresentation ? 1f : 0.5f);

        if (tutorialSafeArea != null)
        {
            tutorialSafeArea.anchorMin = safeMin;
            tutorialSafeArea.anchorMax = safeMax;
            tutorialSafeArea.offsetMin = Vector2.zero;
            tutorialSafeArea.offsetMax = Vector2.zero;
            tutorialSafeArea.localScale = Vector3.one;
        }

        // Objective stays top-center. Its width is intentionally fixed in reference-space;
        // wider phones reveal more game world rather than inflating this card.
        SetFixedRect(
            objectivePanel,
            anchor: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -24f),
            size: new Vector2(620f, 176f));

        // Put GUIDE in a stable upper-left band. Bottom-anchored absolute Y values drifted
        // when the logical Canvas height changed under wider aspect ratios.
        SetFixedRect(
            guideDialogue,
            anchor: new Vector2(0f, 0.68f),
            pivot: new Vector2(0f, 0.5f),
            position: new Vector2(24f, 0f),
            size: new Vector2(570f, 150f));

        if (IsMobilePresentation)
        {
            // Keep the instructional prompt stable and inside the device safe area. The
            // actual interactive joystick is hosted on GameplayCanvas below.
            SetFixedRect(
                mobilePrompt,
                anchor: new Vector2(0f, 0f),
                pivot: new Vector2(0f, 0f),
                position: new Vector2(24f, 320f),
                size: new Vector2(220f, 64f));
        }
    }

    private void ApplyMobileMovementJoystick(Vector2 safeMin)
    {
        if (!IsMobilePresentation)
        {
            if (mobileMovementJoystick != null)
                mobileMovementJoystick.gameObject.SetActive(false);
            return;
        }

        Transform gameplayRoot = FindNamedTransformInScene(gameObject.scene, "GameplayCanvas");
        if (gameplayRoot == null)
        {
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas == null)
                ownCanvas = GetComponentInParent<Canvas>();
            gameplayRoot = ownCanvas != null ? ownCanvas.transform : transform;
        }

        // GameplayCanvas is only an organizational Transform in this scene. UI controls
        // must live under a real Canvas or their RectTransforms will not render/raycast.
        Transform joystickHost = combatScaler != null
            ? combatScaler.transform
            : attackButton != null
                ? attackButton.GetComponentInParent<Canvas>()?.transform
                : null;
        joystickHost ??= gameplayRoot;

        if (mobileMovementJoystick == null)
        {
            mobileMovementJoystick = FindNamedRect(gameplayRoot, "MobileMovementJoystick");
        }

        if (mobileMovementJoystick == null)
        {
            Debug.LogError(
                "[TutorialResponsiveLayout] MobileMovementJoystick is missing from TutorialScene. " +
                "The mobile joystick must be authored in Edit Mode so Edit Mode and Play Mode use the same UI hierarchy.",
                this);
            return;
        }

        mobileMovementJoystick.gameObject.SetActive(true);
        mobileMovementJoystick.anchorMin = safeMin;
        mobileMovementJoystick.anchorMax = safeMin;
        mobileMovementJoystick.pivot = Vector2.zero;
        mobileMovementJoystick.anchoredPosition = new Vector2(32f, 28f);
        mobileMovementJoystick.sizeDelta = new Vector2(180f, 180f);
        mobileMovementJoystick.localScale = Vector3.one;
        mobileMovementJoystick.SetAsLastSibling();
    }

    private void ApplyMobileCollectButtonLayout(Vector2 safeMin, Vector2 safeMax)
    {
        if (mobileCollectButton == null)
            return;

        // Keep the explicit collect action on the lower-right, away from movement,
        // the word rack, and the centered STRIKE button. Visibility is controlled by
        // TutorialDirector only while the player is actually inside Spellbook range.
        mobileCollectButton.anchorMin = new Vector2(safeMax.x, safeMin.y);
        mobileCollectButton.anchorMax = new Vector2(safeMax.x, safeMin.y);
        mobileCollectButton.pivot = new Vector2(1f, 0f);
        mobileCollectButton.anchoredPosition = new Vector2(-32f, 28f);
        mobileCollectButton.sizeDelta = new Vector2(190f, 64f);
        mobileCollectButton.localScale = Vector3.one;
        mobileCollectButton.SetAsLastSibling();
    }

    private void ApplyLegacyCombatLayout(Vector2 safeMin, Vector2 safeMax, float safeCenterX)
    {
        // Match Height makes the legacy 800x600 UI keep one physical scale across landscape
        // widths. The old player-health Canvas used Match Width, which is why it grew and
        // wandered toward the middle on 20:9 devices.
        ConfigureScaler(playerHealthScaler, LegacyReferenceResolution, 1f);
        ConfigureScaler(enemyHealthScaler, LegacyReferenceResolution, 1f);
        ConfigureScaler(combatScaler, LegacyReferenceResolution, 1f);

        if (playerHealthBar != null)
        {
            playerHealthBar.anchorMin = playerHealthBar.anchorMax = new Vector2(safeMin.x, safeMax.y);
            playerHealthBar.pivot = new Vector2(0f, 1f);
            playerHealthBar.anchoredPosition = new Vector2(44f, -18f);
        }

        if (enemyHealthBar != null)
        {
            enemyHealthBar.anchorMin = enemyHealthBar.anchorMax = new Vector2(safeMax.x, safeMax.y);
            enemyHealthBar.pivot = new Vector2(1f, 1f);
            enemyHealthBar.anchoredPosition = new Vector2(-44f, -18f);
        }

        if (IsMobilePresentation)
        {
            // The serialized Tutorial enemy HUD used a larger local scale. Mobile canonical
            // layout keeps the health bar at its authored RectTransform size instead.
            SetLocalScale(enemyHealthBar, Vector3.one);

            // Tile Pool stays the familiar 8x2 gameplay rack. The Bookworm-style single
            // row belongs to the Word Bar above it, not to the pool itself. These values
            // reproduce the approved A05s Play Mode composition rather than stretching
            // the rack as phones get wider.
            Vector2 mobilePoolSize = new(MobilePoolWidth, MobilePoolHeight);

            SetFixedRect(
                tilePoolPanelDesign,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 56.7f),
                mobilePoolSize);
            SetLocalScale(tilePoolPanelDesign, MobilePoolFrameScale);

            SetFixedRect(
                tilePoolPanel,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 62.1f),
                mobilePoolSize);
            SetLocalScale(tilePoolPanel, MobilePoolScale);

            SetFixedRect(
                tilePoolPanelDesignOverlay,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 74f),
                mobilePoolSize);
            SetLocalScale(tilePoolPanelDesignOverlay, MobilePoolBackdropScale);

            // Word Bar is the Bookworm-style rack: one centered horizontal row that shrinks
            // its tiles as the submitted word gets longer.
            SetFixedRect(
                wordBarPanel,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 215f),
                new Vector2(MobileWordBarWidth, MobileWordBarHeight));
            SetLocalScale(wordBarPanel, MobileWordBarScale);

            ApplyMobileCombatLayering();
        }
        else
        {
            // Preserve the approved PC composition unchanged.
            SetFixedRect(
                tilePoolPanelDesign,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(-2.3319092f, 41.8377f),
                new Vector2(DesignCanvasWidth - 9.7162f, DesignCanvasHeight - 206.2531f));

            SetFixedRect(
                tilePoolPanel,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(-2.3319092f, 51.5265f),
                new Vector2(DesignCanvasWidth - 9.7162f, DesignCanvasHeight - 206.2531f));

            SetFixedRect(
                tilePoolPanelDesignOverlay,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(-2.9000244f, 47.2377f),
                new Vector2(DesignCanvasWidth - 9.7162f, DesignCanvasHeight - 206.2531f));

            SetFixedRect(
                wordBarPanel,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, -88.7f),
                new Vector2(DesignCanvasWidth, DesignCanvasHeight));
        }

        ApplyWordBarArtworkLayout();
        ApplyAttackButtonLayout(safeMin, safeMax, safeCenterX);
    }

    private void ApplyWordBarArtworkLayout()
    {
        if (wordBarPanel == null)
            return;

        // Play Mode authoring: keep the user's individual frame/backdrop
        // transforms on mobile instead of overwriting their serialized values.
        // Desktop continues using its independent responsive art fit.
        if (IsMobilePresentation
            && wordBarPanel.Find("WordBar_OrnateFrame") != null
            && wordBarPanel.Find("WordBar_OrnateBackdrop") != null)
            return;

        // The legacy PC WordBar is tall but has a compressed Y scale, whereas
        // Android has a small RectTransform and nearly uniform scale. Size the
        // two non-interactive visual children in *rendered* units so the new
        // ornate artwork has the same silhouette on both platforms, without
        // changing GridLayoutGroup/tile spacing or the approved parent layout.
        // On desktop the legacy word bar has a wide 800x600-era RectTransform;
        // mobile already uses a much tighter 733x86 rectangle. Use the frame's
        // actual ornament proportions instead of filling the entire old Rect.
        float widthFactor = IsMobilePresentation ? 0.38f : 0.34f;
        float artworkWidth = wordBarPanel.rect.width * widthFactor;
        float renderedWidth = artworkWidth * Mathf.Abs(wordBarPanel.localScale.x);
        float verticalScale = Mathf.Max(0.001f, Mathf.Abs(wordBarPanel.localScale.y));
        float artworkHeight = renderedWidth / (4f * verticalScale);
        Vector2 artworkSize = new(artworkWidth, artworkHeight);

        SetWordBarArtworkRect("WordBar_OrnateBackdrop", artworkSize);
        SetWordBarArtworkRect("WordBar_OrnateFrame", artworkSize);
    }

    private void SetWordBarArtworkRect(string childName, Vector2 size)
    {
        Transform child = wordBarPanel.Find(childName);
        if (child is RectTransform artRect)
        {
            SetFixedRect(artRect, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            // The scene carries the user-authored mobile sprite scale. Restore
            // unit scale for the legacy desktop art fit to avoid multiplying it.
            artRect.localScale = Vector3.one;
        }
    }

    private void ApplyAttackButtonLayout(Vector2 safeMin, Vector2 safeMax, float safeCenterX)
    {
        if (uiButtons == null)
            return;

        if (IsMobilePresentation)
        {
            // Attack remains bottom-center on both PC and mobile. Touch movement gets the
            // lower-left region, but SPELLSTRIKE's existing combat flow expects STRIKE here.
            SetFixedRect(
                uiButtons,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 20f),
                new Vector2(220f, 64f));

            SetFixedRect(
                attackButton,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -19f),
                new Vector2(190f, 50f));
            // The approved ornate STRIKE is deliberately larger than the
            // old plain Attack button. Stage 1 already serializes the exact
            // intended art size for both Edit Mode and Play Mode. Don't
            // overwrite it with the legacy button scale on load.
            Image attackArt = attackButton != null
                ? attackButton.GetComponent<Image>() : null;
            if (attackArt == null || attackArt.sprite == null ||
                attackArt.sprite.name != "StrikeButton_OrnateBlank")
                SetLocalScale(attackButton, MobileAttackScale);
        }
        else
        {
            // Keep the approved PC composition unchanged.
            SetFixedRect(
                uiButtons,
                new Vector2(safeCenterX, safeMin.y),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 36f),
                new Vector2(180f, 54f));

            SetFixedRect(
                attackButton,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(160f, 30f));
        }
    }

    private void ApplyMobileGridLayout()
    {
        if (!IsMobilePresentation)
            return;

        if (tilePoolGrid != null && tilePoolPanel != null)
        {
            const int columns = 8;
            const int paddingLeft = 10;
            const int paddingRight = 10;
            const int paddingTop = 21;
            const int paddingBottom = 5;
            const float spacingX = 5.92f;
            const float spacingY = 5f;
            const float cellWidth = 71.17f;
            const float cellHeight = 66.2f;

            tilePoolGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tilePoolGrid.constraintCount = columns;
            tilePoolGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            tilePoolGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            tilePoolGrid.childAlignment = TextAnchor.MiddleCenter;
            tilePoolGrid.cellSize = new Vector2(cellWidth, cellHeight);
            tilePoolGrid.spacing = new Vector2(spacingX, spacingY);
            tilePoolGrid.padding.left = paddingLeft;
            tilePoolGrid.padding.right = paddingRight;
            tilePoolGrid.padding.top = paddingTop;
            tilePoolGrid.padding.bottom = paddingBottom;

            ApplyTileTypography(tilePoolPanel, Mathf.Min(cellWidth, cellHeight));
            LayoutRebuilder.ForceRebuildLayoutImmediate(tilePoolPanel);
        }

        if (wordBarGrid != null && wordBarPanel != null)
        {
            int tileCount = Mathf.Max(1, CountActiveTileChildren(wordBarPanel));
            const int paddingX = 12;
            const int paddingY = 8;
            const float spacingX = 6f;

            // Align the row inside the frame's inner artwork (not its tall
            // top-center ornament). Grid MiddleCenter splits padding differences
            // in half. The 8-unit visual inset balances top/bottom navy margins.
            RectTransform frame = wordBarPanel.Find("WordBar_OrnateFrame") as RectTransform;
            int centeredBottomPadding = paddingY;
            if (frame != null)
            {
                float innerArtCenterY = Mathf.Max(0f, frame.anchoredPosition.y - 8f);
                centeredBottomPadding += Mathf.RoundToInt(innerArtCenterY * 2f);
            }

            float usableWidth = wordBarPanel.rect.width
                - (paddingX * 2f)
                - (spacingX * Mathf.Max(0, tileCount - 1));
            float usableHeight = wordBarPanel.rect.height - (paddingY * 2f);
            float availableCellSize = Mathf.Clamp(
                Mathf.Min(usableWidth / tileCount, usableHeight),
                24f,
                74f);
            // WordBar ONLY: its selected tiles should sit with breathing room
            // inside the ornate frame, without shrinking the source TilePool
            // tiles. Retain the 24-unit legibility floor for long words.
            const float wordBarOnlyTileScale = 0.83f;
            float cellSize = Mathf.Max(24f, availableCellSize * wordBarOnlyTileScale);

            wordBarGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            wordBarGrid.constraintCount = tileCount;
            wordBarGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            wordBarGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            wordBarGrid.childAlignment = TextAnchor.MiddleCenter;
            wordBarGrid.cellSize = new Vector2(cellSize, cellSize);
            wordBarGrid.spacing = new Vector2(spacingX, 0f);
            wordBarGrid.padding.left = paddingX;
            wordBarGrid.padding.right = paddingX;
            wordBarGrid.padding.top = paddingY;
            wordBarGrid.padding.bottom = centeredBottomPadding;

            ApplyTileTypography(wordBarPanel, cellSize);
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordBarPanel);
        }
    }

    private static void ApplyTileTypography(RectTransform panel, float cellSize)
    {
        if (panel == null)
            return;

        float letterSize = Mathf.Clamp(cellSize * 0.58f, 20f, 42f);
        float counterSize = Mathf.Clamp(cellSize * 0.26f, 11f, 18f);

        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            Tile tile = child.GetComponent<Tile>();
            if (tile == null || !child.gameObject.activeSelf)
                continue;

            if (child is RectTransform tileRect)
                tileRect.localScale = Vector3.one;

            if (tile.textTMP != null)
            {
                tile.textTMP.enableAutoSizing = false;
                tile.textTMP.fontSize = letterSize;
            }

            if (tile.DebuffCounterText != null)
            {
                tile.DebuffCounterText.enableAutoSizing = false;
                tile.DebuffCounterText.fontSize = counterSize;
            }
        }
    }

    private static int CountActiveTileChildren(RectTransform parent)
    {
        if (parent == null)
            return 0;

        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.gameObject.activeSelf && child.GetComponent<Tile>() != null)
                count++;
        }

        return count;
    }
    private static int CountActiveChildren(RectTransform parent)
    {
        if (parent == null)
            return 0;

        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private static CanvasScaler GetParentCanvasScaler(RectTransform rect)
    {
        if (rect == null)
            return null;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
    }

    private static void ConfigureScaler(CanvasScaler scaler, Vector2 referenceResolution, float match)
    {
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = match;
    }

    private static void SetFixedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void ApplyMobileCombatLayering()
    {
        if (wordBarPanel == null
            || tilePoolPanelDesignOverlay == null
            || tilePoolPanel == null
            || tilePoolPanelDesign == null
            || uiButtons == null)
        {
            return;
        }

        Transform parent = wordBarPanel.parent;
        if (parent == null
            || tilePoolPanelDesignOverlay.parent != parent
            || tilePoolPanel.parent != parent
            || tilePoolPanelDesign.parent != parent
            || uiButtons.parent != parent)
        {
            return;
        }

        // Approved mobile draw/input order:
        // Word Bar -> glass backdrop -> tile buttons -> decorative border -> Attack.
        wordBarPanel.SetSiblingIndex(0);
        tilePoolPanelDesignOverlay.SetSiblingIndex(1);
        tilePoolPanel.SetSiblingIndex(2);
        tilePoolPanelDesign.SetSiblingIndex(3);
        uiButtons.SetSiblingIndex(4);

        // Decorative graphics must never consume tile taps even when rendered above them.
        Image borderImage = tilePoolPanelDesign.GetComponent<Image>();
        if (borderImage != null)
            borderImage.raycastTarget = false;

        Image backdropImage = tilePoolPanelDesignOverlay.GetComponent<Image>();
        if (backdropImage != null)
            backdropImage.raycastTarget = false;
    }

    private static void SetLocalScale(RectTransform rect, Vector3 scale)
    {
        if (rect != null)
            rect.localScale = scale;
    }

    private Transform FindTutorialHudRootInScene()
    {
        if (!gameObject.scene.IsValid())
            return null;

        return FindNamedTransformInScene(gameObject.scene, "TutorialHUD_Preview");
    }

    private static Transform FindNamedTransformInScene(UnityEngine.SceneManagement.Scene scene, string objectName)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(includeInactive: true);
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name == objectName)
                    return transforms[j];
            }
        }

        return null;
    }

    private static RectTransform FindNamedRectInScene(
        UnityEngine.SceneManagement.Scene scene,
        string objectName)
    {
        Transform transform = FindNamedTransformInScene(scene, objectName);
        return transform as RectTransform;
    }

    private static T FindComponentInScene<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static bool IsSupportedGameplayScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        return scene.name == TutorialSceneName
               || scene.name == Stage1SceneName
               || scene.name == Stage1OptimizationTestSceneName
               || scene.name == Stage2SceneName;
    }
    private static RectTransform FindNamedRect(Transform root, string objectName)
    {
        if (root == null)
            return null;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(includeInactive: true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == objectName)
                return rects[i];
        }

        return null;
    }
}
