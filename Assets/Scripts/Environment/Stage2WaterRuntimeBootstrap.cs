using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2WaterRuntimeBootstrap
{
    // Keep Stage 2 runtime bootstrap scripts in the same compilation refresh path after NavMesh diagnostics.
    private const string Stage2SceneName = "Stage2Scene";
    private const string RuntimeVolumeName = "Stage2_ShallowWaterVolume_Runtime";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    /// <summary>
    /// Idempotent Stage 2 water bootstrap. Besides the sceneLoaded hook, the
    /// persistent player calls this explicitly while binding Stage2Scene so DDOL
    /// scene transitions cannot miss water registration due to callback timing.
    /// </summary>
    public static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != Stage2SceneName)
            return;

        GameObject water = FindSceneObject(scene, "Stage2_WaterSurface");
        GameObject map = FindSceneObject(scene, "STAGE2MAP");
        if (water == null || map == null)
        {
            Debug.LogWarning("[Stage2WaterRuntimeBootstrap] Water surface or STAGE2MAP was not found. Water interaction was skipped.");
            return;
        }

        Transform existing = water.transform.Find(RuntimeVolumeName);
        Stage2ShallowWaterVolume volume;
        if (existing != null)
        {
            volume = existing.GetComponent<Stage2ShallowWaterVolume>();
            if (volume == null)
                volume = existing.gameObject.AddComponent<Stage2ShallowWaterVolume>();
            volume.Configure(water.transform, map.transform, 2.5f);
        }
        else
        {
            GameObject trigger = new GameObject(RuntimeVolumeName)
            {
                hideFlags = HideFlags.DontSave
            };
            trigger.layer = water.layer;
            trigger.transform.SetParent(water.transform, false);
            trigger.transform.localPosition = Vector3.zero;
            trigger.transform.localRotation = Quaternion.identity;
            trigger.transform.localScale = Vector3.one;

            BoxCollider box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.75f, 0f);
            box.size = new Vector3(1f, 3.5f, 1f);

            volume = trigger.AddComponent<Stage2ShallowWaterVolume>();
            volume.Configure(water.transform, map.transform, 2.5f);
        }

        Stage2WaterInteraction[] interactions = Object.FindObjectsByType<Stage2WaterInteraction>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int boundCount = 0;
        for (int i = 0; i < interactions.Length; i++)
        {
            Stage2WaterInteraction interaction = interactions[i];
            if (interaction == null || !interaction.gameObject.activeInHierarchy)
                continue;

            bool belongsToStage2Scene = interaction.gameObject.scene == scene;
            bool isPersistentPlayer = PersistentPlayerRoot.Instance != null
                                      && interaction.gameObject == PersistentPlayerRoot.Instance.gameObject;
            if (!belongsToStage2Scene && !isPersistentPlayer)
                continue;

            interaction.RegisterStage2Volume(volume);
            boundCount++;
        }

        Debug.Log($"[Stage2WaterRuntimeBootstrap] READY waterY={water.transform.position.y:F3} shallowDepth=2.500 trigger={RuntimeVolumeName} boundInteractions={boundCount}");
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeepChild(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
