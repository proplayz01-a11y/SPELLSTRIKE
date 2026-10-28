using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Transfers manual Play Mode layout adjustments into the ISOLATED working
/// prefab. The production scene and original prefab are never saved or edited.
/// </summary>
public static class WizardsVaultPlayModeLayoutSnapshot
{
    private const string ScenePath = "Assets/Scenes/ItemSelectionScene.unity";
    private const string ReviewScenePath = "Assets/Scenes/WizardsVault_UIReview.unity";
    private const string OriginalPrefab = "Assets/Prefabs/UI/ItemSelectionUI.prefab";
    private const string WorkingPrefab = "Assets/Prefabs/UI/WizardsVault_Layered_Working.prefab";
    private const string SnapshotPath = "Library/WizardsVault_UI/ItemSelection_PlayModeLayout.json";
    private const string ProductionRootName = "ItemSelectionUI_Prefab";
    private const string PreviewRootName = "ItemSelectionUI_WorkingPreview";
    private const int SnapshotVersion = 2;

    [Serializable]
    private class Snapshot
    {
        public int version;
        public string scene;
        public string rootName;
        public string prefabGuid;
        public string savedAtUtc;
        public int sourceScreenWidth;
        public int sourceScreenHeight;
        public Vector2 referenceResolution;
        public List<Node> nodes = new List<Node>();
    }

    [Serializable]
    private class Node
    {
        public string path;
        public int siblingIndex;
        public Vector2 anchorMin, anchorMax, pivot;
        public Vector3 anchoredPosition3D, localScale;
        public Vector2 sizeDelta;
        public Quaternion localRotation;

        public bool hasText;
        public float fontSize, fontSizeMin, fontSizeMax;
        public bool enableAutoSizing;
        public int fontStyle, alignment, wrapping, overflow;
        public float characterSpacing, wordSpacing, lineSpacing, paragraphSpacing;
        public Vector4 textMargin;
        public bool captureTextColor;
        public Color textColor;

        public bool hasLayoutElement;
        public bool layoutElementEnabled;
        public bool ignoreLayout;
        public float preferredWidth, preferredHeight, minWidth, minHeight;
        public float flexibleWidth, flexibleHeight;
        public int layoutPriority;

        // 0 = none, 1 = horizontal, 2 = vertical, 3 = grid.
        public int layoutKind;
        public bool layoutEnabled;
        public int padLeft, padRight, padTop, padBottom, childAlignment;
        public float spacing;
        public bool childControlWidth, childControlHeight;
        public bool childForceExpandWidth, childForceExpandHeight;
        public bool childScaleWidth, childScaleHeight;
        public Vector2 gridSpacing, gridCellSize;
        public int gridStartCorner, gridStartAxis, gridConstraint, gridConstraintCount;

        public bool hasContentSizeFitter;
        public bool contentSizeFitterEnabled;
        public int horizontalFit, verticalFit;
        public bool hasAspectRatioFitter;
        public bool aspectRatioFitterEnabled;
        public int aspectMode;
        public float aspectRatio;

        // Static image styling is captured; live button state remains dynamic.
        public bool hasStaticImage;
        public Color imageColor;
        public bool imagePreserveAspect;
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/0 Preview Isolated Working Prefab In Play Mode")]
    private static void PreviewWorkingPrefab()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Enter Play Mode in ItemSelectionScene first.");

        GameObject workingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab);
        if (!workingAsset)
            throw new InvalidOperationException("The isolated working prefab is missing.");

        GameObject production = GameObject.Find(ProductionRootName);
        if (!production || production.scene.path != ScenePath)
            throw new InvalidOperationException("Play ItemSelectionScene to preview the isolated UI.");

        Transform canvasTransform = production.transform.parent;
        Canvas canvas = production.GetComponentInParent<Canvas>();
        if (!canvas || canvasTransform == null)
            throw new InvalidOperationException("Production UI has no Canvas parent.");

        if (canvasTransform.Find(PreviewRootName))
            throw new InvalidOperationException(
                "A working preview already exists. Keep adjusting that one, then capture it.");

        ItemSelectionSceneController controller = canvas.GetComponent<ItemSelectionSceneController>();
        if (!controller || !controller.isActiveAndEnabled)
            throw new InvalidOperationException("The ItemSelectionSceneController must be active.");

        RectTransform originalRoot = production.GetComponent<RectTransform>();
        if (!originalRoot)
            throw new InvalidOperationException("Production UI must have a RectTransform.");

        GameObject preview = null;
        try
        {
            preview = (GameObject)PrefabUtility.InstantiatePrefab(workingAsset, canvasTransform);
            if (!preview)
                throw new InvalidOperationException("Could not instantiate isolated UI prefab.");

            preview.name = PreviewRootName;
            preview.transform.SetSiblingIndex(production.transform.GetSiblingIndex());
            // The controller's Start has already run by the time the user selects
            // this Play Mode menu. Refresh its private cached UI references through
            // the existing binding methods, without touching gameplay scripts.
            production.SetActive(false);
            controller.prebuiltUiRoot = preview.GetComponent<RectTransform>();
            if (!controller.prebuiltUiRoot)
                throw new InvalidOperationException("Preview prefab has no RectTransform root.");
            controller.SendMessage("BindOrBuildUI", SendMessageOptions.RequireReceiver);
            controller.SendMessage("RefreshUI", SendMessageOptions.RequireReceiver);
        }
        catch
        {
            production.SetActive(true);
            controller.prebuiltUiRoot = originalRoot;
            try
            {
                controller.SendMessage("BindOrBuildUI", SendMessageOptions.DontRequireReceiver);
                controller.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception restoreError)
            {
                Debug.LogWarning("[WIZARD VAULT SNAPSHOT] Could not fully rebind original UI: " +
                                 restoreError.Message);
            }
            if (preview)
                UnityEngine.Object.DestroyImmediate(preview);
            throw;
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        Debug.Log("[WIZARD VAULT SNAPSHOT] Working prefab shown ONLY in Play Mode. " +
                  "Adjust '" + PreviewRootName + "' in the Hierarchy, then run menu 1 " +
                  "before stopping. Canvas reference resolution: " +
                  (scaler ? scaler.referenceResolution.ToString() : "none") +
                  "; decorative layer coordinates are authored in a 2400x1080 frame. " +
                  "Stopping Play restores the scene automatically.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/1 Capture Layout From Play Mode")]
    private static void CaptureFromPlay()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Enter Play Mode, preview menu 0, adjust UI, then capture.");

        GameObject workingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab);
        if (!workingAsset)
            throw new InvalidOperationException("Isolated working prefab not found.");
        GameObject root = FindWorkingCaptureRoot(workingAsset);

        var canvas = root.GetComponentInParent<Canvas>();
        var scaler = canvas ? canvas.GetComponent<CanvasScaler>() : null;
        var snapshot = new Snapshot
        {
            version = SnapshotVersion,
            scene = root.scene.path,
            rootName = root.name,
            prefabGuid = AssetDatabase.AssetPathToGUID(WorkingPrefab),
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            sourceScreenWidth = Screen.width,
            sourceScreenHeight = Screen.height,
            referenceResolution = scaler ? scaler.referenceResolution : Vector2.zero
        };

        // Only record objects actually present in the working prefab. The scene
        // controller may have generated runtime rows, which must not block apply.
        var template = MapRects(workingAsset.transform);
        var current = MapRects(root.transform);
        int ignoredRuntimeObjects = 0;
        foreach (var pair in current)
        {
            if (!template.ContainsKey(pair.Key))
            {
                ignoredRuntimeObjects++;
                continue;
            }
            snapshot.nodes.Add(CaptureNode(pair.Key, pair.Value));
        }

        foreach (string path in template.Keys)
            if (!current.ContainsKey(path))
                throw new InvalidOperationException(
                    "Working preview is missing prefab UI node '" + path +
                    "'. No layout captured; restore the node and try again.");

        snapshot.nodes.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
        string pathOnDisk = Path.GetFullPath(SnapshotPath);
        Directory.CreateDirectory(Path.GetDirectoryName(pathOnDisk));
        // Saved ONLY under Library while playing; prefab/scene never saved here.
        File.WriteAllText(pathOnDisk, JsonUtility.ToJson(snapshot, true));
        Debug.Log("[WIZARD VAULT SNAPSHOT] Captured " + snapshot.nodes.Count +
                  " isolated UI RectTransforms (ignored " + ignoredRuntimeObjects +
                  " runtime-only children), " + snapshot.sourceScreenWidth + "x" +
                  snapshot.sourceScreenHeight + " Game view. Snapshot stored under Library. " +
                  "STOP Play Mode, then use menu 3 to apply.");
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/2 Create Isolated Working Prefab")]
    private static void CreateWorkingPrefab()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab))
            throw new InvalidOperationException("The isolated working prefab exists. Refusing overwrite.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPrefab))
            throw new InvalidOperationException("Original prefab missing.");
        if (!AssetDatabase.CopyAsset(OriginalPrefab, WorkingPrefab))
            throw new InvalidOperationException("Could not duplicate the UI prefab.");
        AssetDatabase.ImportAsset(WorkingPrefab);
        Debug.Log("[WIZARD VAULT SNAPSHOT] Created isolated working copy: " + WorkingPrefab);
    }

    [MenuItem("SPELLSTRIKE/UI/Wizard Vault/3 Apply Snapshot To Working Prefab")]
    private static void ApplyToWorkingPrefab()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before applying the captured layout.");

        string pathOnDisk = Path.GetFullPath(SnapshotPath);
        if (!File.Exists(pathOnDisk))
            throw new FileNotFoundException("Capture the preview layout during Play Mode first.", pathOnDisk);
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(WorkingPrefab))
            throw new InvalidOperationException("The isolated working prefab is missing.");

        Snapshot snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(pathOnDisk));
        if (snapshot == null || snapshot.nodes == null || snapshot.nodes.Count == 0 ||
            snapshot.version != SnapshotVersion ||
            !((snapshot.scene == ScenePath && snapshot.rootName == PreviewRootName) ||
              (snapshot.scene == ReviewScenePath && !string.IsNullOrEmpty(snapshot.rootName))) ||
            snapshot.prefabGuid != AssetDatabase.AssetPathToGUID(WorkingPrefab))
            throw new InvalidOperationException(
                "Snapshot is stale, from the wrong prefab, or incomplete. Capture menu 1 again.");

        GameObject prefab = PrefabUtility.LoadPrefabContents(WorkingPrefab);
        try
        {
            Dictionary<string, RectTransform> map = MapRects(prefab.transform);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            // Validate all paths AND component structures before mutating anything.
            foreach (Node node in snapshot.nodes)
            {
                if (string.IsNullOrEmpty(node.path) || !seen.Add(node.path) ||
                    !map.TryGetValue(node.path, out RectTransform rect))
                    throw new InvalidOperationException(
                        "UI hierarchy changed (path: '" + node.path + "'). No prefab saved.");
                ValidateComponents(node, rect);
            }
            if (seen.Count != map.Count)
                throw new InvalidOperationException(
                    "Working prefab structure changed since capture. No prefab saved.");

            foreach (Node node in snapshot.nodes)
                ApplyNode(node, map[node.path]);

            // Restore sibling order only after layout values, preserving the
            // foreground artwork layering the user arranged during Play Mode.
            snapshot.nodes.Sort((a, b) =>
            {
                int depth = PathDepth(a.path).CompareTo(PathDepth(b.path));
                if (depth != 0) return depth;
                int parent = string.CompareOrdinal(ParentPath(a.path), ParentPath(b.path));
                return parent != 0 ? parent : a.siblingIndex.CompareTo(b.siblingIndex);
            });
            foreach (Node node in snapshot.nodes)
                if (node.path != ".")
                    map[node.path].SetSiblingIndex(node.siblingIndex);

            if (!PrefabUtility.SaveAsPrefabAsset(prefab, WorkingPrefab))
                throw new InvalidOperationException("Could not save isolated working prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        Debug.Log("[WIZARD VAULT SNAPSHOT] Applied " + snapshot.nodes.Count +
                  " adjusted UI RectTransforms into ISOLATED " + WorkingPrefab +
                  ". Game-view " + snapshot.sourceScreenWidth + "x" +
                  snapshot.sourceScreenHeight + ", CanvasScaler reference " +
                  snapshot.referenceResolution + ". The scene CanvasScaler is " +
                  "scene-owned and unchanged. Original scene/prefab untouched.");
    }

    private static Node CaptureNode(string path, RectTransform rect)
    {
        var node = new Node
        {
            path = path,
            siblingIndex = rect.GetSiblingIndex(),
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            anchoredPosition3D = rect.anchoredPosition3D,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localRotation = rect.localRotation
        };

        TextMeshProUGUI tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp)
        {
            node.hasText = true;
            node.fontSize = tmp.fontSize;
            node.fontSizeMin = tmp.fontSizeMin;
            node.fontSizeMax = tmp.fontSizeMax;
            node.enableAutoSizing = tmp.enableAutoSizing;
            node.fontStyle = (int)tmp.fontStyle;
            node.alignment = (int)tmp.alignment;
            node.wrapping = (int)tmp.textWrappingMode;
            node.overflow = (int)tmp.overflowMode;
            node.characterSpacing = tmp.characterSpacing;
            node.wordSpacing = tmp.wordSpacing;
            node.lineSpacing = tmp.lineSpacing;
            node.paragraphSpacing = tmp.paragraphSpacing;
            node.textMargin = tmp.margin;
            node.captureTextColor = !IsDynamicText(path);
            if (node.captureTextColor) node.textColor = tmp.color;
            // Intentionally never capture TMP.text: counters, status, equipped
            // slots and stage names come from live gameplay bindings.
        }

        LayoutElement element = rect.GetComponent<LayoutElement>();
        if (element)
        {
            node.hasLayoutElement = true;
            node.layoutElementEnabled = element.enabled;
            node.ignoreLayout = element.ignoreLayout;
            node.minWidth = element.minWidth;
            node.minHeight = element.minHeight;
            node.preferredWidth = element.preferredWidth;
            node.preferredHeight = element.preferredHeight;
            node.flexibleWidth = element.flexibleWidth;
            node.flexibleHeight = element.flexibleHeight;
            node.layoutPriority = element.layoutPriority;
        }

        LayoutGroup layout = rect.GetComponent<LayoutGroup>();
        if (layout)
        {
            node.layoutEnabled = layout.enabled;
            node.padLeft = layout.padding.left;
            node.padRight = layout.padding.right;
            node.padTop = layout.padding.top;
            node.padBottom = layout.padding.bottom;
            node.childAlignment = (int)layout.childAlignment;
            var horizontal = layout as HorizontalLayoutGroup;
            var vertical = layout as VerticalLayoutGroup;
            var horizontalOrVertical = layout as HorizontalOrVerticalLayoutGroup;
            var grid = layout as GridLayoutGroup;
            if (horizontal != null || vertical != null)
            {
                node.layoutKind = horizontal != null ? 1 : 2;
                node.spacing = horizontalOrVertical.spacing;
                node.childControlWidth = horizontalOrVertical.childControlWidth;
                node.childControlHeight = horizontalOrVertical.childControlHeight;
                node.childForceExpandWidth = horizontalOrVertical.childForceExpandWidth;
                node.childForceExpandHeight = horizontalOrVertical.childForceExpandHeight;
                node.childScaleWidth = horizontalOrVertical.childScaleWidth;
                node.childScaleHeight = horizontalOrVertical.childScaleHeight;
            }
            else if (grid)
            {
                node.layoutKind = 3;
                node.gridSpacing = grid.spacing;
                node.gridCellSize = grid.cellSize;
                node.gridStartCorner = (int)grid.startCorner;
                node.gridStartAxis = (int)grid.startAxis;
                node.gridConstraint = (int)grid.constraint;
                node.gridConstraintCount = grid.constraintCount;
            }
        }

        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter)
        {
            node.hasContentSizeFitter = true;
            node.contentSizeFitterEnabled = fitter.enabled;
            node.horizontalFit = (int)fitter.horizontalFit;
            node.verticalFit = (int)fitter.verticalFit;
        }
        AspectRatioFitter aspect = rect.GetComponent<AspectRatioFitter>();
        if (aspect)
        {
            node.hasAspectRatioFitter = true;
            node.aspectRatioFitterEnabled = aspect.enabled;
            node.aspectMode = (int)aspect.aspectMode;
            node.aspectRatio = aspect.aspectRatio;
        }

        // Skip Image colors driven by Button transitions or equipment status.
        // Other panels and artwork can be styled directly in Play Mode.
        Image image = rect.GetComponent<Image>();
        if (image && !rect.GetComponent<Button>() &&
            !path.Contains("/PassiveList/PassiveListViewport/PassiveListContent/"))
        {
            node.hasStaticImage = true;
            node.imageColor = image.color;
            node.imagePreserveAspect = image.preserveAspect;
        }
        return node;
    }

    private static GameObject FindWorkingCaptureRoot(GameObject workingAsset)
    {
        string activePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (activePath != ScenePath && activePath != ReviewScenePath)
            throw new InvalidOperationException(
                "Capture only in ItemSelectionScene or the isolated WizardsVault_UIReview scene.");

        GameObject match = null;
        foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (!rect || !rect.gameObject.activeInHierarchy ||
                rect.gameObject.scene.path != activePath)
                continue;

            // Prefab source equality identifies the prefab ROOT and excludes
            // nested UI children or any similarly named production instance.
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(rect.gameObject);
            if (source != workingAsset ||
                (activePath == ScenePath && rect.name != PreviewRootName))
                continue;

            if (match)
                throw new InvalidOperationException(
                    "Multiple isolated working UI instances exist. Keep one preview before capture.");
            match = rect.gameObject;
        }

        // The standalone review scene was created from an unpacked clone of
        // the ISOLATED working prefab, not a linked prefab instance. In Play
        // Mode this clone has no GetCorrespondingObjectFromSource reference.
        // Permit only that exact known review-scene root, and only if every
        // RectTransform path matches the working prefab BEFORE capturing.
        // The real ItemSelectionScene never uses this fallback.
        if (!match && activePath == ReviewScenePath)
        {
            GameObject candidate = GameObject.Find(ProductionRootName);
            if (candidate && candidate.activeInHierarchy &&
                candidate.scene.path == ReviewScenePath &&
                candidate.GetComponentInParent<Canvas>() &&
                candidate.GetComponent<RectTransform>())
            {
                Dictionary<string, RectTransform> working = MapRects(workingAsset.transform);
                Dictionary<string, RectTransform> live = MapRects(candidate.transform);
                if (working.Count == live.Count)
                {
                    bool identicalPaths = true;
                    foreach (string path in working.Keys)
                        if (!live.ContainsKey(path)) { identicalPaths = false; break; }
                    if (identicalPaths) match = candidate;
                }
            }
        }

        if (!match)
            throw new InvalidOperationException(activePath == ScenePath
                ? "Run menu 0 in ItemSelectionScene first, then adjust and capture the working preview."
                : "Review UI hierarchy differs from isolated working prefab. No snapshot captured.");

        return match;
    }

    private static void ValidateComponents(Node node, RectTransform rect)
    {
        if (node.hasText != (rect.GetComponent<TextMeshProUGUI>() != null) ||
            node.hasLayoutElement != (rect.GetComponent<LayoutElement>() != null) ||
            node.hasContentSizeFitter != (rect.GetComponent<ContentSizeFitter>() != null) ||
            node.hasAspectRatioFitter != (rect.GetComponent<AspectRatioFitter>() != null) ||
            node.layoutKind != GetLayoutKind(rect.GetComponent<LayoutGroup>()) ||
            (node.hasStaticImage && !rect.GetComponent<Image>()))
            throw new InvalidOperationException(
                "Component structure changed at '" + node.path + "'. No prefab saved.");
    }

    private static int GetLayoutKind(LayoutGroup layout)
    {
        if (layout is HorizontalLayoutGroup) return 1;
        if (layout is VerticalLayoutGroup) return 2;
        if (layout is GridLayoutGroup) return 3;
        return 0;
    }

    private static void ApplyNode(Node node, RectTransform rect)
    {
        rect.anchorMin = node.anchorMin;
        rect.anchorMax = node.anchorMax;
        rect.pivot = node.pivot;
        rect.sizeDelta = node.sizeDelta;
        rect.anchoredPosition3D = node.anchoredPosition3D;
        rect.localScale = node.localScale;
        rect.localRotation = node.localRotation;

        if (node.hasText)
        {
            TextMeshProUGUI tmp = rect.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = node.fontSize;
            tmp.fontSizeMin = node.fontSizeMin;
            tmp.fontSizeMax = node.fontSizeMax;
            tmp.enableAutoSizing = node.enableAutoSizing;
            tmp.fontStyle = (FontStyles)node.fontStyle;
            tmp.alignment = (TextAlignmentOptions)node.alignment;
            tmp.textWrappingMode = (TextWrappingModes)node.wrapping;
            tmp.overflowMode = (TextOverflowModes)node.overflow;
            tmp.characterSpacing = node.characterSpacing;
            tmp.wordSpacing = node.wordSpacing;
            tmp.lineSpacing = node.lineSpacing;
            tmp.paragraphSpacing = node.paragraphSpacing;
            tmp.margin = node.textMargin;
            if (node.captureTextColor) tmp.color = node.textColor;
        }

        if (node.hasLayoutElement)
        {
            LayoutElement element = rect.GetComponent<LayoutElement>();
            element.enabled = node.layoutElementEnabled;
            element.ignoreLayout = node.ignoreLayout;
            element.minWidth = node.minWidth;
            element.minHeight = node.minHeight;
            element.preferredWidth = node.preferredWidth;
            element.preferredHeight = node.preferredHeight;
            element.flexibleWidth = node.flexibleWidth;
            element.flexibleHeight = node.flexibleHeight;
            element.layoutPriority = node.layoutPriority;
        }

        if (node.layoutKind != 0)
        {
            LayoutGroup layout = rect.GetComponent<LayoutGroup>();
            layout.enabled = node.layoutEnabled;
            layout.padding = new RectOffset(
                node.padLeft, node.padRight, node.padTop, node.padBottom);
            layout.childAlignment = (TextAnchor)node.childAlignment;
            var horizontalOrVertical = layout as HorizontalOrVerticalLayoutGroup;
            if (horizontalOrVertical)
            {
                horizontalOrVertical.spacing = node.spacing;
                horizontalOrVertical.childControlWidth = node.childControlWidth;
                horizontalOrVertical.childControlHeight = node.childControlHeight;
                horizontalOrVertical.childForceExpandWidth = node.childForceExpandWidth;
                horizontalOrVertical.childForceExpandHeight = node.childForceExpandHeight;
                horizontalOrVertical.childScaleWidth = node.childScaleWidth;
                horizontalOrVertical.childScaleHeight = node.childScaleHeight;
            }
            var grid = layout as GridLayoutGroup;
            if (grid)
            {
                grid.spacing = node.gridSpacing;
                grid.cellSize = node.gridCellSize;
                grid.startCorner = (GridLayoutGroup.Corner)node.gridStartCorner;
                grid.startAxis = (GridLayoutGroup.Axis)node.gridStartAxis;
                grid.constraint = (GridLayoutGroup.Constraint)node.gridConstraint;
                grid.constraintCount = node.gridConstraintCount;
            }
        }

        if (node.hasContentSizeFitter)
        {
            ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
            fitter.enabled = node.contentSizeFitterEnabled;
            fitter.horizontalFit = (ContentSizeFitter.FitMode)node.horizontalFit;
            fitter.verticalFit = (ContentSizeFitter.FitMode)node.verticalFit;
        }
        if (node.hasAspectRatioFitter)
        {
            AspectRatioFitter aspect = rect.GetComponent<AspectRatioFitter>();
            aspect.enabled = node.aspectRatioFitterEnabled;
            aspect.aspectMode = (AspectRatioFitter.AspectMode)node.aspectMode;
            aspect.aspectRatio = node.aspectRatio;
        }
        if (node.hasStaticImage)
        {
            Image image = rect.GetComponent<Image>();
            image.color = node.imageColor;
            image.preserveAspect = node.imagePreserveAspect;
        }
    }

    private static bool IsDynamicText(string path)
    {
        return path.EndsWith("/StageTitle", StringComparison.Ordinal) ||
               path.EndsWith("/PotionCounts", StringComparison.Ordinal) ||
               path.Contains("/EquippedSlot") ||
               (path.Contains("/PassiveList/") &&
                (path.EndsWith("/Title", StringComparison.Ordinal) ||
                 path.EndsWith("/Body", StringComparison.Ordinal) ||
                 path.EndsWith("/Status", StringComparison.Ordinal)));
    }

    private static Dictionary<string, RectTransform> MapRects(Transform root)
    {
        var map = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            string path = RelativePath(root, rect.transform);
            if (map.ContainsKey(path))
                throw new InvalidOperationException(
                    "Duplicate UI path '" + path +
                    "'. Give each sibling a unique name before layout capture.");
            map.Add(path, rect);
        }
        return map;
    }

    private static string RelativePath(Transform root, Transform current)
    {
        if (current == root) return ".";
        var parts = new Stack<string>();
        for (Transform node = current; node && node != root; node = node.parent)
        {
            if (node.name.Contains("/"))
                throw new InvalidOperationException("UI object name contains '/': " + node.name);
            parts.Push(node.name);
        }
        return string.Join("/", parts.ToArray());
    }

    private static int PathDepth(string path)
    {
        if (path == ".") return 0;
        int depth = 1;
        foreach (char c in path) if (c == '/') depth++;
        return depth;
    }

    private static string ParentPath(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? "." : path.Substring(0, slash);
    }
}
