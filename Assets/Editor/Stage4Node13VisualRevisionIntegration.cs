// Visible, guarded environment-art-only integration into the ACTUAL Stage4Scene.
// Imported full-map GLBs, their GUIDs, collision and all gameplay objects remain unchanged.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Stage4Node13VisualRevisionIntegration
{
    private const string ScenePath = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string AssetsRoot = "Assets/Stage 4/ConvergenceCitadel_v003";
    private const string ImportRoot = AssetsRoot + "/Environment/Node13_ArtRevisions_20261020";
    private const string BackupRoot = "Library/Stage4Node13RevisionIntegration_20261020_2230";
    private const string ReportPath = AssetsRoot + "/Validation/Stage4Node13ActualSceneArtIntegration_20261020.md";
    private const string InstallationName = "Node13_RevisedVisuals_20261020";
    private static readonly string[] AssetNames =
    {
        "ConvergenceCitadel_Node1.glb",
        "ConvergenceCitadel_Node1_LOD1.glb",
        "ConvergenceCitadel_Node3.glb",
        "ConvergenceCitadel_Node3_LOD1.glb"
    };
    private static readonly string[] SourceSHA =
    {
        "eac37cbdf635ef5f7a8554a58ab146cb29656809d068bc05ef5b4397963a3470",
        "834ec1241224892f53376f77ab2757276351b84569cc7ffaa037ce3c61b43a2e",
        "328ef75b886f4b0d55845a20af1ff66383013de0ae0032498f1f62780518352e",
        "05d260abddc436e3882412e728e2e55702b3a5b9756b7941b9d08245e5e71b4a"
    };

    [MenuItem("Stage 4/Convergence Citadel v003/INTEGRATE BOTH Node1 and Node3 Art Into ACTUAL Stage4Scene")]
    private static void Install()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Stage4 Node13] Refusing scene modifications during Play Mode.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[Stage4 Node13] Open ACTUAL Stage4Scene, exit Play, SAVE all existing changes, then retry. No changes made.");
            return;
        }
        if (!File.Exists(ScenePath) || File.Exists(ReportPath) || File.Exists(BackupRoot + "/Stage4Scene_before_Node13_revision.unity"))
        {
            Debug.LogError("[Stage4 Node13] Missing scene or an existing integration/backup. Refusing repeat.");
            return;
        }
        try
        {
            // All validations happen before any new GameObject or scene modification.
            var prefabs = new GameObject[4];
            for (int i = 0; i < prefabs.Length; i++)
            {
                string path = ImportRoot + "/" + AssetNames[i];
                if (!File.Exists(path) || !Sha256(path).Equals(SourceSHA[i], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Source hash mismatch / missing imported GLB: " + path);
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabs[i] == null) throw new InvalidOperationException("glTFast prefab import incomplete: " + path);
            }
            int[] expected = { 25, 25, 24, 24 };
            for (int i = 0; i < prefabs.Length; i++)
            {
                MeshRenderer[] meshes = prefabs[i].GetComponentsInChildren<MeshRenderer>(true);
                if (meshes.Length != expected[i])
                    throw new InvalidOperationException("Expected " + expected[i] + " imported renderers for " + AssetNames[i] + "; found " + meshes.Length);
                string[] names = meshes.Select(x => x.name).ToArray();
                bool isNode1 = i < 2;
                if (names.Distinct().Count() != names.Length ||
                    names.Any(x => !((isNode1 ? x.StartsWith("01_") || x.StartsWith("I1_") || x.StartsWith("Kingdom_")
                                               : x.StartsWith("03_") || x.StartsWith("I3_") || x.StartsWith("Titan_")))))
                    throw new InvalidOperationException("GLB contains duplicated or out-of-scope mesh names: " + AssetNames[i]);
                foreach (MeshRenderer mesh in meshes)
                foreach (Material material in mesh.sharedMaterials)
                {
                    if (material == null || material.shader == null ||
                        (material.GetTag("RenderPipeline", false, "") != "UniversalPipeline" &&
                         !material.shader.name.Contains("Universal Render Pipeline") &&
                         !material.shader.name.Contains("Shader Graph")))
                        throw new InvalidOperationException("Missing/non-URP material in " + AssetNames[i] + ": " + (material ? material.name : "<null>"));
                }
            }

            GameObject stage = scene.GetRootGameObjects().Single(x => x.name == "Stage4_ConvergenceCitadel_PRODUCTION");
            Transform env = stage.GetComponentsInChildren<Transform>(true).Single(x => x.name == "00_Environment");
            Transform oldMain = env.GetComponentsInChildren<Transform>(true).Single(x => x.name == "ConvergenceCitadel_v003_Visual_LOD0");
            Transform oldLod = env.GetComponentsInChildren<Transform>(true).Single(x => x.name == "ConvergenceCitadel_v003_LOD1_SWITCHED_BY_LODGroups");
            if (env.GetComponentsInChildren<Transform>(true).Any(x => x.name == InstallationName))
                throw new InvalidOperationException("Node 1/3 revision is already in scene. Refusing second integration.");
            LODGroup[] allGroups = stage.GetComponentsInChildren<LODGroup>(true);
            if (allGroups.Length != 11 || allGroups.Any(g => g.GetLODs().Length != 2))
                throw new InvalidOperationException("Existing 11 two-level LODGroups are missing or changed.");
            LODGroup node1 = allGroups.Single(x => x.name == "LOD_Island01");
            LODGroup node3 = allGroups.Single(x => x.name == "LOD_Island03");
            ValidateExistingGroup(node1, 17, oldMain, oldLod);
            ValidateExistingGroup(node3, 16, oldMain, oldLod);

            Transform physics = stage.GetComponentsInChildren<Transform>(true)
                .Single(x => x.name == "Independent_StaticMeshColliders");
            MeshCollider[] colliders = physics.GetComponentsInChildren<MeshCollider>(true);
            if (colliders.Count(x => x.enabled) != 19 || colliders.Length != 20 ||
                colliders.Single(x => x.name == "COL_Bridge_04_Deck_PHYSICS").enabled ||
                !colliders.Single(x => x.name == "Bridge04_SmoothCollision_Validated_Traversable").enabled)
                throw new InvalidOperationException("19 enabled physics colliders and validated Bridge04 proxy are not in expected state.");
            // A preexisting additional enabled MeshCollider is attached to an imported
            // full-map visual prefab child outside Independent_StaticMeshColliders.
            // Do not disable, delete or otherwise change user-owned physics while
            // replacing renderers. It was present in the saved pre-change scene.
            if (stage.GetComponentsInChildren<MeshCollider>(true).Length != 21)
                throw new InvalidOperationException("Unexpected number of non-physics-root colliders in current user scene.");
            if (scene.GetRootGameObjects().Count(x => x.name == "PersistentHUDRoot") != 1 ||
                scene.GetRootGameObjects().Count(x => x.name == "Main Camera") != 1 ||
                scene.GetRootGameObjects().Count(x => x.name == "_Stage4Systems") != 1 ||
                scene.GetRootGameObjects().Count(x => x.name == "_PlayerSetup") != 1)
                throw new InvalidOperationException("Expected production player, camera, HUD or managers are missing.");

            // This is the exact ON-DISK state just saved by the user-visible editor.
            Directory.CreateDirectory(BackupRoot);
            string backup = BackupRoot + "/Stage4Scene_before_Node13_revision.unity";
            File.Copy(ScenePath, backup, false);
            string beforeHash = Sha256(backup);
            string[] nineGroupSignatures = allGroups.Where(x => x != node1 && x != node3)
                .OrderBy(x => x.name).Select(GroupSignature).ToArray();
            var old1 = node1.GetLODs();
            var old3 = node3.GetLODs();

            GameObject root = new GameObject(InstallationName);
            root.transform.SetParent(env, false);
            var created = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                created[i] = PrefabUtility.InstantiatePrefab(prefabs[i], scene) as GameObject;
                if (created[i] == null) throw new InvalidOperationException("Could not instantiate new GLB " + AssetNames[i]);
                created[i].name = (i < 2 ? "Node1_" : "Node3_") + (i % 2 == 0 ? "Revised_LOD0" : "Revised_LOD1");
                created[i].transform.SetParent(root.transform, false);
                // Both modular GLBs are world-space exports, with the same source coordinates
                // as the full-map GLBs. Match original full-map prefab instance pose exactly.
                Transform sourcePose = i % 2 == 0 ? oldMain : oldLod;
                created[i].transform.position = sourcePose.position;
                created[i].transform.rotation = sourcePose.rotation;
                created[i].transform.localScale = sourcePose.lossyScale;
            }

            // Replace the TWO existing category groups; do not create new LODGroup roles.
            ReplaceGroup(node1, old1, created[0], created[1]);
            ReplaceGroup(node3, old3, created[2], created[3]);
            foreach (Renderer renderer in old1.SelectMany(x => x.renderers)
                .Concat(old3.SelectMany(x => x.renderers)).Distinct())
                renderer.enabled = false; // keep existing GameObjects, markers AND any colliders active

            if (stage.GetComponentsInChildren<LODGroup>(true).Length != 11 ||
                physics.GetComponentsInChildren<MeshCollider>(true).Count(x => x.enabled) != 19 ||
                !allGroups.Where(x => x != node1 && x != node3).OrderBy(x => x.name)
                    .Select(GroupSignature).SequenceEqual(nineGroupSignatures))
                throw new InvalidOperationException("Post-replacement invariant failed, DO NOT SAVE.");
            string[] node1New = created[0].GetComponentsInChildren<MeshRenderer>(true).Select(x => x.name).OrderBy(x => x).ToArray();
            string[] node1LodNew = created[1].GetComponentsInChildren<MeshRenderer>(true).Select(x => x.name).OrderBy(x => x).ToArray();
            string[] node3New = created[2].GetComponentsInChildren<MeshRenderer>(true).Select(x => x.name).OrderBy(x => x).ToArray();
            string[] node3LodNew = created[3].GetComponentsInChildren<MeshRenderer>(true).Select(x => x.name).OrderBy(x => x).ToArray();
            if (!node1New.SequenceEqual(node1LodNew) || !node3New.SequenceEqual(node3LodNew))
                throw new InvalidOperationException("New LOD0 vs LOD1 renderer name sets are not identical; DO NOT SAVE.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the real Stage4Scene.");
            var sb = new StringBuilder();
            sb.AppendLine("# Actual production Stage 4: both Node 1 + Node 3 art revisions");
            sb.AppendLine("- Scene: `" + ScenePath + "` (saved; no QA-scene substitution)");
            sb.AppendLine("- Before SHA-256: `" + beforeHash + "`");
            sb.AppendLine("- After SHA-256: `" + Sha256(ScenePath) + "`");
            sb.AppendLine("- Prior scene backup: `" + backup + "`");
            for (int i = 0; i < 4; i++)
                sb.AppendLine("- Imported `" + ImportRoot + "/" + AssetNames[i] + "`, SHA-256 `" + SourceSHA[i] + "`, " + expected[i] + " renderers");
            sb.AppendLine("- Existing Node 1 LODGroup: " + old1[0].renderers.Length + "/" + old1[1].renderers.Length + " superseded, 25/25 current; screen thresholds " + old1[0].screenRelativeTransitionHeight + "/" + old1[1].screenRelativeTransitionHeight);
            sb.AppendLine("- Existing Node 3 LODGroup: " + old3[0].renderers.Length + "/" + old3[1].renderers.Length + " superseded, 24/24 current; screen thresholds " + old3[0].screenRelativeTransitionHeight + "/" + old3[1].screenRelativeTransitionHeight);
            sb.AppendLine("- PASS: 11 scene-local LODGroups; other nine group signatures unchanged; original old renderers deactivated scene-only.");
            sb.AppendLine("- PASS: 19 enabled MeshColliders in independent static collision root; validated Bridge04 proxy retained; no NavMesh or gameplay/prefab/save/BuildSettings edits.");
            sb.AppendLine("- PRESERVED EXISTING NON-ROOT COLLIDER: One additionally enabled MeshCollider on an original visual prefab child was present in the saved scene before this integration; left unchanged (total enabled across entire environment = 20).");
            sb.AppendLine("- PENDING: visual camera review, forced LOD0/LOD1, geometry intersection, gameplay traversal and A05s physical performance.");
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log("[Stage4 Node13] BOTH NODES INTEGRATED INTO ACTUAL Stage4Scene: Node1 25+25 / Node3 24+24; 11 LODGroups, 19 colliders. Report: " + ReportPath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[Stage4 Node13] Integration FAILED. If scene is dirty, DO NOT SAVE; restore from the dedicated backup only after reviewing current changes.");
        }
    }

    private static void ValidateExistingGroup(LODGroup group, int expected, Transform main, Transform lesser)
    {
        LOD[] lods = group.GetLODs();
        if (lods.Length != 2 || lods[0].renderers.Length != expected || lods[1].renderers.Length != expected ||
            Math.Abs(lods[0].screenRelativeTransitionHeight - 0.26f) > 0.0001f ||
            Math.Abs(lods[1].screenRelativeTransitionHeight - 0.002f) > 0.0001f)
            throw new InvalidOperationException(group.name + " unexpectedly changed before integration.");
        for (int level = 0; level < 2; level++)
        foreach (Renderer render in lods[level].renderers)
        {
            if (render == null || render.transform.childCount != 0 ||
                !render.transform.IsChildOf(level == 0 ? main : lesser))
                throw new InvalidOperationException("Old " + group.name + " mesh is not a leaf in the approved source prefab.");
        }
    }

    private static void ReplaceGroup(LODGroup group, LOD[] prior, GameObject detailed, GameObject simplified)
    {
        Renderer[] first = detailed.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().OrderBy(x => x.name).ToArray();
        Renderer[] second = simplified.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().OrderBy(x => x.name).ToArray();
        LOD zero = new LOD(prior[0].screenRelativeTransitionHeight, first);
        LOD one = new LOD(prior[1].screenRelativeTransitionHeight, second);
        zero.fadeTransitionWidth = prior[0].fadeTransitionWidth;
        one.fadeTransitionWidth = prior[1].fadeTransitionWidth;
        group.SetLODs(new[] { zero, one });
        group.fadeMode = LODFadeMode.None;
        group.RecalculateBounds();
        group.ForceLOD(-1);
    }

    private static string GroupSignature(LODGroup group)
    {
        return group.name + "|" + group.fadeMode + "|" + string.Join(";", group.GetLODs().Select(x =>
            x.screenRelativeTransitionHeight + ":" +
            string.Join(",", x.renderers.Select(y => y == null ? "null" : y.GetInstanceID().ToString()))));
    }

    [MenuItem("Stage 4/Convergence Citadel v003/Node13 QA/Force Both Revised Nodes LOD0 (Unsaved)")]
    private static void Preview0() => Preview(0);
    [MenuItem("Stage 4/Convergence Citadel v003/Node13 QA/Force Both Revised Nodes LOD1 (Unsaved)")]
    private static void Preview1() => Preview(1);
    [MenuItem("Stage 4/Convergence Citadel v003/Node13 QA/Restore Automatic LOD (Unsaved)")]
    private static void PreviewAuto() => Preview(-1);
    private static void Preview(int level)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || Application.isPlaying) return;
        var groups = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LODGroup>(true)).ToArray();
        if (groups.Length != 11) return;
        foreach (var g in groups.Where(x => x.name == "LOD_Island01" || x.name == "LOD_Island03"))
            g.ForceLOD(level);
        SceneView.RepaintAll();
        Debug.Log("[Stage4 Node13 QA] Forced both revision nodes to level " + level + ". Scene not saved.");
    }

    private static string Sha256(string path)
    {
        using (var sha = SHA256.Create())
        using (var file = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
    }
}
#endif
