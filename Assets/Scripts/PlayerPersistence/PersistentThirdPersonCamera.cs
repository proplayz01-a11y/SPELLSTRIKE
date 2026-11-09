using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Owns the Huts Studio FreeLook camera as a persistent runtime object while
/// attaching a Cinemachine Brain to each scene's physical Main Camera.
/// The gameplay scene and its dirty editor state are never modified/saved.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PersistentThirdPersonCamera : MonoBehaviour
{
    [Header("Imported Huts Studio Camera")]
    [SerializeField] private GameObject cameraRigPrefab;

    [Header("Imported CameraTarget local offset")]
    [SerializeField] private Vector3 cameraTargetLocalOffset = new Vector3(0.397f, -0.11f, 0f);

    private GameObject cameraRigInstance;
    private CinemachineFreeLook freeLook;
    private SpellstrikeCinemachineInputBridge inputBridge;
    private Transform followTarget;
    private Transform lookTarget;
    private CombatCamera combatCamera;
    private float lastOrbitScale = -1f;
    private bool initialized;

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (!Application.isPlaying)
            EditorApplication.delayCall += SetupEditModeCameraPreview;
    }

    private void OnDisable()
    {
        EditorApplication.delayCall -= SetupEditModeCameraPreview;
    }

    private void SetupEditModeCameraPreview()
    {
        if (this == null || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        EnsureTargets();
        followTarget.localPosition = cameraTargetLocalOffset;
        lookTarget.localPosition = cameraTargetLocalOffset;

        freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
        if (freeLook == null)
            return;

        cameraRigInstance = freeLook.gameObject;
        Vector3 playerScale = transform.lossyScale;
        cameraRigInstance.transform.localScale = new Vector3(
            Mathf.Abs(playerScale.x) > 0.0001f ? 1f / playerScale.x : 1f,
            Mathf.Abs(playerScale.y) > 0.0001f ? 1f / playerScale.y : 1f,
            Mathf.Abs(playerScale.z) > 0.0001f ? 1f / playerScale.z : 1f);
        freeLook.Follow = followTarget;
        freeLook.LookAt = lookTarget;

        Camera outputCamera = Camera.main;
        if (outputCamera == null || outputCamera.gameObject.scene != gameObject.scene)
        {
            outputCamera = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.enabled &&
                    candidate.gameObject.scene == gameObject.scene);
        }

        if (outputCamera == null)
            return;

        CinemachineBrain brain = outputCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = outputCamera.gameObject.AddComponent<CinemachineBrain>();

        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
    }
#endif

    public void Initialize()
    {
        if (initialized)
            return;

        EnsureTargets();

        if (cameraRigPrefab == null)
        {
            Debug.LogError("[PersistentThirdPersonCamera] FreeLook camera prefab is not assigned.", this);
            return;
        }

        // Script recompiles/domain reloads do not serialize private runtime
        // references. Re-adopt an existing DDOL rig rather than creating a
        // second camera when Unity reloads scripts during Play Mode.
        freeLook = Resources.FindObjectsOfTypeAll<CinemachineFreeLook>()
            .FirstOrDefault(candidate => candidate != null
                && candidate.name == "Persistent Huts Studio FreeLook Camera"
                && candidate.gameObject.scene.name == "DontDestroyOnLoad");

        if (freeLook != null)
        {
            cameraRigInstance = freeLook.gameObject;
            // A player prefab may already be under DontDestroyOnLoad when
            // Initialize runs. In that case the nested preview rig is also
            // discoverable as a DDOL object, but must still be detached from
            // the player so it does not inherit character rotation or scale.
            if (cameraRigInstance.transform.IsChildOf(transform))
            {
                cameraRigInstance.transform.SetParent(null, true);
                cameraRigInstance.name = "Persistent Huts Studio FreeLook Camera";
                DontDestroyOnLoad(cameraRigInstance);
            }
        }
        else
        {
            // Prefer the serialized Edit Mode preview nested in the player
            // prefab. Detach it at runtime so Van's changing scale never scales
            // the camera rig, then persist it as the original standalone rig.
            freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
            if (freeLook != null)
            {
                cameraRigInstance = freeLook.gameObject;
                cameraRigInstance.transform.SetParent(null, false);
                cameraRigInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                cameraRigInstance.transform.localScale = Vector3.one;
                cameraRigInstance.name = "Persistent Huts Studio FreeLook Camera";
                DontDestroyOnLoad(cameraRigInstance);
            }
            else
            {
                cameraRigInstance = Instantiate(cameraRigPrefab, Vector3.zero, Quaternion.identity);
                cameraRigInstance.name = "Persistent Huts Studio FreeLook Camera";
                DontDestroyOnLoad(cameraRigInstance);
                freeLook = cameraRigInstance.GetComponent<CinemachineFreeLook>();
            }
        }

        if (freeLook == null)
        {
            Debug.LogError("[PersistentThirdPersonCamera] Imported camera prefab has no CinemachineFreeLook.", cameraRigInstance);
            Destroy(cameraRigInstance);
            cameraRigInstance = null;
            return;
        }

        inputBridge = cameraRigInstance.GetComponent<SpellstrikeCinemachineInputBridge>();
        if (inputBridge == null)
            inputBridge = cameraRigInstance.AddComponent<SpellstrikeCinemachineInputBridge>();

        // Use the vendor's three-orbit FreeLook, but route input through the
        // project's existing CombatCamera gesture ownership instead of the
        // sample's global pointer input actions.
        freeLook.m_XAxis.m_InputAxisName = string.Empty;
        freeLook.m_YAxis.m_InputAxisName = string.Empty;
        freeLook.m_XAxis.m_MaxSpeed = 300f;
        freeLook.m_YAxis.m_MaxSpeed = 6.25f;
        freeLook.m_XAxis.m_InvertInput = false;
        freeLook.m_YAxis.m_InvertInput = true;
        freeLook.m_XAxis.SetInputAxisProvider(0, inputBridge);
        freeLook.m_YAxis.SetInputAxisProvider(1, inputBridge);
        // Preserve the orbit angles authored on the camera prefab.  The live
        // Play Mode framing is intentionally stored there instead of being
        // reset to a generic centered FreeLook position on every load.
        freeLook.Follow = followTarget;
        freeLook.LookAt = lookTarget;

        initialized = true;
        ApplyScaleDependentOrbits();
        BindSceneCamera();
    }

    public void BindSceneCamera()
    {
        if (!initialized)
        {
            Initialize();
            return;
        }

        if (freeLook == null)
            return;

        Camera outputCamera = Camera.main;
        if (outputCamera == null)
        {
            Debug.LogWarning("[PersistentThirdPersonCamera] No MainCamera found in the newly loaded scene.", this);
            return;
        }

        CinemachineBrain brain = outputCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = outputCamera.gameObject.AddComponent<CinemachineBrain>();

        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

        combatCamera = outputCamera.GetComponent<CombatCamera>();
        if (combatCamera == null)
            combatCamera = FindFirstObjectByType<CombatCamera>();

        if (combatCamera != null)
            combatCamera.SetCinemachineDriven(true);

        inputBridge?.SetSource(combatCamera);
        freeLook.Follow = followTarget;
        freeLook.LookAt = lookTarget;
        ApplyScaleDependentOrbits();

        Debug.Log($"[PersistentThirdPersonCamera] Cinemachine Brain bound to '{outputCamera.name}' for scene '{outputCamera.gameObject.scene.name}'.", outputCamera);
    }

    private void LateUpdate()
    {
        if (!initialized || followTarget == null || lookTarget == null)
            return;

        followTarget.localPosition = cameraTargetLocalOffset;

        Vector3 focus = combatCamera != null
            ? combatCamera.GetCurrentLookTarget()
            : transform.position;
        lookTarget.position = focus + transform.TransformVector(cameraTargetLocalOffset);

        ApplyScaleDependentOrbits();
    }

    private void EnsureTargets()
    {
        followTarget = transform.Find("CameraFollowTarget");
        if (followTarget == null)
        {
            GameObject target = new GameObject("CameraFollowTarget");
            followTarget = target.transform;
            followTarget.SetParent(transform, false);
        }

        lookTarget = transform.Find("CameraLookTarget");
        if (lookTarget == null)
        {
            GameObject target = new GameObject("CameraLookTarget");
            lookTarget = target.transform;
            lookTarget.SetParent(transform, false);
        }
    }

    private void ApplyScaleDependentOrbits()
    {
        if (freeLook == null)
            return;

        float scale = Mathf.Max(0.1f, Mathf.Abs(transform.lossyScale.x));
        if (Mathf.Abs(scale - lastOrbitScale) < 0.01f)
            return;

        // The source game's player is authored at 10x world scale. These
        // orbit values retain its established ~57-unit middle camera distance
        // while using the imported FreeLook's top/middle/bottom orbit blending.
        float factor = scale / 10f;
        freeLook.m_Orbits = new[]
        {
            new CinemachineFreeLook.Orbit(18f * factor, 40f * factor),
            new CinemachineFreeLook.Orbit(5f * factor, 57f * factor),
            new CinemachineFreeLook.Orbit(-5f * factor, 40f * factor)
        };

        lastOrbitScale = scale;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && cameraRigInstance != null)
            Destroy(cameraRigInstance);
    }
}
