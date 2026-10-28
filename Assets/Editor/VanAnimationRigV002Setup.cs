using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds an independent candidate prefab or adds exactly the same rig to
/// the live Stage3 Van IN PLAY MODE ONLY, so real terrain QA never dirties
/// the user's production scene/prefab.
/// </summary>
public static class VanAnimationRigV002Setup
{
    private const string Source = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string CandidateFolder = "Assets/VAN AERT/ProceduralRigging_V002";
    private const string Candidate = CandidateFolder + "/VanAert_AnimationRigFootPlacement_V002_Candidate.prefab";
    private const string RootName = "VanFootRig_V002";

    [MenuItem("SPELLSTRIKE/Van/Animation Rigging V002/Build Separate Candidate Prefab")]
    public static void BuildCandidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogError("[VAN RIG V002] Build candidate only in idle Edit Mode.");
            return;
        }
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if (!src || !src.GetComponent<Animator>()?.isHuman == true)
        {
            Debug.LogError("[VAN RIG V002] Canonical Van Humanoid prefab invalid.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Candidate) != null)
        {
            Debug.LogError("[VAN RIG V002] Candidate already exists; refusing overwrite.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(CandidateFolder))
            AssetDatabase.CreateFolder("Assets/VAN AERT", "ProceduralRigging_V002");

        GameObject root = PrefabUtility.LoadPrefabContents(Source);
        try
        {
            Configure(root);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, Candidate);
            if (saved == null) throw new Exception("Could not save candidate prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[VAN RIG V002] Separate candidate built: " + Candidate +
                  ". Canonical player, Animator and all original scenes untouched.");
    }

    [MenuItem("SPELLSTRIKE/Van/Animation Rigging V002/QA Real Stage3 Van In Play Mode (never saves)")]
    public static void InstallOnLiveVan()
    {
        if (!Application.isPlaying ||
            SceneManager.GetActiveScene().path != "Assets/Scenes/Stage3Scene/Stage3Scene.unity")
        {
            Debug.LogError("[VAN RIG V002] Live QA ONLY in Stage3Scene Play Mode.");
            return;
        }
        MovementofPlayer[] players = UnityEngine.Object.FindObjectsByType<MovementofPlayer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (players.Length != 1 || players[0].GetComponent<VanAnimationRiggingFootPlacementV002>())
        {
            Debug.LogError("[VAN RIG V002] Expected exactly one unmodified live Van.");
            return;
        }
        GameObject root = players[0].gameObject;
        VanProceduralFootPlacement old = root.GetComponent<VanProceduralFootPlacement>();
        if (!old) throw new Exception("Original IK fallback missing.");
        old.enabled = false;
        try
        {
            Configure(root);
            var builder = root.GetComponent<RigBuilder>();
            if (!builder.Build()) throw new Exception("RigBuilder.Build() failed.");
            Debug.Log("[VAN RIG V002] LIVE PLAY-ONLY candidate rig built. " +
                "Original Animator IK disabled only in this Play Mode session. " +
                "GraphValid=" + builder.graph.IsValid());
        }
        catch (Exception ex)
        {
            Debug.LogError("[VAN RIG V002] Play-only setup failed: " + ex);
            old.enabled = true;
        }
    }

    private static void Configure(GameObject root)
    {
        Animator a = root.GetComponent<Animator>();
        if (!a || !a.isHuman) throw new Exception("Humanoid Animator required.");
        if (root.transform.Find(RootName) != null ||
            root.GetComponent<VanAnimationRiggingFootPlacementV002>() != null)
            throw new Exception("Rig already exists.");

        VanProceduralFootPlacement previous = root.GetComponent<VanProceduralFootPlacement>();
        if (previous) previous.enabled = false;
        var holder = Make(root.transform, RootName);
        var rig = holder.gameObject.AddComponent<Rig>();
        rig.weight = 1f;
        Transform hips = Bone(a, HumanBodyBones.Hips);
        var hipNode = Make(holder, "PelvisPosition");
        var hipTarget = Make(holder, "PelvisTarget");
        hipTarget.position = hips.position;
        var hipConstraint = hipNode.gameObject.AddComponent<MultiPositionConstraint>();
        var hipData = hipConstraint.data;
        hipData.constrainedObject = hips;
        hipData.sourceObjects = SourceArray(hipTarget);
        hipData.constrainedXAxis = false;
        hipData.constrainedYAxis = true;
        hipData.constrainedZAxis = false;
        hipData.maintainOffset = false;
        hipConstraint.data = hipData;
        hipConstraint.weight = 0f;

        var left = AddLeg(a, holder, "Left", HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        var right = AddLeg(a, holder, "Right", HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

        var driver = root.AddComponent<VanAnimationRiggingFootPlacementV002>();
        SerializedObject so = new SerializedObject(driver);
        Set(so, "footRig", rig);
        Set(so, "pelvisConstraint", hipConstraint);
        Set(so, "pelvisTarget", hipTarget);
        Set(so, "leftLeg", left.leg);
        Set(so, "rightLeg", right.leg);
        Set(so, "leftFootRotation", left.rot);
        Set(so, "rightFootRotation", right.rot);
        Set(so, "leftTarget", left.target);
        Set(so, "rightTarget", right.target);
        Set(so, "leftHint", left.hint);
        Set(so, "rightHint", right.hint);
        Set(so, "leftRotationTarget", left.rotation);
        Set(so, "rightRotationTarget", right.rotation);
        so.ApplyModifiedPropertiesWithoutUndo();
        if (Application.isPlaying && !driver.ReinitializeConfiguredRig())
            throw new Exception("V002 driver failed to initialize after binding targets.");

        RigBuilder builder = root.GetComponent<RigBuilder>();
        if (!builder) builder = root.AddComponent<RigBuilder>();
        builder.layers.Clear();
        builder.layers.Add(new RigLayer(rig));
        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(builder);
        EditorUtility.SetDirty(rig);
    }

    private struct LegObjects
    {
        public TwoBoneIKConstraint leg;
        public MultiRotationConstraint rot;
        public Transform target, hint, rotation;
    }

    private static LegObjects AddLeg(Animator a, Transform rig, string side,
        HumanBodyBones thighBone, HumanBodyBones kneeBone, HumanBodyBones footBone)
    {
        Transform thigh = Bone(a, thighBone);
        Transform knee = Bone(a, kneeBone);
        Transform foot = Bone(a, footBone);
        var node = Make(rig, side + "Leg");
        var target = Make(rig, side + "AnkleTarget");
        var hint = Make(rig, side + "KneeHint");
        var rotate = Make(rig, side + "RotationTarget");
        target.SetPositionAndRotation(foot.position, foot.rotation);
        rotate.SetPositionAndRotation(foot.position, foot.rotation);
        Vector3 kneeDirection = knee.position -
            Vector3.Lerp(thigh.position, foot.position, .5f);
        if (kneeDirection.sqrMagnitude < .0001f) kneeDirection = a.transform.forward;
        hint.position = knee.position + kneeDirection.normalized *
            (Vector3.Distance(thigh.position, knee.position) +
             Vector3.Distance(knee.position, foot.position)) * .18f;

        var constraint = node.gameObject.AddComponent<TwoBoneIKConstraint>();
        var data = constraint.data;
        data.root = thigh; data.mid = knee; data.tip = foot;
        data.target = target; data.hint = hint;
        data.targetPositionWeight = 1f;
        // Rotation is applied exactly once by the MultiRotation constraint.
        data.targetRotationWeight = 0f;
        data.hintWeight = 1f;
        data.maintainTargetPositionOffset = false;
        data.maintainTargetRotationOffset = false;
        constraint.data = data;
        constraint.weight = 0f;

        // Constraint hierarchy order: IK solves the leg first, then the foot
        // rotation adjusts to the supporting ground's actual normal.
        var rotNode = Make(rig, side + "FootRotation");
        var rot = rotNode.gameObject.AddComponent<MultiRotationConstraint>();
        var rotData = rot.data;
        rotData.constrainedObject = foot;
        rotData.sourceObjects = SourceArray(rotate);
        rotData.constrainedXAxis = true;
        rotData.constrainedYAxis = true;
        rotData.constrainedZAxis = true;
        rotData.maintainOffset = false;
        rot.data = rotData;
        rot.weight = 0f;
        return new LegObjects { leg=constraint, rot=rot, target=target,
            hint=hint, rotation=rotate };
    }

    private static WeightedTransformArray SourceArray(Transform source)
    {
        var a = new WeightedTransformArray(1);
        a[0] = new WeightedTransform(source, 1f);
        return a;
    }

    private static Transform Bone(Animator a, HumanBodyBones bone)
    {
        Transform t = a.GetBoneTransform(bone);
        if (!t) throw new Exception("Van Humanoid bone missing: " + bone);
        return t;
    }

    private static Transform Make(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void Set(SerializedObject so, string key, UnityEngine.Object value)
    {
        SerializedProperty prop = so.FindProperty(key);
        if (prop == null) throw new Exception("V002 driver field missing: " + key);
        prop.objectReferenceValue = value;
    }
}
