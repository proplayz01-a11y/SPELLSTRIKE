using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 2 Android-only render-cost reductions that do not alter gameplay,
/// collision, navigation, or the desktop presentation.
/// </summary>
public static class Stage2AndroidPerformanceProfile
{
    private const string Stage2SceneName = "Stage2Scene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        return;
#else
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
#endif
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        return;
#else
        if (!scene.IsValid() || scene.name != Stage2SceneName)
            return;

        Transform map = FindSceneTransform(scene, "STAGE2MAP");
        Transform water = FindSceneTransform(scene, "Stage2_WaterSurface");

        int changed = 0;
        int triangleEstimate = 0;

        if (map != null)
        {
            Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer is SkinnedMeshRenderer)
                    continue;

                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    changed++;
                }

                Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null)
                    continue;

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    triangleEstimate += (int)(mesh.GetIndexCount(sub) / 3u);
            }
        }

        // A huge transparent water plane should never spend a shadow-map draw.
        if (water != null)
        {
            Renderer[] renderers = water.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    changed++;
                }
            }
        }

        Debug.Log($"[Stage2Perf] Android static-environment shadow casting disabled renderers={changed} mapTriangleEstimate={triangleEstimate:N0}. Shadow receiving is preserved for dynamic character shadows.");
#endif
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeep(root.transform, objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        return null;
    }
}
