using System;
using System.IO;
using Spellstrike.ComicMode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Isolated preview installer. Never overwrites a scene or changes Build Settings.
/// Created by a manual menu action only after prime has approved Editor access.
/// </summary>
public static class ComicModeInstaller
{
    private const string PreviewPath =
        "Assets/Cinematics/ComicMode/Preview/ComicMode_Review_v01.unity";
    private const string EntryPath =
        "Assets/Cinematics/ComicMode/Preview/ComicMode_StoryModeEntry_v01.unity";
    private const string ResourcesDir = "Assets/Resources/ComicMode";

    [MenuItem("SPELLSTRIKE/Comic Mode/Validate historical 42-panel images")]
    public static void Validate()
    {
        string report;
        bool valid = ValidateFiles(out report);
        if (valid) Debug.Log("[ComicMode] " + report);
        else Debug.LogError("[ComicMode] " + report);
        EditorUtility.DisplayDialog("Comic Mode validation", report, "OK");
    }

    [MenuItem("SPELLSTRIKE/Comic Mode/Create isolated preview scene v01")]
    public static void CreateIsolatedPreview()
    {
        CreateSafeScene(PreviewPath, "ComicMode_PreviewStart", typeof(ComicModePreviewStart));
    }

    [MenuItem("SPELLSTRIKE/Comic Mode/Create isolated story mode entry scene v01")]
    public static void CreateStoryModeEntry()
    {
        CreateSafeScene(EntryPath, "ComicMode_RouteBootstrap", typeof(ComicModeRouteBootstrap));
    }

    private static void CreateSafeScene(string outputPath, string objectName, Type component)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Scene protected",
                "Exit Play Mode before creating a new comic review scene.", "OK");
            return;
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene existing = SceneManager.GetSceneAt(i);
            if (existing.IsValid() && existing.isDirty)
            {
                EditorUtility.DisplayDialog("Scene protected",
                    "A loaded scene has unsaved changes: " + existing.name +
                    ". Save it yourself before creating a comic preview.", "OK");
                return;
            }
        }
        if (File.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("Version protected",
                "Scene already exists; this command will never overwrite " + outputPath, "OK");
            return;
        }
        if (!ValidateFiles(out string report))
        {
            EditorUtility.DisplayDialog("Comic images missing", report, "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Cinematics/ComicMode/Preview"))
            AssetDatabase.CreateFolder("Assets/Cinematics/ComicMode", "Preview");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject(objectName, component);
        if (!EditorSceneManager.SaveScene(scene, outputPath))
        {
            Debug.LogError("[ComicMode] Failed to save isolated scene at " + outputPath);
            return;
        }

        Debug.Log("[ComicMode] New isolated scene created: " + outputPath +
                  ". Open manually and press Play to inspect." +
                  " No Build Settings or production scenes were changed.");
    }

    private static bool ValidateFiles(out string report)
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(
            ResourcesDir + "/manifest.json");
        if (json == null)
        {
            report = "Missing ComicMode manifest.json; run ComicModeBuildAssets.py.";
            return false;
        }

        ComicModeManifest data;
        try { data = JsonUtility.FromJson<ComicModeManifest>(json.text); }
        catch (Exception ex)
        {
            report = "Manifest parse failure: " + ex.Message;
            return false;
        }
        if (data == null || data.panels == null || data.panels.Length != 42)
        {
            report = "Expected exactly 42 panel records.";
            return false;
        }
        int rejected = 0;
        int fallbacks = 0;
        int exceptions = 0;
        for (int i = 0; i < 42; i++)
        {
            ComicModePanel panel = data.panels[i];
            if (panel == null || panel.id != i + 1 ||
                !File.Exists(ResourcesDir + "/Panel_" + (i + 1).ToString("00") + ".bytes"))
            {
                report = "Missing or out-of-order historical image for panel " + (i + 1);
                return false;
            }

            if (panel.artStatus == "rejected") rejected++;
            else if (panel.artStatus == "original_fallback") fallbacks++;
            else if (panel.artStatus == "approved_exception") exceptions++;
            else
            {
                report = "Unknown classification for panel " + (i + 1);
                return false;
            }
        }
        report = "42 panels found. Rejected: " + rejected + ", Work fallback: " + fallbacks +
                 ", approved exception: " + exceptions +
                 ". Check ComicModeMapping.md for exact source paths.";
        return true;
    }
}
