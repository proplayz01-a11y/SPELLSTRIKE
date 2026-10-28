using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage3VioletFragmentSetupMenu
{
    private const string Stage3ScenePath = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string FragmentModelPath = "Assets/Maps/FRAGMENT_PIECES.fbx";
    private const string SourceMaterialPath = "Assets/Materials/FragmentMaterial.mat";
    private const string Stage3MaterialFolder = "Assets/Stage 3/Materials";
    private const string VioletMaterialPath = Stage3MaterialFolder + "/Stage3_Fragment_Violet.mat";
    private const string FragmentRootName = "_Stage3_VioletFragments";

    private static readonly string[] PreferredMeshNames =
    {
        "Icosphere",
        "Icosphere.003",
        "Icosphere.004"
    };

    private static readonly Vector3[] FragmentEulerAngles =
    {
        new Vector3(12f, 25f, -18f),
        new Vector3(-16f, 135f, 22f),
        new Vector3(18f, 245f, -12f)
    };

    [MenuItem("SPELLSTRIKE/Stage 3/Install Violet Node Fragments")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError(
                $"[Stage3Fragments] Open '{Stage3ScenePath}' before running this installer. " +
                "The installer will not switch scenes automatically because the current scene may contain unsaved work.");
            return;
        }

        StageNodeController[] nodeControllers = UnityEngine.Object
            .FindObjectsByType<StageNodeController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(controller => controller != null && controller.gameObject.scene == scene)
            .ToArray();

        StageNodeController[] nodes = new StageNodeController[3];
        for (int nodeIndex = 1; nodeIndex <= 3; nodeIndex++)
        {
            nodes[nodeIndex - 1] = nodeControllers.FirstOrDefault(controller =>
                string.Equals(controller.gameObject.name, $"Node {nodeIndex}", StringComparison.Ordinal));

            if (nodes[nodeIndex - 1] == null)
            {
                Debug.LogError($"[Stage3Fragments] Could not find StageNodeController on 'Node {nodeIndex}'.");
                return;
            }
        }

        Mesh[] sourceMeshes = AssetDatabase.LoadAllAssetsAtPath(FragmentModelPath).OfType<Mesh>().ToArray();
        if (sourceMeshes.Length == 0)
        {
            Debug.LogError($"[Stage3Fragments] No meshes found in Stage 2 fragment model '{FragmentModelPath}'.");
            return;
        }

        Material violetMaterial = GetOrCreateVioletMaterial();
        if (violetMaterial == null)
            return;

        GameObject existingRoot = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == FragmentRootName);
        if (existingRoot != null)
            UnityEngine.Object.DestroyImmediate(existingRoot);

        GameObject fragmentRoot = new GameObject(FragmentRootName);
        SceneManager.MoveGameObjectToScene(fragmentRoot, scene);

        Physics.SyncTransforms();

        for (int i = 0; i < nodes.Length; i++)
        {
            int nodeIndex = i + 1;
            StageNodeController node = nodes[i];
            Mesh mesh = ResolveMesh(sourceMeshes, PreferredMeshNames[i], i);

            GameObject fragment = CreateFragment(
                nodeIndex,
                node,
                mesh,
                violetMaterial,
                FragmentEulerAngles[i],
                fragmentRoot.transform);

            WireNodeToFragment(node, fragment);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[Stage3Fragments] PASS installed violet Stage 2 fragment-model shards on Stage 3 Nodes 1-3. " +
            "Enemy defeat now unlocks each shard; collecting the shard completes the node and unlocks progression.");
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Audit Violet Node Fragments")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage3ScenePath)
        {
            Debug.LogError($"[Stage3Fragments] Audit requires active scene '{Stage3ScenePath}'.");
            return;
        }

        bool pass = true;
        for (int nodeIndex = 1; nodeIndex <= 3; nodeIndex++)
        {
            GameObject nodeObject = GameObject.Find($"Node {nodeIndex}");
            GameObject fragment = GameObject.Find($"Stage3_Node{nodeIndex}_VioletFragment");

            if (nodeObject == null || fragment == null)
            {
                Debug.LogError($"[Stage3Fragments] Node {nodeIndex}: missing node or fragment.");
                pass = false;
                continue;
            }

            StageNodeController node = nodeObject.GetComponent<StageNodeController>();
            FragmentPickup pickup = fragment.GetComponent<FragmentPickup>();
            MeshRenderer renderer = fragment.GetComponent<MeshRenderer>();

            SerializedObject serializedNode = new SerializedObject(node);
            bool nodeWiringPass =
                serializedNode.FindProperty("fragmentPickup").objectReferenceValue == pickup &&
                serializedNode.FindProperty("fragmentVisual").objectReferenceValue == fragment &&
                serializedNode.FindProperty("lockFragmentAtStart").boolValue &&
                serializedNode.FindProperty("unlockFragmentOnEnemyDefeat").boolValue &&
                !serializedNode.FindProperty("completeNodeOnEnemyDefeat").boolValue &&
                serializedNode.FindProperty("hideFragmentVisualOnComplete").boolValue;

            bool pickupPass = pickup != null && pickup.pickupReceiver == node &&
                              pickup.pickupMessage == "OnFragmentCollected";

            bool centeredPass = Vector2.Distance(
                new Vector2(fragment.transform.position.x, fragment.transform.position.z),
                new Vector2(node.transform.position.x, node.transform.position.z)) < 0.05f;

            bool materialPass = renderer != null && renderer.sharedMaterial != null &&
                                AssetDatabase.GetAssetPath(renderer.sharedMaterial) == VioletMaterialPath;

            bool nodePass = nodeWiringPass && pickupPass && centeredPass && materialPass;
            pass &= nodePass;

            Debug.Log(
                $"[Stage3Fragments] Node {nodeIndex}: {(nodePass ? "PASS" : "FAIL")} " +
                $"center={centeredPass}, wiring={nodeWiringPass}, pickup={pickupPass}, violetMaterial={materialPass}, " +
                $"position={fragment.transform.position}.");
        }

        Debug.Log($"[Stage3Fragments] AUDIT {(pass ? "PASS" : "FAIL")}.");
    }

    private static GameObject CreateFragment(
        int nodeIndex,
        StageNodeController node,
        Mesh mesh,
        Material violetMaterial,
        Vector3 eulerAngles,
        Transform parent)
    {
        GameObject fragment = new GameObject($"Stage3_Node{nodeIndex}_VioletFragment");
        fragment.transform.SetParent(parent, false);

        Vector3 nodeCenter = node.transform.position;
        float platformY = FindPlatformSurfaceY(nodeCenter);
        fragment.transform.position = new Vector3(nodeCenter.x, platformY + 1.75f, nodeCenter.z);
        fragment.transform.rotation = Quaternion.Euler(eulerAngles);
        fragment.transform.localScale = Vector3.one * 3.22f;

        MeshFilter meshFilter = fragment.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = fragment.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = violetMaterial;

        SphereCollider trigger = fragment.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.center = mesh.bounds.center;
        trigger.radius = Mathf.Max(
            1.5f,
            Mathf.Max(mesh.bounds.extents.x, Mathf.Max(mesh.bounds.extents.y, mesh.bounds.extents.z)) * 1.8f);

        FragmentPickup pickup = fragment.AddComponent<FragmentPickup>();
        pickup.pickupReceiver = node;
        pickup.pickupMessage = "OnFragmentCollected";

        return fragment;
    }

    private static float FindPlatformSurfaceY(Vector3 nodeCenter)
    {
        Vector3 rayOrigin = nodeCenter + Vector3.up * 35f;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            80f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance))
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.GetComponentInParent<EnemyHealth>() != null)
                continue;

            if (hit.collider.CompareTag("Enemy"))
                continue;

            if (Mathf.Abs(hit.point.y - nodeCenter.y) <= 8f)
                return hit.point.y;
        }

        return nodeCenter.y;
    }

    private static void WireNodeToFragment(StageNodeController node, GameObject fragment)
    {
        FragmentPickup pickup = fragment.GetComponent<FragmentPickup>();
        SerializedObject serializedNode = new SerializedObject(node);

        serializedNode.FindProperty("fragmentPickup").objectReferenceValue = pickup;
        serializedNode.FindProperty("fragmentVisual").objectReferenceValue = fragment;
        serializedNode.FindProperty("lockFragmentAtStart").boolValue = true;
        serializedNode.FindProperty("unlockFragmentOnEnemyDefeat").boolValue = true;
        serializedNode.FindProperty("completeNodeOnEnemyDefeat").boolValue = false;
        serializedNode.FindProperty("hideFragmentVisualOnComplete").boolValue = true;

        // Stage 3 does not use the Stage 2 fragment counter UI.
        serializedNode.FindProperty("fragmentGoalIncrement").intValue = 0;

        serializedNode.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(node);
        EditorUtility.SetDirty(fragment);
        EditorUtility.SetDirty(pickup);
    }

    private static Mesh ResolveMesh(Mesh[] sourceMeshes, string preferredName, int fallbackIndex)
    {
        Mesh preferred = sourceMeshes.FirstOrDefault(mesh =>
            string.Equals(mesh.name, preferredName, StringComparison.Ordinal));

        if (preferred != null)
            return preferred;

        Mesh fallback = sourceMeshes[Mathf.Clamp(fallbackIndex, 0, sourceMeshes.Length - 1)];
        Debug.LogWarning(
            $"[Stage3Fragments] Mesh '{preferredName}' was not found. Using Stage 2 mesh '{fallback.name}' instead.");
        return fallback;
    }

    private static Material GetOrCreateVioletMaterial()
    {
        EnsureFolder(Stage3MaterialFolder);

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogError($"[Stage3Fragments] Missing Stage 2 source material '{SourceMaterialPath}'.");
            return null;
        }

        Material violet = AssetDatabase.LoadAssetAtPath<Material>(VioletMaterialPath);
        if (violet == null)
        {
            violet = new Material(source)
            {
                name = "Stage3_Fragment_Violet"
            };
            AssetDatabase.CreateAsset(violet, VioletMaterialPath);
        }

        Color baseViolet = new Color(0.46f, 0.12f, 0.88f, 0.34f);
        Color emissionViolet = new Color(2.6f, 0.35f, 8.6f, 1f);

        if (violet.HasProperty("_BaseColor"))
            violet.SetColor("_BaseColor", baseViolet);
        if (violet.HasProperty("_Color"))
            violet.SetColor("_Color", baseViolet);
        if (violet.HasProperty("_EmissionColor"))
            violet.SetColor("_EmissionColor", emissionViolet);

        violet.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(violet);
        return violet;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
