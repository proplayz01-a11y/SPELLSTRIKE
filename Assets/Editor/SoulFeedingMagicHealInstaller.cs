using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the user's Y Bot@Magic Heal FBX as an upper-body override layer
/// for the ten existing humanoid feeding enemies across Stages 1–3.
///
/// Creates *copies* of each approved combat Animator Controller rather than
/// altering the source controller. Every existing layer/state/parameter stays
/// in its original position; the new layer is appended at zero default weight.
/// The SoulFragmentFeeding component fades it in before combat, then disables
/// it immediately on battle activation. Do not commit the user-supplied
/// animation or its derived controllers without appropriate permissions.
/// </summary>
public static class SoulFeedingMagicHealInstaller
{
    private const string SourceAnimation =
        "Assets/Animations/SoulFeeding/YBot_MagicHeal.fbx";
    private const string AnimationName = "SoulFeeding_MagicHeal_Loop";
    private const string OutputFolder = "Assets/Animations/SoulFeeding/Controllers";
    private const string MaskPath = "Assets/Animations/SoulFeeding/MagicHeal_UpperBody.mask";
    private const string LayerName = "Soul Feeding - Magic Heal";
    private const string Backup = "Temp/MagicHealAnimationBackup_20261123";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity"
    };

    [MenuItem("SPELLSTRIKE/Story/Retarget User Magic Heal to Enemies (Backed Up)")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[MagicHeal] Stop Play Mode and save current scene first.");
            return;
        }
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(SourceAnimation)
            .OfType<AnimationClip>().FirstOrDefault(a => a.name == AnimationName);
        Avatar sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(SourceAnimation)
            .OfType<Avatar>().FirstOrDefault(a => a.isHuman && a.isValid);
        if (clip == null || !clip.humanMotion || sourceAvatar == null)
        {
            Debug.LogError("[MagicHeal] FBX must import as valid Humanoid with looping " +
                           AnimationName + ". No scenes were modified.");
            return;
        }
        Directory.CreateDirectory(Backup);
        foreach (string scene in ScenePaths)
        {
            string backupName = scene.Replace('/', '_') + ".before_magic_heal.unity";
            string destination = Path.Combine(Backup, backupName);
            if (!File.Exists(destination))
                File.Copy(scene, destination);
        }

        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Animations/SoulFeeding", "Controllers");
        AvatarMask mask = EnsureUpperBodyMask();
        string returnScene = EditorSceneManager.GetActiveScene().path;
        try
        {
            foreach (string scenePath in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                SoulFragmentFeeding[] all = UnityEngine.Object.FindObjectsByType<SoulFragmentFeeding>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                    // Hollow Knight's fragment is embedded in his body.
                    // Never add an external feeding animation to this boss.
                    .Where(f => f.gameObject.scene == scene &&
                                f.GetComponent<HollowKnightController>() == null).ToArray();
                int updated = 0;
                foreach (SoulFragmentFeeding feeding in all)
                {
                    var serial = new SerializedObject(feeding);
                    var animator = serial.FindProperty("characterAnimator")
                        .objectReferenceValue as Animator;
                    if (animator == null || animator.avatar == null ||
                        !animator.isHuman || !animator.avatar.isValid)
                    {
                        // Existing authored Titan/Stone Golem behavior
                        // (and its nonhumanoid visual fallback) is preserved.
                        continue;
                    }
                    var original = animator.runtimeAnimatorController as AnimatorController;
                    if (original == null)
                    {
                        Debug.LogWarning("[MagicHeal] Skipping Animator without standard controller: " +
                                         animator.name, animator);
                        continue;
                    }
                    // Keep Bramble's user-approved seamless 28–42 clip when
                    // this general retargeting installer is run again.
                    // Never silently revert the first==last-keyframe fix.
                    AnimationClip assignedClip = clip;
                    if (feeding.GetComponent<BrambleSpriteController>() != null)
                    {
                        var seamless = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                            "Assets/Animations/SoulFeeding/Bramble_MagicHeal_Seamless.anim");
                        if (seamless != null && seamless.humanMotion && seamless.isLooping)
                            assignedClip = seamless;
                    }
                    AnimatorController target = EnsureDerivedController(
                        original, scene.name, feeding.name, mask, assignedClip);
                    if (target == null)
                        continue;
                    if (animator.runtimeAnimatorController != target)
                    {
                        animator.runtimeAnimatorController = target;
                        EditorUtility.SetDirty(animator);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    }
                    serial.Update();
                    serial.FindProperty("useImportedMagicHeal").boolValue = true;
                    serial.FindProperty("magicHealLayerName").stringValue = LayerName;
                    serial.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feeding);
                    ++updated;
                    Debug.Log("[MagicHeal] " + scene.name + " " + feeding.name +
                              " -> " + AssetDatabase.GetAssetPath(target));
                }
                if (updated > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Scene failed to save: " + scenePath);
                }
                Debug.Log("[MagicHeal] " + scene.name + ": " + updated +
                          " humanoid feeding Animators installed; other actors unchanged.");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(returnScene))
                EditorSceneManager.OpenScene(returnScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }
    }

    private static AvatarMask EnsureUpperBodyMask()
    {
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, MaskPath);
        }
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; ++i)
        {
            var part = (AvatarMaskBodyPart)i;
            bool enabled = part == AvatarMaskBodyPart.Body ||
                           part == AvatarMaskBodyPart.Head ||
                           part == AvatarMaskBodyPart.LeftArm ||
                           part == AvatarMaskBodyPart.RightArm ||
                           part == AvatarMaskBodyPart.LeftFingers ||
                           part == AvatarMaskBodyPart.RightFingers;
            mask.SetHumanoidBodyPartActive(part, enabled);
        }
        EditorUtility.SetDirty(mask);
        return mask;
    }

    private static AnimatorController EnsureDerivedController(
        AnimatorController source, string scene, string enemy, AvatarMask mask,
        AnimationClip clip)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(sourcePath))
            return null;
        string filename = MakeSafe(scene + "_" + enemy + "_MagicHeal") + ".controller";
        string newPath = OutputFolder + "/" + filename;
        var target = AssetDatabase.LoadAssetAtPath<AnimatorController>(newPath);
        if (target == null)
        {
            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                Debug.LogError("[MagicHeal] Could not duplicate controller: " + sourcePath);
                return null;
            }
            target = AssetDatabase.LoadAssetAtPath<AnimatorController>(newPath);
        }
        if (target == null)
            return null;
        int existingLayer = Array.FindIndex(target.layers, l => l.name == LayerName);
        if (existingLayer < 0)
            target.AddLayer(LayerName);

        AnimatorControllerLayer[] layers = target.layers;
        int idx = Array.FindIndex(layers, l => l.name == LayerName);
        if (idx < 0)
            return null;
        AnimatorControllerLayer layer = layers[idx];
        layer.avatarMask = mask;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.defaultWeight = 0f;
        layer.iKPass = false;
        layer.syncedLayerIndex = -1;
        layer.stateMachine.name = "Soul Feeding Upper Body";
        AnimatorState state = layer.stateMachine.states
            .Select(s => s.state)
            .FirstOrDefault(s => s.name == "Magic Heal (loop)");
        if (state == null)
            state = layer.stateMachine.AddState("Magic Heal (loop)");
        state.motion = clip;
        state.speed = 1f;
        layer.stateMachine.defaultState = state;
        layers[idx] = layer;
        target.layers = layers;
        EditorUtility.SetDirty(target);
        return target;
    }

    private static string MakeSafe(string text)
    {
        return new string(text.Select(c =>
            char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
    }
}
