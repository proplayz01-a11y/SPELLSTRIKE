using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Draws the same five layered player HUD sprites in SampleScene Edit Mode.
/// Never saves preview objects, edits the user's scene, or invokes Play Mode.
/// The preview disappears before Play; the real PlayerStatusHudSkin owns Play.
/// </summary>
[InitializeOnLoad]
internal static class PlayerStatusHudScenePreview
{
    private const string PreviewName = "SpellstrikePlayerStatusSkin_EditPreview";
    private const string SceneName = "SampleScene";
    private static readonly Dictionary<CanvasRenderer, float> OriginalAlphas =
        new Dictionary<CanvasRenderer, float>();

    private static GameObject preview;
    private static Slider target;
    private static double nextRefresh;

    static PlayerStatusHudScenePreview()
    {
        EditorApplication.update += RefreshInEditor;
        EditorApplication.playModeStateChanged += HandlePlayModeChange;
        EditorSceneManager.sceneClosing += HandleSceneClosing;
        EditorSceneManager.sceneSaving += HandleSceneSaving;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
    }

    [MenuItem("SPELLSTRIKE/HUD/Make Player Status Editable")]
    private static void MakeEditable()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName ||
            PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning(
                "[Player HUD] Open SampleScene in Edit Mode first. " +
                "This command will not modify other scenes or prefabs.");
            return;
        }

        Slider slider = FindSceneHealthSlider(scene);
        if (slider == null)
        {
            Debug.LogWarning(
                "[Player HUD] SampleScene's persistent player HealthBar was not found.");
            return;
        }

        // Never replace a layout the artist has already customized.
        Transform alreadyEditable =
            slider.transform.Find(PlayerStatusHudSkin.EditableRootName);
        if (alreadyEditable != null)
        {
            ClearPreview();
            Selection.activeGameObject = alreadyEditable.gameObject;
            Debug.Log("[Player HUD] Editable hierarchy already exists. " +
                      "Selected it without rebuilding or overwriting anything.");
            return;
        }

        // Remove the old non-selectable ghost and restore canvas renderer
        // alphas before constructing normal, serialized, selectable UI.
        ClearPreview();
        GameObject editable = PlayerStatusHudSkin.CreateEditableSceneHud(slider);
        if (editable == null)
        {
            Debug.LogError("[Player HUD] Cannot create editable HUD. " +
                           "Check the five SpellstrikePlayerHUD Sprite resources.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(editable, "Create Editable Player Status HUD");
        foreach (Graphic graphic in slider.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == null || graphic.transform.IsChildOf(editable.transform))
                continue;
            if (graphic.enabled)
            {
                Undo.RecordObject(graphic, "Hide Legacy Player HP Artwork");
                graphic.enabled = false;
            }
        }

        Selection.activeGameObject = editable;
        SceneView.RepaintAll();
        Debug.Log(
            "[Player HUD] Real editable player HUD created under the existing " +
            "HealthBar. Expand SpellstrikePlayerStatusSkin in Hierarchy and edit " +
            "child RectTransforms/Image/Text via Inspector. Unity Undo is supported. " +
            "The scene was NOT auto-saved.");
    }

    [MenuItem("SPELLSTRIKE/HUD/Make Player Status Editable", true)]
    private static bool CanMakeEditable()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("SPELLSTRIKE/HUD/Refresh Player Status Scene Preview")]
    private static void ForceRefresh()
    {
        nextRefresh = 0;
        RefreshInEditor();
    }

    private static void HandleSceneClosing(Scene scene, bool removingScene)
    {
        ClearPreview();
    }

    private static void HandleSceneSaving(Scene scene, string path)
    {
        // Protect the user's original SampleScene and any unsaved work:
        // editor-only preview objects must never be written into the file.
        ClearPreview();
        nextRefresh = EditorApplication.timeSinceStartup + 0.5;
    }

    private static void HandlePlayModeChange(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.EnteredPlayMode)
            ClearPreview();

        if (state == PlayModeStateChange.EnteredEditMode)
            nextRefresh = 0;
    }

    private static void RefreshInEditor()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            Application.isPlaying)
        {
            ClearPreview();
            return;
        }

        if (EditorApplication.timeSinceStartup < nextRefresh)
            return;
        nextRefresh = EditorApplication.timeSinceStartup + 0.5;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName ||
            PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            ClearPreview();
            return;
        }

        Slider slider = FindSceneHealthSlider(scene);

        if (slider == null)
        {
            ClearPreview();
            return;
        }

        if (target != slider)
        {
            ClearPreview();
            target = slider;
        }

        // The scene owns real editable objects now. Leave them alone, never
        // spawn another preview or hide the user's new layers.
        if (slider.transform.Find(PlayerStatusHudSkin.EditableRootName) != null)
        {
            ClearPreview();
            return;
        }

        if (preview == null)
        {
            Transform existing = slider.transform.Find(PreviewName);
            preview = existing != null
                ? existing.gameObject
                : PlayerStatusHudSkin.CreateEditorScenePreview(slider);
            if (preview == null)
                return;

            // Do NOT mark the scene dirty. This non-saving child exists only
            // while SampleScene is being inspected outside Play Mode.
            preview.hideFlags = HideFlags.HideInHierarchy |
                                HideFlags.DontSaveInEditor |
                                HideFlags.DontSaveInBuild;
            SceneView.RepaintAll();
        }

        // CanvasRenderer's alpha is transient, not serialized in the scene.
        // Hide just the three legacy heart/border/fill graphics in Edit Mode
        // without disabling components or changing their Inspector values.
        foreach (Graphic graphic in slider.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == null || graphic.transform.IsChildOf(preview.transform))
                continue;
            CanvasRenderer renderer = graphic.canvasRenderer;
            if (renderer == null)
                continue;
            if (!OriginalAlphas.ContainsKey(renderer))
                OriginalAlphas.Add(renderer, renderer.GetAlpha());
            renderer.SetAlpha(0f);
        }
    }

    private static Slider FindSceneHealthSlider(Scene scene)
    {
        PersistentHUDRoot root = Object.FindObjectsByType<PersistentHUDRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.gameObject.scene == scene);
        return root != null
            ? root.GetComponentsInChildren<Slider>(true)
                .FirstOrDefault(item => item.name == "HealthBar")
            : null;
    }

    private static void ClearPreview()
    {
        foreach (KeyValuePair<CanvasRenderer, float> pair in OriginalAlphas)
            if (pair.Key != null)
                pair.Key.SetAlpha(pair.Value);
        OriginalAlphas.Clear();

        if (preview != null)
            Object.DestroyImmediate(preview);
        preview = null;
        target = null;
    }
}
