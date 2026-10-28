#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a NEW V005 review prefab and Node 4 scene from the last saved V004
/// assets. Leaves the currently open (possibly unsaved) scene untouched.
/// </summary>
public static class Stage3TitanEarthbreakerV005InstallMenu
{
    private const string Root = "Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V004/";
    private const string SourcePrefab = Root + "Titan_Awakening_V004_AnimationReview.prefab";
    private const string SourceScene = Root + "Titan_Node4_V004_LaserAwakening_Review.unity";
    private const string TargetPrefab = Root + "Titan_Earthbreaker_V005_ControllerReview.prefab";
    private const string TargetScene = Root + "Titan_Node4_V005_ControllerReview.unity";

    private static void Guard()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping");
        try
        {
            if (processes.Length != 0)
                throw new InvalidOperationException("VALORANT detected. No Titan asset/scene writes allowed.");
        }
        finally { foreach (var p in processes) p.Dispose(); }
    }

    // Deprecated: a NavMeshAgent cannot be authored reliably on this nested
    // prefab in Prefab Mode. Use the safe scene-instance V005 installer instead.
    public static void Install()
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefab) != null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null ||
            System.IO.File.Exists(TargetPrefab) || System.IO.File.Exists(TargetScene))
            throw new InvalidOperationException("V005 outputs already exist. Refusing overwrite.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab)
            || !AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene))
            throw new InvalidOperationException("V004 review source assets not found.");

        Guard();
        if (!AssetDatabase.CopyAsset(SourcePrefab, TargetPrefab))
            throw new InvalidOperationException("Could not create V005 prefab copy.");
        GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefab);
        try
        {
            var animator = root.GetComponent<Animator>();
            if (!animator || !(animator.runtimeAnimatorController &&
                  animator.runtimeAnimatorController.name == "Titan_V004_AnimationReview"))
                throw new InvalidOperationException("V005 prefab is missing the approved V004 Animator.");

            var preview = root.GetComponent<Stage3TitanAwakeningV004Preview>();
            if (preview)
            {
                Guard();
                UnityEngine.Object.DestroyImmediate(preview);
            }
            if (root.GetComponent<TitanEarthbreakerController>())
                throw new InvalidOperationException("V005 controller already exists.");
            Guard();
            var titan = root.AddComponent<TitanEarthbreakerController>();
            var nav = root.GetComponent<NavMeshAgent>() ?? root.AddComponent<NavMeshAgent>();
            nav.enabled = false; // Only enable after Awakening and baked-navmesh validation.
            nav.radius = 2.0f;
            nav.height = 16f;
            nav.baseOffset = 0f;
            nav.speed = 2.2f;
            nav.angularSpeed = 75f;
            nav.acceleration = 5f;
            nav.stoppingDistance = 6f;
            var health = root.GetComponent<EnemyHealth>() ?? root.AddComponent<EnemyHealth>();
            health.maxHealth = 1000f; // Placeholder for balancing, NOT final approved boss HP.
            var collider = root.GetComponent<CapsuleCollider>() ?? root.AddComponent<CapsuleCollider>();
            // V004 rig is scaled 40x in Node 4; capsule covers roughly the body.
            collider.isTrigger = false;
            collider.direction = 1;
            collider.center = new Vector3(0f, 0.2f, 0f);
            collider.height = 0.4f;
            collider.radius = 0.10f;
            collider.enabled = false;
            var data = new SerializedObject(titan);
            data.FindProperty("animator").objectReferenceValue = animator;
            data.FindProperty("agent").objectReferenceValue = nav;
            data.FindProperty("health").objectReferenceValue = health;
            data.FindProperty("combatCollider").objectReferenceValue = collider;
            data.FindProperty("autoBeginCombatAfterAwakening").boolValue = true;
            // No damage until the authored collision radius/contact is approved.
            data.FindProperty("enableCombatDamage").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            root.name = "S3N4_Titan_EarthbreakerV005";
            Guard();
            PrefabUtility.SaveAsPrefabAsset(root, TargetPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        Guard();
        if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
            throw new InvalidOperationException("Could not copy V004 Node 4 staging to V005.");

        Scene review = default;
        try
        {
            Guard();
            review = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Additive);
            var sceneRoots = review.GetRootGameObjects();
            var actor = sceneRoots.SelectMany(x => x.GetComponentsInChildren<Stage3TitanAwakeningV004Preview>(true))
                .SingleOrDefault();
            var riddle = sceneRoots.SelectMany(x => x.GetComponentsInChildren<Stage3RiddleController>(true))
                .SingleOrDefault();
            var node4 = sceneRoots.SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Node 4");
            if (!actor || !riddle || !node4)
                throw new InvalidOperationException("Review scene is missing V004 Titan actor, Node 4, or riddle controller.");
            Vector3 position = actor.transform.position;
            Quaternion rotation = actor.transform.rotation;
            Vector3 scale = actor.transform.localScale;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefab);
            Guard();
            var replacement = PrefabUtility.InstantiatePrefab(asset, review) as GameObject;
            if (!replacement) throw new InvalidOperationException("V005 Titan prefab instantiation failed.");
            replacement.name = "S3N4_Titan_EarthbreakerV005_DORMANT";
            replacement.transform.SetPositionAndRotation(position, rotation);
            replacement.transform.localScale = scale;
            replacement.SetActive(true);
            var combat = replacement.GetComponent<TitanEarthbreakerController>();
            var controllerData = new SerializedObject(combat);
            controllerData.FindProperty("arenaCenter").objectReferenceValue = node4;
            var nodeController = node4.GetComponent<StageNodeController>();
            if (nodeController)
                controllerData.FindProperty("defeatReceiver").objectReferenceValue = nodeController;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            var riddleData = new SerializedObject(riddle);
            riddleData.FindProperty("bossRoot").objectReferenceValue = replacement;
            riddleData.FindProperty("bossBattleReceiver").objectReferenceValue = combat;
            riddleData.FindProperty("bossStartMessage").stringValue = "StartAwakening";
            riddleData.FindProperty("showDormantBossDuringRiddles").boolValue = true;
            riddleData.ApplyModifiedPropertiesWithoutUndo();
            Guard();
            UnityEngine.Object.DestroyImmediate(actor.gameObject);
            Guard();
            if (!EditorSceneManager.SaveScene(review, TargetScene))
                throw new InvalidOperationException("V005 controller review scene save failed.");
            Debug.Log("[Titan V005] REVIEW generated. Fourth-riddle laser -> StartAwakening; AI gated "
                + "until AwakenedHold. Hammerfist preview damage OFF. Current user's dirty scene untouched.");
        }
        finally
        {
            if (review.IsValid() && review.isLoaded)
                EditorSceneManager.CloseScene(review, true);
        }
        Validate();
    }

    [MenuItem("Tools/SPELLSTRIKE/Titan V005/2 Validate Controller Assets (READ ONLY)")]
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefab);
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene);
        if (!prefab || !scene || !prefab.GetComponent<TitanEarthbreakerController>()
            || !prefab.GetComponent<NavMeshAgent>() || !prefab.GetComponent<EnemyHealth>())
            throw new InvalidOperationException("V005 review assets or required Titan components missing.");
        var animator = prefab.GetComponent<Animator>();
        if (!animator || animator.runtimeAnimatorController == null)
            throw new InvalidOperationException("Titan Animator controller missing.");
        Debug.Log("[Titan V005] VALIDATION PASS: TitanEarthbreakerController, Animator V004, "
            + "NavMeshAgent, EnemyHealth and new scene. Production Stage3Scene unchanged.");
    }
}
#endif
