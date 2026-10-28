#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class Stage1CopyTutorialJoystickOnce
{
    private const string Marker = "Temp/Stage1CopyTutorialJoystick.trigger";
    private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string TargetScenePath = "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity";

    static Stage1CopyTutorialJoystickOnce() => EditorApplication.update += TryRun;

    private static void TryRun()
    {
        string marker = Path.GetFullPath(Marker);
        if (!File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(marker);

        Scene target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Scene tutorial = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);
        try
        {
            RectTransform source = FindRect(tutorial, "MobileMovementJoystick");
            RectTransform destination = FindRect(target, "MobileMovementJoystick");
            if (source == null || destination == null)
                throw new InvalidOperationException("Tutorial or Stage 1 joystick was not found.");

            CopyRect(source, destination);
            CopyImage(source.GetComponent<Image>(), destination.GetComponent<Image>());

            RectTransform sourceKnob = source.Find("Knob") as RectTransform;
            RectTransform destinationKnob = destination.Find("Knob") as RectTransform;
            if (sourceKnob == null || destinationKnob == null)
                throw new InvalidOperationException("Tutorial or Stage 1 joystick knob was not found.");

            CopyRect(sourceKnob, destinationKnob);
            CopyImage(sourceKnob.GetComponent<Image>(), destinationKnob.GetComponent<Image>());

            MobileJoystickController sourceController = source.GetComponent<MobileJoystickController>();
            MobileJoystickController destinationController = destination.GetComponent<MobileJoystickController>();
            if (sourceController == null || destinationController == null)
                throw new InvalidOperationException("Tutorial or Stage 1 MobileJoystickController was not found.");

            SerializedObject sourceSo = new(sourceController);
            SerializedObject destinationSo = new(destinationController);
            float range = sourceSo.FindProperty("joystickRange").floatValue;
            destinationSo.FindProperty("handle").objectReferenceValue = destinationKnob;
            destinationSo.FindProperty("joystickRange").floatValue = range;
            destinationSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target);
            Debug.Log($"[Stage1 Joystick Sync] COMPLETE exact TutorialScene joystick copied to Stage1 test | size={destination.sizeDelta} knobScale={destinationKnob.localScale.x:F2} range={range:F0}");
        }
        finally
        {
            if (tutorial.IsValid() && tutorial.isLoaded)
                EditorSceneManager.CloseScene(tutorial, true);
        }
    }

    private static RectTransform FindRect(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name)
                    return rect;
        return null;
    }

    private static void CopyRect(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.localScale = source.localScale;
        destination.localRotation = source.localRotation;
        EditorUtility.SetDirty(destination);
    }

    private static void CopyImage(Image source, Image destination)
    {
        if (source == null || destination == null) return;
        destination.sprite = source.sprite;
        destination.type = source.type;
        destination.color = source.color;
        destination.raycastTarget = source.raycastTarget;
        destination.preserveAspect = source.preserveAspect;
        EditorUtility.SetDirty(destination);
    }
}
#endif
