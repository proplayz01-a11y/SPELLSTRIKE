#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialSceneHierarchyBuilder
{
    private const string TutorialSceneName = "TutorialScene";

    private static readonly string[] RootOrder =
    {
        "_SYSTEMS",
        "_PLAYER",
        "_ENVIRONMENT",
        "_TUTORIAL",
        "_UI",
        "_LIGHTING",
        "_AUDIO",
        "_SPAWN_POINTS"
    };

    [MenuItem("SPELLSTRIKE/Tutorial/Build Tutorial Scene Hierarchy")]
    public static void BuildHierarchy()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != TutorialSceneName)
        {
            EditorUtility.DisplayDialog(
                "Open TutorialScene first",
                "This organizer only runs when TutorialScene is the active scene.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build SPELLSTRIKE Tutorial Hierarchy");

        Dictionary<string, Transform> roots = new();
        foreach (string rootName in RootOrder)
            roots[rootName] = EnsureRoot(scene, rootName).transform;

        BuildSystems(roots["_SYSTEMS"]);
        BuildPlayer(roots["_PLAYER"]);
        BuildEnvironment(roots["_ENVIRONMENT"]);
        BuildTutorial(roots["_TUTORIAL"]);
        BuildUI(roots["_UI"]);
        BuildLighting(roots["_LIGHTING"]);
        BuildAudio(roots["_AUDIO"]);
        BuildSpawnPoints(roots["_SPAWN_POINTS"]);

        MoveUniqueRootObject(scene, "EventSystem", FindDirectChild(roots["_SYSTEMS"], "EventSystem"));
        MoveUniqueRootObject(scene, "Main Camera", FindDirectChild(roots["_PLAYER"], "PlayerCamera"));
        MoveUniqueRootObject(scene, "Directional Light", FindDirectChild(roots["_LIGHTING"], "DirectionalLight"));
        MoveUniqueRootObject(scene, "TutorialHUD_Preview", FindDirectChild(roots["_UI"], "TutorialCanvas"));
        MoveUniqueSceneObject(scene, "VAN AERT", FindDirectChild(roots["_PLAYER"], "Player"));
        MoveUniqueSceneObject(scene, "_Managers", FindDirectChild(roots["_SYSTEMS"], "GameManager"));
        MoveUniqueSceneObject(scene, "UIs", FindDirectChild(roots["_UI"], "GameplayCanvas"));
        MoveUniqueSceneObject(scene, "SpellbookCanvas", FindDirectChild(roots["_UI"], "GameplayCanvas"));
        MoveUniqueSceneObjectByPrefix(
            scene,
            "Nature by 3Donimus",
            FindDirectChild(roots["_ENVIRONMENT"], "TutorialForest"));

        for (int i = 0; i < RootOrder.Length; i++)
            roots[RootOrder[i]].SetSiblingIndex(i);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = roots["_SYSTEMS"].gameObject;
        EditorUtility.DisplayDialog(
            "Tutorial hierarchy prepared",
            "The target hierarchy was created non-destructively.\n\n" +
            "Safe unique objects were organized. Ambiguous Stage 1-derived objects were left untouched for manual review.\n\n" +
            "Inspect the Hierarchy, then save TutorialScene if it looks correct.",
            "OK");
    }

    private static void BuildSystems(Transform parent)
    {
        EnsureChild(parent, "TutorialDirector");
        EnsureChild(parent, "GameManager");
        EnsureChild(parent, "DictionaryManager");
        EnsureChild(parent, "TileManager");
        EnsureChild(parent, "AudioManager");
        EnsureChild(parent, "EventSystem");
    }

    private static void BuildPlayer(Transform parent)
    {
        Transform player = EnsureChild(parent, "Player").transform;
        EnsureChild(player, "VisualModel");
        EnsureChild(player, "CameraTarget");
        EnsureChild(player, "GroundCheck");
        EnsureChild(player, "ProjectileSpawnPoint");
        EnsureChild(player, "InteractionPoint");
        EnsureChild(parent, "PlayerCamera");
    }

    private static void BuildEnvironment(Transform parent)
    {
        Transform forest = EnsureChild(parent, "TutorialForest").transform;
        EnsureChild(forest, "Terrain");
        EnsureChild(forest, "Trees");
        EnsureChild(forest, "Rocks");
        EnsureChild(forest, "River");
        EnsureChild(forest, "GuidedPath");
        EnsureChild(forest, "InvisibleBoundaries");

        Transform arena = EnsureChild(parent, "TrainingArena").transform;
        EnsureChild(arena, "StonePlatform");
        EnsureChild(arena, "Entrance");
        EnsureChild(arena, "ArenaBoundary");
        EnsureChild(arena, "TrainingDummy");
        EnsureChild(arena, "Statue_01");
        EnsureChild(arena, "Statue_02");
        EnsureChild(arena, "Statue_03");
        EnsureChild(arena, "Statue_04");

        Transform spellbook = EnsureChild(parent, "SpellbookArea").transform;
        EnsureChild(spellbook, "SpellbookPedestal");
        EnsureChild(spellbook, "SpellbookPickup");
        EnsureChild(spellbook, "PickupVFX");

        Transform exit = EnsureChild(parent, "ExitArea").transform;
        EnsureChild(exit, "ExitPortal");
        EnsureChild(exit, "PortalTrigger");
        EnsureChild(exit, "PortalVFX");
    }

    private static void BuildTutorial(Transform parent)
    {
        Transform triggers = EnsureChild(parent, "StepTriggers").transform;
        EnsureChild(triggers, "MovementCompleteTrigger");
        EnsureChild(triggers, "CameraCompleteTrigger");
        EnsureChild(triggers, "TileAreaTrigger");
        EnsureChild(triggers, "ArenaEntranceTrigger");
        EnsureChild(triggers, "SpellbookAreaTrigger");

        Transform tiles = EnsureChild(parent, "TutorialTiles").transform;
        EnsureChild(tiles, "TileSpawnPoint_01");
        EnsureChild(tiles, "TileSpawnPoint_02");
        EnsureChild(tiles, "TileSpawnPoint_03");
        EnsureChild(tiles, "TileSpawnPoint_04");

        Transform indicators = EnsureChild(parent, "Indicators").transform;
        EnsureChild(indicators, "WorldArrow");
        EnsureChild(indicators, "DestinationMarker");
        EnsureChild(indicators, "TileHighlight");
        EnsureChild(indicators, "TargetHighlight");

        Transform guide = EnsureChild(parent, "TutorialGuide").transform;
        EnsureChild(guide, "GuideNPC");
        EnsureChild(guide, "DialogueTrigger");
    }

    private static void BuildUI(Transform parent)
    {
        Transform gameplay = EnsureChild(parent, "GameplayCanvas").transform;
        EnsureChild(gameplay, "PlayerHUD");
        EnsureChild(gameplay, "TilePoolPanel");
        EnsureChild(gameplay, "WordFormationPanel");
        EnsureChild(gameplay, "StrikeButton");
        EnsureChild(gameplay, "MobileControls");

        Transform tutorial = EnsureChild(parent, "TutorialCanvas").transform;
        EnsureChild(tutorial, "TutorialOverlay");
        Transform instruction = EnsureChild(tutorial, "InstructionPanel").transform;
        EnsureChild(instruction, "InstructionText");
        EnsureChild(instruction, "KeyboardPrompt");
        EnsureChild(instruction, "MobilePrompt");
        EnsureChild(instruction, "ContinueIndicator");
        EnsureChild(tutorial, "DialoguePanel");
        EnsureChild(tutorial, "AnimatedPointer");
        EnsureChild(tutorial, "ScreenHighlight");
        EnsureChild(tutorial, "CompletionMessage");

        Transform fade = EnsureChild(parent, "FadeCanvas").transform;
        EnsureChild(fade, "FadePanel");
    }

    private static void BuildLighting(Transform parent)
    {
        EnsureChild(parent, "DirectionalLight");
        EnsureChild(parent, "GlobalVolume");
        EnsureChild(parent, "ReflectionProbe");
        EnsureChild(parent, "LightProbes");
    }

    private static void BuildAudio(Transform parent)
    {
        EnsureChild(parent, "AmbientAudio");
        EnsureChild(parent, "MusicAudio");
        EnsureChild(parent, "TutorialSFX");
    }

    private static void BuildSpawnPoints(Transform parent)
    {
        EnsureChild(parent, "PlayerSpawnPoint");
        EnsureChild(parent, "TrainingDummySpawnPoint");
        EnsureChild(parent, "PortalDestination");
    }

    private static GameObject EnsureRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        GameObject created = new(name);
        SceneManager.MoveGameObjectToScene(created, scene);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        return created;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null)
            return existing.gameObject;

        GameObject created = new(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        Undo.SetTransformParent(created.transform, parent, $"Parent {name}");
        created.transform.localPosition = Vector3.zero;
        created.transform.localRotation = Quaternion.identity;
        created.transform.localScale = Vector3.one;
        return created;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static void MoveUniqueRootObject(Scene scene, string sourceName, Transform destination)
    {
        if (destination == null)
            return;

        List<GameObject> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == sourceName)
                matches.Add(root);
        }

        if (matches.Count != 1)
            return;

        GameObject source = matches[0];
        if (source == destination.gameObject)
            return;

        if (destination.childCount == 0 && destination.GetComponents<Component>().Length == 1)
        {
            destination.name += "_Placeholder";
            source.name = destination.name.Replace("_Placeholder", string.Empty);
            int siblingIndex = destination.GetSiblingIndex();
            Undo.SetTransformParent(source.transform, destination.parent, $"Organize {sourceName}");
            source.transform.SetSiblingIndex(siblingIndex);
            Undo.DestroyObjectImmediate(destination.gameObject);
            return;
        }

        Undo.SetTransformParent(source.transform, destination, $"Organize {sourceName}");
    }

    private static void MoveUniqueSceneObject(Scene scene, string sourceName, Transform destination)
    {
        MoveUniqueSceneObjectByPredicate(
            scene,
            candidate => candidate.name == sourceName,
            sourceName,
            destination);
    }

    private static void MoveUniqueSceneObjectByPrefix(Scene scene, string prefix, Transform destination)
    {
        MoveUniqueSceneObjectByPredicate(
            scene,
            candidate => candidate.name.StartsWith(prefix),
            prefix,
            destination);
    }

    private static void MoveUniqueSceneObjectByPredicate(
        Scene scene,
        System.Predicate<GameObject> predicate,
        string label,
        Transform destination)
    {
        if (destination == null)
            return;

        List<GameObject> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectMatches(root.transform, predicate, matches);

        if (matches.Count != 1)
            return;

        GameObject source = matches[0];
        if (source == destination.gameObject || source.transform.IsChildOf(destination))
            return;

        Undo.SetTransformParent(source.transform, destination, $"Organize {label}");
    }

    private static void CollectMatches(
        Transform current,
        System.Predicate<GameObject> predicate,
        List<GameObject> results)
    {
        if (predicate(current.gameObject))
            results.Add(current.gameObject);

        for (int i = 0; i < current.childCount; i++)
            CollectMatches(current.GetChild(i), predicate, results);
    }
}
#endif
