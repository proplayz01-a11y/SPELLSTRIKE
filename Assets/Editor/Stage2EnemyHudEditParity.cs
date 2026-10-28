// Scene-local presentation-only finalizer. Stage2's separately-authored
// EnemyHealthBar is visible without an encounter in Edit Mode, but the
// persistent runtime HUD hides it until an enemy actually starts combat.
// Preserve the entire Slider and references, change only initial visibility.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class Stage2EnemyHudEditParity
{
    private const string PathToScene="Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string Backup="Library/GameplayHudEditModeParity_20261021/Stage2_BeforeEnemyBarDefaultHide.unity";

    [MenuItem("SPELLSTRIKE/HUD/Stage 2 Hide Unbound Enemy HP in Edit Mode (Official)",priority=237)]
    private static void Apply()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path != PathToScene || scene.isDirty || File.Exists(Backup))
        {
            Debug.LogError("[Stage2 HUD Parity] Requires saved Stage2 scene in Edit Mode with fresh backup.");
            return;
        }
        Transform[] bars=scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
            .Where(t=>t.name=="EnemyHealthBar").ToArray();
        if(bars.Length != 1 || bars[0].GetComponent<Slider>()==null)
        {
            Debug.LogError("[Stage2 HUD Parity] Expected exactly one scene-owned EnemyHealthBar Slider.");
            return;
        }
        if(!bars[0].gameObject.activeSelf)
        {
            Debug.Log("[Stage2 HUD Parity] The enemy HP preview is already hidden. No change.");
            return;
        }
        File.Copy(PathToScene,Backup,false);
        bars[0].gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,PathToScene))
            throw new IOException("Could not save Stage2 scene.");
        Debug.Log("[Stage2 HUD Parity] PASS: Unbound red enemy HP bar hidden in official Stage2 Edit Mode. Original Slider, serial refs and encounter show/hide logic untouched.");
    }
}
#endif
