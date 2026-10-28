using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Makes a separate, review-only scene that uses the art working prefab with
/// the original ItemSelectionSceneController. Never saves or edits the real scene.
/// </summary>
public static class WizardsVaultPreviewSceneBuilder
{
    private const string SourceScene = "Assets/Scenes/ItemSelectionScene.unity";
    private const string PreviewScene = "Assets/Scenes/WizardsVault_UIReview.unity";
    private const string WorkingPrefab = "Assets/Prefabs/UI/WizardsVault_Layered_Working.prefab";

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/8 Reset Stale Geometry Overrides In Preview Only")]
    private static void ResetPreviewGeometryOverrides()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before normalizing preview geometry.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != PreviewScene)
            throw new InvalidOperationException("Only the isolated WizardsVault_UIReview scene is supported.");
        if (System.IO.File.Exists("Library/WizardsVault_UI/ItemSelection_PlayModeLayout.json"))
            throw new InvalidOperationException("A Play Mode layout snapshot already exists. Do not reset tuned geometry.");

        GameObject root = FindSceneObject(scene, "ItemSelectionUI_Prefab");
        GameObject source = root ? PrefabUtility.GetCorrespondingObjectFromSource(root) : null;
        string expected = AssetDatabase.GetAssetPath(source);
        if (expected != WorkingPrefab)
            throw new InvalidOperationException("Preview scene is not linked to the isolated working prefab: " + expected);

        PropertyModification[] before = PrefabUtility.GetPropertyModifications(root);
        if (before == null || before.Length == 0)
        {
            Debug.Log("[WIZARD VAULT] Preview has no prefab property overrides to normalize.");
            return;
        }
        var retained = new System.Collections.Generic.List<PropertyModification>(before.Length);
        int removed = 0;
        foreach (PropertyModification mod in before)
        {
            string path = mod.propertyPath ?? string.Empty;
            bool geometry = mod.target is RectTransform &&
                (path.StartsWith("m_AnchorMin", StringComparison.Ordinal) ||
                 path.StartsWith("m_AnchorMax", StringComparison.Ordinal) ||
                 path.StartsWith("m_AnchoredPosition", StringComparison.Ordinal) ||
                 path.StartsWith("m_SizeDelta", StringComparison.Ordinal) ||
                 path.StartsWith("m_Pivot", StringComparison.Ordinal) ||
                 path.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
                 path.StartsWith("m_LocalRotation", StringComparison.Ordinal) ||
                 path.StartsWith("m_LocalScale", StringComparison.Ordinal));
            if (geometry) removed++;
            else retained.Add(mod);
        }
        if (removed == 0)
        {
            Debug.Log("[WIZARD VAULT] No stale RectTransform geometry overrides found.");
            return;
        }
        PrefabUtility.SetPropertyModifications(root, retained.ToArray());
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save normalized isolated preview scene.");
        Debug.Log("[WIZARD VAULT] Removed " + removed +
                  " stale preview-only geometry overrides; retained " + retained.Count +
                  " non-geometry properties and controller bindings. Original scene/prefab unchanged.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/6 Create Isolated Play Preview Scene")]
    private static void CreatePreview()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before building preview.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScene))
            throw new InvalidOperationException("Preview scene already exists. Refusing to overwrite saved adjustments.");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab);
        if (!prefab)
            throw new InvalidOperationException("Isolated working prefab is missing.");
        if (!AssetDatabase.CopyAsset(SourceScene, PreviewScene))
            throw new InvalidOperationException("Could not copy the original scene into an isolated review scene.");

        Scene oldActive = SceneManager.GetActiveScene();
        Scene review = default;
        bool saved = false;
        try
        {
            // Additive mode protects a potentially dirty, unsaved production/editor scene.
            review = EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Additive);
            var oldUi = FindSceneObject(review, "ItemSelectionUI_Prefab");
            if (!oldUi || !(oldUi.transform is RectTransform previousRect))
                throw new InvalidOperationException("Original scene UI root missing in clone.");
            var parent = previousRect.parent;
            if (!parent)
                throw new InvalidOperationException("Original scene UI is not inside a Canvas.");
            int sibling = previousRect.GetSiblingIndex();

            // Create the working prefab inside the cloned scene before removing old UI.
            var instance = PrefabUtility.InstantiatePrefab(prefab, review) as GameObject;
            if (!instance || !(instance.transform is RectTransform replacementRect))
                throw new InvalidOperationException("Could not instantiate working UI in preview.");
            replacementRect.SetParent(parent, false);
            replacementRect.name = "ItemSelectionUI_Prefab";
            CopyLayout(previousRect, replacementRect);
            replacementRect.SetSiblingIndex(sibling);

            foreach (GameObject root in review.GetRootGameObjects())
            foreach (var controller in root.GetComponentsInChildren<ItemSelectionSceneController>(true))
            {
                var serialized = new SerializedObject(controller);
                var uiRef = serialized.FindProperty("prebuiltUiRoot");
                if (uiRef == null)
                    throw new InvalidOperationException("Controller.prebuiltUiRoot binding not found.");
                uiRef.objectReferenceValue = replacementRect;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // The source controller's text palette was designed for brown
                // parchment. The right inventory is now dark navy. Keep the
                // controller logic unchanged and tint this preview copy only.
                controller.textColor = new Color(.93f, .85f, .67f, 1f);
                controller.mutedTextColor = new Color(.73f, .70f, .64f, 1f);
                controller.slotColor = new Color(.09f, .18f, .29f, .98f);
                controller.selectedSlotColor = new Color(.13f, .26f, .39f, 1f);
                controller.lockedSlotColor = new Color(.11f, .20f, .31f, .97f);
                controller.goldColor = new Color(.95f, .76f, .36f, 1f);
                controller.greenColor = new Color(.42f, .85f, .57f, 1f);
                controller.redColor = new Color(.89f, .42f, .37f, 1f);
            }

            var canvas = parent.GetComponent<Canvas>();
            if (!canvas) canvas = parent.GetComponentInParent<Canvas>();
            if (!canvas)
                throw new InvalidOperationException("Review scene canvas is missing.");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2400f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (!canvas.GetComponent<WizardsVaultPreviewCanvasGuard>())
                canvas.gameObject.AddComponent<WizardsVaultPreviewCanvasGuard>();

            // Match the approved isolated UI composition's dark exterior.
            // Only mutate the cloned review scene; the real stage cameras stay
            // untouched and the panels are still separate UI sprites.
            foreach (GameObject sceneRoot in review.GetRootGameObjects())
            foreach (Camera cam in sceneRoot.GetComponentsInChildren<Camera>(true))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            // The old scene includes two large, flattened background/foreground
            // images that are not part of the approved standalone-panel design.
            // Hide them in the review clone only, never the source scene.
            foreach (Transform child in canvas.transform)
            {
                if (child == replacementRect || child.name != "Image") continue;
                var graphic = child.GetComponent<Image>();
                if (graphic)
                {
                    graphic.raycastTarget = false;
                    child.gameObject.SetActive(false);
                }
            }

            UnityEngine.Object.DestroyImmediate(oldUi);
            EditorSceneManager.MarkSceneDirty(review);
            saved = EditorSceneManager.SaveScene(review, PreviewScene, false);
            if (!saved) throw new InvalidOperationException("Review scene could not be saved.");
            Debug.Log("[WIZARD VAULT] Created separate working scene: " + PreviewScene +
                      ". Original ItemSelectionScene, its prefab, and current editor scene unchanged.");
        }
        catch
        {
            if (!saved) AssetDatabase.DeleteAsset(PreviewScene);
            throw;
        }
        finally
        {
            if (review.IsValid() && review.isLoaded)
                EditorSceneManager.CloseScene(review, true);
            if (oldActive.IsValid() && oldActive.isLoaded)
                SceneManager.SetActiveScene(oldActive);
        }
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/7 Open Isolated Play Preview Scene")]
    private static void OpenPreview()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode first.");
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScene))
            throw new InvalidOperationException("Create preview scene with menu 6 first.");
        foreach (Scene scene in GetOpenScenes())
        {
            if (scene.isDirty)
                throw new InvalidOperationException("Current scene has UNSAVED changes: " + scene.path +
                    ". Save it manually before switching to the isolated preview; nothing was discarded.");
        }
        EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Single);
    }

    private static System.Collections.Generic.IEnumerable<Scene> GetOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            yield return SceneManager.GetSceneAt(i);
    }

    private static GameObject FindSceneObject(Scene scene, string exactName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursively(root.transform, exactName);
            if (found) return found.gameObject;
        }
        return null;
    }

    private static Transform FindRecursively(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindRecursively(child, name);
            if (found) return found;
        }
        return null;
    }

    private static void CopyLayout(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }
}
