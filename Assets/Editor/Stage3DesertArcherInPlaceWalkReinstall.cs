using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Stage3DesertArcherInPlaceWalkReinstall
{
    private const string Root = "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 2/Animations/";

    private readonly struct ClipSpec
    {
        public readonly string Name;
        public readonly string FbxPath;
        public readonly string AnimPath;

        public ClipSpec(string name)
        {
            Name = name;
            FbxPath = Root + "S3N2_DesertArcher_Mixamo@" + name + ".fbx";
            AnimPath = Root + name + ".anim";
        }
    }

    private static readonly ClipSpec[] Clips =
    {
        new ClipSpec("Standing Aim Walk Back"),
        new ClipSpec("Standing Aim Walk Left"),
        new ClipSpec("Standing Aim Walk Right")
    };

    [MenuItem("SPELLSTRIKE/Stage 3/Desert Archer/Reinstall In-Place Walk Clips %#i")]
    public static void Reinstall()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (ClipSpec spec in Clips)
                ReinstallClip(spec);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DesertArcher][IN-PLACE-REINSTALL] PASS Reinstalled Back/Left/Right walk motion data into the existing standalone .anim assets. Animator state references/GUIDs were preserved.");
    }

    private static void ReinstallClip(ClipSpec spec)
    {
        AssetDatabase.ImportAsset(spec.FbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        AnimationClip[] importedClips = AssetDatabase.LoadAllAssetsAtPath(spec.FbxPath)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (importedClips.Length != 1)
        {
            string names = string.Join(", ", importedClips.Select(clip => clip.name));
            throw new InvalidOperationException(
                $"Expected exactly one imported animation clip in '{spec.FbxPath}', found {importedClips.Length}: [{names}]. No standalone .anim was changed for this clip.");
        }

        AnimationClip source = importedClips[0];
        AnimationClip target = AssetDatabase.LoadAssetAtPath<AnimationClip>(spec.AnimPath);
        if (target == null)
            throw new InvalidOperationException($"Standalone animation '{spec.AnimPath}' is missing.");

        AnimationClipSettings preservedSettings = AnimationUtility.GetAnimationClipSettings(target);
        string preservedName = target.name;

        EditorUtility.CopySerialized(source, target);
        target.name = preservedName;
        AnimationUtility.SetAnimationClipSettings(target, preservedSettings);
        EditorUtility.SetDirty(target);

        Debug.Log(
            $"[DesertArcher][IN-PLACE-REINSTALL] '{spec.Name}' source='{source.name}' " +
            $"length={source.length:0.000}s -> existing '{spec.AnimPath}', loop={preservedSettings.loopTime}.");
    }
}
