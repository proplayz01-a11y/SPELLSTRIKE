using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private Camera boundOutputCamera;
    private int boundSceneHandle = int.MinValue;
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
        BindSceneCamera(SceneManager.GetActiveScene());
    }

    // The persistent player passes the newly loaded scene explicitly, because
    // SceneManager.sceneLoaded can fire before Camera.main changes ownership.
    public void BindSceneCamera(Scene scene)
    {
        if (!initialized)
        {
            Initialize();
            if (!initialized)
                return;
        }

        if (freeLook == null)
            return;

        Camera outputCamera = FindOutputCameraInScene(scene);
        if (outputCamera == null)
        {
            Debug.LogWarning($"[PersistentThirdPersonCamera] No active scene camera found in '{scene.name}'.", this);
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
        DisableAutomaticObstacleAvoidance();

        // This object is already a DontDestroyOnLoad FreeLook rig. Keep ONE
        // collision guard on the rig across all stages; only its physical
        // output camera reference changes when the next scene loads.
        SpellstrikeCameraObstacleGuard guard =
            cameraRigInstance.GetComponent<SpellstrikeCameraObstacleGuard>();
        if (guard == null)
            guard = cameraRigInstance.AddComponent<SpellstrikeCameraObstacleGuard>();
        guard.Bind(outputCamera, transform, followTarget, lookTarget);
        guard.enabled = true;

        // A guard attached to a scene camera by an older loaded assembly must
        // not fight the persistent guard after a Play Mode script hot-reload.
        SpellstrikeCameraObstacleGuard oldSceneGuard =
            outputCamera.GetComponent<SpellstrikeCameraObstacleGuard>();
        if (oldSceneGuard != null && oldSceneGuard != guard)
            oldSceneGuard.enabled = false;

        boundOutputCamera = outputCamera;
        boundSceneHandle = scene.handle;

        Debug.Log($"[PersistentThirdPersonCamera] Cinemachine Brain bound to '{outputCamera.name}' for scene '{outputCamera.gameObject.scene.name}'.", outputCamera);
    }

    private void LateUpdate()
    {
        // Hot-reloading scripts during Play Mode clears private runtime
        // fields. Re-adopt the existing DDOL rig and rebind the new camera
        // instead of silently losing collision after recompilation.
        if (Application.isPlaying && !initialized)
            Initialize();

        if (!initialized || followTarget == null || lookTarget == null)
            return;

        // Scene changes and Play Mode script hot-reloads can leave a running
        // physical camera without its newly compiled collision components.
        // Rebind once when that happens, instead of requiring another Play
        // Mode restart or checking the components on every healthy frame.
        if (Application.isPlaying)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SpellstrikeCameraObstacleGuard guard = cameraRigInstance != null
                ? cameraRigInstance.GetComponent<SpellstrikeCameraObstacleGuard>()
                : null;
            if (activeScene.IsValid() && activeScene.isLoaded &&
                (boundOutputCamera == null || boundSceneHandle != activeScene.handle ||
                 !boundOutputCamera.isActiveAndEnabled || guard == null ||
                 !guard.isActiveAndEnabled || !guard.IsBoundTo(boundOutputCamera)))
                BindSceneCamera(activeScene);
        }

        followTarget.localPosition = cameraTargetLocalOffset;

        Vector3 focus = combatCamera != null
            ? combatCamera.GetCurrentLookTarget()
            : transform.position;
        lookTarget.position = focus + transform.TransformVector(cameraTargetLocalOffset);

        ApplyScaleDependentOrbits();
    }

    private static Camera FindOutputCameraInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled &&
            main.gameObject.scene == scene)
            return main;

        Camera fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;
                if (candidate.CompareTag("MainCamera"))
                    return candidate;
                if (fallback == null &&
                    (candidate.GetComponent<CombatCamera>() != null ||
                     candidate.GetComponent<CinemachineBrain>() != null))
                    fallback = candidate;
            }
        }
        return fallback;
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
            // The imported bottom orbit went 5 world units BELOW Van's
            // footing; on uneven terrain this put the camera under the floor.
            new CinemachineFreeLook.Orbit(3f * factor, 40f * factor)
        };

        lastOrbitScale = scale;
    }

    private void DisableAutomaticObstacleAvoidance()
    {
        if (!Application.isPlaying || freeLook == null)
            return;

        // This rig may already have both extensions from an older play session.
        // Both move the virtual camera and can fight the surface-stop guard.
        CinemachineDeoccluder deoccluder =
            freeLook.GetComponent<CinemachineDeoccluder>();
        if (deoccluder != null)
            deoccluder.enabled = false;
        CinemachineDecollider decollider =
            freeLook.GetComponent<CinemachineDecollider>();
        if (decollider != null)
            decollider.enabled = false;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && cameraRigInstance != null)
            Destroy(cameraRigInstance);
    }
}

/// <summary>
/// Stops the physical camera against walls, rocks and terrain surfaces without
/// swinging around objects. Executes after CinemachineBrain and before the
/// Arcane Surge impact impulse (execution order 10000).
/// </summary>
[DefaultExecutionOrder(9000)]
[DisallowMultipleComponent]
internal sealed class SpellstrikeCameraObstacleGuard : MonoBehaviour
{
    // Stage 3 architecture uses MeshColliders rather than TerrainColliders.
    // Exclude UI and Ignore Raycast, and filter the player's own colliders.
    private const int SolidLayers = ~((1 << 2) | (1 << 5));
    private const int MaxHits = 128;
    private readonly RaycastHit[] obstructionHits = new RaycastHit[MaxHits];
    private readonly RaycastHit[] floorHits = new RaycastHit[MaxHits];
    private readonly Collider[] overlapHits = new Collider[MaxHits];

    [SerializeField, Min(0.15f)] private float cameraRadius = 1.2f;
    [SerializeField, Min(0f)] private float wallPadding = 0.25f;
    [SerializeField, Min(0.15f)] private float floorClearance = 1.7f;
    [SerializeField, Min(0.15f)] private float heightAbovePlayerFeet = 2.0f;
    [SerializeField, Min(0.15f)] private float cameraPivotHeight = 11f;
    [SerializeField, Min(1f)] private float minimumFramingDistance = 17f;
    [SerializeField, Min(0f)] private float maxSurfaceClimb = 25f;
    [SerializeField, Min(0.01f)] private float returnSmoothSeconds = 0.24f;
    [SerializeField, Min(0f)] private float edgeReleaseDelay = 0.14f;

    private Camera outputCamera;
    private Transform playerRoot;
    private Transform lookTarget;
    private float previousDistance = -1f;
    private float returnVelocity;
    private float lastSnapTime = -100f;

    public bool IsBoundTo(Camera camera)
    {
        return camera != null && outputCamera == camera &&
               playerRoot != null && lookTarget != null;
    }

    public void Bind(Camera camera, Transform player, Transform follow, Transform look)
    {
        bool changed = outputCamera != camera || playerRoot != player;
        outputCamera = camera;
        playerRoot = player;
        lookTarget = look;
        if (changed)
        {
            previousDistance = -1f;
            returnVelocity = 0f;
            lastSnapTime = -100f;
        }
    }

    private void LateUpdate()
    {
        // The Spirit Remnant's temporary high-priority Cinemachine shot uses
        // the same physical camera. This gameplay-only obstacle guard must not
        // pull cinematic framing back toward Van's orbit pivot.
        if (SpiritRemnantCinematic.IsCinematicActive)
            return;

        if (!Application.isPlaying || outputCamera == null
            || !outputCamera.isActiveAndEnabled || playerRoot == null
            || lookTarget == null)
            return;

        float scale = Mathf.Clamp(Mathf.Abs(playerRoot.lossyScale.y) / 10f, 0.4f, 2f);
        Transform cameraTransform = outputCamera.transform;
        Vector3 rawPosition = cameraTransform.position;
        Quaternion rawRotation = cameraTransform.rotation;
        float radius = cameraRadius * scale;
        float safeY = playerRoot.position.y + heightAbovePlayerFeet * scale;

        // Project only as high as the ground immediately under the requested
        // camera. Do not mistake a distant cave ceiling for the floor.
        Vector3 desired = rawPosition;
        desired.y = Mathf.Max(desired.y, safeY);
        float floor = FindWalkableFloorHeight(desired, scale,
            desired.y + 2f * scale);
        desired.y = Mathf.Max(desired.y, floor + floorClearance * scale);

        // Start above Van's torso. At 10x scale his mesh is ~20 units tall:
        // casting from the feet used to catch walls just 2-6 units away and
        // put the camera INSIDE the character.
        Vector3 pivot = playerRoot.position + Vector3.up * (cameraPivotHeight * scale);
        Vector3 direction = desired - pivot;
        float fullDistance = direction.magnitude;
        if (fullDistance < 0.05f)
            return;
        direction /= fullDistance;

        bool blocked = TryGetSurfaceDistance(pivot, direction, fullDistance,
            radius, scale, out float safeDistance);

        // If the obstacle is extremely close to Van, prefer parking ABOVE a
        // walkable rock/cliff top at the intended camera XZ (when that creates
        // a clear shot), instead of zooming into Van's back. No orbiting around
        // a wall: the camera's intended azimuth is unchanged.
        if (blocked && safeDistance < minimumFramingDistance * scale
            && maxSurfaceClimb > 0f)
        {
            float top = FindWalkableFloorHeight(desired, scale,
                desired.y + maxSurfaceClimb * scale);
            if (top > desired.y - floorClearance * scale)
            {
                Vector3 onTop = new Vector3(desired.x,
                    Mathf.Max(desired.y, top + floorClearance * scale), desired.z);
                Vector3 topRay = onTop - pivot;
                float topDistance = topRay.magnitude;
                if (topDistance > 0.05f)
                {
                    topRay /= topDistance;
                    TryGetSurfaceDistance(pivot, topRay, topDistance,
                        radius, scale, out float topSafeDistance);
                    if (topSafeDistance >= minimumFramingDistance * scale
                        && topSafeDistance > safeDistance + 3f * scale)
                    {
                        desired = onTop;
                        direction = topRay;
                        fullDistance = topDistance;
                        safeDistance = topSafeDistance;
                        blocked = safeDistance < fullDistance - 0.001f;
                    }
                }
            }
        }

        // Snap IN to the nearest solid surface immediately; pause briefly at
        // collision edges and smooth ONLY the outward return. This prevents
        // a rock-edge hit/no-hit alternation from rattling the camera.
        float distance = safeDistance;
        if (previousDistance > 0f)
        {
            if (safeDistance < previousDistance - 0.01f)
            {
                lastSnapTime = Time.unscaledTime;
                returnVelocity = 0f;
            }
            else if (safeDistance > previousDistance)
            {
                distance = Time.unscaledTime - lastSnapTime < edgeReleaseDelay
                    ? previousDistance
                    : Mathf.Min(safeDistance, Mathf.SmoothDamp(previousDistance,
                        safeDistance, ref returnVelocity, returnSmoothSeconds,
                        Mathf.Infinity, Time.unscaledDeltaTime));
            }
        }
        previousDistance = distance;

        Vector3 corrected = pivot + direction * distance;
        // On slopes and platforms the nearest wall contact can be at a
        // different floor elevation than the original target point.
        float contactFloor = FindWalkableFloorHeight(corrected, scale,
            corrected.y + 2f * scale);
        float contactMinY = Mathf.Max(safeY, contactFloor + floorClearance * scale);
        if (corrected.y < contactMinY)
        {
            corrected.y = contactMinY; // rest on top of solid ground
            // Re-check the lifted pose against the near-side wall, so the
            // vertical floor correction cannot tunnel through geometry.
            Vector3 liftRay = corrected - pivot;
            float liftDistance = liftRay.magnitude;
            if (liftDistance > 0.05f)
            {
                liftRay /= liftDistance;
                if (TryGetSurfaceDistance(pivot, liftRay, liftDistance,
                    radius, scale, out float liftSafeDistance))
                    corrected = pivot + liftRay * liftSafeDistance;
            }
            corrected.y = Mathf.Max(corrected.y, contactMinY);
        }

        // Mesh seams and steep rock faces can still overlap the camera sphere
        // even after a sweep. Stay on the near side of that surface: retreat
        // along the same camera ray until the sphere is fully outside.
        // This never chooses a new horizontal orbit around the obstacle.
        if (HasSolidOverlap(corrected, radius) && !HasSolidOverlap(pivot, radius))
        {
            Vector3 towardSurface = corrected - pivot;
            float near = 0f;
            float far = 1f;
            for (int i = 0; i < 12; i++)
            {
                float middle = (near + far) * 0.5f;
                if (HasSolidOverlap(pivot + towardSurface * middle, radius))
                    far = middle;
                else
                    near = middle;
            }
            corrected = pivot + towardSurface * near;
            previousDistance = Mathf.Min(previousDistance, towardSurface.magnitude * near);
            returnVelocity = 0f;
            lastSnapTime = Time.unscaledTime;
        }

        if ((corrected - rawPosition).sqrMagnitude < 0.0001f)
            return;

        cameraTransform.position = corrected;
        // Keep the character in view when the obstruction displaces camera.
        Vector3 aim = lookTarget.position;
        aim.y = Mathf.Max(aim.y, playerRoot.position.y + 10f * scale);
        Vector3 view = aim - corrected;
        if (view.sqrMagnitude > 0.01f)
        {
            // Blend the corrected aim with the authored Cinemachine aim near
            // contact boundaries; never flip abruptly between feet and torso.
            float weight = Mathf.Clamp01(
                (corrected - rawPosition).magnitude / (10f * scale));
            Quaternion torsoAim = Quaternion.LookRotation(view.normalized, Vector3.up);
            cameraTransform.rotation = Quaternion.Slerp(rawRotation, torsoAim, weight);
        }
    }

    private bool TryGetSurfaceDistance(Vector3 origin, Vector3 direction,
        float distance, float radius, float scale, out float safeDistance)
    {
        safeDistance = distance;
        int count = Physics.SphereCastNonAlloc(origin, radius, direction,
            obstructionHits, distance, SolidLayers, QueryTriggerInteraction.Ignore);
        bool hitSolid = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = obstructionHits[i];
            if (IgnoreCollider(hit.collider))
                continue;
            hitSolid = true;
            safeDistance = Mathf.Min(safeDistance,
                Mathf.Max(0.05f, hit.distance - wallPadding * scale));
        }
        return hitSolid;
    }

    private bool HasSolidOverlap(Vector3 point, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(point, radius, overlapHits,
            SolidLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!IgnoreCollider(overlapHits[i]))
                return true;
        }
        return false;
    }

    private float FindWalkableFloorHeight(Vector3 point, float scale,
        float maxAllowedHeight)
    {
        // A walkable surface at or under camera level is real terrain, rock or
        // floor; overhead ceilings are ignored unless explicitly attempting
        // to rest the camera on top of an obstructing rock/cliff.
        Vector3 origin = point + Vector3.up * (30f * scale);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, floorHits,
            100f * scale, SolidLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = floorHits[i];
            if (IgnoreCollider(hit.collider) || hit.normal.y < 0.55f
                || hit.point.y > maxAllowedHeight)
                continue;
            best = Mathf.Max(best, hit.point.y);
        }
        return best;
    }

    private bool IgnoreCollider(Collider candidate)
    {
        if (candidate == null || !candidate.enabled || candidate.isTrigger)
            return true;
        Transform t = candidate.transform;
        if (playerRoot != null && (t == playerRoot || t.IsChildOf(playerRoot)))
            return true;
        // NPCs, loose pickups, and hitboxes are not stage architecture. They
        // should never zoom the camera inward during a sword/hammer animation.
        if (candidate.GetComponentInParent<EnemyHealth>() != null)
            return true;
        return false;
    }
}
