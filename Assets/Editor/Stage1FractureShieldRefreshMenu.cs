#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1FractureShieldRefreshMenu
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Refresh Node 2 FRACTURE Shield Instances %#9";
    private const string ShieldAssetPath =
        "Assets/Characters/STONE-SENTINEL-SHIELD.glb";
    private const string GroundShieldName = "STONE-SENTINEL-SHIELD";
    private const string HeldShieldName = "FRACTURE_HeldShield";

    [MenuItem(MenuPath)]
    public static void RefreshShieldInstances()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject shieldAsset =
            AssetDatabase.LoadAssetAtPath<GameObject>(ShieldAssetPath);
        StoneSentinelCounterspellController counterspell =
            FindSceneComponent<StoneSentinelCounterspellController>(scene);
        StoneSentinelsController sentinel =
            FindSceneComponent<StoneSentinelsController>(scene);
        GameObject node2 = FindSceneObject(scene, "NODE 2");

        if (!scene.IsValid()
            || !scene.isLoaded
            || shieldAsset == null
            || counterspell == null
            || sentinel == null
            || node2 == null)
        {
            Debug.LogError(
                "[FRACTURE Shield Refresh] Open SampleScene and confirm that "
                + "the updated shield GLB and Counterspell controller exist.");
            return;
        }

        SerializedObject counterspellObject = new SerializedObject(counterspell);
        GameObject oldGround =
            counterspellObject.FindProperty("groundShield").objectReferenceValue
                as GameObject;
        GameObject oldHeld =
            counterspellObject.FindProperty("heldShield").objectReferenceValue
                as GameObject;

        oldGround ??= FindSceneObject(scene, GroundShieldName);
        oldHeld ??= FindSceneObject(scene, HeldShieldName);

        Transform leftHand = FindRecursive(sentinel.transform, "mixamorig:LeftHand");
        if (leftHand == null)
        {
            Debug.LogError(
                "[FRACTURE Shield Refresh] The Stone Sentinel left-hand bone "
                + "'mixamorig:LeftHand' was not found.",
                counterspell);
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refresh Node 2 FRACTURE shield instances");

        try
        {
            TransformSnapshot groundTransform = oldGround != null
                ? TransformSnapshot.Capture(oldGround.transform)
                : TransformSnapshot.Create(
                    node2.transform,
                    new Vector3(-969.2f, -2f, -754.8f),
                    Quaternion.Euler(-29.9f, 17.06f, 0f),
                    Vector3.one * 10.29f,
                    node2.transform.childCount);
            TransformSnapshot heldTransform = oldHeld != null
                ? TransformSnapshot.Capture(oldHeld.transform)
                : TransformSnapshot.Create(
                    leftHand,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one * 10.29f,
                    leftHand.childCount);

            if (oldGround != null)
                RenameAsDisabledBackup(oldGround, GroundShieldName);
            if (oldHeld != null)
                RenameAsDisabledBackup(oldHeld, HeldShieldName);

            GameObject newGround = InstantiateShield(
                shieldAsset,
                groundTransform,
                GroundShieldName);
            GameObject newHeld = InstantiateShield(
                shieldAsset,
                heldTransform,
                HeldShieldName);

            DisableHeldShieldPhysics(newHeld);
            newGround.SetActive(false);
            newHeld.SetActive(false);

            counterspellObject.Update();
            counterspellObject.FindProperty("groundShield").objectReferenceValue =
                newGround;
            counterspellObject.FindProperty("heldShield").objectReferenceValue =
                newHeld;
            counterspellObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(newGround);
            EditorUtility.SetDirty(newHeld);
            EditorUtility.SetDirty(counterspell);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = newHeld;
            EditorGUIUtility.PingObject(newHeld);

            Debug.Log(
                "[FRACTURE Shield Refresh] Updated shield instances created, "
                + "Counterspell references rewired, and both new shields saved "
                + "disabled. Previous instances remain disabled as backups.",
                counterspell);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[FRACTURE Shield Refresh] Refresh failed: {exception.Message}");
            Undo.RevertAllDownToGroup(undoGroup);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static GameObject InstantiateShield(
        GameObject shieldAsset,
        TransformSnapshot snapshot,
        string objectName)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(
            shieldAsset,
            snapshot.Parent.gameObject.scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException(
                $"Could not instantiate '{ShieldAssetPath}'.");

        Undo.RegisterCreatedObjectUndo(instance, $"Create {objectName}");
        Undo.SetTransformParent(instance.transform, snapshot.Parent, $"Parent {objectName}");
        instance.name = objectName;
        snapshot.ApplyTo(instance.transform);
        return instance;
    }

    private static void RenameAsDisabledBackup(
        GameObject target,
        string originalName)
    {
        Undo.RecordObject(target, $"Preserve old {originalName}");
        target.name = $"{originalName}_LEGACY_DISABLED";
        target.SetActive(false);
        EditorUtility.SetDirty(target);
    }

    private static void DisableHeldShieldPhysics(GameObject heldShield)
    {
        foreach (Collider collider in heldShield.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable held-shield collider");
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }

        foreach (Rigidbody rigidbody in heldShield.GetComponentsInChildren<Rigidbody>(true))
        {
            Undo.RecordObject(rigidbody, "Configure held-shield Rigidbody");
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            EditorUtility.SetDirty(rigidbody);
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private readonly struct TransformSnapshot
    {
        public Transform Parent { get; }
        private Vector3 LocalPosition { get; }
        private Quaternion LocalRotation { get; }
        private Vector3 LocalScale { get; }
        private int SiblingIndex { get; }

        private TransformSnapshot(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            int siblingIndex)
        {
            Parent = parent;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            SiblingIndex = siblingIndex;
        }

        public static TransformSnapshot Capture(Transform source)
        {
            if (source.parent == null)
                throw new InvalidOperationException(
                    $"'{source.name}' must have a parent before it can be refreshed.");

            return new TransformSnapshot(
                source.parent,
                source.localPosition,
                source.localRotation,
                source.localScale,
                source.GetSiblingIndex());
        }

        public static TransformSnapshot Create(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            int siblingIndex)
        {
            return new TransformSnapshot(
                parent,
                localPosition,
                localRotation,
                localScale,
                siblingIndex);
        }

        public void ApplyTo(Transform target)
        {
            target.localPosition = LocalPosition;
            target.localRotation = LocalRotation;
            target.localScale = LocalScale;
            target.SetSiblingIndex(SiblingIndex);
        }
    }
}
#endif
