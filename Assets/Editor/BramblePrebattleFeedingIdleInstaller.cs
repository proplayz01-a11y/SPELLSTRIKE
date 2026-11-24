using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Bramble-only feeding fix. The original combat controller defaulted its
/// full-body Base Layer to Approach (Walk), even when NavMesh velocity was
/// zero. A frozen first-frame pose on the base layer prevents walk-in-place,
/// while the upper-body Magic Heal state still loops frames 28–42.
/// The BrambleSpriteController returns to Approach when combat starts.
/// </summary>
public static class BramblePrebattleFeedingIdleInstaller
{
    private const string ControllerPath =
        "Assets/Animations/SoulFeeding/Controllers/SampleScene_BrambleSprite_MagicHeal.controller";
    private const string FeedingClipPath =
        "Assets/Animations/SoulFeeding/Bramble_MagicHeal_Seamless.anim";
    private const string BackupFolder = "Temp/BrambleFeedingIdleFix_20261123";
    private const string IdleStateName = "Soul Feeding Idle";

    [MenuItem("SPELLSTRIKE/Story/Fix Bramble prebattle idle (keep Magic Heal looping)")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            Debug.LogWarning("[BrambleFeedingIdle] Stop Play Mode first.");
            return;
        }

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimationClip feedingClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(FeedingClipPath);
        if (controller == null || feedingClip == null ||
            !feedingClip.isLooping || !feedingClip.humanMotion ||
            Mathf.Abs(feedingClip.length - 14f / 30f) > 0.01f)
        {
            Debug.LogError("[BrambleFeedingIdle] Bramble controller or seamless Humanoid clip invalid.");
            return;
        }

        AnimatorControllerLayer[] layers = controller.layers;
        if (layers.Length < 2 ||
            layers[0].stateMachine.states.All(x => x.state.name != "Approach"))
        {
            Debug.LogError("[BrambleFeedingIdle] Expected Bramble combat Approach state is missing.");
            return;
        }
        int feedingIndex = Array.FindIndex(layers,
            l => l.name == "Soul Feeding - Magic Heal");
        if (feedingIndex < 1 ||
            layers[feedingIndex].stateMachine.defaultState.motion != feedingClip)
        {
            Debug.LogError("[BrambleFeedingIdle] Unexpected Magic Heal state; no changes made.");
            return;
        }

        Directory.CreateDirectory(BackupFolder);
        string backup = Path.Combine(BackupFolder,
            "SampleScene_BrambleSprite_MagicHeal.controller.before_idle");
        if (!File.Exists(backup))
            File.Copy(ControllerPath, backup);

        AnimatorStateMachine baseMachine = layers[0].stateMachine;
        AnimatorState idle = baseMachine.states
            .Select(x => x.state)
            .FirstOrDefault(x => x.name == IdleStateName);
        if (idle == null)
            idle = baseMachine.AddState(IdleStateName);

        // Same first-frame Humanoid pose as the looping upper-body action,
        // but speed ZERO here so no leg stepping or root movement. No new
        // transitions to/from combat were invented.
        idle.motion = feedingClip;
        idle.speed = 0f;
        idle.writeDefaultValues = true;
        baseMachine.defaultState = idle;

        EditorUtility.SetDirty(idle);
        EditorUtility.SetDirty(baseMachine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[BrambleFeedingIdle] Bramble Base Layer default: " +
                  IdleStateName + " (speed 0), upper-body Magic Heal 28–42 " +
                  "still loops independently. Approach combat state preserved.");
    }
}
