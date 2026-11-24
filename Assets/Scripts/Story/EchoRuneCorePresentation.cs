using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A story-only 3D prop. Its Blender-authored model manifests from the
/// Spirit's essence, travels toward Van, and dissolves into his keeping.
/// The canonical story flag is awarded by SpiritRemnantEncounter, never here.
/// </summary>
[DisallowMultipleComponent]
public sealed class EchoRuneCorePresentation : MonoBehaviour
{
    [SerializeField] private GameObject coreModelPrefab;
    [SerializeField, Min(0.1f)] private float displayScale = 2.2f;
    [SerializeField, Min(0.1f)] private float travelSeconds = 2.15f;
    [SerializeField] private Color glowColor = new Color(0.08f, 0.72f, 1f);

    [Header("Spirit gift reveal — clear of the Spirit's face")]
    [SerializeField, Min(0.1f)] private float spiritForwardDistance = 7.4f;
    [SerializeField, Min(0f)] private float spiritDownDistance = 2.1f;
    [SerializeField] private float spiritLateralDistance = 1.7f;

    [Header("Van's hand — world-scale safe for the Stage 1 enlarged character")]
    [SerializeField] private HumanBodyBones receivingHandBone = HumanBodyBones.LeftHand;
    // Palm center, midway toward the Mixamo index-knuckle on Van's rig.
    [SerializeField] private Vector3 handLocalOffset = new Vector3(0f, 0.075f, 0.005f);
    [SerializeField] private Vector3 handLocalEuler = new Vector3(20f, 25f, -10f);
    [SerializeField, Min(0.05f)] private float heldWorldScale = 0.85f;
    [SerializeField, Min(0.05f)] private float grabBlendSeconds = 0.32f;

    private enum Phase { Hidden, Reveal, Travel, Held, Absorb }
    private Phase phase;
    private Transform actor;
    private Transform spiritHead;
    private Transform actorHead;
    private Transform actorHand;
    private GameObject coreObject;
    private Light coreLight;
    private float phaseStart;
    private Vector3 transferStart;
    private Vector3 grabWorldStart;
    private Quaternion grabRotationStart;
    private bool attachedToHand;
    private GameObject notification;
    private float notificationStarted;

    public Transform CoreTransform => coreObject != null ? coreObject.transform : null;
    public Transform ReceivingHand => actorHand;

    public void BeginGift(Transform player, SpiritRemnantAmbientMotion spirit)
    {
        ClearCore();
        actor = player;
        spiritHead = spirit != null ? spirit.HeadBone : null;
        actorHead = null;
        actorHand = null;
        attachedToHand = false;
        if (player != null)
        {
            Animator animator = player.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid)
            {
                actorHead = animator.GetBoneTransform(HumanBodyBones.Head);
                actorHand = animator.GetBoneTransform(receivingHandBone);
            }
        }
        if (actorHand == null)
            Debug.LogWarning("[EchoRuneCore] No humanoid receiving hand found. Using the existing Van receiving point fallback.", this);
        phase = Phase.Hidden;
    }

    public void ShowBeat(int giftLine)
    {
        if (giftLine < 2)
            return;
        if (phase == Phase.Hidden)
            Reveal();
        if (giftLine >= 4 && phase == Phase.Reveal && coreObject != null)
        {
            transferStart = coreObject.transform.position;
            phase = Phase.Travel;
            phaseStart = Time.unscaledTime;
        }
        if (giftLine >= 6 && phase == Phase.Travel)
        {
            phase = Phase.Held;
            phaseStart = Time.unscaledTime;
            AttachToVanHand();
        }
        if (giftLine >= 8 && phase != Phase.Absorb)
        {
            phase = Phase.Absorb;
            phaseStart = Time.unscaledTime;
        }
    }

    private Vector3 SpiritOrigin()
    {
        // Put the gift well clear of the Spirit's head and front silhouette.
        // The Blender model has bright rune rings extending beyond its
        // central shell, so a 3.5-unit offset still obscured his mouth/eyes.
        // Derive the stage direction from Van, never the GLB bone's local axes.
        if (spiritHead != null)
        {
            Vector3 direction = actor != null ? actor.position - spiritHead.position :
                transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.05f)
                direction = transform.forward;
            direction.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            return spiritHead.position - Vector3.up * spiritDownDistance
                + direction * spiritForwardDistance
                + right * spiritLateralDistance;
        }
        return transform.position + Vector3.up * 9f;
    }

    private Vector3 VanReceivingPoint()
    {
        if (actorHand != null)
            return actorHand.TransformPoint(handLocalOffset);
        if (actorHead != null)
            return actorHead.position - Vector3.up * 2.15f + actor.forward * 1.65f;
        return actor != null ? actor.position + Vector3.up * 7f + actor.forward * 1.5f
                             : SpiritOrigin() + transform.forward * 6f;
    }

    /// <summary>
    /// Really parent the gift to Van's *animated* hand, not to the player root.
    /// SetParent(worldPositionStays) prevents a teleport at the story beat;
    /// LateUpdate then blends the orb to the palm while the bone keeps moving.
    /// </summary>
    private void AttachToVanHand()
    {
        if (coreObject == null || actorHand == null || attachedToHand)
            return;
        grabWorldStart = coreObject.transform.position;
        grabRotationStart = coreObject.transform.rotation;
        coreObject.transform.SetParent(actorHand, true);
        attachedToHand = true;
    }

    // Van is uniformly scaled 11x in Stage 1. A naive localScale=1.35
    // would inflate the prop to 14.85 world units. Preserve world scale.
    private void SetCoreWorldScale(float worldScale)
    {
        if (coreObject == null)
            return;
        Transform parent = coreObject.transform.parent;
        if (parent == null)
        {
            coreObject.transform.localScale = Vector3.one * Mathf.Max(0.001f, worldScale);
            return;
        }
        Vector3 inherited = parent.lossyScale;
        coreObject.transform.localScale = new Vector3(
            worldScale / Mathf.Max(0.001f, Mathf.Abs(inherited.x)),
            worldScale / Mathf.Max(0.001f, Mathf.Abs(inherited.y)),
            worldScale / Mathf.Max(0.001f, Mathf.Abs(inherited.z)));
    }

    private void Reveal()
    {
        if (coreModelPrefab == null)
        {
            Debug.LogWarning("[EchoRuneCore] The Blender model has not been assigned; the story dialogue will continue without the visual.", this);
            return;
        }

        coreObject = Instantiate(coreModelPrefab, SpiritOrigin(), Quaternion.identity);
        coreObject.name = "EchoRuneCore_GiftCinematic";
        coreObject.transform.localScale = Vector3.zero;
        GameObject lightHolder = new GameObject("EssenceLight");
        lightHolder.transform.SetParent(coreObject.transform, false);
        coreLight = lightHolder.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = glowColor;
        coreLight.range = 17f;
        coreLight.intensity = 5f;
        coreLight.shadows = LightShadows.None;
        phase = Phase.Reveal;
        phaseStart = Time.unscaledTime;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (notification != null && now - notificationStarted > 3.5f)
        {
            Destroy(notification);
            notification = null;
        }
    }

    // Animator writes the hands before LateUpdate; update prop afterward so
    // the rune does not trail a frame behind Van's talking/idle animation.
    private void LateUpdate()
    {
        if (coreObject == null)
            return;

        float now = Time.unscaledTime;
        float elapsed = now - phaseStart;
        if (attachedToHand && actorHand != null)
        {
            if (phase == Phase.Held)
            {
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / grabBlendSeconds));
                Vector3 goalPosition = actorHand.TransformPoint(handLocalOffset);
                Quaternion goalRotation = actorHand.rotation * Quaternion.Euler(handLocalEuler);
                // Transform remains a child throughout the blend: no
                // dependency on arbitrary FBX bone orientation in world space.
                coreObject.transform.position = Vector3.Lerp(grabWorldStart, goalPosition, blend);
                coreObject.transform.rotation = Quaternion.Slerp(grabRotationStart, goalRotation, blend);
                SetCoreWorldScale(Mathf.Lerp(displayScale, heldWorldScale, blend));
            }
            else if (phase == Phase.Absorb)
            {
                float dissolve = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.0f));
                coreObject.transform.localPosition = handLocalOffset * (1f - dissolve * 0.3f);
                coreObject.transform.localRotation = Quaternion.Euler(handLocalEuler);
                SetCoreWorldScale(Mathf.Max(0.001f, heldWorldScale * (1f - dissolve)));
            }
        }
        else
        {
            Vector3 basePosition;
            float scale = displayScale;
            switch (phase)
            {
                case Phase.Reveal:
                    basePosition = SpiritOrigin();
                    scale *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.1f));
                    break;
                case Phase.Travel:
                    float progress = Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / travelSeconds));
                    basePosition = Vector3.Lerp(transferStart, VanReceivingPoint(), progress);
                    break;
                case Phase.Held:
                    basePosition = VanReceivingPoint();
                    scale = heldWorldScale;
                    break;
                case Phase.Absorb:
                    basePosition = VanReceivingPoint();
                    scale = heldWorldScale *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.0f)));
                    break;
                default:
                    basePosition = SpiritOrigin();
                    break;
            }
            coreObject.transform.position = basePosition
                + Vector3.up * (0.11f * Mathf.Sin(now * 3.5f));
            coreObject.transform.rotation = Quaternion.Euler(13f, now * 18f, 21f);
            SetCoreWorldScale(scale);
        }
        if (coreLight != null)
            coreLight.intensity = (3.7f + 1.3f * Mathf.Sin(now * 5f))
                * (phase == Phase.Absorb
                    ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.0f))
                    : 1f);
    }

    /// <summary>Cancellation destroys the temporary prop without granting progress.</summary>
    public void FinishGift(bool acquired)
    {
        ClearCore();
        phase = Phase.Hidden;
        if (acquired)
            ShowAcquiredNotice();
    }

    private void ClearCore()
    {
        if (coreObject != null)
            Destroy(coreObject);
        coreObject = null;
        coreLight = null;
        attachedToHand = false;
        actorHand = null;
    }

    private void ShowAcquiredNotice()
    {
        if (notification != null)
            Destroy(notification);
        notification = new GameObject("EchoRuneCoreAcquiredUI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = notification.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 710;
        CanvasScaler scaler = notification.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameObject title = new GameObject("CoreAcquiredTitle",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = title.GetComponent<RectTransform>();
        rect.SetParent(notification.transform, false);
        rect.anchorMin = new Vector2(0.18f, 0.75f);
        rect.anchorMax = new Vector2(0.82f, 0.86f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = title.GetComponent<TextMeshProUGUI>();
        label.text = "ECHO RUNE CORE ACQUIRED\nA fragment of the Spirit's essence";
        label.fontSize = 34f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 19f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.70f, 0.93f, 1f);
        label.raycastTarget = false;
        notificationStarted = Time.unscaledTime;
    }

    private void OnDisable()
    {
        ClearCore();
        if (notification != null)
            Destroy(notification);
        notification = null;
    }
}
