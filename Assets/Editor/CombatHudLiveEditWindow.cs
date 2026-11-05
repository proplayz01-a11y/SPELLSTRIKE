using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Edits the actual persistent combat HUD runtime instance even when a Prefab Stage
/// is open and its Hierarchy is therefore showing only prefab contents.
/// </summary>
internal sealed class CombatHudLiveEditWindow : EditorWindow
{
    private Vector2 scroll;
    private bool showTilePoolArtwork;
    private bool showWordBarArtwork;

    [MenuItem("SPELLSTRIKE/HUD/Open Live HUD Editor", priority = 219)]
    private static void Open()
    {
        var window = GetWindow<CombatHudLiveEditWindow>("Live HUD Editor");
        window.minSize = new Vector2(360f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Persistent Combat HUD", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Runtime Inspector", EditorStyles.miniLabel);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to edit the actual running HUD. This window can stay open while the PersistentGameplayHUD Prefab Stage is open.",
                MessageType.Info);
            return;
        }

        if (!TryGetRuntimeTarget(out PersistentHUDRoot root, out TutorialResponsiveLayout layout))
        {
            EditorGUILayout.HelpBox(
                "The running PersistentHUDRoot has not been found yet. Keep the gameplay scene running and wait for its Awake/Canvas setup to finish.",
                MessageType.Warning);
            if (GUILayout.Button("Refresh Runtime Search"))
                Repaint();
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Runtime target", root.gameObject.name);
        EditorGUILayout.LabelField("Runtime scene", root.gameObject.scene.name);
        EditorGUILayout.LabelField("Layout component", layout != null ? "Found on combat Canvas" : "Missing");

        using (new EditorGUI.DisabledScope(layout == null))
        {
            bool paused = layout != null && layout.IsLiveHudEditingPaused;
            bool newPaused = EditorGUILayout.ToggleLeft(
                "Pause Responsive Layout For Live Editing", paused);

            if (layout != null && newPaused != paused)
                layout.SetLiveHudEditing(newPaused);

            if (layout != null && layout.IsLiveHudEditingPaused)
            {
                bool autoAlign = layout.AutoAlignPanelArtworkDuringLiveEditing;
                bool newAutoAlign = EditorGUILayout.ToggleLeft(
                    "Auto Align Panel Artwork During Live Editing", autoAlign);
                if (newAutoAlign != autoAlign)
                    layout.SetAutoAlignPanelArtworkDuringLiveEditing(newAutoAlign);

                EditorGUILayout.HelpBox(
                    "Live edits below are applied directly to the running Game View. TileManager and the responsive layout are paused from overwriting panel/grid values. Unity will normally revert these edits when Play Mode ends; a timestamped JSON snapshot is captured automatically on exit.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Enable Pause Responsive Layout before editing. The responsive controller currently owns these values.",
                    MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();

        if (layout == null || !layout.IsLiveHudEditingPaused)
            return;

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawRectTransform("Tile Pool Panel", layout.TilePoolPanelTransform);
        DrawGridLayout("Tile Pool Grid", layout.TilePoolGrid);

        EditorGUILayout.Space(6f);
        DrawRectTransform("Word Bar Panel", layout.WordBarPanelTransform);
        DrawGridLayout("Word Bar Grid", layout.WordBarGrid);

        EditorGUILayout.Space(6f);
        DrawRectTransform("STRIKE Container", layout.UiButtonsTransform);
        DrawRectTransform("STRIKE Button", layout.AttackButtonTransform);

        if (!layout.AutoAlignPanelArtworkDuringLiveEditing)
        {
            showTilePoolArtwork = EditorGUILayout.Foldout(
                showTilePoolArtwork, "Tile Pool Frame / Fill Layers", true);
            if (showTilePoolArtwork)
            {
                DrawRectTransform("Tile Pool Frame", layout.TilePoolFrameTransform);
                DrawRectTransform("Tile Pool Fill", layout.TilePoolFillTransform);
            }

            showWordBarArtwork = EditorGUILayout.Foldout(
                showWordBarArtwork, "Word Bar Frame / Fill Layers", true);
            if (showWordBarArtwork)
            {
                DrawRectTransform("Word Bar Frame", layout.WordBarFrameTransform);
                DrawRectTransform("Word Bar Fill", layout.WordBarFillTransform);
            }
        }

        EditorGUILayout.Space(6f);
        DrawFillOpacity(root);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Game View Layout"))
            layout.RefreshLiveHudPreview();
        if (GUILayout.Button("Resume Responsive Layout"))
            layout.SetLiveHudEditing(false);
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawRectTransform(string label, RectTransform rect)
    {
        if (rect == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Vector2 anchorMin = EditorGUILayout.Vector2Field("Anchor Min", rect.anchorMin);
        Vector2 anchorMax = EditorGUILayout.Vector2Field("Anchor Max", rect.anchorMax);
        Vector2 pivot = EditorGUILayout.Vector2Field("Pivot", rect.pivot);
        Vector2 position = EditorGUILayout.Vector2Field("Anchored Position", rect.anchoredPosition);
        Vector2 size = EditorGUILayout.Vector2Field("Size Delta", rect.sizeDelta);
        Vector3 scale = EditorGUILayout.Vector3Field("Local Scale", rect.localScale);

        if (EditorGUI.EndChangeCheck())
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = scale;
            Canvas.ForceUpdateCanvases();
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawGridLayout(string label, GridLayoutGroup grid)
    {
        if (grid == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        RectOffset currentPadding = grid.padding ?? new RectOffset();
        EditorGUI.BeginChangeCheck();
        int left = EditorGUILayout.IntField("Padding Left", currentPadding.left);
        int right = EditorGUILayout.IntField("Padding Right", currentPadding.right);
        int top = EditorGUILayout.IntField("Padding Top", currentPadding.top);
        int bottom = EditorGUILayout.IntField("Padding Bottom", currentPadding.bottom);
        Vector2 cellSize = EditorGUILayout.Vector2Field("Cell Size", grid.cellSize);
        Vector2 spacing = EditorGUILayout.Vector2Field("Spacing", grid.spacing);
        var startCorner = (GridLayoutGroup.Corner)EditorGUILayout.EnumPopup("Start Corner", grid.startCorner);
        var startAxis = (GridLayoutGroup.Axis)EditorGUILayout.EnumPopup("Start Axis", grid.startAxis);
        var childAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", grid.childAlignment);
        var constraint = (GridLayoutGroup.Constraint)EditorGUILayout.EnumPopup("Constraint", grid.constraint);
        int constraintCount = EditorGUILayout.IntField("Constraint Count", grid.constraintCount);

        if (EditorGUI.EndChangeCheck())
        {
            grid.padding = new RectOffset(left, right, top, bottom);
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.startCorner = startCorner;
            grid.startAxis = startAxis;
            grid.childAlignment = childAlignment;
            grid.constraint = constraint;
            grid.constraintCount = Mathf.Max(1, constraintCount);
            RectTransform rect = grid.transform as RectTransform;
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawFillOpacity(PersistentHUDRoot root)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Panel Fill Opacity", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float wordBar = EditorGUILayout.Slider(
            "Word Bar Fill", root.WordBarFillOpacity, 0f, 1f);
        float tilePool = EditorGUILayout.Slider(
            "Tile Pool Fill", root.TilePoolFillOpacity, 0f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            root.SetWordBarFillOpacity(wordBar);
            root.SetTilePoolFillOpacity(tilePool);
        }

        EditorGUILayout.EndVertical();
    }

    internal static bool TryGetRuntimeTarget(
        out PersistentHUDRoot root, out TutorialResponsiveLayout layout)
    {
        root = PersistentHUDRoot.Instance;
        if (root == null)
        {
            PersistentHUDRoot[] roots = Resources.FindObjectsOfTypeAll<PersistentHUDRoot>();
            for (int i = 0; i < roots.Length; i++)
            {
                PersistentHUDRoot candidate = roots[i];
                if (candidate == null || EditorUtility.IsPersistent(candidate))
                    continue;
                if (!candidate.gameObject.scene.IsValid()
                    || candidate.gameObject.scene.name != "DontDestroyOnLoad")
                    continue;

                root = candidate;
                break;
            }
        }

        layout = root != null ? root.ResponsiveLayout : null;
        return root != null && layout != null;
    }
}

[InitializeOnLoad]
internal static class CombatHudPlayModeSnapshotCapture
{
    private const string CaptureDirectory = "RecoveryBackups/HUD_FRAME_FILL_20261104";

    static CombatHudPlayModeSnapshotCapture()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
            return;

        if (!CombatHudLiveEditWindow.TryGetRuntimeTarget(
                out PersistentHUDRoot root, out TutorialResponsiveLayout layout))
        {
            Debug.LogWarning(
                "[HUD Live Editor] Play Mode is exiting, but the persistent runtime HUD could not be found. No runtime snapshot was written.");
            return;
        }

        try
        {
            Directory.CreateDirectory(CaptureDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(
                CaptureDirectory,
                "CombatHudLiveLayoutCapture_" + timestamp + ".json");

            var snapshot = new CombatHudLiveLayoutSnapshot
            {
                capturedAtLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                runtimeScene = root.gameObject.scene.name,
                liveEditingPaused = layout.IsLiveHudEditingPaused,
                autoAlignPanelArtwork = layout.AutoAlignPanelArtworkDuringLiveEditing,
                wordBarFillOpacity = root.WordBarFillOpacity,
                tilePoolFillOpacity = root.TilePoolFillOpacity,
                tilePoolPanel = CombatHudLiveLayoutSnapshot.RectData.From(layout.TilePoolPanelTransform),
                tilePoolFrame = CombatHudLiveLayoutSnapshot.RectData.From(layout.TilePoolFrameTransform),
                tilePoolFill = CombatHudLiveLayoutSnapshot.RectData.From(layout.TilePoolFillTransform),
                wordBarPanel = CombatHudLiveLayoutSnapshot.RectData.From(layout.WordBarPanelTransform),
                wordBarFrame = CombatHudLiveLayoutSnapshot.RectData.From(layout.WordBarFrameTransform),
                wordBarFill = CombatHudLiveLayoutSnapshot.RectData.From(layout.WordBarFillTransform),
                uiButtons = CombatHudLiveLayoutSnapshot.RectData.From(layout.UiButtonsTransform),
                attackButton = CombatHudLiveLayoutSnapshot.RectData.From(layout.AttackButtonTransform),
                tilePoolGrid = CombatHudLiveLayoutSnapshot.GridData.From(layout.TilePoolGrid),
                wordBarGrid = CombatHudLiveLayoutSnapshot.GridData.From(layout.WordBarGrid)
            };

            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
            Debug.Log("[HUD Live Editor] Runtime HUD snapshot captured before Play Mode exit: " + Path.GetFullPath(path));
        }
        catch (Exception exception)
        {
            Debug.LogError("[HUD Live Editor] Failed to capture runtime HUD values: " + exception);
        }
    }
}

[Serializable]
internal sealed class CombatHudLiveLayoutSnapshot
{
    public string capturedAtLocal;
    public string runtimeScene;
    public bool liveEditingPaused;
    public bool autoAlignPanelArtwork;
    public float wordBarFillOpacity;
    public float tilePoolFillOpacity;
    public RectData tilePoolPanel;
    public RectData tilePoolFrame;
    public RectData tilePoolFill;
    public RectData wordBarPanel;
    public RectData wordBarFrame;
    public RectData wordBarFill;
    public RectData uiButtons;
    public RectData attackButton;
    public GridData tilePoolGrid;
    public GridData wordBarGrid;

    [Serializable]
    public sealed class RectData
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;

        public static RectData From(RectTransform rect)
        {
            if (rect == null)
                return null;

            return new RectData
            {
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition,
                sizeDelta = rect.sizeDelta,
                localScale = rect.localScale
            };
        }
    }

    [Serializable]
    public sealed class GridData
    {
        public int paddingLeft;
        public int paddingRight;
        public int paddingTop;
        public int paddingBottom;
        public Vector2 cellSize;
        public Vector2 spacing;
        public int startCorner;
        public int startAxis;
        public int childAlignment;
        public int constraint;
        public int constraintCount;

        public static GridData From(GridLayoutGroup grid)
        {
            if (grid == null)
                return null;

            return new GridData
            {
                paddingLeft = grid.padding.left,
                paddingRight = grid.padding.right,
                paddingTop = grid.padding.top,
                paddingBottom = grid.padding.bottom,
                cellSize = grid.cellSize,
                spacing = grid.spacing,
                startCorner = (int)grid.startCorner,
                startAxis = (int)grid.startAxis,
                childAlignment = (int)grid.childAlignment,
                constraint = (int)grid.constraint,
                constraintCount = grid.constraintCount
            };
        }
    }
}