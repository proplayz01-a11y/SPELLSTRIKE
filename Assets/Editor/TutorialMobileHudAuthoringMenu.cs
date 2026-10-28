#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Authors the approved Tutorial mobile HUD directly into TutorialScene so the scene already
/// looks like the runtime result before Play is pressed. Runtime responsiveness then enforces
/// the same values instead of creating a second hierarchy.
/// </summary>
public static class TutorialMobileHudAuthoringMenu
{
    private const string TutorialSceneName = "TutorialScene";

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Author Canonical Mobile HUD in Edit Mode")]
    internal static void AuthorCanonicalMobileHud()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[TutorialMobileAuthoring] Exit Play Mode before authoring the Tutorial HUD.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != TutorialSceneName)
        {
            Debug.LogError($"[TutorialMobileAuthoring] Open {TutorialSceneName} before running this command.");
            return;
        }

        Transform gameplayRoot = FindNamedTransformInScene(scene, "GameplayCanvas");
        if (gameplayRoot == null)
        {
            Debug.LogError("[TutorialMobileAuthoring] GameplayCanvas was not found.");
            return;
        }

        RectTransform tilePoolPanel = FindNamedRect(gameplayRoot, "TilePoolPanel");
        RectTransform tilePoolPanelDesign = FindNamedRect(gameplayRoot, "TilePoolPanelDesign");
        RectTransform tilePoolPanelDesignOverlay = FindNamedRect(gameplayRoot, "TilePoolPanelDesign (1)");
        RectTransform wordBarPanel = FindNamedRect(gameplayRoot, "WordBarPanel");
        RectTransform uiButtons = FindNamedRect(gameplayRoot, "UI_Buttons");
        RectTransform attackButton = FindNamedRect(gameplayRoot, "AttackButton");
        RectTransform playerHealthBar = FindNamedRect(gameplayRoot, "HealthBar");
        RectTransform enemyHealthBar = FindNamedRect(gameplayRoot, "EnemyHealthBar");

        if (tilePoolPanel == null || tilePoolPanelDesign == null || tilePoolPanelDesignOverlay == null
            || wordBarPanel == null || uiButtons == null || attackButton == null
            || playerHealthBar == null || enemyHealthBar == null)
        {
            Debug.LogError("[TutorialMobileAuthoring] One or more canonical combat-HUD objects are missing.");
            return;
        }

        Undo.SetCurrentGroupName("Author Tutorial Mobile HUD");
        int undoGroup = Undo.GetCurrentGroup();

        AuthorCanonicalCombatLayout(
            tilePoolPanel,
            tilePoolPanelDesign,
            tilePoolPanelDesignOverlay,
            wordBarPanel,
            uiButtons,
            attackButton,
            playerHealthBar,
            enemyHealthBar);

        RectTransform joystick = AuthorJoystick(gameplayRoot, attackButton);
        if (joystick == null)
            return;

        TutorialDirector director = FindTutorialDirector(scene);
        if (director == null)
        {
            Debug.LogError("[TutorialMobileAuthoring] TutorialDirector was not found.");
            return;
        }

        RectTransform collectButton = AuthorCollectButton(gameplayRoot, attackButton, director);
        if (collectButton == null)
            return;

        TutorialResponsiveLayout responsive = gameplayRoot.GetComponent<TutorialResponsiveLayout>();
        if (responsive == null)
            responsive = Undo.AddComponent<TutorialResponsiveLayout>(gameplayRoot.gameObject);

        EditorUtility.SetDirty(responsive);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = joystick.gameObject;
        Debug.Log(
            "[TutorialMobileAuthoring] Canonical mobile HUD authored in TutorialScene. " +
            "The joystick and Spellbook COLLECT button now exist in Edit Mode and runtime will reuse these same objects. " +
            "Scene was marked dirty but not auto-saved.",
            joystick);
    }

    private static void AuthorCanonicalCombatLayout(
        RectTransform tilePoolPanel,
        RectTransform tilePoolPanelDesign,
        RectTransform tilePoolPanelDesignOverlay,
        RectTransform wordBarPanel,
        RectTransform uiButtons,
        RectTransform attackButton,
        RectTransform playerHealthBar,
        RectTransform enemyHealthBar)
    {
        // Keep the authored Health HUD identical to the runtime mobile baseline.
        // Runtime safe-area handling can still move these anchors on a device with
        // non-zero insets, but at the A05s 2400x1080 authoring profile this is the
        // exact state Play Mode applies.
        SetRect(playerHealthBar, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(44f, -18f), playerHealthBar.sizeDelta, playerHealthBar.localScale);
        SetRect(enemyHealthBar, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-44f, -18f), enemyHealthBar.sizeDelta, Vector3.one);

        ConfigureScalerForMobile(playerHealthBar);
        ConfigureScalerForMobile(enemyHealthBar);

        SetRect(tilePoolPanelDesign, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 56.7f), new Vector2(720f, 190f), new Vector3(0.83f, 0.83f, 0.83f));
        SetRect(tilePoolPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 64.7f), new Vector2(720f, 190f), new Vector3(0.83f, 0.83f, 0.83f));
        SetRect(tilePoolPanelDesignOverlay, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 74f), new Vector2(720f, 190f), new Vector3(0.79f, 0.67f, 0.7946805f));
        SetRect(wordBarPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 215f), new Vector2(733.3334f, 86f), new Vector3(0.97f, 0.86f, 0.77f));
        SetRect(uiButtons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 20f), new Vector2(220f, 64f), Vector3.one);
        SetRect(attackButton, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -19f), new Vector2(190f, 50f), new Vector3(1.08f, 1.067727f, 1.067727f));

        GridLayoutGroup poolGrid = tilePoolPanel.GetComponent<GridLayoutGroup>();
        if (poolGrid != null)
        {
            Undo.RecordObject(poolGrid, "Author Tutorial Tile Pool Grid");
            poolGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            poolGrid.constraintCount = 8;
            poolGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            poolGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            poolGrid.childAlignment = TextAnchor.MiddleCenter;
            poolGrid.cellSize = new Vector2(71.17f, 66.2f);
            poolGrid.spacing = new Vector2(5.92f, 5f);
            poolGrid.padding.left = 10;
            poolGrid.padding.right = 10;
            poolGrid.padding.top = 21;
            poolGrid.padding.bottom = 5;
            EditorUtility.SetDirty(poolGrid);
        }

        wordBarPanel.SetSiblingIndex(0);
        tilePoolPanelDesignOverlay.SetSiblingIndex(1);
        tilePoolPanel.SetSiblingIndex(2);
        tilePoolPanelDesign.SetSiblingIndex(3);
        uiButtons.SetSiblingIndex(4);

        Image border = tilePoolPanelDesign.GetComponent<Image>();
        if (border != null)
        {
            Undo.RecordObject(border, "Disable Tutorial Border Raycast");
            border.raycastTarget = false;
            EditorUtility.SetDirty(border);
        }

        Image backdrop = tilePoolPanelDesignOverlay.GetComponent<Image>();
        if (backdrop != null)
        {
            Undo.RecordObject(backdrop, "Disable Tutorial Backdrop Raycast");
            backdrop.raycastTarget = false;
            EditorUtility.SetDirty(backdrop);
        }
    }

    private static void ConfigureScalerForMobile(RectTransform rect)
    {
        if (rect == null)
            return;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler == null)
            return;

        Undo.RecordObject(scaler, "Author Tutorial Health HUD Scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        EditorUtility.SetDirty(scaler);
    }

    private static RectTransform AuthorJoystick(Transform gameplayRoot, RectTransform attackButton)
    {
        RectTransform joystick = FindNamedRect(gameplayRoot, "MobileMovementJoystick");
        if (joystick == null)
        {
            Canvas canvas = attackButton.GetComponentInParent<Canvas>();
            Transform host = canvas != null ? canvas.transform : gameplayRoot;

            GameObject root = new GameObject(
                "MobileMovementJoystick",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MobileJoystickController));
            Undo.RegisterCreatedObjectUndo(root, "Create Tutorial Mobile Joystick");
            joystick = root.GetComponent<RectTransform>();
            joystick.SetParent(host, false);

            GameObject knobObject = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(knobObject, "Create Tutorial Mobile Joystick Knob");
            RectTransform knob = knobObject.GetComponent<RectTransform>();
            knob.SetParent(joystick, false);

            Image baseImage = root.GetComponent<Image>();
            baseImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            baseImage.color = new Color(0.10f, 0.74f, 0.95f, 0.34f);
            baseImage.raycastTarget = true;

            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            knobImage.color = new Color(0.95f, 0.73f, 0.20f, 0.95f);
            knobImage.raycastTarget = false;

            SetRect(knob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(112f, 112f), new Vector3(0.72f, 0.72f, 0.72f));

            MobileJoystickController controller = root.GetComponent<MobileJoystickController>();
            controller.Configure(knob, 52f);
            EditorUtility.SetDirty(controller);
        }

        SetRect(joystick, Vector2.zero, Vector2.zero,
            new Vector2(32f, 28f), new Vector2(180f, 180f), Vector3.one, Vector2.zero);
        joystick.SetAsLastSibling();
        joystick.gameObject.SetActive(true);
        return joystick;
    }

    private static RectTransform AuthorCollectButton(
        Transform gameplayRoot,
        RectTransform attackButton,
        TutorialDirector director)
    {
        RectTransform collectRect = FindNamedRect(gameplayRoot, "MobileCollectButton");
        if (collectRect == null)
        {
            Canvas canvas = attackButton.GetComponentInParent<Canvas>();
            Transform host = canvas != null ? canvas.transform : gameplayRoot;

            GameObject root = new GameObject(
                "MobileCollectButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(root, "Create Tutorial Mobile Collect Button");
            collectRect = root.GetComponent<RectTransform>();
            collectRect.SetParent(host, false);

            Image image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.42f, 0.23f, 0.035f, 0.98f);
            image.raycastTarget = true;

            Button button = root.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, director.TryClaimSpellbook);

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create Tutorial Mobile Collect Label");
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(collectRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "COLLECT";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = new Color(0.96f, 0.94f, 0.84f, 1f);
            label.raycastTarget = false;

            SerializedObject serializedDirector = new(director);
            serializedDirector.FindProperty("mobileCollectButton").objectReferenceValue = root;
            serializedDirector.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);
        }

        SetRect(
            collectRect,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-32f, 28f),
            new Vector2(190f, 64f),
            Vector3.one);
        collectRect.SetAsLastSibling();
        collectRect.gameObject.SetActive(false);
        return collectRect;
    }

    private static TutorialDirector FindTutorialDirector(Scene scene)
    {
        TutorialDirector[] directors = Object.FindObjectsByType<TutorialDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < directors.Length; i++)
        {
            if (directors[i] != null && directors[i].gameObject.scene == scene)
                return directors[i];
        }

        return null;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Vector3 scale,
        Vector2? anchorMaxOverride = null)
    {
        if (rect == null)
            return;

        Undo.RecordObject(rect, "Author Tutorial Mobile Rect");
        rect.anchorMin = anchor;
        rect.anchorMax = anchorMaxOverride ?? anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = scale;
        EditorUtility.SetDirty(rect);
    }

    private static Transform FindNamedTransformInScene(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name == objectName)
                    return transforms[j];
            }
        }

        return null;
    }

    private static RectTransform FindNamedRect(Transform root, string objectName)
    {
        if (root == null)
            return null;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == objectName)
                return rects[i];
        }

        return null;
    }
}
#endif
