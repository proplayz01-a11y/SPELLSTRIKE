using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DesertArcherBowDrawDriver : MonoBehaviour
{
    [SerializeField] private Transform bowRoot;
    [SerializeField] private string blendShapeName = "Draw";
    [SerializeField, Range(0f, 1f)] private float previewDraw01;

    private readonly List<SkinnedDrawTarget> skinnedTargets = new List<SkinnedDrawTarget>();
    private readonly List<MeshDrawTarget> meshTargets = new List<MeshDrawTarget>();
    private float currentDraw01 = -1f;
    private bool initialized;
    private bool warnedUnreadableMesh;

    public int DrawTargetCount => skinnedTargets.Count + meshTargets.Count;
    public float CurrentDraw01 => Mathf.Max(0f, currentDraw01);

    public void BindBowRoot(Transform targetBowRoot)
    {
        if (targetBowRoot == bowRoot && initialized)
            return;

        RestoreRuntimeMeshes();
        bowRoot = targetBowRoot;
        initialized = false;
        currentDraw01 = -1f;
        EnsureInitialized();
    }

    private void Awake()
    {
        EnsureInitialized();
        SetDraw01(0f);
    }

    public void SetDraw01(float value)
    {
        EnsureInitialized();

        float clamped = Mathf.Clamp01(value);
        if (Mathf.Abs(clamped - currentDraw01) < 0.0025f)
            return;

        currentDraw01 = clamped;

        for (int i = 0; i < skinnedTargets.Count; i++)
        {
            SkinnedDrawTarget target = skinnedTargets[i];
            if (target.Renderer != null)
                target.Renderer.SetBlendShapeWeight(target.BlendShapeIndex, clamped * 100f);
        }

        for (int i = 0; i < meshTargets.Count; i++)
            meshTargets[i].Apply(clamped);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        skinnedTargets.Clear();
        meshTargets.Clear();

        if (bowRoot == null)
            return;

        SkinnedMeshRenderer[] skinned = bowRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            Mesh mesh = skinned[i].sharedMesh;
            if (mesh == null)
                continue;

            int shapeIndex = mesh.GetBlendShapeIndex(blendShapeName);
            if (shapeIndex >= 0)
                skinnedTargets.Add(new SkinnedDrawTarget(skinned[i], shapeIndex));
        }

        // The current v013 bow is intentionally split into many MeshFilter +
        // MeshRenderer parts. Unity imports their Draw data into Mesh assets, but a
        // MeshRenderer cannot evaluate blend shapes. Clone only the affected meshes
        // at runtime and apply the imported blend-shape deltas ourselves.
        MeshFilter[] filters = bowRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh source = filters[i].sharedMesh;
            if (source == null)
                continue;

            int shapeIndex = source.GetBlendShapeIndex(blendShapeName);
            if (shapeIndex < 0 || source.GetBlendShapeFrameCount(shapeIndex) <= 0)
                continue;

            if (!source.isReadable)
            {
                if (!warnedUnreadableMesh)
                {
                    warnedUnreadableMesh = true;
                    Debug.LogWarning(
                        "[DesertArcherBowDraw] Bow mesh Read/Write is disabled. Static Draw blend shapes cannot be driven until the v013 bow model is imported as readable.",
                        this);
                }
                continue;
            }

            MeshDrawTarget target = MeshDrawTarget.Create(filters[i], source, shapeIndex);
            if (target != null)
                meshTargets.Add(target);
        }
    }

    private void RestoreRuntimeMeshes()
    {
        for (int i = 0; i < meshTargets.Count; i++)
            meshTargets[i].Dispose();
        meshTargets.Clear();
        skinnedTargets.Clear();
    }

    private void OnDestroy()
    {
        RestoreRuntimeMeshes();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        previewDraw01 = Mathf.Clamp01(previewDraw01);
        if (Application.isPlaying)
            SetDraw01(previewDraw01);
    }
#endif

    private sealed class SkinnedDrawTarget
    {
        public readonly SkinnedMeshRenderer Renderer;
        public readonly int BlendShapeIndex;

        public SkinnedDrawTarget(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            Renderer = renderer;
            BlendShapeIndex = blendShapeIndex;
        }
    }

    private sealed class MeshDrawTarget
    {
        private readonly MeshFilter filter;
        private readonly Mesh sourceMesh;
        private readonly Mesh runtimeMesh;
        private readonly Vector3[] baseVertices;
        private readonly Vector3[] baseNormals;
        private readonly Vector4[] baseTangents;
        private readonly Vector3[] deltaVertices;
        private readonly Vector3[] deltaNormals;
        private readonly Vector3[] deltaTangents;
        private readonly Vector3[] workingVertices;
        private readonly Vector3[] workingNormals;
        private readonly Vector4[] workingTangents;

        private MeshDrawTarget(
            MeshFilter targetFilter,
            Mesh original,
            Mesh runtime,
            Vector3[] vertices,
            Vector3[] normals,
            Vector4[] tangents,
            Vector3[] vertexDelta,
            Vector3[] normalDelta,
            Vector3[] tangentDelta)
        {
            filter = targetFilter;
            sourceMesh = original;
            runtimeMesh = runtime;
            baseVertices = vertices;
            baseNormals = normals;
            baseTangents = tangents;
            deltaVertices = vertexDelta;
            deltaNormals = normalDelta;
            deltaTangents = tangentDelta;
            workingVertices = new Vector3[vertices.Length];
            workingNormals = normals != null && normals.Length == vertices.Length
                ? new Vector3[normals.Length]
                : null;
            workingTangents = tangents != null && tangents.Length == vertices.Length
                ? new Vector4[tangents.Length]
                : null;
        }

        public static MeshDrawTarget Create(MeshFilter filter, Mesh source, int shapeIndex)
        {
            int frameCount = source.GetBlendShapeFrameCount(shapeIndex);
            if (frameCount <= 0)
                return null;

            Vector3[] vertices = source.vertices;
            if (vertices == null || vertices.Length == 0)
                return null;

            Vector3[] deltaVertices = new Vector3[vertices.Length];
            Vector3[] deltaNormals = new Vector3[vertices.Length];
            Vector3[] deltaTangents = new Vector3[vertices.Length];
            source.GetBlendShapeFrameVertices(
                shapeIndex,
                frameCount - 1,
                deltaVertices,
                deltaNormals,
                deltaTangents);

            Mesh runtime = Object.Instantiate(source);
            runtime.name = source.name + " (Runtime Draw)";
            filter.sharedMesh = runtime;

            return new MeshDrawTarget(
                filter,
                source,
                runtime,
                vertices,
                source.normals,
                source.tangents,
                deltaVertices,
                deltaNormals,
                deltaTangents);
        }

        public void Apply(float draw01)
        {
            for (int i = 0; i < baseVertices.Length; i++)
                workingVertices[i] = baseVertices[i] + deltaVertices[i] * draw01;
            runtimeMesh.vertices = workingVertices;

            if (workingNormals != null)
            {
                for (int i = 0; i < baseNormals.Length; i++)
                    workingNormals[i] = (baseNormals[i] + deltaNormals[i] * draw01).normalized;
                runtimeMesh.normals = workingNormals;
            }

            if (workingTangents != null)
            {
                for (int i = 0; i < baseTangents.Length; i++)
                {
                    Vector4 tangent = baseTangents[i];
                    Vector3 xyz = new Vector3(tangent.x, tangent.y, tangent.z) + deltaTangents[i] * draw01;
                    xyz.Normalize();
                    workingTangents[i] = new Vector4(xyz.x, xyz.y, xyz.z, tangent.w);
                }
                runtimeMesh.tangents = workingTangents;
            }

            runtimeMesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (filter != null && filter.sharedMesh == runtimeMesh)
                filter.sharedMesh = sourceMesh;

            if (runtimeMesh != null)
                Object.Destroy(runtimeMesh);
        }
    }
}
