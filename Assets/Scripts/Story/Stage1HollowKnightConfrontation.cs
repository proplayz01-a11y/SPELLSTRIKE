using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Runs only after the Royal Castle's LIBERATE vocabulary encounter succeeds.
/// Van demands answers about Nevali, Hollow Knight mocks him, then the same
/// encounter's StartBossCombat() begins. One physical Main Camera, one
/// temporary high-priority Cinemachine camera; no scene swap/teleport.
///
/// OPTIONAL final-presentation upgrade: plug retargeted, user-authorized
/// talking mocap clips into the two serialized AnimationClip slots. They
/// play only while their respective character speaks; missing clips do not
/// block the dialogue, Cinemachine, or boss fight.
/// </summary>
[DisallowMultipleComponent]
public sealed class Stage1HollowKnightConfrontation : MonoBehaviour
{
    public static bool IsCinematicActive { get; private set; }

    [Header("Cinematic")]
    [SerializeField, Min(0.2f)] private float enterBlendSeconds = 0.7f;
    [SerializeField, Min(0.2f)] private float localShotMoveSeconds = 0.65f;

    [Header("Final polish — optional Humanoid talking mocap")]
    [SerializeField] private AnimationClip vanTalkingMocap;
    [SerializeField] private AnimationClip hollowKnightTalkingMocap;

    private struct DialogueBeat
    {
        public string speaker;
        public string words;
        public int shot;
        public DialogueBeat(string speaker, string words, int shot)
        {
            this.speaker = speaker;
            this.words = words;
            this.shot = shot;
        }
    }

    // Never reveal Vander Poel, or falsely assert Nevali is already dead.
    private static readonly DialogueBeat[] Beats =
    {
        new DialogueBeat("VAN AERT",
            "Hollow Knight! Where did you take my daughter?", 0),
        new DialogueBeat("HOLLOW KNIGHT",
            "So the grieving wizard has finally found my halls.", 1),
        new DialogueBeat("VAN AERT",
            "WHERE IS NEVALI?! Answer me!", 2),
        new DialogueBeat("HOLLOW KNIGHT",
            "You crossed a broken kingdom for a child you'll never see again.", 3),
        new DialogueBeat("VAN AERT",
            "You took her. You tore her voice away. Tell me where she is!", 2),
        new DialogueBeat("HOLLOW KNIGHT",
            "That voice belongs to the darkness now. Come and try to take it.", 3),
        new DialogueBeat("VAN AERT",
            "Then I will tear through every shadow between us. Nevali, I'm coming!", 4)
    };

    private HollowKnightController knight;
    private Transform van;
    private Animator vanAnimator;
    private Animator knightAnimator;
    private MovementofPlayer movement;
    private AttackController attacks;
    private bool movementLocked;
    private bool attacksWereLocked;
    private Quaternion originalVanRotation;
    private bool rotatedVan;
    private Camera gameCamera;
    private CinemachineBrain brain;
    private CinemachineBlendDefinition previousBlend;
    private GameObject temporaryCameraObject;
    private CinemachineCamera virtualCamera;
    private Vector3 fromPosition, toPosition;
    private Quaternion fromRotation, toRotation;
    private float fromFov, toFov, shotTime;
    private GameObject dialogueCanvasObject;
    private TMP_Text speakerText, wordsText, nextText;
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private Action onCompleted;
    private bool playing, preview;
    private bool revealing;
    private int lineIndex;
    private float revealed;
    private PlayableGraph mocapGraph;
    private Animator activeMocapAnimator;
    private bool mocapOldRootMotion;

    public bool IsRunning => playing;
    public int CurrentLine => lineIndex;
    public string CurrentSpeaker => playing ? Beats[lineIndex].speaker : string.Empty;
    public string CurrentWords => playing ? Beats[lineIndex].words : string.Empty;
    public bool IsMocapAssigned => vanTalkingMocap != null;

    /// <returns>False to let the original trigger start combat immediately.</returns>
    public bool Begin(HollowKnightController boss, Action startCombat)
    {
        if (boss == null || startCombat == null || playing ||
            IsCinematicActive || SpiritRemnantCinematic.IsCinematicActive ||
            Stage1PostJesterEchoCall.IsCinematicActive ||
            Stage1FirstFragmentReveal.IsCinematicActive)
            return false;
        return BeginInternal(boss, startCombat, false);
    }

#if UNITY_EDITOR
    // No progress/boss activation when assessing camera/dialogue in the editor.
    public bool BeginEditorPreview(HollowKnightController boss)
    {
        return boss != null && BeginInternal(boss, null, true);
    }
#endif

    private bool BeginInternal(HollowKnightController boss, Action startCombat,
        bool editorPreview)
    {
        if (playing || IsCinematicActive || boss == null)
            return false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        gameCamera = Camera.main;
        brain = gameCamera != null ? gameCamera.GetComponent<CinemachineBrain>() : null;
        if (player == null || brain == null)
        {
            Debug.LogWarning("[HollowConfrontation] Player or CinemachineBrain unavailable.");
            return false;
        }

        knight = boss;
        knightAnimator = boss.animator != null
            ? boss.animator : boss.GetComponentInChildren<Animator>(true);
        movement = player.GetComponent<MovementofPlayer>();
        van = movement != null ? movement.transform : player.transform;
        vanAnimator = van.GetComponentInChildren<Animator>(true);
        attacks = van.GetComponent<AttackController>();
        onCompleted = startCombat;
        preview = editorPreview;
        playing = true;
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
                attacksWereLocked = attacks.IsAttackInputLocked;
                if (!attacksWereLocked)
                    attacks.SetAttackInputLocked(true);
            }
            originalVanRotation = van.rotation;
            Vector3 towardKnight = knight.transform.position - van.position;
            towardKnight.y = 0f;
            if (towardKnight.sqrMagnitude > 0.01f)
            {
                van.rotation = Quaternion.LookRotation(towardKnight.normalized, Vector3.up);
                rotatedVan = true;
            }

            HideGameplayCanvases();
            BuildDialogueCanvas();
            previousBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, enterBlendSeconds);
            temporaryCameraObject = new GameObject("Stage1_HollowKnight_PreBossCinemachineCamera");
            temporaryCameraObject.transform.SetPositionAndRotation(
                gameCamera.transform.position, gameCamera.transform.rotation);
            virtualCamera = temporaryCameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Priority.Enabled = true;
            virtualCamera.Priority.Value = 14000;
            virtualCamera.Lens.FieldOfView = 46f;
            ShowLine(0);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[HollowConfrontation] Setup failed; boss combat will proceed: " + e, this);
            Finish(false);
            return false;
        }
    }

    private void Update()
    {
        if (!playing)
            return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            Advance();
        if (revealing && wordsText != null)
        {
            revealed += Time.unscaledDeltaTime * 38f;
            wordsText.maxVisibleCharacters = Mathf.CeilToInt(revealed);
            if (wordsText.maxVisibleCharacters >= wordsText.textInfo.characterCount)
                revealing = false;
            RefreshNext();
        }
    }

    private void LateUpdate()
    {
        if (!playing || temporaryCameraObject == null || virtualCamera == null)
            return;
        float t = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((Time.unscaledTime - shotTime) /
                Mathf.Max(0.1f, localShotMoveSeconds)));
        temporaryCameraObject.transform.SetPositionAndRotation(
            Vector3.Lerp(fromPosition, toPosition, t),
            Quaternion.Slerp(fromRotation, toRotation, t));
        virtualCamera.Lens.FieldOfView = Mathf.Lerp(fromFov, toFov, t);
    }

    public void Advance()
    {
        if (!playing || wordsText == null)
            return;
        if (revealing)
        {
            wordsText.maxVisibleCharacters = int.MaxValue;
            revealing = false;
            RefreshNext();
            return;
        }
        if (lineIndex == Beats.Length - 1)
        {
            Finish(true);
            return;
        }
        ShowLine(lineIndex + 1);
    }

    private void ShowLine(int index)
    {
        lineIndex = index;
        DialogueBeat beat = Beats[index];
        speakerText.text = beat.speaker;
        wordsText.text = beat.words;
        wordsText.ForceMeshUpdate();
        wordsText.maxVisibleCharacters = 0;
        revealed = 0f;
        revealing = true;
        RefreshNext();
        ChangeShot(beat.shot);
        // Final mocap slots are deliberately optional. The script is fully
        // functional before the user supplies a legitimately usable clip.
        PlayTalkingMocap(beat.speaker == "VAN AERT"
            ? vanAnimator : knightAnimator,
            beat.speaker == "VAN AERT"
                ? vanTalkingMocap : hollowKnightTalkingMocap);
    }

    private static Vector3 Face(Animator animator, Transform fallback)
    {
        if (animator != null && animator.isHuman && animator.avatar != null &&
            animator.avatar.isValid)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                return head.position;
        }
        return fallback.position + Vector3.up * 17f;
    }

    private void ChangeShot(int shot)
    {
        if (virtualCamera == null || van == null || knight == null)
            return;

        Vector3 vanFace = Face(vanAnimator, van);
        Vector3 knightFace = Face(knightAnimator, knight.transform);
        Vector3 across = knightFace - vanFace;
        across.y = 0f;
        if (across.sqrMagnitude < 1f) across = van.forward;
        across.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, across).normalized;
        bool onVan = shot == 0 || shot == 2 || shot == 4;
        Vector3 focus = onVan ? vanFace : knightFace;
        Vector3 target;
        float fov;
        switch (shot)
        {
            case 0: // Father's first demand
                focus += Vector3.down * 2f;
                target = focus + across * 24f - side * 13f + Vector3.up * 5f;
                fov = 44f;
                break;
            case 1: // Knight's cold recognition
                focus += Vector3.down * 2.5f;
                target = focus - across * 25f + side * 13f + Vector3.up * 4f;
                fov = 46f;
                break;
            case 2: // Close-up: Van loses his restraint
                focus += Vector3.down * 1f;
                target = focus + across * 18f - side * 10f + Vector3.up * 2f;
                fov = 39f;
                break;
            case 3: // Knight's intimate, mocking threat
                focus += Vector3.down * 1.2f;
                target = focus - across * 16f + side * 8f + Vector3.up * 2f;
                fov = 37f;
                break;
            default: // Van's resolve before returning to combat
                focus += Vector3.down * 1f;
                target = focus + across * 16f - side * 7f + Vector3.up * 1.5f;
                fov = 36f;
                break;
        }

        Vector3 cast = target - focus;
        float allowed = cast.magnitude;
        if (allowed > 0.01f)
        {
            foreach (RaycastHit hit in Physics.SphereCastAll(focus, 0.7f,
                         cast.normalized, allowed, ~((1 << 2) | (1 << 5)),
                         QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(van) ||
                    hit.collider.transform.IsChildOf(knight.transform))
                    continue;
                if (hit.distance > 4f)
                    allowed = Mathf.Min(allowed, hit.distance - 1.1f);
            }
            target = focus + cast.normalized * Mathf.Max(5f, allowed);
        }

        // The actors can be separated by hundreds of world units in this
        // stage. Cut straight between their close-ups: never dolly the
        // camera through the entire castle, roofs or collision geometry.
        bool distantCut = Vector3.Distance(
            temporaryCameraObject.transform.position, target) > 100f;
        if (distantCut)
            temporaryCameraObject.transform.SetPositionAndRotation(target,
                Quaternion.LookRotation(focus - target, Vector3.up));

        fromPosition = temporaryCameraObject.transform.position;
        fromRotation = temporaryCameraObject.transform.rotation;
        fromFov = distantCut ? fov : virtualCamera.Lens.FieldOfView;
        toPosition = target;
        toRotation = Quaternion.LookRotation(focus - target, Vector3.up);
        toFov = fov;
        shotTime = Time.unscaledTime;
    }

    private void PlayTalkingMocap(Animator actor, AnimationClip clip)
    {
        StopTalkingMocap();
        if (actor == null || clip == null)
            return;

        // Play only the imported talking mocap, without changing either
        // combat Animator Controller or modifying the source asset. Stop
        // the temporary graph before the boss fight resumes.
        mocapOldRootMotion = actor.applyRootMotion;
        activeMocapAnimator = actor;
        actor.applyRootMotion = false;
        mocapGraph = PlayableGraph.Create("HollowKnightConfrontation_TalkingMocap");
        mocapGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationClipPlayable playable = AnimationClipPlayable.Create(mocapGraph, clip);
        playable.SetApplyFootIK(false);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            mocapGraph, "TalkingActor", actor);
        output.SetSourcePlayable(playable);
        mocapGraph.Play();
    }

    private void StopTalkingMocap()
    {
        if (mocapGraph.IsValid())
            mocapGraph.Destroy();
        if (activeMocapAnimator != null)
            activeMocapAnimator.applyRootMotion = mocapOldRootMotion;
        activeMocapAnimator = null;
    }

    private void RefreshNext()
    {
        if (nextText != null)
            nextText.text = revealing ? "REVEAL >" :
                lineIndex == Beats.Length - 1 ? "FIGHT >" : "CONTINUE >";
    }

    private void HideGameplayCanvases()
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

    private void BuildDialogueCanvas()
    {
        dialogueCanvasObject = new GameObject("HollowKnightConfrontation_DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = dialogueCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        CanvasScaler scaler = dialogueCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Panel(dialogueCanvasObject.transform, "TopLetterbox",
            new Vector2(0, 0.88f), Vector2.one,
            new Color(0.008f, 0.012f, 0.035f, 0.97f));
        Panel(dialogueCanvasObject.transform, "BottomLetterbox",
            Vector2.zero, new Vector2(1, 0.30f),
            new Color(0.008f, 0.012f, 0.035f, 0.98f));

        Text(dialogueCanvasObject.transform, "Title",
            new Vector2(0.07f, 0.908f), new Vector2(0.93f, 0.963f),
            "THE KNIGHT WITHOUT MERCY", 34f,
            new Color(1f, 0.84f, 0.54f), TextAlignmentOptions.Center);
        Text(dialogueCanvasObject.transform, "Subtitle",
            new Vector2(0.07f, 0.874f), new Vector2(0.93f, 0.910f),
            "SPELLSTRIKE / ENCHANTED KINGDOM / THE ROYAL CASTLE",
            18f, new Color(0.75f, 0.83f, 0.96f), TextAlignmentOptions.Center);
        speakerText = Text(dialogueCanvasObject.transform, "Speaker",
            new Vector2(0.13f, 0.21f), new Vector2(0.84f, 0.265f),
            string.Empty, 28f, new Color(1f, 0.84f, 0.5f),
            TextAlignmentOptions.Left);
        wordsText = Text(dialogueCanvasObject.transform, "Dialogue",
            new Vector2(0.13f, 0.073f), new Vector2(0.84f, 0.205f),
            string.Empty, 32f, Color.white, TextAlignmentOptions.TopLeft);
        wordsText.enableAutoSizing = true;
        wordsText.fontSizeMin = 24;
        wordsText.fontSizeMax = 32;
        RectTransform buttonRoot = Panel(dialogueCanvasObject.transform, "ContinueButton",
            new Vector2(0.80f, 0.027f), new Vector2(0.93f, 0.076f),
            new Color(0.50f, 0.33f, 0.14f, 0.95f));
        Button button = buttonRoot.gameObject.AddComponent<Button>();
        button.onClick.AddListener(Advance);
        nextText = Text(buttonRoot, "NextText", Vector2.zero, Vector2.one,
            "CONTINUE >", 20f, Color.white, TextAlignmentOptions.Center);
        Text(dialogueCanvasObject.transform, "InputHint",
            new Vector2(0.09f, 0.018f), new Vector2(0.49f, 0.064f),
            "E / TAP NEXT TO CONTINUE", 16f,
            new Color(0.7f, 0.79f, 0.9f), TextAlignmentOptions.Left);
    }

    private static RectTransform Panel(Transform root, string name,
        Vector2 min, Vector2 max, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text Text(Transform root, string name,
        Vector2 min, Vector2 max, string value, float size,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform),
            typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text label = obj.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private void Finish(bool startCombat)
    {
        if (!playing)
            return;

        playing = false;
        IsCinematicActive = false;
        StopTalkingMocap();
        if (brain != null) brain.DefaultBlend = previousBlend;
        if (virtualCamera != null) virtualCamera.Priority.Value = 0;
        if (temporaryCameraObject != null) Destroy(temporaryCameraObject);
        temporaryCameraObject = null;
        virtualCamera = null;
        if (dialogueCanvasObject != null) Destroy(dialogueCanvasObject);
        dialogueCanvasObject = null;
        speakerText = wordsText = nextText = null;
        foreach (Canvas canvas in hiddenCanvases)
            if (canvas != null) canvas.enabled = true;
        hiddenCanvases.Clear();
        if (movement != null && movementLocked)
            movement.ReleaseExternalMovementLock();
        movementLocked = false;
        if (attacks != null && !attacksWereLocked)
            attacks.SetAttackInputLocked(false);
        if (van != null && rotatedVan)
            van.rotation = originalVanRotation;
        rotatedVan = false;
        movement = null;
        attacks = null;
        van = null;
        vanAnimator = null;
        knightAnimator = null;
        knight = null;
        brain = null;

        Action finished = onCompleted;
        onCompleted = null;
        bool isPreview = preview;
        preview = false;
        if (startCombat && !isPreview)
            finished?.Invoke();
    }

    private void OnDisable()
    {
        // A running story must never permanently softlock the locked arena.
        // No activation callback is installed for editor-only previews.
        if (playing)
            Finish(true);
    }

    private void OnDestroy()
    {
        if (playing)
            Finish(true);
    }
}
