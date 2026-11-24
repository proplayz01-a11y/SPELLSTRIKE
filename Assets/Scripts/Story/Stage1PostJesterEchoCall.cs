using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Cracked Rune Core lets VAN initiate a scripted call to the Spirit
/// immediately after the Cursed Jester fragment is actually collected.
/// Neither the Spirit nor the fragment is teleported into this scene.
/// Uses the same Main Camera/Brain as the existing Spirit cinematic.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage1PostJesterEchoCall : MonoBehaviour
{
    public static bool IsCinematicActive { get; private set; }

    [Header("Authored story references")]
    [SerializeField] private GameObject echoRuneCorePrefab;
    [SerializeField] private Transform royalCastleApproach;
    [SerializeField, Min(0.2f)] private float blendSeconds = 0.75f;
    [SerializeField, Min(0.3f)] private float shotMoveSeconds = 0.85f;
    [SerializeField, Min(0.1f)] private float coreWorldScale = 1.2f;

    private struct Line
    {
        public string speaker;
        public string words;
        public int shot;
        public Line(string name, string text, int view)
        {
            speaker = name;
            words = text;
            shot = view;
        }
    }

    private static readonly Line[] Script =
    {
        new Line("VAN AERT", "Spirit Remnant... can you hear me?", 0),
        new Line("SPIRIT REMNANT", "I hear you, Van Aert. The heart of my essence still answers your call.", 0),
        new Line("VAN AERT", "I reclaimed another piece of Nevali. And... I can see the Royal Castle from here.", 1),
        new Line("SPIRIT REMNANT", "Then you are close to the Hollow Knight. He carries the fragment of her voice within his armor.", 2),
        new Line("VAN AERT", "The knight who took her? Tell me what I should expect.", 1),
        new Line("SPIRIT REMNANT", "He will mock your hope and turn your fear against you. Do not believe his cruelty. Her voice can still be reclaimed.", 0),
        new Line("VAN AERT", "Then I'll face him. Nevali... I'm coming for you.", 3)
    };

    private Coroutine pending;
    private bool running;
    private bool previewOnly;
    private bool finishedThisSession;
    private Transform van;
    private Transform vanHead;
    private Transform vanHand;
    private Quaternion originalVanRotation;
    private bool rotatedVanForDialogue;
    private MovementofPlayer movement;
    private AttackController attacks;
    private bool movementLocked;
    private bool attacksPreviouslyLocked;
    private Camera outputCamera;
    private CinemachineBrain brain;
    private CinemachineBlendDefinition oldBlend;
    private GameObject cameraObject;
    private CinemachineCamera cinematicCamera;
    private Vector3 shotStartPosition;
    private Vector3 shotTargetPosition;
    private Quaternion shotStartRotation;
    private Quaternion shotTargetRotation;
    private float startFov;
    private float targetFov;
    private float shotStartTime;
    private GameObject runeObject;
    private Light runeLight;
    private GameObject canvasObject;
    private TMP_Text speakerLabel;
    private TMP_Text dialogueLabel;
    private TMP_Text nextLabel;
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private int lineIndex;
    private int visibleCharacters;
    private bool inReveal;
    private float revealProgress;

    public bool IsRunning => running;
    public int CurrentLine => lineIndex;
    public string CurrentWords => running ? Script[lineIndex].words : string.Empty;
    public bool HasCompleted => finishedThisSession ||
        (GameDatabaseManager.Instance != null &&
         GameDatabaseManager.Instance.HasCompletedJesterEchoCall());

    public void RequestAfterPickup()
    {
        if (running || pending != null || HasCompleted)
            return;
        pending = StartCoroutine(AfterPickup());
    }

    private IEnumerator AfterPickup()
    {
        // FragmentPickup invokes Node3.OnFragmentCollected before SetActive(false).
        // Only start filming after the pickup has visibly disappeared.
        yield return new WaitForEndOfFrame();
        pending = null;
        // Direct Stage 1 Play Mode can have an inactive/bootstrap manager.
        // Resolve the normal game database just as the Spirit gift does,
        // rather than quietly skipping the story after a valid pickup.
        var db = GameDatabaseManager.EnsureInstance();
        if (db == null || !db.HasEchoRuneCore())
        {
            Debug.LogWarning("[PostJesterEcho] Core not yet acquired; no remote call can start.", this);
            yield break;
        }
        GameObject actor = GameObject.FindGameObjectWithTag("Player");
        if (actor == null || !Begin(actor.transform, false))
            Debug.LogWarning("[PostJesterEcho] Cinematic unavailable after pickup; existing Node 3 progress is unaffected.", this);
    }

#if UNITY_EDITOR
    /// <summary>Preview camera/dialogue without awarding or writing story flags.</summary>
    public bool BeginEditorPreview(Transform actor)
    {
        return Begin(actor, true);
    }
#endif

    private bool Begin(Transform actor, bool editorPreview)
    {
        if (running || IsCinematicActive || SpiritRemnantCinematic.IsCinematicActive ||
            Stage1FirstFragmentReveal.IsCinematicActive || actor == null)
            return false;
        if (!editorPreview && HasCompleted)
            return false;
        if (echoRuneCorePrefab == null)
        {
            Debug.LogWarning("[PostJesterEcho] Rune core model is not assigned.", this);
            return false;
        }
        outputCamera = Camera.main;
        brain = outputCamera != null ? outputCamera.GetComponent<CinemachineBrain>() : null;
        if (brain == null)
            return false;
        movement = actor.GetComponentInParent<MovementofPlayer>();
        van = movement != null ? movement.transform : actor;
        attacks = van.GetComponent<AttackController>();
        Animator animator = van.GetComponentInChildren<Animator>(true);
        vanHead = animator != null && animator.isHuman && animator.avatar != null &&
                  animator.avatar.isValid
            ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        vanHand = animator != null && animator.isHuman && animator.avatar != null &&
                  animator.avatar.isValid
            ? animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
        running = true;
        previewOnly = editorPreview;
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
                attacksPreviouslyLocked = attacks.IsAttackInputLocked;
                if (!attacksPreviouslyLocked)
                    attacks.SetAttackInputLocked(true);
            }
            // Van's gameplay rig can retain substantial pitch/roll from its
            // last spell animation. Stage the dialogue upright regardless of
            // approach angle, then restore exactly what gameplay had.
            originalVanRotation = van.rotation;
            Vector3 towardCastle = royalCastleApproach != null
                ? royalCastleApproach.position - van.position : van.forward;
            towardCastle.y = 0f;
            if (towardCastle.sqrMagnitude > 0.01f)
            {
                van.rotation = Quaternion.LookRotation(
                    towardCastle.normalized, Vector3.up);
                rotatedVanForDialogue = true;
            }
            HideGameplayHud();
            BuildDialogueUi();
            oldBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, blendSeconds);
            cameraObject = new GameObject("Stage1_PostJester_EchoCinemachineCamera");
            cameraObject.transform.SetPositionAndRotation(
                outputCamera.transform.position, outputCamera.transform.rotation);
            cinematicCamera = cameraObject.AddComponent<CinemachineCamera>();
            cinematicCamera.Priority.Enabled = true;
            cinematicCamera.Priority.Value = 13000;
            cinematicCamera.Lens.FieldOfView = 52f;
            SpawnRemoteCore();
            ShowLine(0);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("[PostJesterEcho] Setup failed: " + error.Message, this);
            Finish(false);
            return false;
        }
    }

    private Vector3 GetVanFace()
    {
        return vanHead != null ? vanHead.position :
            van.position + Vector3.up * 13f;
    }

    private Vector3 GetRunePosition()
    {
        return vanHand != null
            ? vanHand.position + Vector3.up * 2.1f + van.forward * 1.7f
            : GetVanFace() - Vector3.up * 3f + van.forward * 3f;
    }

    private void SpawnRemoteCore()
    {
        runeObject = Instantiate(echoRuneCorePrefab, GetRunePosition(), Quaternion.identity);
        runeObject.name = "EchoRuneCore_RemoteCall";
        runeObject.transform.localScale = Vector3.one * coreWorldScale;
        var glow = new GameObject("RemoteEchoLight");
        glow.transform.SetParent(runeObject.transform, false);
        runeLight = glow.AddComponent<Light>();
        runeLight.type = LightType.Point;
        runeLight.color = new Color(0.22f, 0.78f, 1f);
        runeLight.range = 22f;
        runeLight.intensity = 4f;
        runeLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (!running)
            return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return))
            Advance();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape first reveals a partly displayed line; otherwise
            // advances. This story milestone cannot be silently discarded.
            Advance();
        }
        if (inReveal && dialogueLabel != null)
        {
            revealProgress += Time.unscaledDeltaTime * 30f;
            visibleCharacters = Mathf.Min(
                dialogueLabel.textInfo.characterCount,
                Mathf.CeilToInt(revealProgress));
            dialogueLabel.maxVisibleCharacters = visibleCharacters;
            if (visibleCharacters >= dialogueLabel.textInfo.characterCount)
                inReveal = false;
            UpdateNextLabel();
        }
    }

    private void LateUpdate()
    {
        if (!running)
            return;
        if (runeObject != null && van != null)
        {
            float time = Time.unscaledTime;
            runeObject.transform.position = GetRunePosition() +
                Vector3.up * (0.32f * Mathf.Sin(time * 2.4f));
            runeObject.transform.rotation = Quaternion.Euler(15f, time * 32f, 9f);
            if (runeLight != null)
                runeLight.intensity = 4.0f + 0.7f * Mathf.Sin(time * 4.1f);
        }
        if (cameraObject != null && cinematicCamera != null)
        {
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((Time.unscaledTime - shotStartTime) /
                              Mathf.Max(0.1f, shotMoveSeconds)));
            cameraObject.transform.SetPositionAndRotation(
                Vector3.Lerp(shotStartPosition, shotTargetPosition, t),
                Quaternion.Slerp(shotStartRotation, shotTargetRotation, t));
            cinematicCamera.Lens.FieldOfView = Mathf.Lerp(startFov, targetFov, t);
        }
    }

    public void Advance()
    {
        if (!running || dialogueLabel == null)
            return;
        if (inReveal)
        {
            inReveal = false;
            dialogueLabel.maxVisibleCharacters = int.MaxValue;
            UpdateNextLabel();
            return;
        }
        if (lineIndex >= Script.Length - 1)
        {
            Finish(true);
            return;
        }
        ShowLine(lineIndex + 1);
    }

    private void ShowLine(int index)
    {
        lineIndex = index;
        Line line = Script[index];
        speakerLabel.text = line.speaker;
        dialogueLabel.text = line.words;
        dialogueLabel.maxVisibleCharacters = 0;
        dialogueLabel.ForceMeshUpdate();
        revealProgress = 0f;
        visibleCharacters = 0;
        inReveal = true;
        ChangeShot(line.shot);
        UpdateNextLabel();
    }

    private void UpdateNextLabel()
    {
        if (nextLabel == null)
            return;
        nextLabel.text = inReveal ? "REVEAL  >" :
            lineIndex == Script.Length - 1 ? "FINISH  >" : "CONTINUE  >";
    }

    private void ChangeShot(int shot)
    {
        if (van == null || cameraObject == null || cinematicCamera == null)
            return;
        Vector3 face = GetVanFace();
        Vector3 forward = van.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 rune = GetRunePosition();
        Vector3 focus;
        Vector3 position;
        float fov;
        switch (shot)
        {
            case 0: // Floating core and Van's hand, without a local Spirit body
                focus = Vector3.Lerp(rune, face, 0.42f);
                position = focus + forward * 20f + side * 12f + Vector3.up * 4f;
                fov = 42f;
                break;
            case 1: // Van makes the discovery and initiates his questions
                focus = face + Vector3.down * 1.2f;
                position = focus + forward * 23f - side * 12f + Vector3.up * 3f;
                fov = 41f;
                break;
            case 2: // Remote Spirit voice: close on the speaking rune.
                // From the Jester arena the castle is behind dense scenery;
                // the old distant cutaway intersected roofs and wooden beams.
                // The Spirit must NOT physically appear or initiate the call.
                focus = Vector3.Lerp(rune, face, 0.22f);
                position = focus + forward * 10f - side * 6f + Vector3.up * 2f;
                fov = 36f;
                break;
            default: // Final resolve: understated close-up on Van
                focus = face + Vector3.down * 0.6f;
                position = focus + forward * 17f - side * 8f + Vector3.up * 2f;
                fov = 37f;
                break;
        }

        // Keep cinematic shots outside nearby solid geometry when possible.
        Vector3 ray = position - focus;
        float allowed = ray.magnitude;
        if (ray.sqrMagnitude > 0.01f)
        {
            foreach (var hit in Physics.SphereCastAll(focus, 0.75f,
                         ray.normalized, allowed, ~((1 << 2) | (1 << 5)),
                         QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null ||
                    (van != null && hit.collider.transform.IsChildOf(van)))
                    continue;
                if (hit.distance > 4f)
                    allowed = Mathf.Min(allowed, Mathf.Max(5f, hit.distance - 1.2f));
            }
        }
        position = focus + ray.normalized * allowed;
        shotStartPosition = cameraObject.transform.position;
        shotStartRotation = cameraObject.transform.rotation;
        startFov = cinematicCamera.Lens.FieldOfView;
        shotTargetPosition = position;
        shotTargetRotation = Quaternion.LookRotation(focus - position, Vector3.up);
        targetFov = fov;
        shotStartTime = Time.unscaledTime;
    }

    private void HideGameplayHud()
    {
        hiddenCanvases.Clear();
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.enabled || !canvas.gameObject.activeInHierarchy ||
                canvas.renderMode == RenderMode.WorldSpace)
                continue;
            canvas.enabled = false;
            hiddenCanvases.Add(canvas);
        }
    }

    private void BuildDialogueUi()
    {
        canvasObject = new GameObject("PostJester_EchoDialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        CreatePanel(canvasObject.transform, "TopLetterbox",
            new Vector2(0, 0.88f), Vector2.one,
            new Color(0.01f, 0.02f, 0.04f, 0.97f));
        CreatePanel(canvasObject.transform, "BottomLetterbox",
            Vector2.zero, new Vector2(1, 0.29f),
            new Color(0.01f, 0.02f, 0.04f, 0.97f));
        var heading = CreateText(canvasObject.transform, "ChapterTitle",
            new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.97f),
            34, new Color(1f, 0.85f, 0.55f), TextAlignmentOptions.Center);
        heading.text = "THE ECHO RUNE AWAKENS";
        var subtitle = CreateText(canvasObject.transform, "ChapterSubtitle",
            new Vector2(0.1f, 0.875f), new Vector2(0.9f, 0.905f),
            18, new Color(0.7f, 0.8f, 1f), TextAlignmentOptions.Center);
        subtitle.text = "SPELLSTRIKE  /  ENCHANTED KINGDOM  /  THE ROAD TO THE CASTLE";
        speakerLabel = CreateText(canvasObject.transform, "Speaker",
            new Vector2(0.135f, 0.205f), new Vector2(0.86f, 0.265f),
            28, new Color(1f, 0.84f, 0.50f), TextAlignmentOptions.Left);
        dialogueLabel = CreateText(canvasObject.transform, "Dialogue",
            new Vector2(0.135f, 0.07f), new Vector2(0.86f, 0.20f),
            30, Color.white, TextAlignmentOptions.TopLeft);
        dialogueLabel.enableAutoSizing = true;
        dialogueLabel.fontSizeMin = 23;
        dialogueLabel.fontSizeMax = 30;
        var buttonBg = CreatePanel(canvasObject.transform, "ContinueButton",
            new Vector2(0.79f, 0.025f), new Vector2(0.92f, 0.073f),
            new Color(0.48f, 0.32f, 0.15f, 0.95f));
        var button = buttonBg.gameObject.AddComponent<Button>();
        button.onClick.AddListener(Advance);
        nextLabel = CreateText(buttonBg, "NextLabel", Vector2.zero, Vector2.one,
            19, Color.white, TextAlignmentOptions.Center);
        var hint = CreateText(canvasObject.transform, "InputHint",
            new Vector2(0.10f, 0.017f), new Vector2(0.47f, 0.065f),
            16, new Color(0.75f, 0.82f, 0.9f), TextAlignmentOptions.Left);
        hint.text = "E / TAP NEXT TO CONTINUE";
    }

    private static RectTransform CreatePanel(Transform parent, string title,
        Vector2 min, Vector2 max, Color color)
    {
        var obj = new GameObject(title, typeof(RectTransform), typeof(Image));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string title,
        Vector2 min, Vector2 max, float fontSize, Color color,
        TextAlignmentOptions align)
    {
        var obj = new GameObject(title, typeof(RectTransform),
            typeof(TextMeshProUGUI));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = obj.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = align;
        label.raycastTarget = false;
        return label;
    }

    private void Finish(bool reachedEnd)
    {
        if (!running)
            return;
        if (reachedEnd && !previewOnly)
        {
            finishedThisSession = true;
            GameDatabaseManager.EnsureInstance()?.MarkJesterEchoCallCompleted();
        }
        running = false;
        IsCinematicActive = false;
        if (brain != null)
            brain.DefaultBlend = oldBlend;
        if (cameraObject != null) Destroy(cameraObject);
        cameraObject = null;
        cinematicCamera = null;
        if (runeObject != null) Destroy(runeObject);
        runeObject = null;
        runeLight = null;
        if (canvasObject != null) Destroy(canvasObject);
        canvasObject = null;
        speakerLabel = dialogueLabel = nextLabel = null;
        foreach (var canvas in hiddenCanvases)
            if (canvas != null) canvas.enabled = true;
        hiddenCanvases.Clear();
        if (movement != null && movementLocked)
            movement.ReleaseExternalMovementLock();
        movementLocked = false;
        if (attacks != null && !attacksPreviouslyLocked)
            attacks.SetAttackInputLocked(false);
        if (van != null && rotatedVanForDialogue)
            van.rotation = originalVanRotation;
        rotatedVanForDialogue = false;
        movement = null;
        attacks = null;
        van = vanHand = vanHead = null;
        brain = null;
        previewOnly = false;
    }

    private void OnDisable()
    {
        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }
        if (running) Finish(false);
    }

    private void OnDestroy()
    {
        if (running) Finish(false);
    }
}
