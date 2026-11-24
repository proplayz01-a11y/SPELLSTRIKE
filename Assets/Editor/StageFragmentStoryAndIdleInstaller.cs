using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the user-approved post-Cursed-Jester call, enlarged collectible
/// crystals, and stationary pre-battle feeding poses. Does not modify
/// pickups/colliders, the Spirit's approved first encounter, or enemy AI.
/// Re-running this menu command is idempotent.
/// </summary>
public static class StageFragmentStoryAndIdleInstaller
{
    private const string Stage1 = "Assets/Scenes/SampleScene.unity";
    private const string Backup = "Temp/Stage1PostJesterCallAndFeedingIdle_20261123";
    private static readonly string[] Scenes =
    {
        Stage1,
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity",
        "Assets/Scenes/Stage4Scene/Stage4Scene.unity"
    };

    [MenuItem("SPELLSTRIKE/Story/Install Post-Jester Echo + Bigger Fragments + Feeding Idle")]
    public static void Install()
    {
        Scene previous = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || previous.isDirty)
        {
            Debug.LogWarning("[FragmentStoryInstaller] Exit Play Mode and save the active scene first.");
            return;
        }
        var storyCore = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Characters/SpiritRemnant/EchoRuneCore/EchoRuneCore.glb");
        var feedingPose = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Animations/SoulFeeding/SoulFeeding_MagicHeal_Loop.anim");
        if (storyCore == null || feedingPose == null || !feedingPose.humanMotion)
        {
            Debug.LogError("[FragmentStoryInstaller] Rune GLB or Humanoid Magic Heal pose missing.");
            return;
        }
        Directory.CreateDirectory(Backup);
        foreach (string path in Scenes)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            string dest = Path.Combine(Backup,
                path.Replace('/', '_') + ".before_story_and_scale");
            if (!File.Exists(dest))
                File.Copy(path, dest);
        }

        string returnPath = previous.path;
        try
        {
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int scaled = 0;
                foreach (Transform visual in UnityEngine.Object
                         .FindObjectsByType<Transform>(FindObjectsInactive.Include,
                             FindObjectsSortMode.None)
                         .Where(t => t.name == "FragmentCrystal_Visual" &&
                                     t.gameObject.scene == scene))
                {
                    var stamp = visual.GetComponent<FragmentCrystalSizeStamp>() ??
                                visual.gameObject.AddComponent<FragmentCrystalSizeStamp>();
                    stamp.SetSize(FragmentCrystalSizeStamp.ApprovedMultiplier);
                    EditorUtility.SetDirty(stamp);
                    EditorUtility.SetDirty(visual);
                    scaled++;
                }

                int idleConfigured = 0;
                foreach (SoulFragmentFeeding f in UnityEngine.Object
                         .FindObjectsByType<SoulFragmentFeeding>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(f => f.gameObject.scene == scene))
                {
                    Animator animator = f.GetComponentInChildren<Animator>(true);
                    AnimatorController controller = animator != null
                        ? animator.runtimeAnimatorController as AnimatorController : null;
                    if (controller == null || !animator.isHuman)
                        continue;
                    string defaultState = controller.layers[0].stateMachine.defaultState.name;
                    // Most Stages 2–3 enemies already have authored Idle,
                    // Float Idle, IDLE or a dormant statue. Preserve them.
                    // Bramble has its own approved Soul Feeding Idle pose.
                    if (defaultState != "Walking" && defaultState != "Walk")
                        continue;

                    AnimatorStateMachine machine = controller.layers[0].stateMachine;
                    AnimatorState state = machine.states
                        .Select(x => x.state)
                        .FirstOrDefault(x => x.name == "Soul Feeding Idle");
                    if (state == null)
                        state = machine.AddState("Soul Feeding Idle");
                    state.motion = feedingPose;
                    state.speed = 0f;
                    state.writeDefaultValues = true;
                    machine.defaultState = state;
                    var so = new SerializedObject(f);
                    so.FindProperty("feedingBaseIdleStateName").stringValue =
                        "Soul Feeding Idle";
                    so.FindProperty("resumeBaseStateName").stringValue = defaultState;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(state);
                    EditorUtility.SetDirty(machine);
                    EditorUtility.SetDirty(controller);
                    EditorUtility.SetDirty(f);
                    idleConfigured++;
                }

                if (path == Stage1)
                {
                    Node3Controller node = UnityEngine.Object.FindObjectsByType<Node3Controller>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .FirstOrDefault(n => n.gameObject.scene == scene);
                    HollowKnightTriggerEncounter castle = UnityEngine.Object
                        .FindObjectsByType<HollowKnightTriggerEncounter>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .FirstOrDefault(n => n.gameObject.scene == scene);
                    if (node == null || castle == null || node.fragmentToUnlock == null ||
                        node.fragmentToUnlock.GetComponent<FragmentPickup>()?.pickupReceiver != node)
                        throw new InvalidOperationException(
                            "Stage 1 Node 3 pickup/receiver/castle references changed unexpectedly.");

                    var call = node.GetComponent<Stage1PostJesterEchoCall>() ??
                               node.gameObject.AddComponent<Stage1PostJesterEchoCall>();
                    var serial = new SerializedObject(call);
                    serial.FindProperty("echoRuneCorePrefab").objectReferenceValue = storyCore;
                    serial.FindProperty("royalCastleApproach").objectReferenceValue =
                        castle.transform;
                    serial.ApplyModifiedPropertiesWithoutUndo();
                    var nodeSo = new SerializedObject(node);
                    nodeSo.FindProperty("postJesterEchoCall").objectReferenceValue = call;
                    nodeSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(call);
                    EditorUtility.SetDirty(node);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Failed to save " + path);
                Debug.Log("[FragmentStoryInstaller] " + scene.name + ": " + scaled +
                          " crystal visuals at " + (FragmentCrystalSizeStamp.ApprovedMultiplier * 100f).ToString("F2") + "%, " + idleConfigured +
                          " walk-base enemies changed to frozen feeding idle.");
            }
        }
        finally
        {
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(returnPath))
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
        }
    }
}
