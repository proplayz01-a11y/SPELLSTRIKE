#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class Stage2CanonicalPlayerReplaceOnce
{
    private const string ScenePath = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string PrefabPath = "Assets/Prefabs/VanAertPlayer.prefab";

    [MenuItem("SPELLSTRIKE/Stage 2/Replace Legacy Player With Canonical VanAertPlayer")]
    public static void RunFromMenu()
    {
        ReplacePlayer();
    }

    private static void ReplacePlayer()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[Stage2 Player Replace] Canonical VanAertPlayer prefab missing."); return; }

        GameObject oldPlayer = GameObject.Find("VAN AERT");
        if (oldPlayer == null) { Debug.LogError("[Stage2 Player Replace] Legacy VAN AERT player not found."); return; }

        Transform oldTransform = oldPlayer.transform;
        Transform parent = oldTransform.parent;
        Vector3 localPosition = oldTransform.localPosition;
        Quaternion localRotation = oldTransform.localRotation;
        Vector3 localScale = oldTransform.localScale;

        GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        replacement.name = "VAN AERT";
        replacement.transform.SetParent(parent, false);
        replacement.transform.localPosition = localPosition;
        replacement.transform.localRotation = localRotation;
        replacement.transform.localScale = localScale;

        Dictionary<UnityEngine.Object, UnityEngine.Object> replacementMap = BuildReplacementMap(oldPlayer, replacement);

        // Carry over Stage 2 scene bindings (UI, TileManager, etc.) while keeping
        // canonical prefab-authored values for everything else.
        foreach (KeyValuePair<UnityEngine.Object, UnityEngine.Object> pair in replacementMap)
        {
            if (!(pair.Key is Component oldComponent) || !(pair.Value is Component newComponent))
                continue;

            CopySceneObjectReferences(oldComponent, newComponent, scene, replacementMap);
        }

        // Preserve any scene references that pointed at the old player, its children,
        // or any of their components.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.transform.IsChildOf(oldPlayer.transform)) continue;

                SerializedObject so = new SerializedObject(component);
                SerializedProperty prop = so.GetIterator();
                bool enterChildren = true;
                bool changed = false;
                while (prop.Next(enterChildren))
                {
                    enterChildren = true;
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object oldReference = prop.objectReferenceValue;
                    if (oldReference != null && replacementMap.TryGetValue(oldReference, out Object newReference))
                    {
                        prop.objectReferenceValue = newReference;
                        changed = true;
                    }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Object.DestroyImmediate(oldPlayer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Stage2 Player Replace] COMPLETE | canonical VanAertPlayer installed; Stage2 transform preserved.");
    }

    private static Dictionary<UnityEngine.Object, UnityEngine.Object> BuildReplacementMap(GameObject oldRoot, GameObject newRoot)
    {
        Dictionary<UnityEngine.Object, UnityEngine.Object> map = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
        map[oldRoot] = newRoot;
        map[oldRoot.transform] = newRoot.transform;

        Dictionary<string, Transform> newByPath = new Dictionary<string, Transform>(StringComparer.Ordinal);
        Dictionary<string, List<Transform>> newByName = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);

        foreach (Transform t in newRoot.GetComponentsInChildren<Transform>(true))
        {
            string path = GetRelativePath(newRoot.transform, t);
            newByPath[path] = t;

            if (!newByName.TryGetValue(t.name, out List<Transform> list))
            {
                list = new List<Transform>();
                newByName[t.name] = list;
            }
            list.Add(t);
        }

        foreach (Transform oldTransform in oldRoot.GetComponentsInChildren<Transform>(true))
        {
            Transform newTransform = null;
            string path = GetRelativePath(oldRoot.transform, oldTransform);

            if (!newByPath.TryGetValue(path, out newTransform) &&
                newByName.TryGetValue(oldTransform.name, out List<Transform> sameName) &&
                sameName.Count == 1)
            {
                newTransform = sameName[0];
            }

            if (newTransform == null) continue;

            map[oldTransform] = newTransform;
            map[oldTransform.gameObject] = newTransform.gameObject;
            MapComponentsByType(oldTransform.gameObject, newTransform.gameObject, map);
        }

        return map;
    }

    private static void MapComponentsByType(
        GameObject oldObject,
        GameObject newObject,
        Dictionary<UnityEngine.Object, UnityEngine.Object> map)
    {
        Component[] oldComponents = oldObject.GetComponents<Component>();
        Component[] newComponents = newObject.GetComponents<Component>();
        Dictionary<Type, int> seen = new Dictionary<Type, int>();

        foreach (Component oldComponent in oldComponents)
        {
            if (oldComponent == null || oldComponent is Transform) continue;

            Type type = oldComponent.GetType();
            seen.TryGetValue(type, out int index);

            int current = 0;
            Component match = null;
            foreach (Component candidate in newComponents)
            {
                if (candidate == null || candidate.GetType() != type) continue;
                if (current == index) { match = candidate; break; }
                current++;
            }

            seen[type] = index + 1;
            if (match != null) map[oldComponent] = match;
        }
    }

    private static void CopySceneObjectReferences(
        Component oldComponent,
        Component newComponent,
        Scene scene,
        Dictionary<UnityEngine.Object, UnityEngine.Object> replacementMap)
    {
        SerializedObject oldSo = new SerializedObject(oldComponent);
        SerializedObject newSo = new SerializedObject(newComponent);
        SerializedProperty oldProp = oldSo.GetIterator();
        bool enterChildren = true;
        bool changed = false;

        while (oldProp.Next(enterChildren))
        {
            enterChildren = true;
            if (oldProp.propertyType != SerializedPropertyType.ObjectReference) continue;

            Object oldReference = oldProp.objectReferenceValue;
            if (oldReference == null) continue;

            Object desiredReference = null;
            if (replacementMap.TryGetValue(oldReference, out Object mappedReference))
            {
                desiredReference = mappedReference;
            }
            else if (!EditorUtility.IsPersistent(oldReference) && IsObjectInScene(oldReference, scene))
            {
                desiredReference = oldReference;
            }

            if (desiredReference == null) continue;

            SerializedProperty newProp = newSo.FindProperty(oldProp.propertyPath);
            if (newProp == null || newProp.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (newProp.objectReferenceValue == desiredReference) continue;

            newProp.objectReferenceValue = desiredReference;
            changed = true;
        }

        if (changed) newSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool IsObjectInScene(Object obj, Scene scene)
    {
        if (obj is GameObject go) return go.scene == scene;
        if (obj is Component component) return component.gameObject.scene == scene;
        return false;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (target == root) return string.Empty;

        Stack<string> names = new Stack<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names.ToArray());
    }
}
#endif
