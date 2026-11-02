using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary visual-QA isolation that preserves pre-existing unsaved scenes.
/// Does not save, unload or overwrite any non-target scene. Restoration uses
/// scene-path + root-name/index to survive play-mode domain reload.
/// </summary>
public static class ModeSelectIsolatedPreviewV01
{
    private const string TargetPath = "Assets/Scenes/MinigameModeSelect_Integration_v01.unity";
    private const string Key = "SPELLSTRIKE.ModeSelect.IsolatedPreview.RootBackup";

    [Serializable] private class Entry
    {
        public string scenePath;
        public int rootIndex;
        public string rootName;
        public bool wasActive;
    }
    [Serializable] private class Saved { public List<Entry> entries = new List<Entry>(); }

    [MenuItem("SPELLSTRIKE/Minigames/QA - Temporarily Hide Other Loaded Scenes")]
    public static void HideOthers()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.HasKey(Key))
        {
            Debug.LogWarning("[Mode Select QA] Previous isolation is still active. Restore first.");
            return;
        }
        Scene active = SceneManager.GetActiveScene();
        if (active.path != TargetPath)
        {
            Debug.LogError("[Mode Select QA] Activate the isolated integration scene first.");
            return;
        }
        Saved snapshot = new Saved();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene other = SceneManager.GetSceneAt(i);
            if (!other.IsValid() || !other.isLoaded || other.path == TargetPath) continue;
            GameObject[] roots = other.GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
                snapshot.entries.Add(new Entry { scenePath=other.path, rootIndex=j,
                    rootName=roots[j].name, wasActive=roots[j].activeSelf });
        }
        // Persist the restore manifest BEFORE hiding any objects.
        EditorPrefs.SetString(Key, JsonUtility.ToJson(snapshot));
        foreach (Entry entry in snapshot.entries)
        {
            Scene other = SceneManager.GetSceneByPath(entry.scenePath);
            if (!other.IsValid() || !other.isLoaded) continue;
            GameObject[] roots = other.GetRootGameObjects();
            if (entry.rootIndex < roots.Length && roots[entry.rootIndex].name == entry.rootName)
                roots[entry.rootIndex].SetActive(false);
        }
        Debug.Log("[Mode Select QA] Temporarily hid " + snapshot.entries.Count +
                  " roots in other scenes. Their original states were backed up in EditorPrefs. DO NOT SAVE OTHER SCENES. " +
                  "Use 'QA - Restore Other Loaded Scenes' when finished.");
    }

    [MenuItem("SPELLSTRIKE/Minigames/QA - Restore Other Loaded Scenes")]
    public static void RestoreOthers()
    {
        if (!EditorPrefs.HasKey(Key))
        {
            Debug.Log("[Mode Select QA] Nothing to restore.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Mode Select QA] Exit Play Mode before restoring.");
            return;
        }
        Saved snapshot = JsonUtility.FromJson<Saved>(EditorPrefs.GetString(Key));
        int restored = 0;
        foreach (Entry entry in snapshot.entries)
        {
            Scene other = SceneManager.GetSceneByPath(entry.scenePath);
            if (!other.IsValid() || !other.isLoaded) continue;
            GameObject[] roots = other.GetRootGameObjects();
            if (entry.rootIndex >= roots.Length || roots[entry.rootIndex].name != entry.rootName)
            {
                Debug.LogError("[Mode Select QA] Scene changed: " + entry.scenePath + "/" + entry.rootName +
                               ". Restore manifest RETAINED; manual reconciliation required.");
                return;
            }
            roots[entry.rootIndex].SetActive(entry.wasActive);
            restored++;
        }
        if (restored == snapshot.entries.Count)
        {
            EditorPrefs.DeleteKey(Key);
            Debug.Log("[Mode Select QA] Restored all " + restored + " external scene root active states. " +
                      "No scenes were saved or unloaded.");
        }
        else Debug.LogWarning("[Mode Select QA] Restored " + restored + "/" + snapshot.entries.Count +
                              "; manifest retained for scenes that were unloaded.");
    }
}
