using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit SIO-only installation. Saving the actual card requires its named menu action.</summary>
public static class ModeSelectSpellItOutInstallerV01
{
    public const string SourceScene = "Assets/Scenes/MinigameModeSelect_Integration_v01.unity";
    public const string Folder = "Assets/Minigames/SpellItOutFloatingV01";
    public const string QAScene = Folder + "/QA/MinigameModeSelect_SIO_SPELL_QA_v01.unity";
    private const string OwnedRT = Folder + "/QA/SIO_SPELL_Preview512_v01.renderTexture";
    private const string SessionKey = "SPELLSTRIKE.SIO.SPELL.V01.";
    private const string CaptureFolder = "C:/Users/David/Documents/Codex/2026-11-01/he/outputs/SpellItOut_Unity_QA";
    private static readonly string[] SlotNames = {
        "V11_S_Slot", "V11_P_Slot", "V11_E_Slot", "V11_L_Slot", "V11_L2_Slot"
    };

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/1 Create Isolated QA Scene")]
    public static void CreateQA()
    {
        GuardEditMode();
        if (File.Exists(QAScene))
            throw new InvalidOperationException("QA scene already exists. Open it, or use the explicit SIO Apply command; this command never overwrites a saved scene.");
        EnsureFolder(Folder);
        EnsureFolder(Folder + "/QA");
        if (!AssetDatabase.CopyAsset(SourceScene, QAScene))
            throw new IOException("Could not copy the saved source into the isolated QA scene.");
        Scene previous = SceneManager.GetActiveScene();
        SessionState.SetString(SessionKey + "PreviousActive", previous.path);
        Scene qa = EditorSceneManager.OpenScene(QAScene, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(qa);
        Install(qa, true);
        if (!EditorSceneManager.SaveScene(qa)) throw new IOException("Could not save owned QA scene.");
        UseQAStart();
        Debug.Log("[SIO SPELL] Owned QA scene created/saved. Shared scene, builder and other loaded scenes were not saved or changed. " + QAScene);
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/2 Apply To Active Scene - Unsaved")]
    public static void ApplyActive()
    {
        GuardEditMode();
        Scene scene = SceneManager.GetActiveScene();
        Install(scene, scene.path == QAScene);
        Debug.Log("[SIO SPELL] SIO-only installation applied with Undo. Scene intentionally remains UNSAVED: " + scene.path);
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/3 Use QA As Play Start")]
    public static void UseQAStart()
    {
        GuardEditMode();
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(QAScene);
        if (asset == null) throw new FileNotFoundException("Create the owned QA scene first.");
        if (!SessionState.GetBool(SessionKey + "StartBackedUp", false))
        {
            string prior = EditorSceneManager.playModeStartScene == null ? "" : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            SessionState.SetString(SessionKey + "PreviousStart", prior);
            SessionState.SetBool(SessionKey + "StartBackedUp", true);
        }
        EditorSceneManager.playModeStartScene = asset;
        Debug.Log("[SIO SPELL] Play loads ONLY the owned QA scene. Other open scenes retain their unsaved state. Use command 4 after testing.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/4 Restore Previous Play Start")]
    public static void RestoreStart()
    {
        GuardEditMode();
        if (SessionState.GetBool(SessionKey + "StartBackedUp", false))
        {
            string prior = SessionState.GetString(SessionKey + "PreviousStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(prior) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);
            SessionState.SetBool(SessionKey + "StartBackedUp", false);
        }
        string active = SessionState.GetString(SessionKey + "PreviousActive", "");
        Scene previous = SceneManager.GetSceneByPath(active);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        Scene qa = SceneManager.GetSceneByPath(QAScene);
        if (qa.IsValid() && qa.isLoaded && !qa.isDirty) EditorSceneManager.CloseScene(qa, true);
        else if (qa.IsValid() && qa.isDirty) Debug.LogWarning("[SIO SPELL] Owned QA scene is dirty and was retained; no unsaved edits discarded.");
        Debug.Log("[SIO SPELL] Previous Play start and active scene restored. Only the clean owned QA scene may be closed; other loaded scenes retain their state.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/5 Save Owned QA Only")]
    public static void SaveOwnedQA()
    {
        GuardEditMode();
        Scene scene = SceneManager.GetSceneByPath(QAScene);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Owned QA scene is not loaded.");
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save owned QA scene.");
        Debug.Log("[SIO SPELL] Saved only " + QAScene);
    }

    /// <summary>Integration entry point. Caller chooses target; this never saves any scene.</summary>
    public static void Install(Scene scene, bool isolatedQA)
    {
        GuardEditMode();
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Choose a loaded mode-select scene.");
        Transform gallery = Unique(scene, "V11_SPEL_3D_AssetReuse");
        Camera camera = Unique(scene, "V11_SP_E_L_DedicatedPreviewCamera").GetComponent<Camera>();
        UnityEngine.UI.RawImage raw = Unique(scene, "V11_Actual3DTiles_SPEL_RawImage").GetComponent<UnityEngine.UI.RawImage>();
        if (camera == null || raw == null || camera.targetTexture == null || raw.texture != camera.targetTexture)
            throw new InvalidOperationException("Existing dedicated camera and RawImage must share a valid RenderTexture.");
        if (!camera.orthographic || camera.cullingMask != (1 << 30) || raw.raycastTarget)
            throw new InvalidOperationException("Unexpected preview camera/culling/raycast contract; reconcile before installing.");
        Transform glow = Unique(scene, "SIO_03_MagicalGlow");
        if (glow.parent != raw.transform.parent || raw.transform.parent.name != "SpellItOutButton")
            throw new InvalidOperationException("SIO glow and preview must share the existing SpellItOutButton parent.");
        for (int i = 0; i < 4; i++)
            if (gallery.Find(SlotNames[i]) == null) throw new InvalidOperationException("Missing existing slot: " + SlotNames[i]);
        if (gallery.childCount != 4 && !(gallery.childCount == 5 && gallery.Find(SlotNames[4]) != null))
            throw new InvalidOperationException("Unexpected gallery children; no automatic rebuild is allowed.");
        if (gallery.Find(SlotNames[4]) == null &&
            (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MenuV2/RodinV11/SharedBases/SPELLSTRIKE_Silver_SharedBase_1024.glb") == null ||
             AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MenuV2/RodinV11/BlackLetters/SPELLSTRIKE_L_Black3D_BothFaces_MobileLean.glb") == null))
            throw new FileNotFoundException("Imported V11 Silver/L models must be available before installation.");

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install SIO SPELL five-tile floating presentation");
        var motion = gallery.GetComponent<ModeSelectSpellItOutTileMotion>();
        if (motion == null) motion = Undo.AddComponent<ModeSelectSpellItOutTileMotion>(gallery.gameObject);
        Undo.RecordObject(motion, "Configure SIO tile motion");
        motion.RestoreAndClearBindings();
        if (gallery.Find(SlotNames[4]) == null) CreateSecondL(gallery);

        float[] x = { -2.12f, -1.05f, .02f, 1.09f, 2.15f };
        float[] y = { .10f, -.16f, .17f, -.12f, .08f };
        float[] z = { .01f, .04f, -.03f, .06f, 0f };
        float[] roll = { 10f, -8f, 5f, -9f, 12f };
        float[] period = { 3.7f, 3.1f, 3.9f, 2.9f, 3.4f };
        float[] phase = { .05f, .31f, .58f, .79f, .93f };
        float[] bob = { .045f, .038f, .052f, .041f, .047f };
        var bindings = new ModeSelectSpellItOutTileMotion.TileMotion[5];
        for (int i = 0; i < 5; i++)
        {
            Transform slot = gallery.Find(SlotNames[i]);
            Undo.RecordObject(slot, "Arrange SIO SPELL slot");
            slot.localPosition = new Vector3(x[i], y[i], z[i]);
            slot.localRotation = Quaternion.Euler(0, 0, roll[i]);
            bindings[i] = new ModeSelectSpellItOutTileMotion.TileMotion {
                target = slot, bobAmplitude = bob[i], periodSeconds = period[i], phaseCycles = phase[i],
                rotationEuler = new Vector3(1.2f, 1.5f, i % 2 == 0 ? 2.4f : -2.8f), depthAmplitude = .012f
            };
        }
        motion.Configure(bindings);
        EditorUtility.SetDirty(motion);
        var gate = raw.transform.parent.GetComponent<ModeSelectSpellItOutHoverGate>();
        if (gate == null) gate = Undo.AddComponent<ModeSelectSpellItOutHoverGate>(raw.transform.parent.gameObject);
        Undo.RecordObject(gate, "Bind SIO pointer-only tile hover");
        gate.Configure(motion);
        EditorUtility.SetDirty(gate);

        // Reuse the sole preview camera; frame the real model bounds with motion margin.
        Bounds bounds = BoundsFor(gallery);
        Undo.RecordObjects(new UnityEngine.Object[] { camera, camera.transform, raw, raw.transform, raw.transform.parent }, "Frame SIO SPELL preview");
        // The saved source retains inactive legacy text children. A fixed sibling
        // index 2 puts the preview behind the opaque base. Position relative to glow.
        int rawIndex = raw.transform.GetSiblingIndex();
        int glowIndex = glow.GetSiblingIndex();
        raw.transform.SetSiblingIndex(glowIndex + (rawIndex < glowIndex ? 0 : 1));
        float aspect = (float)camera.targetTexture.width / camera.targetTexture.height;
        camera.orthographicSize = Mathf.Max(1.65f, bounds.extents.y + .20f, (bounds.extents.x + .22f) / aspect);
        camera.transform.position = bounds.center + new Vector3(0f, .18f, -8f);
        camera.transform.LookAt(bounds.center);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        if (isolatedQA)
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(OwnedRT);
            if (texture == null)
            {
                texture = UnityEngine.Object.Instantiate(camera.targetTexture);
                texture.name = "SIO_SPELL_Preview512_v01";
                AssetDatabase.CreateAsset(texture, OwnedRT);
            }
            camera.targetTexture = texture;
            raw.texture = texture;
            var probe = gallery.GetComponent<ModeSelectSpellItOutQAProbe>();
            if (probe == null) probe = Undo.AddComponent<ModeSelectSpellItOutQAProbe>(gallery.gameObject);
            probe.Configure(motion, gallery, camera, raw, CaptureFolder);
            EditorUtility.SetDirty(probe);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undo);
        Debug.Log("[SIO SPELL] Five separate S/P/E/L/L slots, Bronze/Silver/Bronze/Silver/Silver source models; " +
                  "one existing camera, RT=" + camera.targetTexture.width + "x" + camera.targetTexture.height +
                  " AA=" + camera.targetTexture.antiAliasing + " ortho=" + camera.orthographicSize +
                  ". Navigation, artwork, hitboxes, hover/pressed components unchanged.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/7 Use Actual Card As Play Start")]
    public static void UseActualStart()
    {
        GuardEditMode();
        if (!SessionState.GetBool(SessionKey + "StartBackedUp", false))
        {
            string prior = EditorSceneManager.playModeStartScene == null ? "" : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            SessionState.SetString(SessionKey + "PreviousStart", prior);
            SessionState.SetString(SessionKey + "PreviousActive", SceneManager.GetActiveScene().path);
            SessionState.SetBool(SessionKey + "StartBackedUp", true);
        }
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene);
        Debug.Log("[SIO HOVER] Play starts the actual integrated card scene; other open scenes retain unsaved Edit Mode state.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/SIO SPELL/8 Apply And Save Actual SIO Card")]
    public static void ApplyAndSaveActual()
    {
        GuardEditMode();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SourceScene || scene.isDirty)
            throw new InvalidOperationException("Activate the saved actual integration scene first; existing dirty scene work is never overwritten.");
        Install(scene, false);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the actual SIO card integration.");
        Debug.Log("[SIO HOVER] Applied and saved ONLY the actual SIO card integration. Five tiles start at rest and animate on pointer hover only.");
    }

    private static void CreateSecondL(Transform gallery)
    {
        GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MenuV2/RodinV11/SharedBases/SPELLSTRIKE_Silver_SharedBase_1024.glb");
        GameObject glyphAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MenuV2/RodinV11/BlackLetters/SPELLSTRIKE_L_Black3D_BothFaces_MobileLean.glb");
        if (baseAsset == null || glyphAsset == null) throw new FileNotFoundException("Imported V11 Silver/L model prefabs unavailable.");
        var slot = new GameObject(SlotNames[4]);
        Undo.RegisterCreatedObjectUndo(slot, "Create independent second L");
        slot.transform.SetParent(gallery, false);
        Transform holder = new GameObject("CenteredRodinV11").transform;
        holder.SetParent(slot.transform, false);
        holder.localScale = Vector3.one * .67f;
        holder.localPosition = new Vector3(0f, -.883f * .67f, 0f);
        var body = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset, holder);
        var glyph = (GameObject)PrefabUtility.InstantiatePrefab(glyphAsset, holder);
        body.name = "ExistingV11_Silver_Base";
        glyph.name = "ExistingV11_Black3D_L";
        foreach (Transform child in slot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        foreach (Renderer renderer in slot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        foreach (Collider collider in slot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }

    private static Bounds BoundsFor(Transform gallery)
    {
        Renderer[] renderers = gallery.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Gallery has no real mesh renderers.");
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Transform Unique(Scene scene, string name)
    {
        Transform result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == name)
                {
                    if (result != null) throw new InvalidOperationException("Duplicate target in scene: " + name);
                    result = candidate;
                }
        if (result == null) throw new InvalidOperationException("Expected existing preview object not found: " + name);
        return result;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static void GuardEditMode()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            throw new InvalidOperationException("VALORANT running; SIO development stopped.");
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Exit Play and finish compilation before SIO installation.");
    }
}
