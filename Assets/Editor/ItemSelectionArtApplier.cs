using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ItemSelectionArtApplier
{
    private const string PrefabPath = "Assets/Prefabs/UI/ItemSelectionUI.prefab";
    private const string ScenePath = "Assets/Scenes/ItemSelectionScene.unity";
    private const string ParchmentPanelPath = "Assets/Sprites/ItemSelectionScreen/output-smallpngtools.png";

    [MenuItem("SPELLSTRIKE/UI/Apply Item Selection Art Assets")]
    public static void ApplyArtAssets()
    {
        Sprite parchmentSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ParchmentPanelPath);

        if (parchmentSprite == null)
        {
            Debug.LogError("[ItemSelectionArt] Missing parchment sprite. Check ItemSelectionScreen output files.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        ApplyArtToRoot(prefabRoot, parchmentSprite);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        EditorSceneManager.OpenScene(ScenePath);
        GameObject sceneRoot = GameObject.Find("ItemSelectionUI_Prefab");
        if (sceneRoot != null)
        {
            ApplyArtToRoot(sceneRoot, parchmentSprite);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ItemSelectionArt] Applied clean parchment panel art to ItemSelection UI.");
    }

    private static void ApplyArtToRoot(GameObject root, Sprite parchmentSprite)
    {
        if (root == null)
            return;

        ApplyPageArt(root.transform.Find("WizardVaultContent/Body/StagePage"), parchmentSprite);
        ApplyPageArt(root.transform.Find("WizardVaultContent/Body/ItemPage"), parchmentSprite);
        RestyleItemPage(root.transform.Find("WizardVaultContent/Body/ItemPage"));
        RestyleFooter(root.transform.Find("WizardVaultContent/Footer"));
    }

    private static void ApplyPageArt(Transform page, Sprite parchmentSprite)
    {
        if (page == null)
            return;

        Transform oldReadableLayer = page.Find("ParchmentReadableLayer");
        if (oldReadableLayer != null)
            Object.DestroyImmediate(oldReadableLayer.gameObject);

        Image pageImage = page.GetComponent<Image>();
        if (pageImage != null)
        {
            pageImage.sprite = parchmentSprite;
            pageImage.color = Color.white;
            pageImage.type = Image.Type.Simple;
            pageImage.preserveAspect = false;
            pageImage.raycastTarget = true;
        }

        VerticalLayoutGroup verticalLayout = page.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
            verticalLayout.padding = new RectOffset(54, 54, 62, 56);

        ApplyNestedPadding(page.Find("PotionSummaryPanel"), new RectOffset(24, 24, 18, 18));
        ApplyNestedPadding(page.Find("PassiveList"), new RectOffset(20, 20, 20, 20));
    }

    private static void ApplyNestedPadding(Transform panel, RectOffset padding)
    {
        if (panel == null)
            return;

        VerticalLayoutGroup verticalLayout = panel.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
            verticalLayout.padding = padding;
    }

    private static void RestyleItemPage(Transform itemPage)
    {
        if (itemPage == null)
            return;

        Transform equippedSlots = itemPage.Find("EquippedSlots");
        if (equippedSlots != null)
        {
            HorizontalLayoutGroup slotLayout = equippedSlots.GetComponent<HorizontalLayoutGroup>();
            if (slotLayout != null)
                slotLayout.spacing = 12f;

            for (int i = 1; i <= 3; i++)
            {
                Transform slot = equippedSlots.Find("EquippedSlot" + i);
                if (slot == null)
                    continue;

                LayoutElement slotLayoutElement = slot.GetComponent<LayoutElement>();
                if (slotLayoutElement != null)
                {
                    slotLayoutElement.minWidth = 242f;
                    slotLayoutElement.preferredWidth = 242f;
                }
            }
        }

        Transform passiveList = itemPage.Find("PassiveList");
        if (passiveList != null)
        {
            Transform content = EnsurePassiveListScrollView(passiveList);
            VerticalLayoutGroup listLayout = content == null ? null : content.GetComponent<VerticalLayoutGroup>();
            if (listLayout != null)
            {
                listLayout.padding = new RectOffset(0, 0, 0, 0);
                listLayout.spacing = 9f;
            }

            Transform rowParent = content == null ? passiveList : content;
            foreach (PassiveItemInfo item in PassiveItemCatalog.GetAllItems())
                RestylePassiveRow(rowParent.Find(item.id + "Row"));
        }
    }

    private static Transform EnsurePassiveListScrollView(Transform passiveList)
    {
        RectTransform passiveListRect = passiveList as RectTransform;
        if (passiveListRect == null)
            return null;

        VerticalLayoutGroup oldLayout = passiveList.GetComponent<VerticalLayoutGroup>();
        if (oldLayout != null)
            Object.DestroyImmediate(oldLayout);

        ContentSizeFitter oldSizeFitter = passiveList.GetComponent<ContentSizeFitter>();
        if (oldSizeFitter != null)
            Object.DestroyImmediate(oldSizeFitter);

        LayoutElement listLayout = passiveList.GetComponent<LayoutElement>();
        if (listLayout != null)
        {
            listLayout.minHeight = 334f;
            listLayout.preferredHeight = 334f;
        }

        ScrollRect scrollRect = passiveList.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = passiveList.gameObject.AddComponent<ScrollRect>();

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 34f;

        RectTransform viewport = passiveList.Find("PassiveListViewport") as RectTransform;
        if (viewport == null)
        {
            GameObject viewportObject = new GameObject("PassiveListViewport", typeof(RectTransform));
            viewportObject.layer = passiveList.gameObject.layer;
            viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(passiveList, false);
        }

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -12f);

        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = viewport.Find("PassiveListContent") as RectTransform;
        if (content == null)
        {
            GameObject contentObject = new GameObject("PassiveListContent", typeof(RectTransform));
            contentObject.layer = passiveList.gameObject.layer;
            content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
        }

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        if (contentLayout == null)
            contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
            contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();

        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        List<Transform> directRows = new List<Transform>();
        for (int i = 0; i < passiveList.childCount; i++)
        {
            Transform child = passiveList.GetChild(i);
            if (child == viewport)
                continue;

            directRows.Add(child);
        }

        foreach (Transform row in directRows)
            row.SetParent(content, false);

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = null;
        scrollRect.horizontalScrollbar = null;

        return content;
    }

    private static void RestylePassiveRow(Transform row)
    {
        if (row == null)
            return;

        RectTransform title = row.Find("Title") as RectTransform;
        if (title != null)
            title.offsetMin = new Vector2(18f, title.offsetMin.y);

        RectTransform body = row.Find("Body") as RectTransform;
        if (body != null)
            body.offsetMin = new Vector2(18f, body.offsetMin.y);

        RectTransform status = row.Find("Status") as RectTransform;
        if (status != null)
        {
            status.anchorMin = new Vector2(0.72f, 0f);
            status.anchorMax = new Vector2(1f, 1f);
            status.offsetMin = new Vector2(8f, 0f);
            status.offsetMax = new Vector2(-18f, 0f);
        }
    }

    private static void RestyleFooter(Transform footer)
    {
        if (footer == null)
            return;

        foreach (Button button in footer.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            if (image == null)
                continue;

            if (button.name.Contains("Continue"))
                image.color = new Color(0.62f, 0.39f, 0.12f, 0.95f);
            else
                image.color = new Color(0.34f, 0.19f, 0.07f, 0.86f);
        }
    }
}
