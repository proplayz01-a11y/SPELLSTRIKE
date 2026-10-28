// One-time, visible Unity menu repair for the official Tutorial scene only.
// The persistent player teleporter reads PlayerSpawnPoint.PlayerScale at load.
// Tutorial previously had a named marker but NO PlayerSpawnPoint component,
// causing fallback scale=Vector3.one regardless of editor player size.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class TutorialPlayerScale10Fix
{
    private const string TutorialPath = "Assets/Scenes/TutorialScene.unity";
    private const string BackupPath =
        "Library/TutorialPlayerScale10_20261021_1043/scene_before_scale10_fix.unity";
    private static readonly Vector3 RequiredScale = Vector3.one * 10f;

    [MenuItem("SPELLSTRIKE/Tutorial/Make Persistent Van Aert Scale 10 (Official)")]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded ||
            scene.path != TutorialPath || scene.isDirty ||
            SceneManager.sceneCount != 1)
        {
            Debug.LogError("[Tutorial Scale10] Requires saved official TutorialScene in Edit Mode.");
            return;
        }

        try
        {
            if (File.Exists(BackupPath))
                throw new InvalidOperationException("A prior scale10 repair backup exists. Refusing repeat.");

            var objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform[] markers = objects.Where(t => t.name == "PlayerSpawnPoint").ToArray();
            PersistentPlayerRoot[] playerRoots = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PersistentPlayerRoot>(true)).ToArray();
            if (markers.Length != 1 || playerRoots.Length != 1 ||
                playerRoots[0].name != "VAN AERT" ||
                markers[0].GetComponent<PlayerSpawnPoint>() != null)
                throw new InvalidOperationException(
                    "Unexpected Tutorial scene player/marker state. No change.");

            Transform player = playerRoots[0].transform;
            Vector3 formerScale = player.localScale;
            Vector3 formerPosition = player.position;
            Vector3 markerPosition = markers[0].position;

            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            File.Copy(TutorialPath, BackupPath, overwrite: false);

            PlayerSpawnPoint marker = Undo.AddComponent<PlayerSpawnPoint>(markers[0].gameObject);
            Undo.RecordObject(marker, "Tutorial persistent player spawn scale 10");
            marker.SetPlayerScale(RequiredScale);
            EditorUtility.SetDirty(marker);

            Undo.RecordObject(player, "Tutorial Van Aert editor scale 10");
            player.localScale = RequiredScale;
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            if (marker.PlayerScale != RequiredScale ||
                player.localScale != RequiredScale ||
                player.position != formerPosition ||
                markers[0].position != markerPosition)
                throw new InvalidOperationException("Tutorial scale or position post-check failed.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, TutorialPath))
                throw new IOException("TutorialScene failed to save.");

            Debug.Log("[Tutorial Scale10] PASS: official TutorialScene marker now serializes " +
                "PlayerSpawnPoint.PlayerScale=(10,10,10) and its existing VAN AERT " +
                "prefab instance has editor localScale=(10,10,10). Was " +
                formerScale + ". PersistentPlayerRoot.TeleportTo will use 10 at " +
                "Play Mode/scene load. Original player prefab, movement, camera, " +
                "all other stage scales and saved progress untouched. Backup: " + BackupPath);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[Tutorial Scale10] Stopped; inspect scene and exact backup before any retry.");
        }
    }
}
#endif
