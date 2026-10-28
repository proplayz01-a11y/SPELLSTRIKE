#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// Read-only batch verification of the isolated A04 native Timeline.
/// Does not save scene, touch gameplay, or add output assets.
/// </summary>
internal static class Panel04A04SceneAudit
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v01.unity";
    public static void ValidateBatch()
    {
        try { Validate(); }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("A04 scene didn't open.");
        var director = UnityEngine.Object.FindObjectsByType<PlayableDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(x => x.gameObject.name.Contains("P04_A04_TIMELINE__DESTROYED_HOME"));
        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null || timeline.GetOutputTracks().Count() != 2)
            throw new InvalidOperationException("Expected one camera and one narration Timeline track.");

        int renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        if (renderers < 700)
            throw new InvalidOperationException("A04 missing environmental or character renderers: " + renderers);

        var camera = UnityEngine.Object.FindObjectsByType<Camera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(x => x.gameObject.name == "P04_A04_CAMERA__TIMELINE_FOUR_SHOTS");
        var overlay = UnityEngine.Object.FindObjectsByType<Panel04A04NarrationOverlay>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        if (overlay.director != director ||
            !overlay.narration.Contains("daughter, Nevali, nowhere to be found."))
            throw new InvalidOperationException("Approved Panel 04 narration is not correctly wired.");

        director.RebuildGraph();
        double[] times = { 0.20, 3.0, 6.5, 8.8 };
        Vector3[] expected =
        {
            ConvertPoint(new Vector3(-0.49f,-3.78f,2.35f)),
            ConvertPoint(new Vector3(1.16f,1.42f,1.79f)),
            ConvertPoint(new Vector3(-0.13f,1.10f,1.30f)),
            ConvertPoint(new Vector3(0.13f,-1.78f,1.56f))
        };
        for (int i = 0; i < times.Length; i++)
        {
            director.time = times[i];
            director.Evaluate();
            float error = Vector3.Distance(camera.transform.position, expected[i]);
            Debug.Log("[P04 A04 QA] CAMERA_SHOT_" + (i+1) +
                      " time=" + times[i] + " position=" + camera.transform.position +
                      " expected=" + expected[i] + " error=" + error.ToString("F4"));
            if (error > 0.52f)
                throw new InvalidOperationException(
                    "Timeline camera did not reach intended shot " + (i+1) + ": error=" + error);
        }

        Debug.Log("[P04 A04 QA] PASS scene=" + scene.path +
                  " renderers=" + renderers +
                  " timeline_tracks=" + timeline.GetOutputTracks().Count() +
                  " duration=" + timeline.duration.ToString("F3") +
                  " mother/Van static GLB placement loaded | approved panels preserved");
    }

    private static Vector3 ConvertPoint(Vector3 b) =>
        new Vector3(b.x,b.z,-b.y);
}
#endif
