using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Pre-combat story presentation: an enemy silently draws energy from Nevali's
/// fragment until its real battle controller starts combat. It NEVER changes
/// a FragmentPickup/EnemyHealth, objectives, AI state, NavMeshAgent, or combat
/// authority. The ritual is visual-only and is not an actual health drain.
///
/// The supplied Y Bot@Magic Heal clip is played through an isolated upper-body
/// Animator layer on humanoid enemies. The procedural reaching pose remains
/// as a fallback when the layer or its retargeted clip is unavailable.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulFragmentFeeding : MonoBehaviour
{
    [Header("Authoring")]
    [SerializeField] private Transform fragment;
    [SerializeField] private Animator characterAnimator;
    // All fragment-feeding enemies receive stolen energy in their RIGHT hand.
    // This is part of the scene's story/animation standard, not a per-enemy
    // toggle. Hollow Knight remains excluded (his fragment is embedded).
    [SerializeField] private bool faceFragmentBeforeBattle = true;
    [SerializeField] private bool poseHumanoidWhileFeeding = true;
    [SerializeField, Range(0f, 1f)] private float poseStrength = 0.85f;
    [SerializeField, Range(0.5f, 5f)] private float ritualEnergyWidth = 0.8f;
    [SerializeField, Range(0.1f, 2.5f)] private float soulMoteFlowCyclesPerSecond = 0.43f;

    [Header("Imported Magic Heal (Humanoid only)")]
    [SerializeField] private bool useImportedMagicHeal;
    [SerializeField] private string magicHealLayerName = "Soul Feeding - Magic Heal";
    [SerializeField, Range(0.1f, 2f)] private float magicHealFadeInSeconds = 0.55f;

    [Header("Optional stationary feeding base pose")]
    [Tooltip("Only assign when this enemy's original Animator starts on Walk. Its new frozen pose is used before combat.")]
    [SerializeField] private string feedingBaseIdleStateName = "";
    [Tooltip("The original locomotion state restored when combat starts.")]
    [SerializeField] private string resumeBaseStateName = "";

    private const int CurveSegments = 25;
    private Transform upperArm;
    private Transform lowerArm;
    private Transform hand;
    private Transform faceAnchor;
    private readonly List<BattleStateSource> battleSources = new List<BattleStateSource>();
    private struct BattleStateSource
    {
        public MonoBehaviour component;
        public FieldInfo field;
        public PropertyInfo property;
    }
    private EnemyHealth health;
    private Transform fragmentVisual;
    private LineRenderer energyCore;
    private LineRenderer energyHalo;
    private GameObject vfxRoot;
    private Material runtimeMaterial;
    private Material runtimeHaloMaterial;
    private Material runtimeMoteMaterial;
    private readonly Transform[] energyMotes = new Transform[4];
    private float ritualStartedAt;
    private int magicHealLayer = -1;
    private int feedingIdleHash;
    private int resumeBaseHash;
    private float nextIdleCheck;
    private bool interrupted;
    private bool wasActive;
    private Color sourceColor = new Color(0.35f, 0.77f, 1f);

    public bool IsFeeding => !interrupted && isActiveAndEnabled &&
                             fragment != null && IsFragmentVisible();
    public Transform Fragment => fragment;
    public Transform FeedingHand => hand;

    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>(true);
        feedingIdleHash = !string.IsNullOrEmpty(feedingBaseIdleStateName)
            ? Animator.StringToHash("Base Layer." + feedingBaseIdleStateName) : 0;
        resumeBaseHash = !string.IsNullOrEmpty(resumeBaseStateName)
            ? Animator.StringToHash("Base Layer." + resumeBaseStateName) : 0;
        if (useImportedMagicHeal && characterAnimator != null)
        {
            magicHealLayer = characterAnimator.GetLayerIndex(magicHealLayerName);
            if (magicHealLayer < 0)
                Debug.LogWarning("[SoulFeeding] Imported Magic Heal layer missing; using procedural fallback on " +
                                 name, this);
        }
        health = GetComponent<EnemyHealth>();
        FindBattleAuthority();
        if (characterAnimator != null && characterAnimator.isHuman &&
            characterAnimator.avatar != null && characterAnimator.avatar.isValid)
        {
            upperArm = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            lowerArm = characterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hand = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            faceAnchor = characterAnimator.GetBoneTransform(HumanBodyBones.Chest);
        }
        // Some stage enemies (Stone Golem / Titan) are not imported as
        // valid Unity Humanoids but DO have a right-hand skeleton transform.
        // Their energy must still end at the right hand; do not invent a
        // humanoid arm animation or change their statue/combat state.
        if (hand == null)
            hand = FindRightHandTransform();
        if (fragment != null)
        {
            fragmentVisual = fragment.Find("FragmentCrystal_Visual");
            var renderer = fragmentVisual != null
                ? fragmentVisual.GetComponentInChildren<MeshRenderer>(true) : null;
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var mat = renderer.sharedMaterial;
                string name = mat.name;
                var match = System.Text.RegularExpressions.Regex.Match(
                    name, @"Crystal_([0-9A-Fa-f]{6})_");
                if (match.Success)
                    ColorUtility.TryParseHtmlString("#" + match.Groups[1].Value, out sourceColor);
            }
        }
    }

    private Transform FindRightHandTransform()
    {
        Transform[] bones = characterAnimator != null
            ? characterAnimator.GetComponentsInChildren<Transform>(true)
            : GetComponentsInChildren<Transform>(true);
        foreach (Transform bone in bones)
        {
            if (bone.name.Equals("RightHand", StringComparison.OrdinalIgnoreCase) ||
                bone.name.EndsWith(":RightHand", StringComparison.OrdinalIgnoreCase) ||
                bone.name.Equals("Hand_R", StringComparison.OrdinalIgnoreCase) ||
                bone.name.Equals("R_Hand", StringComparison.OrdinalIgnoreCase) ||
                bone.name.Equals("RightPalm", StringComparison.OrdinalIgnoreCase))
                return bone;
        }
        Debug.LogWarning("[SoulFeeding] No right-hand bone on " + name +
                         "; using a right-side receiving point.", this);
        return null;
    }

    private void OnEnable()
    {
        ritualStartedAt = Time.time;
        interrupted = false;
        wasActive = false;
    }

    private void Start()
    {
        // Optional story work must never hold combat hostage on a missing ref.
        if (fragment == null)
        {
            Debug.LogWarning("[SoulFeeding] No fragment reference; visual skipped: " + name, this);
            enabled = false;
            return;
        }
        if (battleSources.Count == 0)
            Debug.LogWarning("[SoulFeeding] No matching combat state; call InterruptForCombat from battle activation.", this);
    }

    private void FindBattleAuthority()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic |
                                   BindingFlags.Public;
        // Some actors have more than one component with a battleStarted
        // member. Resolve every relevant state ONCE and treat combat as
        // started if any authority has begun; selecting the first flag
        // alone can leave the feeding stream active during actual combat.
        battleSources.Clear();
        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
        {
            if (component == null || component == this)
                continue;
            Type type = component.GetType();
            FieldInfo field = type.GetField("battleStarted", flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                battleSources.Add(new BattleStateSource
                {
                    component = component,
                    field = field
                });
                continue;
            }
            PropertyInfo property = type.GetProperty("BattleStarted", flags) ??
                                    type.GetProperty("IsBattleStarted", flags);
            if (property != null && property.PropertyType == typeof(bool) &&
                property.GetIndexParameters().Length == 0)
            {
                battleSources.Add(new BattleStateSource
                {
                    component = component,
                    property = property
                });
            }
        }
    }

    private bool HasEnteredCombat()
    {
        if (battleSources.Count == 0)
            return false;
        foreach (BattleStateSource source in battleSources)
        {
            if (source.component == null)
                continue;
            try
            {
                if (source.field != null && (bool)source.field.GetValue(source.component))
                    return true;
                if (source.property != null && (bool)source.property.GetValue(source.component))
                    return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SoulFeeding] Combat-state read failed on " +
                                 source.component.GetType().Name + ": " +
                                 exception.Message, this);
                InterruptForCombat();
                return true;
            }
        }
        return false;
    }

    private bool IsFragmentVisible()
    {
        // Stage 3's Titan fragment only appears when the authored riddle
        // progression reveals it; never draw a beam to an invisible prize.
        return fragment != null && fragment.gameObject.activeInHierarchy;
    }

    private void Update()
    {
        if (interrupted)
            return;
        if ((health != null && health.IsDead) || HasEnteredCombat())
        {
            InterruptForCombat();
            return;
        }
        bool activeNow = IsFragmentVisible();
        if (!activeNow)
        {
            SetImportedMagicHealWeight(0f);
            if (wasActive && vfxRoot != null)
                vfxRoot.SetActive(false);
            wasActive = false;
            return;
        }
        if (vfxRoot == null)
            CreateVfx();
        if (vfxRoot != null && !vfxRoot.activeSelf)
            vfxRoot.SetActive(true);
        wasActive = true;
        if (magicHealLayer >= 0 && characterAnimator != null)
        {
            float nextWeight = Mathf.MoveTowards(
                characterAnimator.GetLayerWeight(magicHealLayer),
                1f, Time.deltaTime / Mathf.Max(0.1f, magicHealFadeInSeconds));
            SetImportedMagicHealWeight(nextWeight);
        }

        // A feeding enemy must not appear to walk in place while its ritual
        // is active. Only actors with a WALKING default base state use the
        // authored frozen-pose override. Their combat states remain intact.
        if (feedingIdleHash != 0 && characterAnimator != null &&
            Time.unscaledTime >= nextIdleCheck)
        {
            nextIdleCheck = Time.unscaledTime + 0.2f;
            if (characterAnimator.HasState(0, feedingIdleHash) &&
                !characterAnimator.GetCurrentAnimatorStateInfo(0)
                    .fullPathHash.Equals(feedingIdleHash) &&
                !characterAnimator.IsInTransition(0))
                characterAnimator.CrossFadeInFixedTime(
                    feedingIdleHash, 0.12f, 0, 0f);
        }

        if (faceFragmentBeforeBattle && poseHumanoidWhileFeeding &&
            characterAnimator != null && characterAnimator.isHuman)
        {
            Vector3 toFragment = fragment.position - transform.position;
            toFragment.y = 0f;
            if (toFragment.sqrMagnitude > 0.005f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(toFragment.normalized, Vector3.up),
                    105f * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (!IsFeeding)
            return;
        if (poseHumanoidWhileFeeding && magicHealLayer < 0 &&
            upperArm != null && lowerArm != null && hand != null)
            AnimateReachingArm();
        AnimateEnergy();
    }

    private void SetImportedMagicHealWeight(float weight)
    {
        if (characterAnimator != null && magicHealLayer >= 0 &&
            magicHealLayer < characterAnimator.layerCount)
            characterAnimator.SetLayerWeight(magicHealLayer, weight);
    }

    private void AnimateReachingArm()
    {
        // Applied after the Animator each frame, so existing locomotion and
        // combat animation clips are never edited or overwritten in assets.
        // Only the noncombat upper arm/forearm bones are affected.
        float elapsed = Time.time - ritualStartedAt;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.85f));
        float weight = poseStrength * blend;
        Vector3 shoulder = upperArm.position;
        Vector3 direction = FragmentPoint() - shoulder;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0005f)
            direction = transform.forward;
        direction.Normalize();

        Vector3 elbow = lowerArm.position;
        float upperLength = Vector3.Distance(shoulder, elbow);
        float lowerLength = Vector3.Distance(elbow, hand.position);
        if (upperLength < 0.03f || lowerLength < 0.03f)
            return;

        float breathe = Mathf.Sin(Time.time * 2.4f) * upperLength * 0.07f;
        Vector3 desiredElbow = shoulder +
            direction * upperLength * 0.90f +
            Vector3.up * (upperLength * 0.22f + breathe);
        Vector3 currentUpper = elbow - shoulder;
        Vector3 desiredUpper = desiredElbow - shoulder;
        upperArm.rotation = Quaternion.Slerp(upperArm.rotation,
            Quaternion.FromToRotation(currentUpper, desiredUpper) * upperArm.rotation, weight);

        Vector3 targetHand = shoulder +
            direction * (upperLength + lowerLength) * 0.88f +
            Vector3.up * (upperLength * 0.26f + breathe);
        Vector3 currentForearm = hand.position - lowerArm.position;
        Vector3 desiredForearm = targetHand - lowerArm.position;
        if (currentForearm.sqrMagnitude > 0.001f && desiredForearm.sqrMagnitude > 0.001f)
            lowerArm.rotation = Quaternion.Slerp(lowerArm.rotation,
                Quaternion.FromToRotation(currentForearm, desiredForearm) *
                lowerArm.rotation, weight);
    }

    private Vector3 FragmentPoint()
    {
        return fragmentVisual != null ? fragmentVisual.position : fragment.position;
    }

    private Vector3 EnemyReceivingPoint()
    {
        if (hand != null)
            return hand.position;
        if (faceAnchor != null)
            return faceAnchor.position + transform.right * 0.6f;
        return transform.position + transform.right *
            Mathf.Max(0.6f, transform.lossyScale.x * 0.3f) +
            Vector3.up * Mathf.Max(1.5f, transform.lossyScale.y * 0.1f);
    }

    private void AnimateEnergy()
    {
        if (energyCore == null || energyHalo == null)
            return;
        Vector3 a = FragmentPoint();
        Vector3 b = EnemyReceivingPoint();
        float distance = Vector3.Distance(a, b);
        Vector3 direction = (b - a).normalized;
        Vector3 side = Vector3.Cross(direction, Vector3.up);
        if (side.sqrMagnitude < 0.02f)
            side = Vector3.right;
        side.Normalize();
        Vector3 lift = Vector3.up * Mathf.Clamp(distance * 0.11f, 0.5f, 3.7f);
        float now = Time.time;
        for (int i = 0; i < CurveSegments; ++i)
        {
            float t = i / (float)(CurveSegments - 1);
            Vector3 p = EnergyPoint(t, a, b, lift, side, now);
            energyCore.SetPosition(i, p);
            energyHalo.SetPosition(i, p);
        }
        // Show the actual direction of the stolen magic: these motes travel
        // from Nevali's fragment (t=0) toward the enemy's reaching hand (t=1).
        for (int i = 0; i < energyMotes.Length; ++i)
        {
            Transform mote = energyMotes[i];
            if (mote == null) continue;
            float t = Mathf.Repeat(Time.time * soulMoteFlowCyclesPerSecond +
                                   i / (float)energyMotes.Length, 1f);
            mote.position = EnergyPoint(t, a, b, lift, side, now);
            float motePulse = Mathf.Sin(Mathf.PI * t);
            float size = Mathf.Lerp(0.24f, 0.54f, motePulse);
            mote.localScale = Vector3.one * size;
        }
        float pulse = 0.85f + 0.15f * Mathf.Sin(now * 4.8f);
        float width = ritualEnergyWidth * pulse;
        energyCore.startWidth = width * 0.15f;
        energyCore.endWidth = width * 0.23f;
        energyHalo.startWidth = width * 0.60f;
        energyHalo.endWidth = width * 0.75f;
    }

    private Vector3 EnergyPoint(float t, Vector3 a, Vector3 b,
        Vector3 lift, Vector3 side, float now)
    {
        float envelope = Mathf.Sin(Mathf.PI * t);
        return Vector3.Lerp(a, b, t)
            + lift * envelope
            + side * (Mathf.Sin(t * 15f - now * 5f) *
                      envelope * ritualEnergyWidth * 0.38f);
    }

    private void CreateVfx()
    {
        if (vfxRoot != null || fragment == null)
            return;
        vfxRoot = new GameObject("NevaliSoulFragment_FeedingVFX_RUNTIME_" + name);
        runtimeMaterial = MakeEnergyMaterial(false);
        runtimeHaloMaterial = MakeEnergyMaterial(true);
        runtimeMoteMaterial = MakeEnergyMaterial(false);
        if (runtimeMoteMaterial.HasProperty("_BaseColor"))
            runtimeMoteMaterial.SetColor("_BaseColor", new Color(
                Mathf.Min(3f, sourceColor.r * 2.2f),
                Mathf.Min(3f, sourceColor.g * 2.2f),
                Mathf.Min(3f, sourceColor.b * 2.2f), 1f));
        energyHalo = CreateLine("SoulEnergy_OuterAura", runtimeHaloMaterial,
            new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.19f));
        energyCore = CreateLine("SoulEnergy_InnerStream", runtimeMaterial,
            new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.96f));
        for (int i = 0; i < energyMotes.Length; ++i)
        {
            GameObject mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mote.name = "SoulEnergy_DrawnFromNevali_" + (i + 1);
            mote.transform.SetParent(vfxRoot.transform, false);
            Collider collision = mote.GetComponent<Collider>();
            if (collision != null)
            {
                collision.enabled = false;
                Destroy(collision);
            }
            MeshRenderer renderer = mote.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = runtimeMoteMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            energyMotes[i] = mote.transform;
        }
    }

    private Material MakeEnergyMaterial(bool halo)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        material.name = halo ? "SoulFeeding_Aura_RUNTIME" : "SoulFeeding_Light_RUNTIME";
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        return material;
    }

    private LineRenderer CreateLine(string title, Material material, Color tint)
    {
        var go = new GameObject(title, typeof(LineRenderer));
        go.transform.SetParent(vfxRoot.transform, false);
        var line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.positionCount = CurveSegments;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 3;
        line.numCapVertices = 4;
        line.startColor = tint;
        line.endColor = tint;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    /// <summary>
    /// Optional explicit call by a battle controller; auto-detection also
    /// stops the ritual on the first frame where battleStarted becomes true.
    /// </summary>
    public void InterruptForCombat()
    {
        if (interrupted)
            return;
        interrupted = true;
        // Let the original combat Animator Controller take full control on
        // the same frame that StartBattle() switches the enemy to combat.
        SetImportedMagicHealWeight(0f);
        if (characterAnimator != null && feedingIdleHash != 0 &&
            resumeBaseHash != 0 && characterAnimator.HasState(0, resumeBaseHash) &&
            characterAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == feedingIdleHash)
            characterAnimator.CrossFadeInFixedTime(resumeBaseHash, 0.12f, 0);
        DestroyRuntimeVfx();
    }

    private void DestroyRuntimeVfx()
    {
        if (vfxRoot != null)
            Destroy(vfxRoot);
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (runtimeHaloMaterial != null)
            Destroy(runtimeHaloMaterial);
        if (runtimeMoteMaterial != null)
            Destroy(runtimeMoteMaterial);
        vfxRoot = null;
        runtimeMaterial = runtimeHaloMaterial = runtimeMoteMaterial = null;
        energyCore = energyHalo = null;
        for (int i = 0; i < energyMotes.Length; ++i)
            energyMotes[i] = null;
    }

    private void OnDisable()
    {
        SetImportedMagicHealWeight(0f);
        DestroyRuntimeVfx();
    }

    private void OnDestroy()
    {
        DestroyRuntimeVfx();
    }
}
