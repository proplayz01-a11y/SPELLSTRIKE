using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires Stage 1's actual Tutorial gold arrows and the Bramble discovery
/// Cinemachine event, preserving all existing Node 1 vocabulary and combat
/// machinery. Safe to run again; does not add duplicate story objects.
/// </summary>
public static class Stage1FragmentStoryInstaller
{
    private const string StagePath = "Assets/Scenes/SampleScene.unity";
    private const string TutorialPath = "Assets/Scenes/TutorialScene.unity";
    private const string BackupFolder = "Temp/Stage1FragmentDiscoveryBackup_20261123";

    [MenuItem("SPELLSTRIKE/Story/Install Stage 1 Fragment Guidance + Bramble Reveal")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Stage1FragmentStory] Stop Play Mode and save the scene first.");
            return;
        }
        Directory.CreateDirectory(BackupFolder);
        string backup = Path.Combine(BackupFolder, "SampleScene.before_fragment_story.unity");
        if (!File.Exists(backup))
            File.Copy(StagePath, backup);

        string previous = EditorSceneManager.GetActiveScene().path;
        Scene stage = EditorSceneManager.OpenScene(StagePath, OpenSceneMode.Single);
        var node1 = UnityEngine.Object.FindObjectsByType<Node1Controller>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        var shrine = UnityEngine.Object.FindObjectsByType<ShrineTrigger>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(s => s.nodeController == node1);
        var node2 = UnityEngine.Object.FindObjectsByType<Node2Controller>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        var node3 = UnityEngine.Object.FindObjectsByType<Node3Controller>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        var spirit = UnityEngine.Object.FindObjectsByType<SpiritRemnantEncounter>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        var castle = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m != null && m.GetType().Name == "HollowKnightTriggerEncounter");

        if (node1 == null || shrine == null || node1.fragmentPickup == null ||
            node1.brambleSprite == null || node1.progressionManager == null ||
            node2?.fragmentToUnlock == null || node3?.fragmentToUnlock == null ||
            spirit == null || castle == null)
        {
            Debug.LogError("[Stage1FragmentStory] One or more authored Stage 1 anchors are missing. Scene not saved.");
            if (!string.IsNullOrEmpty(previous) && previous != StagePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            return;
        }

        GameObject root = GameObject.Find("Stage1_SpiritFragmentGuidance");
        if (root == null)
            root = new GameObject("Stage1_SpiritFragmentGuidance");
        var arrow = root.GetComponent<TutorialDirectionalArrowTrail>();
        if (arrow == null)
            arrow = root.AddComponent<TutorialDirectionalArrowTrail>();

        Scene tutorial = EditorSceneManager.OpenScene(TutorialPath, OpenSceneMode.Additive);
        try
        {
            var source = UnityEngine.Object.FindObjectsByType<TutorialDirectionalArrowTrail>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.gameObject.scene == tutorial);
            if (source == null)
                throw new InvalidOperationException("Tutorial directional arrow source missing.");
            // This copies the same gold arrow sprite, scale, spacing, flow
            // speed and material as the existing Tutorial scene.
            EditorUtility.CopySerialized(source, arrow);
            // The actual tutorial arrow settings remain untouched. Stage 1
            // uses a boosted visual, longer path coverage and more legible
            // ground clearance for the much larger outdoor world.
            var visibility = new SerializedObject(arrow);
            visibility.FindProperty("visibilityBoost").floatValue = 5f;
            visibility.FindProperty("showGlowSilhouette").boolValue = false;
            visibility.FindProperty("coverFullRoute").boolValue = true;
            visibility.FindProperty("followGround").boolValue = true;
            visibility.FindProperty("groundClearance").floatValue = 0.55f;
            visibility.FindProperty("groundRefreshSeconds").floatValue = 0.25f;
            visibility.FindProperty("gentleGlowPulse").floatValue = 0.12f;
            visibility.FindProperty("maximumArrows").intValue = 120;
            visibility.FindProperty("arrowWorldWidth").floatValue = 7f;
            visibility.FindProperty("heightOffset").floatValue = 0.45f;
            visibility.FindProperty("arrowMaterial").objectReferenceValue =
                EnsureSingleArrowGlowMaterial();
            visibility.ApplyModifiedPropertiesWithoutUndo();
        }
        finally
        {
            EditorSceneManager.CloseScene(tutorial, true);
        }

        var guide = root.GetComponent<Stage1FragmentGuidance>();
        if (guide == null)
            guide = root.AddComponent<Stage1FragmentGuidance>();
        var reveal = shrine.GetComponent<Stage1FirstFragmentReveal>();
        if (reveal == null)
            reveal = shrine.gameObject.AddComponent<Stage1FirstFragmentReveal>();

        var revealSerialized = new SerializedObject(reveal);
        revealSerialized.FindProperty("fragment").objectReferenceValue = node1.fragmentPickup.transform;
        revealSerialized.FindProperty("bramble").objectReferenceValue = node1.brambleSprite;
        revealSerialized.FindProperty("progression").objectReferenceValue = node1.progressionManager;
        revealSerialized.FindProperty("guidance").objectReferenceValue = guide;
        revealSerialized.ApplyModifiedPropertiesWithoutUndo();

        var shrineSerialized = new SerializedObject(shrine);
        shrineSerialized.FindProperty("firstFragmentReveal").objectReferenceValue = reveal;
        shrineSerialized.ApplyModifiedPropertiesWithoutUndo();

        var guideSerialized = new SerializedObject(guide);
        guideSerialized.FindProperty("arrowTrail").objectReferenceValue = arrow;
        guideSerialized.FindProperty("progression").objectReferenceValue = node1.progressionManager;
        guideSerialized.FindProperty("spirit").objectReferenceValue = spirit;
        guideSerialized.FindProperty("firstFragmentReveal").objectReferenceValue = reveal;
        guideSerialized.FindProperty("firstArena").objectReferenceValue = shrine.transform;
        guideSerialized.FindProperty("secondFragment").objectReferenceValue =
            node2.fragmentToUnlock.transform;
        guideSerialized.FindProperty("thirdFragment").objectReferenceValue =
            node3.fragmentToUnlock.transform;
        guideSerialized.FindProperty("castleApproach").objectReferenceValue = castle.transform;
        guideSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(arrow);
        EditorUtility.SetDirty(guide);
        EditorUtility.SetDirty(reveal);
        EditorUtility.SetDirty(shrine);
        EditorSceneManager.MarkSceneDirty(stage);
        if (!EditorSceneManager.SaveScene(stage))
            throw new IOException("Failed saving Stage 1 fragment story.");

        Debug.Log("[Stage1FragmentStory] Installed authentic tutorial gold arrow trail -> " +
                  shrine.name + " -> " + node2.fragmentToUnlock.name + " -> " +
                  node3.fragmentToUnlock.name + " -> " + castle.name +
                  "; Bramble Cinemachine reveal inserted BEFORE Node 1 vocabulary.");

        if (!string.IsNullOrEmpty(previous) && previous != StagePath)
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }

    private static Material EnsureSingleArrowGlowMaterial()
    {
        const string path = "Assets/Materials/Story/Stage1SingleArrowGoldGlow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;
        Shader shader = Shader.Find("SPELLSTRIKE/Story/Stage1SingleArrowGoldGlow");
        if (shader == null)
            throw new InvalidOperationException("Stage 1 gold arrow glow shader has not imported.");
        mat = new Material(shader) { name = "Stage1SingleArrowGoldGlow" };
        mat.SetFloat("_GlowStrength", 1.1f);
        mat.SetFloat("_GlowSpread", 0.009f);
        mat.SetFloat("_CoreGain", 1.45f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
