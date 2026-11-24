using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dialogue-driven Stage 1 cinematic. One temporary Cinemachine virtual
/// camera takes priority over the existing persistent FreeLook. The scene's
/// physical Main Camera stays untouched, so gameplay rendering and mobile
/// camera configuration are not duplicated or replaced.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public sealed class SpiritRemnantCinematic : MonoBehaviour
{
    public static bool IsCinematicActive { get; private set; }

    private enum Shot
    {
        TwoShot,
        SpiritMedium,
        SpiritClose,
        VanReaction,
        VanClose,
        Farewell,
        GiftReveal,
        GiftTransfer
    }

    [Header("Camera")]
    [SerializeField, Range(0.2f, 2f)] private float entryBlendSeconds = 0.85f;
    [SerializeField, Range(0.15f, 2f)] private float shotTransitionSeconds = 0.85f;
    [SerializeField, Range(0f, 2.5f)] private float subtleDollyAmount = 0.9f;
    [SerializeField] private Transform spiritHead;

    [Header("Encounter Blocking")]
    [Tooltip("Authored Stage 1 mark: Van stands here for every first/repeat Spirit interaction, regardless of approach direction.")]
    [SerializeField] private Transform vanCinematicStagingMark;
    [SerializeField, Min(12f)] private float minimumHorizontalSeparation = 16f;

    private Transform player;
    private Transform giftCoreFocus;
    private Camera outputCamera;
    private CinemachineBrain brain;
    private CinemachineBlendDefinition previousBlend;
    private CinemachineCamera virtualCamera;
    private GameObject cameraObject;
    private MovementofPlayer playerMovement;
    private AttackController playerAttack;
    private bool previousAttackLocked;
    private bool movementLocked;
    private Transform actorVisual;
    private Quaternion originalActorFacing;
    private Quaternion originalPlayerFacing;
    private bool alteredFacing;
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    // Some imported trees have NO colliders. Use separate local-physics
    // scene mesh colliders for the *camera only* (no collisions, triggers or
    // player physics added to Stage 1). Its BVH accelerates repeated sight
    // tests without expensive per-frame CPU triangle loops on Android.
    private Scene occlusionScene;
    private PhysicsScene occlusionPhysics;
    private int occlusionMeshCount;

    private static readonly float[] CameraAngleOffsets =
        { 0f, 24f, -24f, 48f, -48f, 78f, -78f, 110f, -110f, 145f, -145f, 180f };
    private float startTime;
    private float shotTime;
    private Vector3 shotStartPosition;
    private Quaternion shotStartRotation;
    private float shotStartFov;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFov;
    private bool running;
    private Shot currentShot;

    public bool IsRunning => running;
    public string CurrentShot => currentShot.ToString();
    public CinemachineCamera VirtualCamera => virtualCamera;
    public Transform VanStagingMark => vanCinematicStagingMark;

    public bool Begin(Transform actor, Canvas dialogueCanvas)
    {
        if (running)
            return true;

        outputCamera = Camera.main;
        brain = outputCamera != null ? outputCamera.GetComponent<CinemachineBrain>() : null;
        player = actor;
        if (player == null || brain == null)
        {
            Debug.LogWarning("[SpiritRemnantCinematic] No player or Cinemachine Brain; dialogue remains usable without camera takeover.", this);
            return false;
        }

        try
        {
            playerMovement = player.GetComponent<MovementofPlayer>();
            playerAttack = player.GetComponent<AttackController>();
            if (playerMovement != null)
            {
                playerMovement.AcquireExternalMovementLock();
                movementLocked = true;
            }

            if (playerAttack != null)
            {
                previousAttackLocked = playerAttack.IsAttackInputLocked;
                if (!previousAttackLocked)
                    playerAttack.SetAttackInputLocked(true);
            }

            PlaceVanAtCinematicMark();
            FaceTheCharacters();
            CacheNearbyVisibilityMeshes();
            HideGameplayCanvases(dialogueCanvas);

            previousBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, entryBlendSeconds);

            cameraObject = new GameObject("SpiritRemnant_DialogueCinemachineCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, gameObject.scene);
            cameraObject.transform.SetPositionAndRotation(
                outputCamera.transform.position, outputCamera.transform.rotation);
            virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Priority.Enabled = true;
            virtualCamera.Priority.Value = 10000;
            virtualCamera.Lens.FieldOfView = outputCamera.fieldOfView;

            shotStartPosition = cameraObject.transform.position;
            shotStartRotation = cameraObject.transform.rotation;
            shotStartFov = outputCamera.fieldOfView;
            startTime = Time.unscaledTime;
            shotTime = Time.unscaledTime;
            running = true;
            IsCinematicActive = true;
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("[SpiritRemnantCinematic] Cinematic setup failed; reverting safely. " + error.Message, this);
            End();
            return false;
        }
    }

    private void PlaceVanAtCinematicMark()
    {
        if (player == null)
            return;

        Transform head = FindHead();
        if (vanCinematicStagingMark == null || head == null)
        {
            Debug.LogWarning("[SpiritRemnantCinematic] No authored staging mark or Spirit head. Preserving current Van position.", this);
            return;
        }

        Vector3 mark = vanCinematicStagingMark.position;
        Vector3 separation = mark - head.position;
        separation.y = 0f;
        if (separation.magnitude < minimumHorizontalSeparation)
        {
            Debug.LogWarning("[SpiritRemnantCinematic] Staging mark is too close to the Spirit; cannot safely block this encounter.", this);
            return;
        }

        Vector3 alreadyThere = player.position - mark;
        alreadyThere.y = 0f;
        if (alreadyThere.sqrMagnitude < 0.35f * 0.35f)
        {
            // On repeats Van may already be standing on the mark. Don't
            // reset his grounded Y to the editor-authored height again.
            player.rotation = vanCinematicStagingMark.rotation;
            return;
        }

        // Van's CharacterController is scaled with his large Stage 1 rig.
        // Disabling the controller during the short story-only reposition
        // prevents swept-physics collisions with the Spirit/terrain.
        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;
        try
        {
            player.position = mark;
            player.rotation = vanCinematicStagingMark.rotation;
        }
        finally
        {
            if (controllerWasEnabled && controller != null)
                controller.enabled = true;
        }
    }

    private void FaceTheCharacters()
    {
        SpiritRemnantAmbientMotion motion = GetComponent<SpiritRemnantAmbientMotion>();
        actorVisual = motion != null ? motion.VisualRoot : null;
        if (actorVisual == null || player == null)
            return;

        Transform head = FindHead();
        Vector3 spiritPosition = head != null ? head.position : actorVisual.position;
        Vector3 playerPosition = GetPlayerFocus(player);
        Vector3 facePlayer = playerPosition - spiritPosition;
        facePlayer.y = 0f;
        if (facePlayer.sqrMagnitude < 0.001f)
            return;

        originalActorFacing = actorVisual.rotation;
        originalPlayerFacing = player.rotation;
        alteredFacing = true;
        // The imported glTF visual is skinned independently of the legacy
        // Spirit Remnant root. Turning only the visual preserves its encounter
        // anchor, original prefab offsets, and objective trigger location.
        actorVisual.rotation = Quaternion.LookRotation(facePlayer.normalized, Vector3.up);
        player.rotation = Quaternion.LookRotation(-facePlayer.normalized, Vector3.up);
    }

    public void ShowBeat(int lineIndex, bool firstConversation, bool spiritSpeaking)
    {
        if (!running || player == null || virtualCamera == null)
            return;

        Shot next = DetermineShot(lineIndex, firstConversation, spiritSpeaking);
        currentShot = next;
        shotStartPosition = cameraObject.transform.position;
        shotStartRotation = cameraObject.transform.rotation;
        shotStartFov = virtualCamera.Lens.FieldOfView;
        shotTime = Time.unscaledTime;
        ResolveShot(next, out targetPosition, out targetRotation, out targetFov);
    }

    /// <summary>
    /// Extra chapter appended after the approved 16 story beats. The same
    /// Cinemachine camera, input locks, and HUD ownership are reused.
    /// </summary>
    public void ShowGiftBeat(int giftLine, bool spiritSpeaking, Transform core)
    {
        giftCoreFocus = core;
        if (!running || player == null || virtualCamera == null)
            return;
        Shot next;
        if (giftLine < 2)
            next = spiritSpeaking ? Shot.SpiritMedium : Shot.VanReaction;
        else if (giftLine <= 3)
            next = Shot.GiftReveal;
        else if (giftLine <= 6)
            next = Shot.GiftTransfer;
        else if (giftLine == 7)
            next = Shot.Farewell;
        else
            next = Shot.VanClose;
        currentShot = next;
        shotStartPosition = cameraObject.transform.position;
        shotStartRotation = cameraObject.transform.rotation;
        shotStartFov = virtualCamera.Lens.FieldOfView;
        shotTime = Time.unscaledTime;
        ResolveShot(next, out targetPosition, out targetRotation, out targetFov);
    }

    private Shot DetermineShot(int lineIndex, bool firstConversation, bool spiritSpeaking)
    {
        if (!firstConversation)
            return spiritSpeaking ? Shot.SpiritMedium : Shot.VanReaction;

        switch (lineIndex)
        {
            // Stage 1's Spirit is surrounded by giant non-collidable trees.
            // Starting with the wide two-shot placed a trunk between the
            // camera and both actors. Open on the Spirit's readable face
            // instead; later beats can widen once the player is oriented.
            case 0: return Shot.SpiritMedium;
            case 1: return Shot.VanReaction;
            case 2: return Shot.SpiritMedium;
            case 3: return Shot.VanClose;
            case 4: return Shot.SpiritClose; // Hollow Knight / sacrifice reveal
            case 5: return Shot.VanClose;    // The father's shock
            case 6: return Shot.SpiritClose; // Soul-fragment revelation
            case 7: return Shot.VanClose;
            case 8: return Shot.SpiritMedium;
            case 9: return Shot.VanReaction;
            case 10: return Shot.SpiritClose;
            case 11: return Shot.VanClose;   // Where did the fragments go?
            case 12: return Shot.SpiritMedium;
            case 13: return Shot.VanReaction;
            case 14: return Shot.Farewell;  // The road to the castle
            case 15: return Shot.VanClose;  // Quiet resolve, no premature speech
            default: return spiritSpeaking ? Shot.SpiritMedium : Shot.VanReaction;
        }
    }

    private void ResolveShot(Shot shot, out Vector3 cameraPosition,
        out Quaternion cameraRotation, out float fov)
    {
        Transform head = FindHead();
        Vector3 spiritFace = head != null ? head.position :
            transform.position + Vector3.up * 14f;

        Vector3 vanFace = GetPlayerFocus(player);
        Vector3 towardVan = vanFace - spiritFace;
        towardVan.y = 0f;
        if (towardVan.sqrMagnitude < 0.001f)
            towardVan = -transform.forward;
        towardVan.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, towardVan).normalized;
        Vector3 focus;

        // Distances are in Stage 1 world units; both models are large in this
        // scene. Framing uses the actual live head and player world positions,
        // not the legacy T-pose prefab pivot (which is offset from the mesh).
        switch (shot)
        {
            case Shot.GiftReveal:
                focus = giftCoreFocus != null ? giftCoreFocus.position :
                    spiritFace + Vector3.down * 2.6f;
                // Frame the gift from the Spirit's side, rather than through
                // Van's large foreground model or his staff.
                cameraPosition = focus - towardVan * 17f + side * 21f + Vector3.up * 5.5f;
                fov = 41f;
                break;
            case Shot.GiftTransfer:
                focus = giftCoreFocus != null ? giftCoreFocus.position :
                    Vector3.Lerp(spiritFace, vanFace, 0.62f);
                // The protagonist's collider and staff block a straight
                // reverse shot of the core. Approach from the OUTSIDE of the
                // actual receiving hand so the orb remains readable in
                // Van's palm regardless of his current facing direction.
                EchoRuneCorePresentation gift = GetComponent<EchoRuneCorePresentation>();
                Transform receivingHand = gift != null ? gift.ReceivingHand : null;
                Vector3 handOutside = receivingHand != null
                    ? receivingHand.position - vanFace : side;
                handOutside.y = 0f;
                if (handOutside.sqrMagnitude < 0.01f)
                    handOutside = side;
                handOutside.Normalize();
                cameraPosition = focus + handOutside * 14f +
                    towardVan * 7f + Vector3.up * 5f;
                fov = 45f;
                break;
            case Shot.SpiritMedium:
                focus = spiritFace + Vector3.down * 2.6f;
                // Side-biased shots keep Van's large silhouette out of the
                // Spirit's face, even when Van stands directly in front of him.
                cameraPosition = focus + towardVan * 20f + side * 29f + Vector3.up * 3.6f;
                fov = 43f;
                break;
            case Shot.SpiritClose:
                focus = spiritFace + Vector3.down * 1.3f;
                cameraPosition = focus + towardVan * 12f + side * 23f + Vector3.up * 1.6f;
                fov = 35f;
                break;
            case Shot.VanClose:
                // Reverse shot from Van's left/front side.  The Spirit is
                // framed as an ominous foreground silhouette, while Van's
                // expression is clearly visible instead of hidden by the
                // spirit mesh or the sapphire staff.
                focus = vanFace + Vector3.down * 0.6f;
                cameraPosition = focus - towardVan * 9f + side * 12f + Vector3.up * 1f;
                fov = 43f;
                break;
            case Shot.VanReaction:
                focus = vanFace + Vector3.down * 2.8f;
                cameraPosition = focus - towardVan * 15f + side * 22f + Vector3.up * 3f;
                fov = 46f;
                break;
            case Shot.Farewell:
                focus = Vector3.Lerp(vanFace, spiritFace, 0.52f);
                cameraPosition = focus + towardVan * 53f + side * 26f + Vector3.up * 16f;
                fov = 54f;
                break;
            default:
                focus = Vector3.Lerp(vanFace, spiritFace, 0.55f);
                cameraPosition = focus + towardVan * 50f + side * 25f + Vector3.up * 14f;
                fov = 52f;
                break;
        }

        cameraPosition = ChooseClearCameraPosition(focus, cameraPosition,
            spiritFace, vanFace, shot, fov);
        Vector3 view = focus - cameraPosition;
        cameraRotation = view.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(view.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private Transform FindHead()
    {
        if (spiritHead != null)
            return spiritHead;

        SpiritRemnantAmbientMotion motion = GetComponent<SpiritRemnantAmbientMotion>();
        if (motion != null && motion.HeadBone != null)
        {
            spiritHead = motion.HeadBone;
            return spiritHead;
        }

        SkinnedMeshRenderer skinned = Array.Find(GetComponentsInChildren<SkinnedMeshRenderer>(true),
            item => item != null && item.gameObject.activeInHierarchy && item.bones.Length > 0);
        if (skinned == null)
            return null;
        spiritHead = Array.Find(skinned.bones,
            bone => bone != null && bone.name == "Head");
        return spiritHead;
    }

    private static Vector3 GetPlayerFocus(Transform actor)
    {
        Animator animator = actor.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman && animator.avatar != null &&
            animator.avatar.isValid)
        {
            Transform bone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (bone != null)
                return bone.position;
        }
        CharacterController controller = actor.GetComponent<CharacterController>();
        return controller != null
            ? actor.TransformPoint(controller.center + Vector3.up * controller.height * 0.30f)
            : actor.position + Vector3.up * 12f;
    }

    private Vector3 AvoidSceneGeometry(Vector3 focus, Vector3 candidate)
    {
        Vector3 ray = candidate - focus;
        float distance = ray.magnitude;
        if (distance < 0.1f)
            return candidate;
        Vector3 direction = ray / distance;
        RaycastHit[] hits = Physics.SphereCastAll(focus, 0.65f, direction,
            distance, ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
        float nearest = distance;
        foreach (RaycastHit hit in hits)
        {
            Collider collider = hit.collider;
            if (collider == null || collider.isTrigger ||
                collider.transform.IsChildOf(transform) ||
                (player != null && collider.transform.IsChildOf(player)))
                continue;
            nearest = Mathf.Min(nearest, Mathf.Max(4f, hit.distance - 1.1f));
        }
        return focus + direction * nearest;
    }

    private void CacheNearbyVisibilityMeshes()
    {
        ReleaseOcclusionProbes();
        Vector3 anchor = FindHead() != null ? FindHead().position : transform.position;
        MeshRenderer[] environment = FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        occlusionScene = SceneManager.CreateScene(
            "SpiritRemnant_CinematicOcclusion_" + GetInstanceID(),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        occlusionPhysics = occlusionScene.GetPhysicsScene();
        foreach (MeshRenderer renderer in environment)
        {
            if (renderer == null || !renderer.enabled ||
                renderer.transform.IsChildOf(transform) ||
                (player != null && renderer.transform.IsChildOf(player)) ||
                renderer.bounds.SqrDistance(anchor) > 160f * 160f)
                continue;

            Bounds bounds = renderer.bounds;
            // Giant terrain pieces are handled by regular Physics colliders.
            // Only examine local props, rock/tree mesh pieces, and buildings.
            if (bounds.size.magnitude > 460f)
                continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable || mesh.vertexCount > 12000)
                continue;

            if (mesh.triangles.Length == 0 || mesh.triangles.Length > 60000)
                continue;

            GameObject proxy = new GameObject("CinematicOcclusion_" + renderer.name);
            SceneManager.MoveGameObjectToScene(proxy, occlusionScene);
            proxy.transform.SetPositionAndRotation(
                renderer.transform.position, renderer.transform.rotation);
            proxy.transform.localScale = renderer.transform.lossyScale;
            MeshCollider collider = proxy.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            ++occlusionMeshCount;
        }
    }

    private void ReleaseOcclusionProbes()
    {
        if (occlusionScene.IsValid() && occlusionScene.isLoaded)
            SceneManager.UnloadSceneAsync(occlusionScene);
        occlusionScene = default;
        occlusionPhysics = default;
        occlusionMeshCount = 0;
    }

    private Vector3 ChooseClearCameraPosition(Vector3 focus, Vector3 authored,
        Vector3 spiritFace, Vector3 vanFace, Shot shot, float fov)
    {
        Vector3 origin = focus;
        Vector3 relative = authored - origin;
        bool isTwoShot = shot == Shot.TwoShot || shot == Shot.Farewell;
        float bestScore = float.PositiveInfinity;
        Vector3 bestPosition = authored;

        foreach (float angle in CameraAngleOffsets)
        {
            Vector3 candidate = origin + Quaternion.AngleAxis(angle, Vector3.up) * relative;
            candidate = AvoidSceneGeometry(focus, candidate);
            float penalty = 0f;

            // A blocked focus is unacceptable even if the camera body itself
            // does not collide with the tree. Test BOTH characters for the
            // opening and closing two-shot.
            penalty += CountSightObstacles(candidate, focus) * 1200f;
            penalty += CountSightObstacles(candidate, spiritFace) *
                (shot == Shot.VanClose || shot == Shot.VanReaction ? 110f : 900f);
            penalty += CountSightObstacles(candidate, vanFace) *
                (shot == Shot.SpiritClose || shot == Shot.SpiritMedium ? 110f : 900f);
            if (isTwoShot)
            {
                penalty += CountSightObstacles(candidate,
                    spiritFace + Vector3.down * 4.0f) * 500f;
                penalty += CountSightObstacles(candidate,
                    vanFace + Vector3.down * 4.0f) * 500f;
            }
            // A tree can occupy half the screen while missing both actors'
            // exact center rays. Penalize foreground props across the actual
            // frame, not only straight-line visibility to each head.
            penalty += CountForegroundClutter(candidate, focus, fov) * 250f;

            // Prefer the cinematic's authored camera side unless it is
            // obstructed. Avoid parking a wide shot uncomfortably close.
            penalty += Mathf.Abs(angle) * 0.55f;
            if ((candidate - spiritFace).magnitude < 9f ||
                (candidate - vanFace).magnitude < 9f)
                penalty += 650f;

            if (penalty >= bestScore)
                continue;
            bestScore = penalty;
            bestPosition = candidate;
            if (bestScore < 0.01f)
                break;
        }

        return bestPosition;
    }

    private int CountForegroundClutter(Vector3 cameraPosition,
        Vector3 focus, float fov)
    {
        if (!occlusionPhysics.IsValid())
            return 0;
        Vector3 view = focus - cameraPosition;
        float distance = view.magnitude;
        if (distance < 2f)
            return 0;
        Vector3 forward = view / distance;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        float tanV = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
        float tanH = tanV * (outputCamera != null ? outputCamera.aspect : 2.22f);
        int clutter = 0;
        // Only the central 70% is scored. Trees in the far edges can be
        // atmospheric foreground; an entire trunk covering dialogue subjects
        // cannot. The probes use the isolated physics scene's mesh BVH.
        float[] xOffsets = { -0.42f, 0f, 0.42f };
        float[] yOffsets = { -0.30f, 0f, 0.30f };
        foreach (float x in xOffsets)
        {
            foreach (float y in yOffsets)
            {
                Vector3 direction = (forward + right * (x * tanH) +
                                     up * (y * tanV)).normalized;
                if (occlusionPhysics.Raycast(cameraPosition, direction,
                    out RaycastHit hit, distance * 0.94f,
                    ~0, QueryTriggerInteraction.Ignore) &&
                    hit.distance > 0.55f)
                    ++clutter;
            }
        }
        return clutter;
    }

    private int CountSightObstacles(Vector3 from, Vector3 target)
    {
        Vector3 delta = target - from;
        float distance = delta.magnitude;
        if (distance <= 0.1f)
            return 0;

        Ray ray = new Ray(from, delta / distance);
        int blocked = 0;
        RaycastHit[] hits = Physics.RaycastAll(ray, distance,
            ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Collider c = hit.collider;
            if (c == null || c.transform.IsChildOf(transform) ||
                (player != null && c.transform.IsChildOf(player)) ||
                hit.distance < 0.8f || hit.distance > distance - 1.2f)
                continue;
            blocked++;
            break;
        }

        // The separate PhysicsScene contains only temporary *visual* mesh
        // proxies. Imported tree trunks have no gameplay colliders, so they
        // cannot otherwise be detected by the main Physics raycast.
        if (occlusionPhysics.IsValid() &&
            occlusionPhysics.Raycast(from, delta / distance, out RaycastHit visualHit,
                distance, ~0, QueryTriggerInteraction.Ignore) &&
            visualHit.distance > 0.55f && visualHit.distance < distance - 1.2f)
        {
            return blocked + 1;
        }
        return blocked;
    }

    private void LateUpdate()
    {
        if (!running || cameraObject == null || virtualCamera == null)
            return;
        float elapsed = Time.unscaledTime - shotTime;
        float blend = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(elapsed / Mathf.Max(0.1f, shotTransitionSeconds)));
        Vector3 position = Vector3.Lerp(shotStartPosition, targetPosition, blend);
        // An understated push-in prevents the shot from looking frozen while
        // the player reads; movement is independent of timeScale.
        float dolly = Mathf.Clamp(elapsed - shotTransitionSeconds, 0f, 5f) / 5f;
        position += targetRotation * Vector3.forward * (dolly * subtleDollyAmount);
        Quaternion rotation = Quaternion.Slerp(shotStartRotation, targetRotation, blend);
        if (currentShot == Shot.GiftTransfer && giftCoreFocus != null)
        {
            // The core is animated between dialogue beats and eventually
            // parented to Van's live left-hand bone. Follow its moving center,
            // instead of continuing to look at its position at the previous
            // beat; no per-frame expensive scene-occlusion search is needed.
            Vector3 aim = giftCoreFocus.position - position;
            if (aim.sqrMagnitude > 0.01f)
            {
                Quaternion movingAim = Quaternion.LookRotation(aim.normalized, Vector3.up);
                float follow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.5f));
                rotation = Quaternion.Slerp(rotation, movingAim, follow);
            }
        }
        cameraObject.transform.SetPositionAndRotation(position, rotation);
        virtualCamera.Lens.FieldOfView = Mathf.Lerp(shotStartFov, targetFov, blend);
    }

    private void HideGameplayCanvases(Canvas dialogueCanvas)
    {
        hiddenCanvases.Clear();
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == null || canvas == dialogueCanvas ||
                canvas.transform.IsChildOf(dialogueCanvas.transform) ||
                !canvas.enabled || !canvas.gameObject.activeInHierarchy ||
                canvas.renderMode == RenderMode.WorldSpace)
                continue;
            hiddenCanvases.Add(canvas);
            canvas.enabled = false;
        }
    }

    public void End()
    {
        if (alteredFacing)
        {
            if (actorVisual != null) actorVisual.rotation = originalActorFacing;
            if (player != null) player.rotation = originalPlayerFacing;
        }
        alteredFacing = false;
        actorVisual = null;

        // Restore the previous input policy rather than overwriting locks
        // owned by other gameplay systems.
        if (movementLocked && playerMovement != null)
            playerMovement.ReleaseExternalMovementLock();
        movementLocked = false;
        if (playerAttack != null && !previousAttackLocked)
            playerAttack.SetAttackInputLocked(false);
        playerMovement = null;
        playerAttack = null;

        if (brain != null)
            brain.DefaultBlend = previousBlend;
        brain = null;
        outputCamera = null;
        if (cameraObject != null)
            Destroy(cameraObject);
        cameraObject = null;
        virtualCamera = null;
        foreach (Canvas canvas in hiddenCanvases)
            if (canvas != null)
                canvas.enabled = true;
        hiddenCanvases.Clear();
        ReleaseOcclusionProbes();
        running = false;
        IsCinematicActive = false;
        player = null;
        giftCoreFocus = null;
    }

    private void OnDisable() => End();
    private void OnDestroy() => End();
}
