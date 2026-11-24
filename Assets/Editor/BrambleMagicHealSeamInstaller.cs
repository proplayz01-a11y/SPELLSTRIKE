using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Bramble-only fix requested for the Magic Heal frames 28..42 feeding loop:
/// duplicate all Humanoid/muscle/bone curves and make each final frame value
/// identical to its FIRST keyframe. The nine other humanoid feeders continue
/// using the untouched shared Magic Heal clip.
/// </summary>
public static class BrambleMagicHealSeamInstaller
{
    private const string SharedClipPath =
        "Assets/Animations/SoulFeeding/SoulFeeding_MagicHeal_Loop.anim";
    private const string BrambleClipPath =
        "Assets/Animations/SoulFeeding/Bramble_MagicHeal_Seamless.anim";
    private const string BrambleControllerPath =
        "Assets/Animations/SoulFeeding/Controllers/SampleScene_BrambleSprite_MagicHeal.controller";
    private const string LayerName = "Soul Feeding - Magic Heal";
    private const string BackupFolder = "Temp/BrambleSeamAndArrowCleanup_20261123";

    [MenuItem("SPELLSTRIKE/Story/Make Bramble Feeding Loop Seamless (28 to 42)")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            Debug.LogWarning("[BrambleSeam] Exit Play Mode before editing animation assets.");
            return;
        }
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(SharedClipPath);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BrambleControllerPath);
        if (source == null || !source.humanMotion || controller == null ||
            Mathf.Abs(source.length - 14f / 30f) > 0.01f)
        {
            Debug.LogError("[BrambleSeam] Expected 28–42 Humanoid source and Bramble-only controller.");
            return;
        }
        int layerIndex = Array.FindIndex(controller.layers, l => l.name == LayerName);
        if (layerIndex < 0)
        {
            Debug.LogError("[BrambleSeam] Bramble controller has no dedicated feeding layer.");
            return;
        }
        AnimatorState state = controller.layers[layerIndex].stateMachine.defaultState;
        if (state == null || (state.motion != source &&
            AssetDatabase.GetAssetPath(state.motion) != BrambleClipPath))
        {
            Debug.LogError("[BrambleSeam] Unexpected current motion; refusing to overwrite it.");
            return;
        }

        Directory.CreateDirectory(BackupFolder);
        string originalController = Path.Combine(BackupFolder,
            "SampleScene_BrambleSprite_MagicHeal.controller.before_seam");
        if (!File.Exists(originalController))
            File.Copy(BrambleControllerPath, originalController);

        AnimationClip clone = AssetDatabase.LoadAssetAtPath<AnimationClip>(BrambleClipPath);
        if (clone == null)
        {
            clone = UnityEngine.Object.Instantiate(source);
            clone.name = "Bramble_MagicHeal_Seamless";
            AssetDatabase.CreateAsset(clone, BrambleClipPath);
        }
        else
        {
            string targetBackup = Path.Combine(BackupFolder,
                "Bramble_MagicHeal_Seamless.anim.before_rebuild");
            if (!File.Exists(targetBackup))
                File.Copy(BrambleClipPath, targetBackup);
        }

        int modified = 0;
        float end = source.length;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve copied = AnimationUtility.GetEditorCurve(source, binding);
            if (copied == null || copied.length < 2)
                continue;

            Keyframe first = copied.keys[0];
            int lastIndex = copied.length - 1;
            Keyframe last = copied.keys[lastIndex];
            last.time = end;
            last.value = first.value;
            // Match the seam's incoming velocity to its outgoing velocity.
            // Otherwise equal key values can still create a velocity 'pop'.
            last.inTangent = first.outTangent;
            last.outTangent = first.outTangent;
            last.inWeight = first.outWeight;
            last.outWeight = first.outWeight;
            copied.MoveKey(lastIndex, last);
            AnimationUtility.SetEditorCurve(clone, binding, copied);
            modified++;
        }

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (keys == null || keys.Length == 0)
                continue;
            keys[keys.Length - 1].time = end;
            keys[keys.Length - 1].value = keys[0].value;
            AnimationUtility.SetObjectReferenceCurve(clone, binding, keys);
        }

        clone.wrapMode = WrapMode.Loop;
        var serial = new SerializedObject(clone);
        var loopSetting = serial.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopSetting != null)
        {
            loopSetting.boolValue = true;
            serial.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(clone);

        if (!clone.humanMotion || modified < 100)
        {
            Debug.LogError("[BrambleSeam] Copied clip is not a valid Humanoid with all curves. Bramble controller unchanged.");
            return;
        }
        int mismatches = 0;
        foreach (var binding in AnimationUtility.GetCurveBindings(clone))
        {
            var curve = AnimationUtility.GetEditorCurve(clone, binding);
            if (curve != null && Mathf.Abs(curve.Evaluate(0f) - curve.Evaluate(end)) > 0.0001f)
                mismatches++;
        }
        if (mismatches != 0)
        {
            Debug.LogError("[BrambleSeam] Seams differ across " + mismatches +
                           " curves; controller has not been changed.");
            return;
        }

        state.motion = clone; // Only the Bramble-specific controller is edited.
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[BrambleSeam] Bramble-only clip saved: " + modified +
                  " Humanoid curves, first==last on all, 28–42 at 30 FPS. " +
                  "Other enemy controllers and the source FBX remain untouched.");
    }
}
