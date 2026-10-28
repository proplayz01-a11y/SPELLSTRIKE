using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Read-only CPU linear-blend-skinning measurement of the actual V017 foot
/// vertices. SkinnedMeshRenderer.BakeMesh + TransformPoint is NOT correct for
/// this imported model's nested ~4001x renderer transform scale.
/// This helper has no MonoBehaviour, no Animator writes and no gameplay hooks.
/// </summary>
internal sealed class TitanV017DeformedSoleLbs
{
    private readonly SkinnedMeshRenderer renderer;
    private readonly Mesh sampledMesh;
    private readonly Transform[] bones;
    private readonly Vector3[] vertices;
    private readonly BoneWeight[] weights;
    private readonly Matrix4x4[] bindposes;
    private readonly Matrix4x4[] worldMatrices;
    private readonly int[] rightFootVertexIndices;
    private readonly int[] leftFootVertexIndices;

    public int RightVertexCount => rightFootVertexIndices.Length;
    public int LeftVertexCount => leftFootVertexIndices.Length;
    public string RendererName => renderer ? renderer.name : "MISSING";

    private TitanV017DeformedSoleLbs(SkinnedMeshRenderer smr, Vector3[] meshVertices,
        BoneWeight[] boneWeights, Matrix4x4[] poses, int[] right, int[] left)
    {
        renderer = smr;
        sampledMesh = smr.sharedMesh;
        bones = smr.bones;
        vertices = meshVertices;
        weights = boneWeights;
        bindposes = poses;
        worldMatrices = new Matrix4x4[bones.Length];
        rightFootVertexIndices = right;
        leftFootVertexIndices = left;
    }

    public static bool TryCreate(Transform titanRoot, out TitanV017DeformedSoleLbs result,
        out string reason)
    {
        // Retain the general read-only motion-QA entry point. Combat V017
        // instead calls the exact-renderer overload below to ensure its
        // contact sampler reads the body that players actually see.
        return TryCreate(titanRoot, null, null, null, out result, out reason);
    }

    public static bool TryCreate(Transform titanRoot, SkinnedMeshRenderer requiredBody,
        Transform expectedRightFoot, Transform expectedLeftFoot,
        out TitanV017DeformedSoleLbs result, out string reason)
    {
        result = null;
        reason = null;
        if (!titanRoot)
        {
            reason = "No Animator root for deformed-sole check.";
            return false;
        }
        // The standalone QA scene substitutes a separately imported readable
        // mesh on the real body renderer. The production FBX is NOT readable
        // by default. Never search a hidden, unrelated, readable QA renderer
        // as a substitute for an explicitly required visible body.
        if (requiredBody && (!requiredBody.enabled || !requiredBody.gameObject.activeInHierarchy ||
            !requiredBody.transform.IsChildOf(titanRoot)))
        {
            reason = "The chosen V017 body SkinnedMeshRenderer is disabled or outside the animated Titan.";
            return false;
        }
        if (requiredBody && (!expectedRightFoot || !expectedLeftFoot ||
            !expectedRightFoot.IsChildOf(titanRoot) || !expectedLeftFoot.IsChildOf(titanRoot)))
        {
            reason = "Required V017 sole bones are missing or outside the Animator skeleton.";
            return false;
        }
        // V017 has a single body mesh with ~1548/2198 foot-influenced
        // vertices. Require BOTH feet on the SAME actual renderer.
        SkinnedMeshRenderer[] renderers = requiredBody
            ? new[] { requiredBody }
            : titanRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in renderers)
        {
            if (!smr || !smr.sharedMesh)
                continue;
            Mesh mesh = smr.sharedMesh;
            Transform[] rendererBones = smr.bones;
            Matrix4x4[] poses;
            BoneWeight[] boneWeights;
            Vector3[] meshVertices;
            // CPU vertex data can be absent even while bone weights and
            // mesh.vertexCount are present on imported Read/Write=Off FBXs.
            // Never create a sampler from a zero-length vertices array:
            // Skin() would later index outside that array in BeginPreview.
            // The standalone QA scene uses a separately imported readable
            // mesh clone, NOT a mutation of the production body FBX.
            if (!mesh.isReadable)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body '" + smr.name + "' uses CPU-unreadable mesh '" +
                        mesh.name + "' (Read/Write OFF). Production must assign a verified readable " +
                        "body mesh to THIS renderer; solo-scene mesh overrides do not migrate.";
                    return false;
                }
                continue;
            }
            try
            {
                poses = mesh.bindposes;
                boneWeights = mesh.boneWeights;
                meshVertices = mesh.vertices;
            }
            catch (Exception e)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body mesh CPU data could not be read: " + e.GetType().Name;
                    return false;
                }
                continue;
            }
            if (rendererBones == null || rendererBones.Length == 0 ||
                poses.Length != rendererBones.Length)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body mesh bindposes do not match its skinned bones.";
                    return false;
                }
                continue;
            }
            if (meshVertices == null || meshVertices.Length != mesh.vertexCount ||
                boneWeights.Length != meshVertices.Length)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body mesh CPU vertices/weights are missing or incomplete.";
                    return false;
                }
                continue;
            }
            if (requiredBody && (Array.IndexOf(rendererBones, expectedRightFoot) < 0 ||
                Array.IndexOf(rendererBones, expectedLeftFoot) < 0))
            {
                reason = "V017 visible body mesh does not skin the exact active right AND left foot bones.";
                return false;
            }
            bool validBoneIndices = true;
            foreach (BoneWeight w in boneWeights)
            {
                if ((w.weight0 > 0f && (w.boneIndex0 < 0 || w.boneIndex0 >= rendererBones.Length)) ||
                    (w.weight1 > 0f && (w.boneIndex1 < 0 || w.boneIndex1 >= rendererBones.Length)) ||
                    (w.weight2 > 0f && (w.boneIndex2 < 0 || w.boneIndex2 >= rendererBones.Length)) ||
                    (w.weight3 > 0f && (w.boneIndex3 < 0 || w.boneIndex3 >= rendererBones.Length)))
                { validBoneIndices = false; break; }
            }
            if (!validBoneIndices)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body mesh has invalid skin-weight bone indices.";
                    return false;
                }
                continue;
            }
            List<int> right = new List<int>(1600);
            List<int> left = new List<int>(2200);
            for (int i = 0; i < boneWeights.Length; i++)
            {
                BoneWeight w = boneWeights[i];
                float r = 0f, l = 0f;
                SumWeight(rendererBones, w.boneIndex0, w.weight0, ref r, ref l);
                SumWeight(rendererBones, w.boneIndex1, w.weight1, ref r, ref l);
                SumWeight(rendererBones, w.boneIndex2, w.weight2, ref r, ref l);
                SumWeight(rendererBones, w.boneIndex3, w.weight3, ref r, ref l);
                if (r > 0.35f) right.Add(i);
                if (l > 0.35f) left.Add(i);
            }
            if (right.Count < 200 || left.Count < 200)
            {
                if (requiredBody)
                {
                    reason = "V017 visible body has insufficient sole vertices: right=" +
                        right.Count + " left=" + left.Count + " (at least 200 each required).";
                    return false;
                }
                continue;
            }
            result = new TitanV017DeformedSoleLbs(smr, meshVertices, boneWeights,
                poses, right.ToArray(), left.ToArray());
            return true;
        }
        reason = "No readable skinned Titan mesh with BOTH full foot vertex sets; cannot claim real sole contact.";
        return false;
    }

    private static void SumWeight(Transform[] rendererBones, int index, float weight,
        ref float right, ref float left)
    {
        if (weight <= 0f || index < 0 || index >= rendererBones.Length ||
            !rendererBones[index]) return;
        string n = rendererBones[index].name;
        if (n.Contains("RightFoot") || n.Contains("RightToe")) right += weight;
        if (n.Contains("LeftFoot") || n.Contains("LeftToe")) left += weight;
    }

    public bool TrySample(out Vector3 rightLowestVertex, out Vector3 leftLowestVertex)
    {
        rightLowestVertex = default;
        leftLowestVertex = default;
        if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
            !sampledMesh || renderer.sharedMesh != sampledMesh || !sampledMesh.isReadable ||
            bones.Length != bindposes.Length)
            return false;

        // Transform every vertex by its weighted Mixamo bone bindpose.
        // This handles the FBX's unusually scaled nested renderer correctly.
        for (int i = 0; i < bones.Length; i++)
        {
            if (!bones[i]) return false;
            worldMatrices[i] = bones[i].localToWorldMatrix * bindposes[i];
        }
        float rightMinY = float.PositiveInfinity;
        foreach (int index in rightFootVertexIndices)
        {
            Vector3 v = Skin(index);
            if (v.y < rightMinY)
            {
                rightMinY = v.y;
                rightLowestVertex = v;
            }
        }
        float leftMinY = float.PositiveInfinity;
        foreach (int index in leftFootVertexIndices)
        {
            Vector3 v = Skin(index);
            if (v.y < leftMinY)
            {
                leftMinY = v.y;
                leftLowestVertex = v;
            }
        }
        return IsFinite(rightLowestVertex) && IsFinite(leftLowestVertex) &&
            !float.IsInfinity(rightMinY) && !float.IsInfinity(leftMinY);
    }

    private Vector3 Skin(int index)
    {
        BoneWeight w = weights[index];
        Vector3 v = vertices[index];
        Vector3 world = Vector3.zero;
        if (w.weight0 > 0f) world += worldMatrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0;
        if (w.weight1 > 0f) world += worldMatrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1;
        if (w.weight2 > 0f) world += worldMatrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2;
        if (w.weight3 > 0f) world += worldMatrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
        return world;
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
