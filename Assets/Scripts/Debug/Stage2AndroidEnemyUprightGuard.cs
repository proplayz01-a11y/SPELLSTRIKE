using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Android-only correction for Stage 2 creature roots.
///
/// On the A05s Android player, the Stage 2 humanoid Animators can overwrite the
/// actor transform with a 90-degree X/Z root orientation after normal gameplay
/// Update. Stage 1 does not exhibit this behaviour. This guard deliberately runs
/// after normal scripts/Animator evaluation and restores only the upright actor
/// frame while preserving the yaw selected by the enemy controller.
///
/// It does not rotate by a hard-coded +/-90 degrees and does not alter position,
/// scale, NavMesh settings, colliders, or animation bones.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class Stage2AndroidEnemyUprightGuard : MonoBehaviour
{
    private const float UprightCaptureThreshold = 0.65f;

    private float desiredYaw;
    private bool initialized;
    private bool loggedCorrection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        if (!scene.IsValid() || scene.name != "Stage2Scene")
            return;

        AddToControllers(Resources.FindObjectsOfTypeAll<BarnacleHuskController>());
        AddToControllers(Resources.FindObjectsOfTypeAll<CorsairPhantomController>());
        AddToControllers(Resources.FindObjectsOfTypeAll<TideWraithController>());
        AddToControllers(Resources.FindObjectsOfTypeAll<DrownedCaptainController>());
    }

    private static void AddToControllers<T>(T[] controllers) where T : MonoBehaviour
    {
        if (controllers == null)
            return;

        foreach (T controller in controllers)
        {
            if (controller == null || !controller.gameObject.scene.IsValid() || controller.gameObject.scene.name != "Stage2Scene")
                continue;

            if (controller.GetComponent<Stage2AndroidEnemyUprightGuard>() == null)
                controller.gameObject.AddComponent<Stage2AndroidEnemyUprightGuard>();
        }
    }

    private void Awake()
    {
        CaptureCurrentYaw(true);
    }

    private void OnEnable()
    {
        CaptureCurrentYaw(true);
    }

    private void Update()
    {
        // Controller Update runs earlier (default execution order). Capture its
        // intended facing only while the actor is still upright. If Animator has
        // already tipped the root this frame, keep the previous valid yaw instead.
        CaptureCurrentYaw(false);
    }

    private void LateUpdate()
    {
        if (!initialized || Application.platform != RuntimePlatform.Android)
            return;

        if (!loggedCorrection && Vector3.Dot(transform.up, Vector3.up) < UprightCaptureThreshold)
        {
            Debug.Log($"[Stage2UprightGuard] Correcting {name} from worldEuler={transform.eulerAngles:F2} to yaw={desiredYaw:F2}", this);
            loggedCorrection = true;
        }

        transform.rotation = Quaternion.Euler(0f, desiredYaw, 0f);
    }

    private void CaptureCurrentYaw(bool force)
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        if (!force && Vector3.Dot(transform.up, Vector3.up) < UprightCaptureThreshold)
            return;

        desiredYaw = transform.eulerAngles.y;
        initialized = true;
    }
}
