#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileStageControlsAuthoringMenu
{
    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Stage2Scene/Stage2Scene.unity"
    };

    [MenuItem("SPELLSTRIKE/Testing/Mobile/Author Stage 1 + 2 Mobile Controls")]
    public static void AuthorStageMobileControls()
    {
        foreach (string path in StageScenePaths)
            AuthorScene(path);

        AssetDatabase.SaveAssets();
        Debug.Log("[MobileStageControlsAuthoring] Stage 1 and Stage 2 mobile controls authored and saved.");
    }

    private static void AuthorScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            RectTransform attackButton = FindNamedRect(scene, "AttackButton");
            if (attackButton == null)
                throw new System.InvalidOperationException($"AttackButton not found in {path}.");

            Canvas hostCanvas = attackButton.GetComponentInParent<Canvas>();
            if (hostCanvas == null)
                throw new System.InvalidOperationException($"AttackButton has no parent Canvas in {path}.");

            Transform host = hostCanvas.transform;
            RectTransform joystick = AuthorJoystick(scene, host);
            (GameObject interactionRoot, Button interactionButton, TMP_Text interactionLabel) =
                AuthorInteractionButton(scene, host);

            CombatCamera combatCamera = FindComponentInScene<CombatCamera>(scene);
            MobileStageControls controls = host.GetComponent<MobileStageControls>();
            if (controls == null)
                controls = Undo.AddComponent<MobileStageControls>(host.gameObject);

            Undo.RecordObject(controls, "Configure Mobile Stage Controls");
            controls.Configure(joystick, interactionRoot, interactionButton, interactionLabel, combatCamera);
            EditorUtility.SetDirty(controls);

            joystick.SetAsLastSibling();
            interactionRoot.transform.SetAsLastSibling();
            interactionRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MobileStageControlsAuthoring] Authored {scene.name}.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static RectTransform AuthorJoystick(Scene scene, Transform host)
    {
        RectTransform joystick = FindNamedRect(scene, "MobileMovementJoystick");
        if (joystick == null)
        {
            GameObject root = new(
                "MobileMovementJoystick",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MobileJoystickController));
            Undo.RegisterCreatedObjectUndo(root, "Create Stage Mobile Joystick");
            joystick = root.GetComponent<RectTransform>();
            joystick.SetParent(host, false);

            GameObject knobObject = new("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(knobObject, "Create Stage Mobile Joystick Knob");
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

            SetRect(knob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
            knob.localScale = new Vector3(0.72f, 0.72f, 0.72f);

            MobileJoystickController joystickController = root.GetComponent<MobileJoystickController>();
            joystickController.Configure(knob, 52f);
            EditorUtility.SetDirty(joystickController);
        }

        SetRect(joystick, Vector2.zero, Vector2.zero, new Vector2(32f, 28f), new Vector2(180f, 180f));
        joystick.gameObject.SetActive(true);
        return joystick;
    }

    private static (GameObject, Button, TMP_Text) AuthorInteractionButton(Scene scene, Transform host)
    {
        RectTransform existing = FindNamedRect(scene, "MobileInteractionButton");
        GameObject root;
        Button button;
        TMP_Text label;

        if (existing == null)
        {
            root = new GameObject(
                "MobileInteractionButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(root, "Create Stage Mobile Interaction Button");
            root.transform.SetParent(host, false);

            Image image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.22f, 0.13f, 0.06f, 0.94f);

            button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f);
            colors.pressedColor = new Color(0.80f, 0.66f, 0.42f, 1f);
            button.colors = colors;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create Stage Mobile Interaction Label");
            labelObject.transform.SetParent(root.transform, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "INTERACT";
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.98f, 0.84f, 0.48f, 1f);
            label.raycastTarget = false;
            Stretch(labelObject.GetComponent<RectTransform>());
        }
        else
        {
            root = existing.gameObject;
            button = root.GetComponent<Button>();
            label = root.GetComponentInChildren<TMP_Text>(true);
        }

        SetRect((RectTransform)root.transform, Vector2.right, Vector2.right, new Vector2(-36f, 38f), new Vector2(220f, 72f));
        return (root, button, label);
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(rect, "Author Mobile Stage Rect");
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform FindNamedRect(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName)
                    return rect;
            }
        }
        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }
}
#endif
