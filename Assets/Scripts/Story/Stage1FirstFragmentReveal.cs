using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time, story-gated Cinemachine reveal of Bramble Sprite feeding on
/// Nevali's FIRST fragment. Van moves from shocked recognition to heartbreak,
/// then quiet anger. Takes ownership before ShrineTrigger starts the existing
/// MITIGATE vocabulary encounter, then yields control back to Node 1.
/// No enemy combat state, fragment pickup or Spirit NPC is modified.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage1FirstFragmentReveal : MonoBehaviour
{
    public static bool IsCinematicActive { get; private set; }

    [Header("Scene anchors")]
    [SerializeField] private Transform fragment;
    [SerializeField] private BrambleSpriteController bramble;
    [SerializeField] private Stage1ProgressionManager progression;
    [SerializeField] private Stage1FragmentGuidance guidance;
    [Header("Timing")]
    [SerializeField, Min(0.5f)] private float establishingSeconds = 3f;
    [SerializeField, Min(0.5f)] private float fragmentSeconds = 3.6f;
    [SerializeField, Min(0.5f)] private float reactionSeconds = 2.8f;
    [SerializeField, Min(0.5f)] private float heartbreakSeconds = 4.2f;
    [SerializeField, Min(0.25f)] private float finalSilenceSeconds = 1.4f;
    [SerializeField, Range(0.5f, 2f)] private float shotTransitionSeconds = 0.95f;
    [SerializeField, Range(0.1f, 1.5f)] private float cameraBlendSeconds = 0.65f;

    private Transform player;
    private MovementofPlayer movement;
    private AttackController attacks;
    private bool movementLocked;
    private bool attackWasLocked;
    private CinemachineBrain brain;
    private CinemachineBlendDefinition originalBlend;
    private GameObject cameraObject;
    private CinemachineCamera revealCamera;
    private GameObject captionCanvasObject;
    private TMP_Text caption;
    private TMP_Text speakerName;
    private Quaternion originalPlayerRotation;
    private bool playerFacingChanged;
    private Vector3 shotStartPosition;
    private Vector3 shotTargetPosition;
    private Quaternion shotStartRotation;
    private Quaternion shotTargetRotation;
    private float shotStartFov;
    private float shotTargetFov;
    private float shotStartedAt;
    private int currentShotIndex = -1;
    private readonly List<Canvas> suspendedCanvases = new List<Canvas>();
    private Coroutine sequence;
    private Action resumeNode1;
    private bool running;
    private bool completedThisSession;
    private bool skipRequested;
    private bool editorPreview;
#if UNITY_EDITOR
    private bool editorPreviewConsumedThisPlaySession;
#endif

    public bool IsRunning => running;
    public int CurrentShotIndex => currentShotIndex;
    public string CurrentDialogue => caption != null ? caption.text : string.Empty;
    public bool HasRevealed
    {
        get
        {
#if UNITY_EDITOR
            // A no-save editor cinematic preview must not be picked up by
            // ShrineTrigger.OnTriggerStay as a fresh real-player encounter
            // while the tester is still standing inside the large arena.
            if (editorPreviewConsumedThisPlaySession)
                return true;
#endif
            if (completedThisSession || (progression != null && progression.node1Completed))
                return true;
            var db = GameDatabaseManager.Instance;
            return db != null && db.HasSeenFirstFragmentReveal();
        }
    }
    public bool HasPrerequisite
    {
        get
        {
            var db = GameDatabaseManager.Instance;
            return db != null && db.HasMetSpiritRemnant() && db.HasEchoRuneCore();
        }
    }

    public bool TryBegin(Transform entrant, Action onFinished)
    {
        if (running)
            return true; // Prevent repeated trigger/stay from starting combat.
        if ((!editorPreview && (HasRevealed || !HasPrerequisite)) ||
            entrant == null || fragment == null ||
            bramble == null || !isActiveAndEnabled)
            return false;
        if (SpiritRemnantCinematic.IsCinematicActive)
            return true; // Wait until the Spirit's own camera is fully released.

        var actorMovement = entrant.GetComponentInParent<MovementofPlayer>();
        player = actorMovement != null ? actorMovement.transform : entrant.root;
        movement = actorMovement;
        attacks = player.GetComponent<AttackController>();
        var output = Camera.main;
        brain = output != null ? output.GetComponent<CinemachineBrain>() : null;
        if (brain == null)
        {
            Debug.LogWarning("[FirstFragmentReveal] No Cinemachine Brain: original Node 1 encounter can continue.", this);
            return false;
        }

        resumeNode1 = onFinished;
        running = true;
        skipRequested = false;
        IsCinematicActive = true;
        try
        {
            if (movement != null)
            {
                movement.AcquireExternalMovementLock();
                movementLocked = true;
            }
            if (attacks != null)
            {
                attackWasLocked = attacks.IsAttackInputLocked;
                if (!attackWasLocked)
                    attacks.SetAttackInputLocked(true);
            }
            FaceVanTowardFragment();
            guidance?.HideForReveal();
            SuspendGameplayCanvases();
            BuildCaptions();
            originalBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, cameraBlendSeconds);
            cameraObject = new GameObject("Stage1_FirstFragment_RevealCinemachineCamera");
            cameraObject.transform.SetPositionAndRotation(
                output.transform.position, output.transform.rotation);
            revealCamera = cameraObject.AddComponent<CinemachineCamera>();
            revealCamera.Priority.Enabled = true;
            revealCamera.Priority.Value = 12000;
            revealCamera.Lens.FieldOfView = 53f;
            shotStartPosition = shotTargetPosition = cameraObject.transform.position;
            shotStartRotation = shotTargetRotation = cameraObject.transform.rotation;
            shotStartFov = shotTargetFov = revealCamera.Lens.FieldOfView;
            sequence = StartCoroutine(PlayReveal());
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("[FirstFragmentReveal] Setup failed: " + error.Message, this);
            Cleanup();
            return false;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only cinematic inspection. Never awards story flags or starts
    /// the Node 1 vocabulary encounter, including when the preview ends.
    /// </summary>
    public bool BeginEditorPreview(Transform actor)
    {
        if (running)
            return false;
        editorPreview = true;
        if (TryBegin(actor, null) && running)
        {
            editorPreviewConsumedThisPlaySession = true;
            return true;
        }
        editorPreview = false;
        return false;
    }
#endif

    private IEnumerator PlayReveal()
    {
        SetSpeaker(string.Empty);
        caption.text = "THE FIRST FRAGMENT";
        caption.maxVisibleCharacters = int.MaxValue;
        SetShot(0);
        yield return Hold(establishingSeconds);
        if (!skipRequested)
        {
            SetShot(1);
            yield return DeliverVanLine("Nevali...? Is that... you?", fragmentSeconds);
        }
        if (!skipRequested)
        {
            SetShot(2);
            yield return DeliverVanLine("You're feeding on my daughter's soul...", heartbreakSeconds);
        }
        if (!skipRequested)
        {
            SetShot(3);
            yield return DeliverVanLine("Get your hands off her.", reactionSeconds);
        }
        if (!skipRequested)
        {
            // The silence after the warning makes Van's anger land without
            // turning this first encounter into his Hollow Knight outburst.
            SetSpeaker(string.Empty);
            caption.text = string.Empty;
            SetShot(4);
            yield return Hold(finalSilenceSeconds);
        }
        Complete();
    }

    private IEnumerator DeliverVanLine(string words, float beatSeconds)
    {
        SetSpeaker("VAN AERT");
        caption.text = words;
        caption.maxVisibleCharacters = 0;
        float revealDuration = Mathf.Clamp(words.Length * 0.041f, 0.65f, 2.1f);
        float elapsed = 0f;
        while (elapsed < revealDuration && !skipRequested)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                skipRequested = true;
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            caption.maxVisibleCharacters = Mathf.Clamp(
                Mathf.CeilToInt(words.Length * elapsed / revealDuration),
                0, words.Length);
            yield return null;
        }
        caption.maxVisibleCharacters = int.MaxValue;
        if (!skipRequested)
            yield return Hold(Mathf.Max(0.65f, beatSeconds - revealDuration));
    }

    private void SetSpeaker(string value)
    {
        if (speakerName != null)
            speakerName.text = value;
    }

    private IEnumerator Hold(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds && !skipRequested)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                skipRequested = true;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void FaceVanTowardFragment()
    {
        if (player == null || fragment == null)
            return;
        Vector3 direction = fragment.position - player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.2f)
            return;
        originalPlayerRotation = player.rotation;
        playerFacingChanged = true;
        player.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void LateUpdate()
    {
        if (!running || cameraObject == null || revealCamera == null)
            return;
        float t = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((Time.unscaledTime - shotStartedAt) /
                          Mathf.Max(0.15f, shotTransitionSeconds)));
        cameraObject.transform.SetPositionAndRotation(
            Vector3.Lerp(shotStartPosition, shotTargetPosition, t),
            Quaternion.Slerp(shotStartRotation, shotTargetRotation, t));
        revealCamera.Lens.FieldOfView = Mathf.Lerp(shotStartFov, shotTargetFov, t);
    }

    private void SetShot(int shot)
    {
        if (revealCamera == null || bramble == null || fragment == null)
            return;
        currentShotIndex = shot;
        Vector3 shard = fragment.Find("FragmentCrystal_Visual") != null
            ? fragment.Find("FragmentCrystal_Visual").position
            : fragment.position;
        Animator animator = bramble.GetComponentInChildren<Animator>(true);
        Transform head = animator != null && animator.isHuman && animator.avatar != null &&
                         animator.avatar.isValid
            ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 enemy = head != null ? head.position :
            bramble.transform.position + Vector3.up * 10f;
        Vector3 across = enemy - shard;
        across.y = 0f;
        if (across.sqrMagnitude < 0.1f)
            across = Vector3.forward;
        across.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, across).normalized;
        Vector3 vanFace = GetVanFace();
        Vector3 towardArena = shard - vanFace;
        towardArena.y = 0f;
        if (towardArena.sqrMagnitude < 0.1f)
            towardArena = player != null ? player.forward : across;
        towardArena.Normalize();
        Vector3 vanSide = Vector3.Cross(Vector3.up, towardArena).normalized;
        Vector3 focus;
        Vector3 position;
        float fov;
        if (shot == 0)
        {
            focus = Vector3.Lerp(shard, enemy, 0.52f) + Vector3.up * 2f;
            position = focus + side * 38f - across * 26f + Vector3.up * 16f;
            fov = 49f;
        }
        else if (shot == 1)
        {
            // Recognition: Nevali's luminous shard dominates the frame.
            focus = shard + Vector3.up * 0.6f;
            position = focus + side * 17f - across * 10f + Vector3.up * 4f;
            fov = 38f;
        }
        else if (shot == 2)
        {
            // Heartbreak: reveal the enemy draining the girl's soul.
            focus = Vector3.Lerp(shard, enemy, 0.45f) + Vector3.up * 1f;
            position = focus + side * 24f - across * 17f + Vector3.up * 8f;
            fov = 43f;
        }
        else if (shot == 3)
        {
            // Quiet anger: front-side shot of Van's actual face, independent
            // of which direction the player entered the shrine trigger.
            focus = vanFace + Vector3.down * 1.1f;
            // Film from the opposite shoulder so Van's tall sapphire staff
            // remains at the edge of frame instead of hiding his eyes.
            position = focus + towardArena * 19f - vanSide * 12f + Vector3.up * 2.6f;
            fov = 42f;
        }
        else
        {
            // A final understated push-in, held in silence after the warning.
            focus = vanFace + Vector3.down * 0.5f;
            position = focus + towardArena * 14f - vanSide * 8f + Vector3.up * 1.2f;
            fov = 36f;
        }
        // Keep a predictable side-on angle, independent of the direction Van
        // happened to approach the arena. Ignore player/enemy trigger colliders.
        Vector3 ray = position - focus;
        RaycastHit[] hits = Physics.SphereCastAll(focus, 0.7f,
            ray.normalized, ray.magnitude, ~((1 << 2) | (1 << 5)),
            QueryTriggerInteraction.Ignore);
        float nearest = ray.magnitude;
        foreach (var hit in hits)
        {
            if (hit.collider == null ||
                (player != null && hit.collider.transform.IsChildOf(player)) ||
                hit.collider.transform.IsChildOf(bramble.transform) ||
                hit.collider.transform.IsChildOf(fragment))
                continue;
            if (hit.distance > 5f && hit.distance < ray.magnitude - 1f)
                nearest = Mathf.Min(nearest, Mathf.Max(8f, hit.distance - 1.2f));
        }
        if (nearest < ray.magnitude)
            position = focus + ray.normalized * nearest;
        shotStartPosition = cameraObject.transform.position;
        shotStartRotation = cameraObject.transform.rotation;
        shotStartFov = revealCamera.Lens.FieldOfView;
        shotTargetPosition = position;
        shotTargetRotation = Quaternion.LookRotation(focus - position, Vector3.up);
        shotTargetFov = fov;
        shotStartedAt = Time.unscaledTime;
    }

    private Vector3 GetVanFace()
    {
        if (player == null)
            return transform.position + Vector3.up * 12f;
        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman && animator.avatar != null &&
            animator.avatar.isValid)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                return head.position;
        }
        CharacterController controller = player.GetComponent<CharacterController>();
        return controller != null
            ? player.TransformPoint(controller.center + Vector3.up * controller.height * 0.3f)
            : player.position + Vector3.up * 12f;
    }

    private void SuspendGameplayCanvases()
    {
        suspendedCanvases.Clear();
        foreach (var canvas in FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.enabled || !canvas.gameObject.activeInHierarchy ||
                canvas.renderMode == RenderMode.WorldSpace)
                continue;
            suspendedCanvases.Add(canvas);
            canvas.enabled = false;
        }
    }

    private void BuildCaptions()
    {
        captionCanvasObject = new GameObject("Stage1FirstFragment_Captions",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = captionCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = captionCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var band = new GameObject("CinematicCaptionBand",
            typeof(RectTransform), typeof(Image));
        band.transform.SetParent(captionCanvasObject.transform, false);
        var bandRect = band.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0.0f, 0.01f);
        bandRect.anchorMax = new Vector2(1.0f, 0.18f);
        bandRect.offsetMin = bandRect.offsetMax = Vector2.zero;
        band.GetComponent<Image>().color = new Color(0.015f, 0.023f, 0.052f, 0.82f);
        var speakerObject = new GameObject("VanAert_Speaker",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        speakerObject.transform.SetParent(band.transform, false);
        var speakerRect = speakerObject.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.1f, 0.54f);
        speakerRect.anchorMax = new Vector2(0.9f, 0.98f);
        speakerRect.offsetMin = speakerRect.offsetMax = Vector2.zero;
        speakerName = speakerObject.GetComponent<TextMeshProUGUI>();
        speakerName.text = string.Empty;
        speakerName.fontSize = 24f;
        speakerName.alignment = TextAlignmentOptions.Center;
        speakerName.color = new Color(0.97f, 0.80f, 0.50f);
        speakerName.raycastTarget = false;
        var textObject = new GameObject("StoryCaption",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(band.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.02f);
        rect.anchorMax = new Vector2(1f, 0.64f);
        rect.offsetMin = new Vector2(100f, 2f);
        rect.offsetMax = new Vector2(-100f, -2f);
        caption = textObject.GetComponent<TextMeshProUGUI>();
        caption.fontSize = 38;
        caption.enableAutoSizing = true;
        caption.fontSizeMin = 24f;
        caption.fontSizeMax = 38f;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = new Color(1f, 0.89f, 0.64f);
        var skip = new GameObject("SkipRevealButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        skip.transform.SetParent(captionCanvasObject.transform, false);
        RectTransform skipRect = skip.GetComponent<RectTransform>();
        skipRect.anchorMin = skipRect.anchorMax = new Vector2(0.94f, 0.95f);
        skipRect.sizeDelta = new Vector2(125f, 58f);
        skipRect.anchoredPosition = Vector2.zero;
        skip.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.26f, 0.88f);
        skip.GetComponent<Button>().onClick.AddListener(() => skipRequested = true);
        var skipLabelObject = new GameObject("SkipLabel",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        skipLabelObject.transform.SetParent(skip.transform, false);
        var skipLabelRect = skipLabelObject.GetComponent<RectTransform>();
        skipLabelRect.anchorMin = Vector2.zero;
        skipLabelRect.anchorMax = Vector2.one;
        skipLabelRect.offsetMin = skipLabelRect.offsetMax = Vector2.zero;
        var skipLabel = skipLabelObject.GetComponent<TextMeshProUGUI>();
        skipLabel.text = "SKIP";
        skipLabel.fontSize = 24;
        skipLabel.alignment = TextAlignmentOptions.Center;
        skipLabel.color = Color.white;
        skipLabel.raycastTarget = false;
        // Use the stage's existing EventSystem for both pointer and touch.
    }

    private void Complete()
    {
        if (!running)
            return;
        if (!editorPreview)
        {
            completedThisSession = true;
            GameDatabaseManager.EnsureInstance()?.MarkFirstFragmentRevealed();
        }
        Action continueEncounter = resumeNode1;
        Cleanup();
        continueEncounter?.Invoke();
    }

    private void Cleanup()
    {
        running = false;
        IsCinematicActive = false;
        if (sequence != null)
            StopCoroutine(sequence);
        sequence = null;
        if (brain != null)
            brain.DefaultBlend = originalBlend;
        if (cameraObject != null)
            Destroy(cameraObject);
        cameraObject = null;
        revealCamera = null;
        if (captionCanvasObject != null)
            Destroy(captionCanvasObject);
        captionCanvasObject = null;
        caption = null;
        speakerName = null;
        foreach (var canvas in suspendedCanvases)
            if (canvas != null)
                canvas.enabled = true;
        suspendedCanvases.Clear();
        if (movement != null && movementLocked)
            movement.ReleaseExternalMovementLock();
        movementLocked = false;
        if (playerFacingChanged && player != null)
            player.rotation = originalPlayerRotation;
        playerFacingChanged = false;
        if (attacks != null && !attackWasLocked)
            attacks.SetAttackInputLocked(false);
        attacks = null;
        movement = null;
        player = null;
        brain = null;
        resumeNode1 = null;
        editorPreview = false;
        currentShotIndex = -1;
    }

    private void OnDisable()
    {
        if (running)
            Cleanup(); // Scene unload must not launch gameplay from a dead host.
    }

    private void OnDestroy()
    {
        if (running)
            Cleanup();
    }
}
