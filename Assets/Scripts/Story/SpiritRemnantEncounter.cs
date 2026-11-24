using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage 1's story-first NPC. E on PC; reuses MobileStageControls on Android.
/// Gameplay-owned progression is awarded ONLY after the complete first dialogue.
/// The approved Spirit conversation remains intact; the Echo Rune Core gift
/// is a separate chapter using the existing speaker-driven cinematic camera.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpiritRemnantEncounter : MonoBehaviour
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 5)] public string words;
        public DialogueLine(string who, string what) { speaker = who; words = what; }
    }

    [Header("Interaction")]
    [SerializeField, Min(1f)] private float interactionRadius = 11f;
    [SerializeField] private Transform interactionCenter;
    public Transform GuidanceTarget => interactionCenter != null ? interactionCenter : transform;
    public float GuidanceArrivalRadius => interactionRadius;
    [SerializeField] private SpiritRemnantAmbientMotion ambientMotion;
    [SerializeField] private GoalsPanelUI goalsPanelUI;
    [SerializeField] private SpiritRemnantCinematic cinematic;
    [SerializeField] private EchoRuneCorePresentation echoCorePresentation;
    [SerializeField, Range(15f, 110f)] private float dialogueRevealCharactersPerSecond = 51f;

    [Header("Story (editable in Inspector)")]
    [SerializeField] private DialogueLine[] firstMeeting =
    {
        new DialogueLine("SPIRIT REMNANT", "A living wizard...? No living soul has wandered these ruins in ages."),
        new DialogueLine("VAN AERT", "What happened to this place?"),
        new DialogueLine("SPIRIT REMNANT", "Broken Language stole our voices. Our kingdom fell silent long before its walls fell."),
        new DialogueLine("VAN AERT", "I'm looking for a little girl. Bright blue eyes. Have you seen her?"),
        new DialogueLine("SPIRIT REMNANT", "A child...? Yes. She wandered this village, lost and crying. The Hollow Knight took her for a sacrifice."),
        new DialogueLine("VAN AERT", "A sacrifice? No... Where did he take her?"),
        new DialogueLine("SPIRIT REMNANT", "I fear what remains of her is no longer whole. Her existence has been shattered into fragments across this cursed book."),
        new DialogueLine("VAN AERT", "Shattered...? What do you mean, shattered?"),
        new DialogueLine("SPIRIT REMNANT", "Pieces of who she is have been separated and imprisoned. Broken Language can unravel more than words."),
        new DialogueLine("VAN AERT", "But... how is that possible?"),
        new DialogueLine("SPIRIT REMNANT", "I do not know how it was done. I know only that her soul is no longer whole."),
        new DialogueLine("VAN AERT", "Where? Where did those fragments go? Are they still somewhere inside this book?"),
        new DialogueLine("SPIRIT REMNANT", "I cannot tell you where all have gone. But the Hollow Knight may hold the answers you seek."),
        new DialogueLine("VAN AERT", "The Hollow Knight... Where can I find him?"),
        new DialogueLine("SPIRIT REMNANT", "Beyond these ruins, within the Royal Castle. Beware, wizard. Many entered those halls. None returned."),
        new DialogueLine("VAN AERT", "The Royal Castle...")
    };

    [SerializeField] private DialogueLine[] repeatMeeting =
    {
        new DialogueLine("VAN AERT", "Remind me. Where did you say the Hollow Knight took her?"),
        new DialogueLine("SPIRIT REMNANT", "The Royal Castle, beyond the ruins. I know nothing of the other fragments. Please, be careful."),
        new DialogueLine("SPIRIT REMNANT", "Remember: the creatures draw strength from the fragments. Follow the golden trail toward the arena. Break their hold before they consume more of her light.")
    };

    // Separate revelation, so the 16 Inspector-authored first-meeting lines
    // and their approved Cinemachine shot indices remain untouched.
    private static readonly DialogueLine[] FragmentFeedingRevelation =
    {
        new DialogueLine("SPIRIT REMNANT", "One more truth, Van Aert. The creatures guarding these fragments do not merely possess them."),
        new DialogueLine("VAN AERT", "What do you mean? What are they doing to my daughter?"),
        new DialogueLine("SPIRIT REMNANT", "They draw their strength from the pieces of her shattered soul. They feed on the light imprisoned inside each fragment."),
        new DialogueLine("VAN AERT", "You mean every moment they stand there... they're stealing her power?"),
        new DialogueLine("SPIRIT REMNANT", "Yes. Undisturbed, they will keep feeding. Break their hold, defeat them, and reclaim what belongs to Nevali."),
        new DialogueLine("VAN AERT", "Then they won't have another moment of peace. I'm bringing every piece of her home."),
        new DialogueLine("SPIRIT REMNANT", "I sense the first of her fragments near the heart of the arena beyond these ruins. Follow the golden trail. You will see what has taken hold of it."),
        new DialogueLine("VAN AERT", "The arena... I will find it. And whatever is feeding on her, I will stop it.")
    };

    // Intentionally not part of the Inspector-authored 16-line firstMeeting.
    // Existing scene serialization preserves all original dialogue and camera
    // indices. This new ending is played ONLY while the core is not yet owned.
    private static readonly DialogueLine[] CoreGiftLines =
    {
        new DialogueLine("SPIRIT REMNANT", "Van Aert... before you leave, there is something I must give you."),
        new DialogueLine("VAN AERT", "What is it?"),
        new DialogueLine("SPIRIT REMNANT", "A fragment of my essence. What remains of the light this kingdom could not extinguish."),
        new DialogueLine("VAN AERT", "You're giving me a part of yourself?"),
        new DialogueLine("SPIRIT REMNANT", "Through this core, your voice may reach me, even across the darkness."),
        new DialogueLine("VAN AERT", "Then when I find the castle... I'll call upon you."),
        new DialogueLine("SPIRIT REMNANT", "And I shall answer, if my strength allows."),
        new DialogueLine("SPIRIT REMNANT", "Go now, father. Follow the golden trail toward the arena; I sense the first fragment there. Beyond these broken walls... your daughter is waiting."),
        new DialogueLine("VAN AERT", "Hold on, Nevali. I'm coming.")
    };

    private Transform player;
    private float nextPlayerSearch;
    private float nextMobileOffer;
    private bool nearby;
    private bool talking;
    private bool firstConversation;
    private bool giftOnlyConversation;
    private bool metBefore;
    private DialogueLine[] currentLines;
    private int giftStartIndex = -1;
    private int revelationStartIndex = -1;
    private int lineIndex;
    private GameObject canvasObject;
    private GameObject panelObject;
    private TMP_Text speakerLabel;
    private TMP_Text dialogueLabel;
    private TMP_Text chapterLabel;
    private TMP_Text nextLabel;
    private Button advanceButton;
    private int revealedCharacters;
    private int totalCharacters;
    private float revealAccumulator;

    public bool IsTalking => talking;
    public bool HasCompletedFirstConversation => metBefore;

    private void Start()
    {
        interactionCenter ??= transform;
        ambientMotion ??= GetComponent<SpiritRemnantAmbientMotion>();
        cinematic ??= GetComponent<SpiritRemnantCinematic>();
        echoCorePresentation ??= GetComponent<EchoRuneCorePresentation>();
        goalsPanelUI ??= FindFirstObjectByType<GoalsPanelUI>();
        var db = GameDatabaseManager.Instance;
        metBefore = db != null && db.HasMetSpiritRemnant();
        BuildDialogueCanvas();
    }

    private void Update()
    {
        if (talking && revealedCharacters < totalCharacters)
        {
            revealAccumulator += Time.unscaledDeltaTime * dialogueRevealCharactersPerSecond;
            int newlyVisible = Mathf.FloorToInt(revealAccumulator);
            if (newlyVisible > 0)
            {
                revealAccumulator -= newlyVisible;
                revealedCharacters = Mathf.Min(totalCharacters, revealedCharacters + newlyVisible);
                dialogueLabel.maxVisibleCharacters = revealedCharacters;
                if (revealedCharacters >= totalCharacters)
                    UpdateNextLabel();
            }
        }

        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + 0.8f;
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        if (!talking)
        {
            // The spirit hovers above the ground; measure horizontal proximity
            // so a player standing beside it can talk without jumping.
            bool nowNearby = false;
            if (player != null)
            {
                Vector3 delta = player.position - interactionCenter.position;
                nowNearby = delta.x * delta.x + delta.z * delta.z <=
                            interactionRadius * interactionRadius
                            && Mathf.Abs(delta.y) <= 30f;
            }
            if (nowNearby != nearby)
            {
                nearby = nowNearby;
                if (!nearby) MobileStageControls.ClearInteraction(this);
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (talking) Advance();
            else if (nearby) BeginConversation();
        }
        if (talking && Input.GetKeyDown(KeyCode.Escape))
            FinishConversation(false);

        if (nearby && !talking && Time.unscaledTime >= nextMobileOffer)
        {
            nextMobileOffer = Time.unscaledTime + 0.4f;
            MobileStageControls.OfferInteraction(this, "TALK", BeginConversation);
        }
    }

    private void BeginConversation()
    {
        if (talking || !nearby) return;
        var db = GameDatabaseManager.EnsureInstance();
        metBefore = db != null && db.HasMetSpiritRemnant();
        bool hasCore = db != null && db.HasEchoRuneCore();
        firstConversation = !metBefore;
        giftOnlyConversation = metBefore && !hasCore;
        revelationStartIndex = firstConversation ? firstMeeting.Length : -1;
        giftStartIndex = firstConversation ? firstMeeting.Length + FragmentFeedingRevelation.Length :
            giftOnlyConversation ? 0 : -1;
        if (firstConversation)
        {
            currentLines = new DialogueLine[firstMeeting.Length + FragmentFeedingRevelation.Length +
                                            CoreGiftLines.Length];
            Array.Copy(firstMeeting, currentLines, firstMeeting.Length);
            Array.Copy(FragmentFeedingRevelation, 0, currentLines, revelationStartIndex,
                FragmentFeedingRevelation.Length);
            Array.Copy(CoreGiftLines, 0, currentLines, giftStartIndex, CoreGiftLines.Length);
        }
        else
            currentLines = giftOnlyConversation ? CoreGiftLines : repeatMeeting;

        if (giftStartIndex >= 0)
            echoCorePresentation?.BeginGift(player, ambientMotion);
        lineIndex = 0;
        talking = true;
        canvasObject.SetActive(true);
        panelObject.SetActive(true);
        cinematic?.Begin(player, canvasObject.GetComponent<Canvas>());
        ShowLine();
        // The dialogue panel owns its NEXT button on both mouse and touch.
        // Don't also render the mobile interaction button over the dialogue.
        MobileStageControls.ClearInteraction(this);
    }

    public void Advance()
    {
        if (!talking) return;
        if (revealedCharacters < totalCharacters)
        {
            revealedCharacters = totalCharacters;
            dialogueLabel.maxVisibleCharacters = revealedCharacters;
            UpdateNextLabel();
            return;
        }

        lineIndex++;
        if (lineIndex < currentLines.Length)
        {
            ShowLine();
            return;
        }

        FinishConversation(true);
    }

    private void FinishConversation(bool reachedEnd)
    {
        if (!talking)
            return;
        talking = false;
        ambientMotion?.SetSpeaking(false);
        cinematic?.End();
        if (panelObject != null)
            panelObject.SetActive(false);
        if (canvasObject != null)
            canvasObject.SetActive(false);

        bool giftAcquired = reachedEnd && giftStartIndex >= 0;
        echoCorePresentation?.FinishGift(giftAcquired);
        if (giftAcquired)
        {
            metBefore = true;
            GameDatabaseManager.EnsureInstance()?.MarkEchoRuneCoreAcquired();
            // Also refresh the route when an older save has met the Spirit
            // but only receives the Echo Rune Core on this visit.
            ObjectiveManager.ReportSpiritMet(gameObject.scene);
            Debug.Log("[SpiritRemnant] Echo Rune Core acquired. " +
                (PlayerProgression.IsCampaign ? "Campaign story progress saved." : "Temporary test-session story progress."));
        }

        MobileStageControls.ClearInteraction(this);
        nextMobileOffer = 0f;
    }

    private void ShowLine()
    {
        DialogueLine line = currentLines[lineIndex];
        speakerLabel.text = line.speaker;
        dialogueLabel.text = line.words;
        dialogueLabel.maxVisibleCharacters = 0;
        dialogueLabel.ForceMeshUpdate();
        totalCharacters = dialogueLabel.textInfo.characterCount;
        revealedCharacters = 0;
        revealAccumulator = 0f;
        bool spiritSpeaking = string.Equals(line.speaker, "SPIRIT REMNANT", StringComparison.Ordinal);
        ambientMotion?.SetSpeaking(spiritSpeaking);
        bool giftBeat = giftStartIndex >= 0 && lineIndex >= giftStartIndex;
        bool feedingBeat = revelationStartIndex >= 0 &&
                           lineIndex >= revelationStartIndex &&
                           lineIndex < giftStartIndex;
        if (giftBeat)
        {
            int giftIndex = lineIndex - giftStartIndex;
            echoCorePresentation?.ShowBeat(giftIndex);
            cinematic?.ShowGiftBeat(giftIndex, spiritSpeaking,
                echoCorePresentation != null ? echoCorePresentation.CoreTransform : null);
        }
        else if (feedingBeat)
        {
            // Preserve the original sixteen camera beats. The existing
            // Spirit/reaction angles can frame the new explanation safely.
            int beat = (lineIndex - revelationStartIndex) % 2 == 0 ? 12 : 13;
            cinematic?.ShowBeat(beat, true, spiritSpeaking);
        }
        else
            cinematic?.ShowBeat(lineIndex, firstConversation, spiritSpeaking);
        if (chapterLabel != null)
        {
            chapterLabel.text = giftBeat ? "THE GIFT OF THE ECHO RUNE CORE" :
                feedingBeat ? "THE SOUL THEY ARE STEALING" :
                !firstConversation ? "A VOICE IN THE RUINS" :
                lineIndex >= 14 ? "THE ROAD TO THE ROYAL CASTLE" :
                lineIndex >= 6 ? "THE SHATTERED EXISTENCE" :
                lineIndex >= 3 ? "THE MISSING CHILD" :
                "THE KINGDOM WITHOUT VOICES";
        }
        UpdateNextLabel();
    }

    private void UpdateNextLabel()
    {
        if (nextLabel == null) return;
        nextLabel.text = revealedCharacters < totalCharacters ? "REVEAL  >" :
            lineIndex == currentLines.Length - 1
                ? "FINISH  >" : "CONTINUE  >";
    }

    private void BuildDialogueCanvas()
    {
        canvasObject = new GameObject("SpiritRemnantDialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image tint = CreateFlatImage(canvasObject.transform, "CinematicVeil",
            new Vector2(0f, 0f), Vector2.one,
            new Color(0.008f, 0.014f, 0.032f, 0.13f));
        tint.raycastTarget = false;
        Image topBar = CreateFlatImage(canvasObject.transform, "TopCinematicLetterbox",
            new Vector2(0f, 0.87f), Vector2.one,
            new Color(0.008f, 0.014f, 0.028f, 0.98f));
        topBar.raycastTarget = false;
        Image lowerBar = CreateFlatImage(canvasObject.transform, "LowerCinematicLetterbox",
            Vector2.zero, new Vector2(1f, 0.315f),
            new Color(0.008f, 0.014f, 0.028f, 0.94f));
        lowerBar.raycastTarget = false;
        Image goldRule = CreateFlatImage(canvasObject.transform, "TitleGoldRule",
            new Vector2(0.05f, 0.865f), new Vector2(0.95f, 0.868f),
            new Color(0.78f, 0.56f, 0.27f, 0.86f));
        goldRule.raycastTarget = false;

        chapterLabel = AddText(canvasObject.GetComponent<RectTransform>(), "ChapterTitle",
            34f, new Color(0.95f, 0.84f, 0.6f),
            new Vector2(0.08f, 0.898f), new Vector2(0.92f, 0.96f),
            TextAlignmentOptions.Center);
        chapterLabel.text = "A VOICE IN THE RUINS";
        TMP_Text subtitle = AddText(canvasObject.GetComponent<RectTransform>(), "ChapterSubtitle",
            18f, new Color(0.70f, 0.76f, 0.84f, 0.92f),
            new Vector2(0.1f, 0.871f), new Vector2(0.9f, 0.898f),
            TextAlignmentOptions.Center);
        subtitle.text = "SPELLSTRIKE   /   ENCHANTED KINGDOM   /   CHAPTER I";

        Image panelTrim = CreateFlatImage(canvasObject.transform, "SpiritDialogueGoldBorder",
            new Vector2(0.109f, 0.034f), new Vector2(0.891f, 0.286f),
            new Color(0.65f, 0.46f, 0.22f, 0.88f));
        panelTrim.raycastTarget = false;
        panelObject = new GameObject("SpiritRemnantDialoguePanel", typeof(RectTransform), typeof(Image));
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.SetParent(canvasObject.transform, false);
        panel.anchorMin = new Vector2(0.11f, 0.035f);
        panel.anchorMax = new Vector2(0.89f, 0.285f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.016f, 0.030f, 0.064f, 0.97f);

        speakerLabel = AddText(panel, "Speaker", 29f, new Color(1f, 0.82f, 0.48f),
            new Vector2(0.039f, 0.72f), new Vector2(0.90f, 0.94f), TextAlignmentOptions.MidlineLeft);
        dialogueLabel = AddText(panel, "Dialogue", 30f, Color.white,
            new Vector2(0.039f, 0.22f), new Vector2(0.956f, 0.74f), TextAlignmentOptions.TopLeft);
        dialogueLabel.enableAutoSizing = true;
        dialogueLabel.fontSizeMin = 23f;
        dialogueLabel.fontSizeMax = 30f;

        TMP_Text footer = AddText(panel, "KeyboardHint", 17f,
            new Color(0.69f, 0.76f, 0.89f),
            new Vector2(0.04f, 0.035f), new Vector2(0.42f, 0.16f),
            TextAlignmentOptions.MidlineLeft);
        footer.text = "E  /  TAP NEXT TO CONTINUE    |    ESC TO LEAVE";

        GameObject nextObject = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform nextRect = nextObject.GetComponent<RectTransform>();
        nextRect.SetParent(panel, false);
        nextRect.anchorMin = new Vector2(0.82f, 0.04f);
        nextRect.anchorMax = new Vector2(0.975f, 0.22f);
        nextRect.offsetMin = Vector2.zero;
        nextRect.offsetMax = Vector2.zero;
        nextObject.GetComponent<Image>().color = new Color(0.48f, 0.34f, 0.17f, 0.92f);
        advanceButton = nextObject.GetComponent<Button>();
        advanceButton.onClick.AddListener(Advance);
        nextLabel = AddText(nextRect, "NextLabel", 23f, Color.white,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        nextLabel.text = "CONTINUE  >";

        panelObject.SetActive(false);
        canvasObject.SetActive(false);
    }

    private static Image CreateFlatImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text AddText(RectTransform parent, string name, float fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private void OnDisable()
    {
        talking = false;
        MobileStageControls.ClearInteraction(this);
        ambientMotion?.SetSpeaking(false);
        cinematic?.End();
        echoCorePresentation?.FinishGift(false);
        if (panelObject != null) panelObject.SetActive(false);
        if (canvasObject != null) canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvasObject != null) Destroy(canvasObject);
    }
}
