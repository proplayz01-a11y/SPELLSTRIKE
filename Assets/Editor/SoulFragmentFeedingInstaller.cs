using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Additively installs the visual-only precombat soul-channeling component.
/// No enemy AI scripts, pickup scripts, objective counters, timeline assets,
/// animation controllers, or third-party packages are modified.
/// </summary>
public static class SoulFragmentFeedingInstaller
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity",
        "Assets/Scenes/Stage3Scene/Stage3Scene.unity"
    };

    [MenuItem("SPELLSTRIKE/Story/Install Pre-Battle Soul Feeding (Backed Up)")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
            EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[SoulFeedingInstaller] Stop Play Mode and save the current scene first.");
            return;
        }
        string previousScene = EditorSceneManager.GetActiveScene().path;
        string backup = "Temp/SoulFragmentFeedingBackup_20261123";
        Directory.CreateDirectory(backup);
        foreach (string path in Scenes)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing story scene: " + path);
            string dest = Path.Combine(backup,
                path.Replace('/', '_') + ".before_feeding.unity");
            if (!File.Exists(dest))
                File.Copy(path, dest);
        }

        try
        {
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameObject[] enemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(h => h.gameObject.scene == scene)
                    .Select(h => h.gameObject).ToArray();
                Transform[] fragments = UnityEngine.Object.FindObjectsByType<FragmentPickup>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(f => f.gameObject.scene == scene)
                    .Select(f => f.transform)
                    .Concat(UnityEngine.Object.FindObjectsByType<BossFragmentPickup>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(f => f.gameObject.scene == scene)
                        .Select(f => f.transform)).ToArray();
                // Hollow Knight is the special Stage 1 case: Nevali's voice
                // fragment is embedded inside him. He does NOT perform the
                // external soul-feeding ritual. Reserve his boss fragment as
                // well, so another enemy cannot accidentally be linked to it.
                if (scene.name == "SampleScene")
                {
                    HollowKnightController hollowKnight =
                        UnityEngine.Object.FindObjectsByType<HollowKnightController>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                            .FirstOrDefault(h => h.gameObject.scene == scene);
                    if (hollowKnight != null)
                    {
                        Transform embeddedFragment = fragments
                            .Where(f => f.GetComponent<BossFragmentPickup>() != null)
                            .OrderBy(f => PlanarDistance(hollowKnight.transform, f))
                            .FirstOrDefault();
                        if (embeddedFragment != null)
                            fragments = fragments.Where(f => f != embeddedFragment).ToArray();
                    }
                }
                int total = 0;
                // Greedy nearest unique assignment is unambiguous in the
                // existing stage maps (each enemy has one much closer shard).
                foreach (GameObject enemy in enemies
                             .Where(e => e.GetComponent<HollowKnightController>() == null)
                             .OrderBy(e => NearestDistance(e.transform, fragments)))
                {
                    if (fragments.Length == 0)
                    {
                        Debug.LogWarning("[SoulFeedingInstaller] Fragment missing for " + enemy.name);
                        continue;
                    }
                    Transform fragment = fragments.OrderBy(f => PlanarDistance(enemy.transform, f))
                        .First();
                    fragments = fragments.Where(f => f != fragment).ToArray();
                    Animator animator = enemy.GetComponentInChildren<Animator>(true);
                    bool isHumanoid = animator != null && animator.avatar != null &&
                                      animator.isHuman && animator.avatar.isValid;

                    SoulFragmentFeeding feeding = enemy.GetComponent<SoulFragmentFeeding>();
                    if (feeding == null)
                        feeding = Undo.AddComponent<SoulFragmentFeeding>(enemy);
                    var serialized = new SerializedObject(feeding);
                    serialized.FindProperty("fragment").objectReferenceValue = fragment;
                    serialized.FindProperty("characterAnimator").objectReferenceValue = animator;
                    serialized.FindProperty("poseHumanoidWhileFeeding").boolValue = isHumanoid;
                    // Titan must retain its stone-statue pose before awakening,
                    // and golems without a humanoid rig only receive the beam.
                    serialized.FindProperty("faceFragmentBeforeBattle").boolValue = isHumanoid;
                    // All feeding scenes now standardize on the right hand.
                    // The runtime component resolves right-hand Humanoid bones
                    // or right-hand skeleton transforms on nonhumanoids.
                    serialized.FindProperty("poseStrength").floatValue = 0.85f;
                    serialized.FindProperty("ritualEnergyWidth").floatValue = 0.9f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feeding);
                    ++total;

                    Debug.Log("[SoulFeedingInstaller] " + scene.name + ": " +
                              enemy.name + " => " + fragment.name +
                              " (planar gap=" + PlanarDistance(enemy.transform, fragment).ToString("F1") +
                              "m, humanoid=" + isHumanoid + ")");
                }

                if (total > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Couldn't save feeding setup: " + path);
                }
                Debug.Log("[SoulFeedingInstaller] " + scene.name + ": installed " +
                          total + " enemy-feeding sequences.");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    private static float PlanarDistance(Transform a, Transform b)
    {
        Vector2 p = new Vector2(a.position.x, a.position.z);
        Vector2 q = new Vector2(b.position.x, b.position.z);
        return Vector2.Distance(p, q);
    }

    private static float NearestDistance(Transform actor, Transform[] fragments)
    {
        if (fragments.Length == 0)
            return float.MaxValue;
        return fragments.Min(f => PlanarDistance(actor, f));
    }
}
