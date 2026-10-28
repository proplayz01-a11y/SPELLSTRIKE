using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class Stage3RiddleHUDEditableSetupMenu
{
    private const string HudPrefabPath = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string RiddleUiFolder = "Assets/Stage 3/UI/Riddle/Modular";
    private const string ParchmentBasePath = RiddleUiFolder + "/Riddle_ParchmentBase.png";

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Make Current Riddle HUD Editable (Non-Destructive)")]
    public static void MakeEditable()
    {
        Sprite parchmentSprite = EnsureSpriteImport(ParchmentBasePath, false);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            Transform root = FindDescendant(prefabRoot.transform, "RiddleChallengeRoot");
            if (root == null)
                throw new System.InvalidOperationException("RiddleChallengeRoot was not found in PersistentGameplayHUD.prefab.");

            RectTransform body = FindRect(root, "RiddleBodyFrame");
            RectTransform parchment = FindRect(root, "RiddleParchmentArt");
            RectTransform title = FindRect(root, "RiddleTitlePlaqueBody");
            RectTransform progress = FindRect(root, "RiddleProgressPlate");
            RectTransform divider = FindRect(root, "RiddleDivider");

            Image parchmentImage = parchment.GetComponent<Image>();
            if (parchmentImage == null)
                throw new System.InvalidOperationException("RiddleParchmentArt does not have an Image component.");
            parchmentImage.sprite = parchmentSprite;
            parchmentImage.type = Image.Type.Simple;
            parchmentImage.preserveAspect = true;
            parchmentImage.raycastTarget = false;
            SetFixedRect(parchment, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 347f), 0f);

            Transform brokenDecorations = FindDescendant(body, "RiddleBodyDecorations");
            if (brokenDecorations != null)
                Object.DestroyImmediate(brokenDecorations.gameObject);

            TMP_Text titleText = FindText(root, "Title");
            TMP_Text subtitleText = FindText(root, "Subtitle");
            TMP_Text progressText = FindText(root, "ProgressText");
            TMP_Text progressDots = FindText(root, "ProgressDots");
            TMP_Text riddleText = FindText(root, "RiddleBody");
            TMP_Text instructionText = FindText(root, "Instruction");

            Stage3RiddleHUDLayoutAuthoring authoring = root.GetComponent<Stage3RiddleHUDLayoutAuthoring>();
            if (authoring == null)
                authoring = root.gameObject.AddComponent<Stage3RiddleHUDLayoutAuthoring>();

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                // Keep the HUD visible while authoring in Edit Mode. The runtime
                // Stage3RiddleHUDView.Awake() immediately hides it on Play.
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            authoring.Configure(
                body,
                parchment,
                title,
                progress,
                divider,
                titleText,
                subtitleText,
                progressText,
                progressDots,
                riddleText,
                instructionText);

            EditorUtility.SetDirty(authoring);
            if (canvasGroup != null)
                EditorUtility.SetDirty(canvasGroup);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HudPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3RiddleHUDEditableSetup] PASS. Existing RiddleChallengeRoot is now inspector-editable without rebuilding its hierarchy.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Riddle/Audit Editable Riddle HUD")]
    public static void AuditEditable()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            Transform root = FindDescendant(prefabRoot.transform, "RiddleChallengeRoot");
            Stage3RiddleHUDLayoutAuthoring authoring = root != null
                ? root.GetComponent<Stage3RiddleHUDLayoutAuthoring>()
                : null;
            if (authoring == null)
                throw new System.InvalidOperationException("Editable authoring component is missing from RiddleChallengeRoot.");

            string[] required =
            {
                "RiddleBodyFrame",
                "RiddleParchmentArt",
                "RiddleTitlePlaqueBody",
                "RiddleProgressPlate",
                "RiddleDivider",
                "Title",
                "Subtitle",
                "ProgressText",
                "ProgressDots",
                "RiddleBody",
                "Instruction"
            };
            string[] missing = required.Where(name => FindDescendant(root, name) == null).ToArray();
            if (missing.Length > 0)
                throw new System.InvalidOperationException("Missing editable HUD objects: " + string.Join(", ", missing));

            Debug.Log("[Stage3RiddleHUDEditableSetup] AUDIT PASS. Riddle HUD is modular and manually editable in PersistentGameplayHUD.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        Transform t = FindDescendant(root, name);
        if (t == null)
            throw new System.InvalidOperationException($"Required RectTransform '{name}' is missing.");
        return t as RectTransform ?? throw new System.InvalidOperationException($"'{name}' is not a RectTransform.");
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform t = FindDescendant(root, name);
        TMP_Text text = t != null ? t.GetComponent<TMP_Text>() : null;
        if (text == null)
            throw new System.InvalidOperationException($"Required TMP text '{name}' is missing.");
        return text;
    }

    private static Sprite EnsureSpriteImport(string path, bool repeatWrap)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException($"TextureImporter was not found for '{path}'.");

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
        TextureWrapMode wantedWrap = repeatWrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (importer.wrapMode != wantedWrap) { importer.wrapMode = wantedWrap; changed = true; }
        if (changed)
            importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException($"Sprite import failed for '{path}'.");
        return sprite;
    }

    private static void SetFixedRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, float rotation)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root)
        {
            if (child.name == objectName)
                return child;

            Transform found = FindDescendant(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
