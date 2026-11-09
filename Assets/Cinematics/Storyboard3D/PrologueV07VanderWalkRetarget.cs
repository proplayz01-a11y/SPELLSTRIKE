using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// V07 cinematic-only transfer of genuine Mixamo walking joint rotations into
/// the existing canonical Vander story mesh. A versioned Timeline plays the
/// real walking clip on a hidden compatible rig; this component mirrors those
/// joint rotations onto the visible story skeleton each frame.
/// Does not touch gameplay movement, stage actors, root position, or animation
/// source assets. The existing corridor root-motion Timeline stays authoritative.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrologueV07VanderWalkRetarget : MonoBehaviour
{
    [SerializeField] private Animator sourceWalkingRig;
    [SerializeField] private SkinnedMeshRenderer targetStorySkinnedMesh;
    [SerializeField] private PlayableDirector shotDirector;
    [SerializeField] private float walkStartSeconds;
    [SerializeField] private float walkEndSeconds = 8f;
    [SerializeField, Range(0.05f,1f)] private float blendSeconds = .4f;
    [SerializeField] private bool enabledForThisShot = true;
    [SerializeField] private int mappedBoneCount;
    [SerializeField] private int mappedLegBoneCount;
    private Transform[] sourceBones = Array.Empty<Transform>();
    private Transform[] targetBones = Array.Empty<Transform>();
    private Quaternion[] originalLegRotations = Array.Empty<Quaternion>();

    public int MappedBoneCount => mappedBoneCount;
    public int MappedLegBoneCount => mappedLegBoneCount;

    public void Configure(Animator source, SkinnedMeshRenderer target,
        PlayableDirector director, float walkingStart, float walkingEnd)
    {
        sourceWalkingRig = source;
        targetStorySkinnedMesh = target;
        shotDirector = director;
        walkStartSeconds = walkingStart;
        walkEndSeconds = walkingEnd;
        RebuildMap();
    }

    private static string BoneName(string original)
    {
        if (string.IsNullOrEmpty(original)) return original;
        int colon = original.LastIndexOf(':');
        return colon >= 0 ? original.Substring(colon + 1) : original;
    }

    private void Awake() => RebuildMap();

    private void OnEnable() => RebuildMap();

    private void RebuildMap()
    {
        mappedBoneCount = 0;
        mappedLegBoneCount = 0;
        sourceBones = Array.Empty<Transform>();
        targetBones = Array.Empty<Transform>();
        originalLegRotations = Array.Empty<Quaternion>();
        if (sourceWalkingRig == null || targetStorySkinnedMesh == null) return;

        var sources = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (Transform source in sourceWalkingRig.GetComponentsInChildren<Transform>(true))
        {
            string name = BoneName(source.name);
            if (name != null && !sources.ContainsKey(name))
                sources.Add(name, source);
        }

        var sourceList = new List<Transform>();
        var targetList = new List<Transform>();
        var legRotations = new List<Quaternion>();
        foreach (Transform target in targetStorySkinnedMesh.bones)
        {
            if (target == null) continue;
            string name = BoneName(target.name);
            if (!sources.TryGetValue(name, out Transform source))
                continue;
            mappedBoneCount++;
            // Important: preserve the existing Vander dialogue/head-bob and
            // staff-grip animation in P34/P35. Only animate leg joints.
            bool leg = name == "LeftUpLeg" || name == "LeftLeg" ||
                name == "LeftFoot" || name == "LeftToeBase" ||
                name == "RightUpLeg" || name == "RightLeg" ||
                name == "RightFoot" || name == "RightToeBase";
            if (!leg) continue;
            sourceList.Add(source);
            targetList.Add(target);
            legRotations.Add(target.localRotation);
        }
        sourceBones = sourceList.ToArray();
        targetBones = targetList.ToArray();
        originalLegRotations = legRotations.ToArray();
        mappedLegBoneCount = sourceBones.Length;
        if (mappedBoneCount < 35)
            Debug.LogWarning("[PrologueV07] Vander walking transfer matches only " +
                mappedBoneCount + " joints on " + gameObject.name);
        if (mappedLegBoneCount != 8)
            Debug.LogWarning("[PrologueV07] Missing expected eight gait joints: " +
                mappedLegBoneCount + " on " + gameObject.name);
    }

    private void LateUpdate()
    {
        if (!enabledForThisShot || sourceWalkingRig == null ||
            targetStorySkinnedMesh == null || shotDirector == null ||
            !sourceWalkingRig.isActiveAndEnabled)
            return;
        if (mappedBoneCount < 35 || mappedLegBoneCount != 8) return;

        double time = shotDirector.time;
        float weight = 0f;
        if (time >= walkStartSeconds && time <= walkEndSeconds)
        {
            float intro = Mathf.Clamp01(((float)time-walkStartSeconds)/blendSeconds);
            float outro = Mathf.Clamp01((walkEndSeconds-(float)time)/blendSeconds);
            float blend = Mathf.Min(intro,outro);
            weight = blend * blend * (3f - 2f * blend);
        }

        // Lower-limb gait only. NO Hips/root translation, shoulders, hands,
        // fingers, neck, head or spine: retain approved head-bob and staff.
        // Blend into and out of the authored original shot leg pose.
        for (int i = 0; i < sourceBones.Length; i++)
        {
            if (sourceBones[i] != null && targetBones[i] != null)
                targetBones[i].localRotation = Quaternion.Slerp(
                    originalLegRotations[i],sourceBones[i].localRotation,weight);
        }
    }
}
