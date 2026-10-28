using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public static class Stage3DesertArcherUpperBodyAimRigSetup
{
    private const string RigRootName = "UpperBodyAimRig";
    private const string PrecisionRigRootName = "BowPrecisionAimRig";
    private const string ConstraintName = "BowAim_MultiAim";
    private const string HandConstraintName = "BowHandAim_MultiAim";

    [MenuItem("SPELLSTRIKE/Stage 3/Desert Archer/Build Upper Body Bow Aim Rig %#F9")]
    public static void BuildRig()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Stop Play Mode / compilation before building the Desert Archer aim rig.");

        DesertArcherController controller = UnityEngine.Object.FindFirstObjectByType<DesertArcherController>();
        if (controller == null)
            throw new InvalidOperationException("Desert Archer controller was not found in the open scene.");

        Animator animator = controller.GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
            throw new InvalidOperationException("Desert Archer requires a valid Humanoid Animator for the upper-body aim rig.");

        // The Archer's weapon aim affects gameplay even when the renderer is off-camera.
        // CullUpdateTransforms freezes the skeleton/rig while NavMesh + combat logic keep running,
        // so keep this enemy's Animator evaluating consistently.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Transform torso = animator.GetBoneTransform(HumanBodyBones.UpperChest)
            ?? animator.GetBoneTransform(HumanBodyBones.Chest)
            ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        if (torso == null)
            throw new InvalidOperationException("Could not resolve UpperChest/Chest/Spine on the Desert Archer Humanoid.");

        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (leftHand == null)
            throw new InvalidOperationException("Could not resolve LeftHand on the Desert Archer Humanoid.");

        Transform aimTarget = FindChildRecursive(controller.transform, "AimTarget");
        Transform bowAimReference = FindChildRecursive(controller.transform, "ArrowReleaseSpawn")
            ?? FindChildRecursive(controller.transform, "ArrowRest");
        Transform heldBow = FindChildRecursive(controller.transform, "S3N2_HeldBow");
        if (aimTarget == null || bowAimReference == null || heldBow == null)
            throw new InvalidOperationException("AimTarget, held bow, or ArrowReleaseSpawn/ArrowRest is missing; aim rig was not changed.");

        RigBuilder rigBuilder = animator.GetComponent<RigBuilder>();
        if (rigBuilder == null)
            rigBuilder = Undo.AddComponent<RigBuilder>(animator.gameObject);

        Transform rigRootTransform = controller.transform.Find(RigRootName);
        GameObject rigRoot;
        if (rigRootTransform == null)
        {
            rigRoot = new GameObject(RigRootName);
            Undo.RegisterCreatedObjectUndo(rigRoot, "Create Desert Archer Upper Body Aim Rig");
            rigRoot.transform.SetParent(controller.transform, false);
        }
        else
        {
            rigRoot = rigRootTransform.gameObject;
        }

        Rig rig = rigRoot.GetComponent<Rig>();
        if (rig == null)
            rig = Undo.AddComponent<Rig>(rigRoot);
        rig.weight = 0f;

        Transform constraintTransform = rigRoot.transform.Find(ConstraintName);
        GameObject constraintObject;
        if (constraintTransform == null)
        {
            constraintObject = new GameObject(ConstraintName);
            Undo.RegisterCreatedObjectUndo(constraintObject, "Create Desert Archer Bow Aim Constraint");
            constraintObject.transform.SetParent(rigRoot.transform, false);
        }
        else
        {
            constraintObject = constraintTransform.gameObject;
        }

        MultiAimConstraint constraint = constraintObject.GetComponent<MultiAimConstraint>();
        if (constraint == null)
            constraint = Undo.AddComponent<MultiAimConstraint>(constraintObject);

        MultiAimConstraintData data = constraint.data;
        data.constrainedObject = torso;
        data.sourceObjects = new WeightedTransformArray
        {
            new WeightedTransform(aimTarget, 1f)
        };
        data.aimAxis = ResolveClosestAxis(torso, bowAimReference.forward);
        data.upAxis = ResolveClosestAxisExcluding(torso, controller.transform.up, data.aimAxis);
        data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
        data.worldUpAxis = MultiAimConstraintData.Axis.Y;
        data.worldUpObject = null;
        data.maintainOffset = false;
        data.offset = Vector3.zero;
        data.limits = new Vector2(-80f, 80f);
        data.constrainedXAxis = true;
        data.constrainedYAxis = true;
        data.constrainedZAxis = true;
        constraint.data = data;
        constraint.weight = 1f;

        Transform precisionRigRootTransform = controller.transform.Find(PrecisionRigRootName);
        GameObject precisionRigRoot;
        if (precisionRigRootTransform == null)
        {
            precisionRigRoot = new GameObject(PrecisionRigRootName);
            Undo.RegisterCreatedObjectUndo(precisionRigRoot, "Create Desert Archer Bow Precision Aim Rig");
            precisionRigRoot.transform.SetParent(controller.transform, false);
        }
        else
        {
            precisionRigRoot = precisionRigRootTransform.gameObject;
        }

        Rig precisionRig = precisionRigRoot.GetComponent<Rig>();
        if (precisionRig == null)
            precisionRig = Undo.AddComponent<Rig>(precisionRigRoot);
        precisionRig.weight = 0f;

        Transform handConstraintTransform = FindChildRecursive(controller.transform, HandConstraintName);
        GameObject handConstraintObject;
        if (handConstraintTransform == null)
        {
            handConstraintObject = new GameObject(HandConstraintName);
            Undo.RegisterCreatedObjectUndo(handConstraintObject, "Create Desert Archer Bow Hand Aim Constraint");
            handConstraintObject.transform.SetParent(precisionRigRoot.transform, false);
        }
        else
        {
            handConstraintObject = handConstraintTransform.gameObject;
            if (handConstraintObject.transform.parent != precisionRigRoot.transform)
                Undo.SetTransformParent(handConstraintObject.transform, precisionRigRoot.transform, "Move Bow Precision Constraint To Final Rig Layer");
        }

        MultiAimConstraint handConstraint = handConstraintObject.GetComponent<MultiAimConstraint>();
        if (handConstraint == null)
            handConstraint = Undo.AddComponent<MultiAimConstraint>(handConstraintObject);

        MultiAimConstraintData handData = handConstraint.data;
        // Precision stage: use a guaranteed writable Humanoid stream bone.
        // The held bow is parented directly beneath LeftHand, so the bow inherits
        // this final correction after the broad torso aim layer has evaluated.
        handData.constrainedObject = leftHand;
        handData.sourceObjects = new WeightedTransformArray
        {
            new WeightedTransform(aimTarget, 1f)
        };
        handData.aimAxis = ResolveClosestAxis(leftHand, bowAimReference.forward);
        handData.upAxis = ResolveClosestAxisExcluding(leftHand, controller.transform.up, handData.aimAxis);
        handData.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
        handData.worldUpAxis = MultiAimConstraintData.Axis.Y;
        handData.worldUpObject = null;
        handData.maintainOffset = false;

        Vector3 localBowDirection = leftHand.InverseTransformDirection(bowAimReference.forward).normalized;
        Vector3 localHandAimAxis = AxisVector(handData.aimAxis);
        handData.offset = Quaternion.FromToRotation(localBowDirection, localHandAimAxis).eulerAngles;
        handData.limits = new Vector2(-180f, 180f);
        handData.constrainedXAxis = true;
        handData.constrainedYAxis = true;
        handData.constrainedZAxis = true;
        handConstraint.data = handData;
        handConstraint.weight = 1f;

        if (heldBow.GetComponent<RigTransform>() == null)
            Undo.AddComponent<RigTransform>(heldBow.gameObject);

        rigBuilder.layers.Clear();
        rigBuilder.layers.Add(new RigLayer(rig, true));
        rigBuilder.layers.Add(new RigLayer(precisionRig, true));

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("upperBodyAimRig").objectReferenceValue = rig;
        serializedController.FindProperty("bowPrecisionAimRig").objectReferenceValue = precisionRig;
        serializedController.FindProperty("upperBodyAimConstraint").objectReferenceValue = constraint;
        serializedController.FindProperty("bowHandAimConstraint").objectReferenceValue = handConstraint;
        serializedController.FindProperty("bowAimReference").objectReferenceValue = bowAimReference;
        serializedController.FindProperty("enableUpperBodyAimRig").boolValue = true;
        serializedController.FindProperty("upperBodyAimWeight").floatValue = 1f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(rigBuilder);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(rig);
        EditorUtility.SetDirty(precisionRig);
        EditorUtility.SetDirty(constraint);
        EditorUtility.SetDirty(handConstraint);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);

        Debug.Log(
            $"[DesertArcherAimRig] BUILT torso='{torso.name}' aimAxis={data.aimAxis} upAxis={data.upAxis} " +
            $"precision='{leftHand.name}' precisionAimAxis={handData.aimAxis} precisionOffset={handData.offset} " +
            $"target='{aimTarget.name}' bowReference='{bowAimReference.name}' layers=2 RigTransform=True limits={data.limits}.",
            controller);
    }

    private static MultiAimConstraintData.Axis ResolveClosestAxis(Transform basis, Vector3 worldDirection)
    {
        Vector3 local = basis.InverseTransformDirection(worldDirection.normalized);
        return AxisCandidates()
            .OrderByDescending(candidate => Vector3.Dot(AxisVector(candidate), local))
            .First();
    }

    private static MultiAimConstraintData.Axis ResolveClosestAxisExcluding(
        Transform basis,
        Vector3 worldDirection,
        MultiAimConstraintData.Axis excluded)
    {
        Vector3 local = basis.InverseTransformDirection(worldDirection.normalized);
        return AxisCandidates()
            .Where(candidate => candidate != excluded && AxisVector(candidate) != -AxisVector(excluded))
            .OrderByDescending(candidate => Vector3.Dot(AxisVector(candidate), local))
            .First();
    }

    private static MultiAimConstraintData.Axis[] AxisCandidates()
    {
        return new[]
        {
            MultiAimConstraintData.Axis.X,
            MultiAimConstraintData.Axis.X_NEG,
            MultiAimConstraintData.Axis.Y,
            MultiAimConstraintData.Axis.Y_NEG,
            MultiAimConstraintData.Axis.Z,
            MultiAimConstraintData.Axis.Z_NEG
        };
    }

    private static Vector3 AxisVector(MultiAimConstraintData.Axis axis)
    {
        return axis switch
        {
            MultiAimConstraintData.Axis.X => Vector3.right,
            MultiAimConstraintData.Axis.X_NEG => Vector3.left,
            MultiAimConstraintData.Axis.Y => Vector3.up,
            MultiAimConstraintData.Axis.Y_NEG => Vector3.down,
            MultiAimConstraintData.Axis.Z => Vector3.forward,
            MultiAimConstraintData.Axis.Z_NEG => Vector3.back,
            _ => Vector3.forward
        };
    }

    private static Transform FindChildRecursive(Transform root, string exactName)
    {
        if (root == null)
            return null;

        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => child.name == exactName);
    }
}
