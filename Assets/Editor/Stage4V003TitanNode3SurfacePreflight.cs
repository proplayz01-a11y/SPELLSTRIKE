#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only Unity physics geometry acceptance for Titan's Stage4 island.
// This does NOT simulate V017 animation, solver contact, or gameplay damage.
public static class Stage4V003TitanNode3SurfacePreflight
{
    private const string QaScene = "Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity";
    private const string Output = "Assets/Stage 4/ConvergenceCitadel_v003/Validation/UnityTitanNode3SurfacePreflight_20261020.md";

    [MenuItem("Stage 4/Convergence Citadel v003/QA Titan Node3 Floor and Stance Clearance (No Play)")]
    public static void Run()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Output))
        {
            Debug.LogError("[Stage4 Titan FloorQA] Play Mode or existing report; refusing to rerun.");
            return;
        }
        var initial = SceneManager.GetActiveScene();
        var existing = SceneManager.GetSceneByPath(QaScene);
        bool already = existing.IsValid() && existing.isLoaded;
        if (already && existing.isDirty)
        {
            Debug.LogError("[Stage4 Titan FloorQA] Existing validation scene has unsaved work; refusing to touch.");
            return;
        }
        Scene target = default;
        var report = new StringBuilder();
        report.AppendLine("# Titan Earthbreaker / Node 3 surface preflight — Unity Editor-only, 2026-10-20");
        report.AppendLine("No animation, rig, IK, boss state, damage, NavMesh or Play Mode exercised. Imported dedicated Node3 static floor MeshCollider sampled in Unity.");
        report.AppendLine("Center imported Unity marker (-1280,18,-620). Use full V017 stance/sole/animation/hit QA separately.");
        int pass = 0, checks = 0, miss = 0, clearanceFail = 0;
        float largestDelta = 0f, lowestNormalY = 1f;
        try
        {
            target = already ? existing : EditorSceneManager.OpenScene(QaScene, OpenSceneMode.Additive);
            var root = target.GetRootGameObjects().Single(go => go.name.StartsWith("Stage4_ConvergenceCitadel_v003_"));
            var surface = root.GetComponentsInChildren<MeshCollider>(true).Single(c => c.name == "COL_Island_03_ContinuousFloor_PHYSICS");
            if (!surface.enabled || surface.sharedMesh == null || !surface.gameObject.isStatic)
                throw new InvalidOperationException("Invalid Node3 floor collider");
            Vector3 center = new Vector3(-1280f,18f,-620f);
            Physics.SyncTransforms();
            var radii = new[] { 0f,7f,10f,14f,20f,28.2f,49.3f,72.1f,98f,140f,200f };
            var angles = new[] { 0f,45f,90f,135f,180f,225f,270f,315f };
            report.AppendLine("- Sole baseline separation: 20 world units, plus/minus 10 across tangent; floor delta must be <=0.30 at each of the 88 locations.");
            foreach (float radius in radii)
            foreach (float angle in angles)
            {
                float radians = angle * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(radians),0,Mathf.Sin(radians));
                var tangent = new Vector3(-radial.z,0,radial.x);
                var at = center + radial * radius;
                bool ok = Sample(surface, at, out float height, out float normY);
                bool leftOk = Sample(surface, at + tangent * 10f, out float left, out float leftNormal);
                bool rightOk = Sample(surface, at - tangent * 10f, out float right, out float rightNormal);
                checks++;
                if (!ok || !leftOk || !rightOk)
                {
                    miss++;
                    report.AppendLine($"- FAIL missing floor: radius {radius:F1}, azimuth {angle:F0}, center={ok}, left={leftOk}, right={rightOk}");
                    continue;
                }
                float delta = Mathf.Abs(left - right);
                largestDelta = Mathf.Max(largestDelta, delta);
                lowestNormalY = Mathf.Min(lowestNormalY, normY, leftNormal, rightNormal);
                bool cleared = delta <= .30f && normY >= .90f && leftNormal >= .90f && rightNormal >= .90f;
                if (cleared) pass++;
                else
                {
                    clearanceFail++;
                    report.AppendLine($"- FAIL stance gradient radius={radius:F1} bearing={angle:F0} height={height:F3}, L={left:F3}, R={right:F3}, delta={delta:F3}, normals={normY:F3}/{leftNormal:F3}/{rightNormal:F3}");
                }
            }
            report.AppendLine($"- Sampled flat static floor: {pass}/{checks} stance stations PASS; no-floor={miss}; excessive tilt/sole elevation delta={clearanceFail}; max sole height delta={largestDelta:F4}; min normalY={lowestNormalY:F4}");
            report.AppendLine("- Does NOT establish V017 4/4 real right-sole contacts, 94%-reach, 0.35m native stance preflight, frame28 support, animation timing, 98m wave hit logic, actual rig movement or PC/Android FPS.");
        }
        catch (Exception e)
        {
            report.AppendLine("- AUDIT ERROR/FAIL: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (initial.IsValid() && initial.isLoaded) SceneManager.SetActiveScene(initial);
            if (!already && target.IsValid() && target.isLoaded) EditorSceneManager.CloseScene(target, true);
            File.WriteAllText(Output,report.ToString(),new UTF8Encoding(false));
            AssetDatabase.ImportAsset(Output);
            Debug.Log("[Stage4 Titan FloorQA] Preflight evidence written to " + Output + " : " + pass + "/" + checks);
        }
    }

    private static bool Sample(MeshCollider floor, Vector3 p, out float ground, out float normalY)
    {
        var ray = new Ray(new Vector3(p.x, 200f, p.z), Vector3.down);
        var hits = Physics.RaycastAll(ray, 360f, ~0, QueryTriggerInteraction.Ignore);
        var result = hits.Where(h => h.collider == floor).OrderByDescending(h => h.point.y).FirstOrDefault();
        if (result.collider == null) { ground = 0; normalY = 0; return false; }
        ground = result.point.y;
        normalY = result.normal.y;
        return true;
    }
}
#endif
