#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage2VocabularyReviewUIBuilder
{
    private const string MenuPath =
        "SPELLSTRIKE/Vocabulary/Build Stage 2 Vocabulary Review UI";

    private const string ReviewRootName =
        "Stage2VocabularyReviewRoot";

    private const string ProfilePath =
        "Assets/Data/Vocabulary/Stage2_Vocabulary_Candidate.asset";

    private static readonly string[] Stage2Words =
    {
        "SUBMERGE",
        "NAVIGATE",
        "UNDERTOW",
        "NAUTICAL"
    };

    [MenuItem(MenuPath)]
    private static void BuildStage2VocabularyReviewUI()
    {
        StageVocabularyProfile profile =
            AssetDatabase.LoadAssetAtPath<StageVocabularyProfile>(
                ProfilePath);

        if (profile == null)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + $"Could not load Stage 2 vocabulary profile at "
                + $"'{ProfilePath}'. No scene changes were made.");

            return;
        }

        if (profile.StageNumber != 2
            || profile.WordCount != Stage2Words.Length)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + $"Expected a Stage 2 profile with {Stage2Words.Length} words, "
                + $"but found Stage {profile.StageNumber} with {profile.WordCount}. "
                + "No scene changes were made.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + "No valid loaded scene is currently active.");

            return;
        }

        if (activeScene.name != "StageSelectScene")
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + $"Open StageSelectScene before running this builder. "
                + $"The active scene is '{activeScene.name}', so no scene changes were made.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(
            "Build Stage 2 Vocabulary Review UI");

        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = ResolveOrCreateCanvas(activeScene);

        if (canvas == null)
        {
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        GameObject reviewRoot =
            FindObjectByNameInScene(
                activeScene,
                ReviewRootName);

        bool createdRoot = reviewRoot == null;

        if (createdRoot)
        {
            reviewRoot = CreateUIObject(
                ReviewRootName,
                canvas.transform);

            StretchToParent(
                reviewRoot.GetComponent<RectTransform>());
        }
        else
        {
            RectTransform existingRootRect =
                reviewRoot.GetComponent<RectTransform>();

            if (existingRootRect == null)
            {
                Debug.LogError(
                    $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                    + $"An object named '{ReviewRootName}' already "
                    + "exists, but it is not a UI RectTransform. "
                    + "The builder will not modify it.");

                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            if (reviewRoot.GetComponentInParent<Canvas>() == null)
            {
                Undo.SetTransformParent(
                    reviewRoot.transform,
                    canvas.transform,
                    "Move Stage 2 Vocabulary Review UI Under Canvas");

                StretchToParent(existingRootRect);
            }
        }

        StageVocabularyReviewController controller =
            reviewRoot.GetComponent<StageVocabularyReviewController>();

        if (controller == null)
        {
            controller =
                Undo.AddComponent<StageVocabularyReviewController>(
                    reviewRoot);
        }

        /*
         * Stage2VocabularyReviewRoot
         * └── ReviewPanel
         *     └── ContentPanel
         *         ├── HeaderRoot
         *         │   ├── TitleText
         *         │   ├── TargetWordText
         *         │   ├── ProgressText
         *         ├── ProgressIndicatorsRoot
         *         ├── QuestionText
         *         ├── AnswerButtonsRoot
         *         ├── FeedbackRoot
         *         ├── SummaryPanel
         *         └── ContinueButton
         */

        GameObject reviewPanel =
            GetOrCreateUIObject(
                "ReviewPanel",
                reviewRoot.transform,
                reviewRoot.transform);

        StretchToParent(
            reviewPanel.GetComponent<RectTransform>());

        Image reviewBackdrop =
            GetOrAddComponent<Image>(
                reviewPanel);

        Undo.RecordObject(
            reviewBackdrop,
            "Configure Review Backdrop");

        reviewBackdrop.color =
            new Color(0.015f, 0.055f, 0.08f, 0.94f);

        reviewBackdrop.raycastTarget = true;

        CanvasGroup reviewCanvasGroup =
            GetOrAddComponent<CanvasGroup>(
                reviewPanel);

        Undo.RecordObject(
            reviewCanvasGroup,
            "Configure Review Canvas Group");

        reviewCanvasGroup.alpha = 1f;
        reviewCanvasGroup.interactable = true;
        reviewCanvasGroup.blocksRaycasts = true;

        GameObject contentPanel =
            GetOrCreateUIObject(
                "ContentPanel",
                reviewPanel.transform,
                reviewRoot.transform);

        RectTransform contentRect =
            contentPanel.GetComponent<RectTransform>();

        Undo.RecordObject(
            contentRect,
            "Configure Review Content Panel");

        contentRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        contentRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        contentRect.pivot =
            new Vector2(0.5f, 0.5f);

        contentRect.sizeDelta =
            new Vector2(1000f, 820f);

        contentRect.anchoredPosition =
            Vector2.zero;

        Image contentBackground =
            GetOrAddComponent<Image>(
                contentPanel);

        Undo.RecordObject(
            contentBackground,
            "Configure Review Content Background");

        contentBackground.color =
            new Color(0.025f, 0.13f, 0.18f, 0.98f);

        VerticalLayoutGroup contentLayout =
            GetOrAddComponent<VerticalLayoutGroup>(
                contentPanel);

        Undo.RecordObject(
            contentLayout,
            "Configure Review Content Layout");

        contentLayout.padding =
            new RectOffset(48, 48, 36, 36);

        contentLayout.spacing = 18f;
        contentLayout.childAlignment =
            TextAnchor.UpperCenter;

        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        /*
         * Header
         */

        GameObject headerRoot =
            GetOrCreateUIObject(
                "HeaderRoot",
                contentPanel.transform,
                reviewRoot.transform,
                "Header");

        AddOrConfigureLayoutElement(
            headerRoot,
            150f);

        VerticalLayoutGroup headerLayout =
            GetOrAddComponent<VerticalLayoutGroup>(
                headerRoot);

        Undo.RecordObject(
            headerLayout,
            "Configure Review Header");

        headerLayout.spacing = 6f;
        headerLayout.childAlignment =
            TextAnchor.UpperCenter;

        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;

        TextMeshProUGUI titleText =
            GetOrCreateText(
                "TitleText",
                headerRoot.transform,
                reviewRoot.transform,
                "STAGE 2 VOCABULARY REVIEW",
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            titleText.gameObject,
            48f);

        TextMeshProUGUI targetWordText =
            GetOrCreateText(
                "TargetWordText",
                headerRoot.transform,
                reviewRoot.transform,
                Stage2Words[0],
                48f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            targetWordText.gameObject,
            62f);

        Undo.RecordObject(
            targetWordText,
            "Configure Target Word Text");

        targetWordText.color =
            new Color(0.22f, 0.90f, 1f, 1f);

        targetWordText.characterSpacing = 4f;

        TextMeshProUGUI progressText =
            GetOrCreateText(
                "ProgressText",
                headerRoot.transform,
                reviewRoot.transform,
                "1 / 4",
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            progressText.gameObject,
            30f);

        Undo.RecordObject(
            progressText,
            "Configure Progress Text");

        progressText.color =
            new Color(0.78f, 0.90f, 0.94f, 1f);

        GameObject progressIndicatorsRoot =
            GetOrCreateUIObject(
                "ProgressIndicatorsRoot",
                contentPanel.transform,
                reviewRoot.transform,
                "ProgressIndicators");

        AddOrConfigureLayoutElement(
            progressIndicatorsRoot,
            34f);

        HorizontalLayoutGroup progressLayout =
            GetOrAddComponent<HorizontalLayoutGroup>(
                progressIndicatorsRoot);

        Undo.RecordObject(
            progressLayout,
            "Configure Progress Indicators Layout");

        progressLayout.spacing = 18f;
        progressLayout.childAlignment =
            TextAnchor.MiddleCenter;

        progressLayout.childControlWidth = false;
        progressLayout.childControlHeight = false;
        progressLayout.childForceExpandWidth = false;
        progressLayout.childForceExpandHeight = false;

        Image[] progressIndicators =
            new Image[4];

        for (int i = 0; i < progressIndicators.Length; i++)
        {
            GameObject indicatorRoot =
                GetOrCreateUIObject(
                    $"ProgressIndicator_{i + 1}_{Stage2Words[i]}",
                    progressIndicatorsRoot.transform,
                    reviewRoot.transform);

            RectTransform indicatorRootRect =
                indicatorRoot.GetComponent<RectTransform>();

            Undo.RecordObject(
                indicatorRootRect,
                "Configure Progress Indicator");

            indicatorRootRect.sizeDelta =
                new Vector2(26f, 26f);

            indicatorRootRect.localRotation =
                Quaternion.Euler(0f, 0f, 45f);

            Image outline =
                GetOrAddComponent<Image>(
                    indicatorRoot);

            Undo.RecordObject(
                outline,
                "Configure Progress Indicator Outline");

            outline.color =
                new Color(1f, 1f, 1f, 0.88f);

            outline.raycastTarget = false;

            GameObject fillObject =
                GetOrCreateDirectUIObject(
                    "ProgressFill",
                    indicatorRoot.transform);

            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();

            Undo.RecordObject(
                fillRect,
                "Configure Progress Indicator Fill");

            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot =
                new Vector2(0.5f, 0.5f);

            fillRect.offsetMin =
                new Vector2(4f, 4f);

            fillRect.offsetMax =
                new Vector2(-4f, -4f);

            Image fill =
                GetOrAddComponent<Image>(
                    fillObject);

            Undo.RecordObject(
                fill,
                "Configure Progress Indicator Fill");

            fill.color =
                i == 0
                    ? new Color(
                        0.16f,
                        0.82f,
                        0.90f,
                        1f)
                    : new Color(
                        1f,
                        1f,
                        1f,
                        0.38f);

            fill.raycastTarget = false;
            fill.enabled = true;

            progressIndicators[i] = fill;
        }

        /*
         * Question
         */

        TextMeshProUGUI questionText =
            GetOrCreateText(
                "QuestionText",
                contentPanel.transform,
                reviewRoot.transform,
                "Choose the sentence that uses the word correctly.",
                28f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            questionText.gameObject,
            78f);

        /*
         * Answer buttons
         */

        GameObject answerButtonsRoot =
            GetOrCreateUIObject(
                "AnswerButtonsRoot",
                contentPanel.transform,
                reviewRoot.transform,
                "AnswerButtons");

        AddOrConfigureLayoutElement(
            answerButtonsRoot,
            250f);

        VerticalLayoutGroup answerLayout =
            GetOrAddComponent<VerticalLayoutGroup>(
                answerButtonsRoot);

        Undo.RecordObject(
            answerLayout,
            "Configure Answer Buttons Layout");

        answerLayout.spacing = 10f;
        answerLayout.childAlignment =
            TextAnchor.UpperCenter;

        answerLayout.childControlWidth = true;
        answerLayout.childControlHeight = false;
        answerLayout.childForceExpandWidth = true;
        answerLayout.childForceExpandHeight = false;

        Button[] answerButtons =
            new Button[4];

        TMP_Text[] answerLabels =
            new TMP_Text[4];

        for (int i = 0; i < 4; i++)
        {
            int displayIndex = i + 1;

            GameObject answerButtonObject =
                GetOrCreateUIObject(
                    $"AnswerButton_{displayIndex}",
                    answerButtonsRoot.transform,
                    reviewRoot.transform);

            AddOrConfigureLayoutElement(
                answerButtonObject,
                54f);

            Image answerBackground =
                GetOrAddComponent<Image>(
                    answerButtonObject);

            Undo.RecordObject(
                answerBackground,
                "Configure Answer Button Background");

            answerBackground.color =
                new Color(
                    0.08f,
                    0.25f,
                    0.31f,
                    1f);

            Button answerButton =
                GetOrAddComponent<Button>(
                    answerButtonObject);

            Undo.RecordObject(
                answerButton,
                "Configure Answer Button");

            answerButton.targetGraphic =
                answerBackground;

            ColorBlock answerColors =
                answerButton.colors;

            answerColors.normalColor =
                new Color(
                    0.08f,
                    0.25f,
                    0.31f,
                    1f);

            answerColors.highlightedColor =
                new Color(
                    0.10f,
                    0.38f,
                    0.46f,
                    1f);

            answerColors.pressedColor =
                new Color(
                    0.07f,
                    0.20f,
                    0.25f,
                    1f);

            answerColors.selectedColor =
                answerColors.highlightedColor;

            answerColors.disabledColor =
                new Color(
                    0.08f,
                    0.13f,
                    0.15f,
                    0.60f);

            answerColors.colorMultiplier = 1f;
            answerColors.fadeDuration = 0.08f;

            answerButton.colors =
                answerColors;

            TextMeshProUGUI answerLabel =
                GetOrCreateText(
                    $"AnswerLabel_{displayIndex}",
                    answerButtonObject.transform,
                    reviewRoot.transform,
                    $"Answer option {displayIndex}",
                    21f,
                    FontStyles.Normal,
                    TextAlignmentOptions.MidlineLeft);

            StretchToParent(
                answerLabel.rectTransform);

            Undo.RecordObject(
                answerLabel.rectTransform,
                "Configure Answer Label");

            answerLabel.rectTransform.offsetMin =
                new Vector2(24f, 6f);

            answerLabel.rectTransform.offsetMax =
                new Vector2(-24f, -6f);

            answerLabel.raycastTarget = false;

            answerButtons[i] =
                answerButton;

            answerLabels[i] =
                answerLabel;
        }

        /*
         * Feedback
         */

        GameObject feedbackRoot =
            GetOrCreateUIObject(
                "FeedbackRoot",
                contentPanel.transform,
                reviewRoot.transform,
                "Feedback");

        AddOrConfigureLayoutElement(
            feedbackRoot,
            110f);

        VerticalLayoutGroup feedbackLayout =
            GetOrAddComponent<VerticalLayoutGroup>(
                feedbackRoot);

        Undo.RecordObject(
            feedbackLayout,
            "Configure Feedback Layout");

        feedbackLayout.spacing = 4f;
        feedbackLayout.childAlignment =
            TextAnchor.UpperCenter;

        feedbackLayout.childControlWidth = true;
        feedbackLayout.childControlHeight = false;
        feedbackLayout.childForceExpandWidth = true;
        feedbackLayout.childForceExpandHeight = false;

        CanvasGroup feedbackCanvasGroup =
            GetOrAddComponent<CanvasGroup>(
                feedbackRoot);

        Undo.RecordObject(
            feedbackCanvasGroup,
            "Configure Feedback Canvas Group");

        feedbackCanvasGroup.alpha = 0f;
        feedbackCanvasGroup.interactable = false;
        feedbackCanvasGroup.blocksRaycasts = false;

        TextMeshProUGUI feedbackTitleText =
            GetOrCreateText(
                "FeedbackTitleText",
                feedbackRoot.transform,
                reviewRoot.transform,
                string.Empty,
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            feedbackTitleText.gameObject,
            34f);

        TextMeshProUGUI feedbackExplanationText =
            GetOrCreateText(
                "FeedbackExplanationText",
                feedbackRoot.transform,
                reviewRoot.transform,
                string.Empty,
                19f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            feedbackExplanationText.gameObject,
            70f);

        /*
         * Summary
         */

        GameObject summaryPanel =
            GetOrCreateUIObject(
                "SummaryPanel",
                contentPanel.transform,
                reviewRoot.transform);

        AddOrConfigureLayoutElement(
            summaryPanel,
            250f);

        VerticalLayoutGroup summaryLayout =
            GetOrAddComponent<VerticalLayoutGroup>(
                summaryPanel);

        Undo.RecordObject(
            summaryLayout,
            "Configure Summary Layout");

        summaryLayout.spacing = 8f;
        summaryLayout.childAlignment =
            TextAnchor.UpperCenter;

        summaryLayout.childControlWidth = true;
        summaryLayout.childControlHeight = false;
        summaryLayout.childForceExpandWidth = true;
        summaryLayout.childForceExpandHeight = false;

        TextMeshProUGUI summaryTitleText =
            GetOrCreateText(
                "SummaryTitleText",
                summaryPanel.transform,
                reviewRoot.transform,
                "STAGE 2 REVIEW COMPLETE",
                32f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            summaryTitleText.gameObject,
            46f);

        TextMeshProUGUI scoreValueText =
            GetOrCreateText(
                "ScoreValueText",
                summaryPanel.transform,
                reviewRoot.transform,
                "0 / 4",
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            scoreValueText.gameObject,
            44f);

        TextMeshProUGUI needsReviewText =
            GetOrCreateText(
                "NeedsReviewText",
                summaryPanel.transform,
                reviewRoot.transform,
                "None",
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            needsReviewText.gameObject,
            42f);

        TextMeshProUGUI completionText =
            GetOrCreateText(
                "CompletionText",
                summaryPanel.transform,
                reviewRoot.transform,
                "Review completed.",
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

        AddOrConfigureLayoutElement(
            completionText.gameObject,
            72f);

        /*
         * Continue button
         */

        GameObject continueButtonObject =
            GetOrCreateUIObject(
                "ContinueButton",
                contentPanel.transform,
                reviewRoot.transform);

        AddOrConfigureLayoutElement(
            continueButtonObject,
            58f);

        RectTransform continueRect =
            continueButtonObject.GetComponent<RectTransform>();

        Undo.RecordObject(
            continueRect,
            "Configure Continue Button");

        continueRect.sizeDelta =
            new Vector2(320f, 58f);

        Image continueBackground =
            GetOrAddComponent<Image>(
                continueButtonObject);

        Undo.RecordObject(
            continueBackground,
            "Configure Continue Button Background");

        continueBackground.color =
            new Color(
                0.05f,
                0.62f,
                0.73f,
                1f);

        Button continueButton =
            GetOrAddComponent<Button>(
                continueButtonObject);

        Undo.RecordObject(
            continueButton,
            "Configure Continue Button");

        continueButton.targetGraphic =
            continueBackground;

        ColorBlock continueColors =
            continueButton.colors;

        continueColors.normalColor =
            new Color(
                0.05f,
                0.62f,
                0.73f,
                1f);

        continueColors.highlightedColor =
            new Color(
                0.08f,
                0.76f,
                0.86f,
                1f);

        continueColors.pressedColor =
            new Color(
                0.04f,
                0.48f,
                0.58f,
                1f);

        continueColors.selectedColor =
            continueColors.highlightedColor;

        continueColors.disabledColor =
            new Color(
                0.12f,
                0.26f,
                0.29f,
                0.55f);

        continueColors.colorMultiplier = 1f;
        continueColors.fadeDuration = 0.08f;

        continueButton.colors =
            continueColors;

        TextMeshProUGUI continueButtonLabel =
            GetOrCreateText(
                "ContinueButtonLabel",
                continueButtonObject.transform,
                reviewRoot.transform,
                "CONTINUE",
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

        StretchToParent(
            continueButtonLabel.rectTransform);

        continueButtonLabel.raycastTarget =
            false;

        /*
         * Initial editor-visible state.
         *
         * ReviewPanel stays active in edit mode so the generated hierarchy
         * can be inspected. StageVocabularyReviewController.Awake() hides it
         * when the scene enters play mode.
         */

        Undo.RecordObject(
            reviewPanel,
            "Configure Review Panel State");

        reviewPanel.SetActive(true);

        Undo.RecordObject(
            headerRoot,
            "Configure Header State");

        headerRoot.SetActive(true);

        Undo.RecordObject(
            answerButtonsRoot,
            "Configure Answer Buttons State");

        answerButtonsRoot.SetActive(true);

        Undo.RecordObject(
            summaryPanel,
            "Configure Summary Panel State");

        summaryPanel.SetActive(false);

        /*
         * Wire every serialized UI dependency on the existing
         * StageVocabularyReviewController.
         */

        Undo.RecordObject(
            controller,
            "Wire Stage 2 Vocabulary Review Controller");

        SerializedObject serializedController =
            new SerializedObject(controller);

        SetObjectReference(
            serializedController,
            "profile",
            profile);

        SetObjectReference(
            serializedController,
            "reviewPanel",
            reviewPanel);

        SetObjectReference(
            serializedController,
            "headerRoot",
            headerRoot);

        SetObjectReference(
            serializedController,
            "titleText",
            titleText);

        SetObjectReference(
            serializedController,
            "targetWordText",
            targetWordText);

        SetObjectReference(
            serializedController,
            "progressText",
            progressText);

        SetObjectReference(
            serializedController,
            "questionText",
            questionText);

        SetObjectReference(
            serializedController,
            "answerButtonsRoot",
            answerButtonsRoot);

        SetObjectReferenceArray(
            serializedController,
            "answerButtons",
            answerButtons);

        SetObjectReferenceArray(
            serializedController,
            "answerLabels",
            answerLabels);

        SetObjectReference(
            serializedController,
            "feedbackCanvasGroup",
            feedbackCanvasGroup);

        SetObjectReference(
            serializedController,
            "feedbackTitleText",
            feedbackTitleText);

        SetObjectReference(
            serializedController,
            "feedbackExplanationText",
            feedbackExplanationText);

        SetObjectReference(
            serializedController,
            "continueButton",
            continueButton);

        SetObjectReference(
            serializedController,
            "continueButtonLabel",
            continueButtonLabel);

        SetObjectReference(
            serializedController,
            "reviewCanvasGroup",
            reviewCanvasGroup);

        SetObjectReference(
            serializedController,
            "summaryPanel",
            summaryPanel);

        SetObjectReference(
            serializedController,
            "summaryTitleText",
            summaryTitleText);

        SetObjectReference(
            serializedController,
            "scoreValueText",
            scoreValueText);

        SetObjectReference(
            serializedController,
            "needsReviewText",
            needsReviewText);

        SetObjectReference(
            serializedController,
            "completionText",
            completionText);

        SetObjectReferenceArray(
            serializedController,
            "progressIndicators",
            progressIndicators);

        serializedController.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);

        Selection.activeGameObject =
            reviewRoot;

        EditorGUIUtility.PingObject(
            reviewRoot);

        EditorSceneManager.MarkSceneDirty(
            activeScene);

        Undo.CollapseUndoOperations(
            undoGroup);

        Debug.Log(
            $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
            + (createdRoot
                ? "Created"
                : "Updated")
            + " the reusable Stage 2 vocabulary review UI and wired "
            + "the existing StageVocabularyReviewController contract. "
            + "No gameplay scripts or other scenes were modified.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateBuildStage2VocabularyReviewUI()
    {
        // Stage 1 and Stage 2 deliberately share ReviewRoot in StageSelectScene.
        // Keep this legacy builder unavailable rather than creating a second UI.
        return false;
    }

    private static Canvas ResolveOrCreateCanvas(
        Scene activeScene)
    {
        GameObject canvasObject =
            FindObjectByNameInScene(
                activeScene,
                "Stage2VocabularyReviewCanvas");

        if (canvasObject == null)
        {
            canvasObject =
                new GameObject(
                    "Stage2VocabularyReviewCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Create Stage 2 Vocabulary Review Canvas");
        }
        else if (canvasObject.GetComponent<RectTransform>() == null)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + "An object named 'Stage2VocabularyReviewCanvas' exists but is not a UI RectTransform.");
            return null;
        }

        Canvas canvas =
            GetOrAddComponent<Canvas>(canvasObject);

        Undo.RecordObject(
            canvas,
            "Configure Stage 2 Vocabulary Review Canvas");

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        CanvasScaler scaler =
            GetOrAddComponent<CanvasScaler>(canvasObject);

        Undo.RecordObject(
            scaler,
            "Configure Stage 2 Vocabulary Review Canvas Scaler");

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(canvasObject);

        return canvas;
    }

    private static GameObject FindObjectByNameInScene(
        Scene scene,
        string objectName)
    {
        foreach (GameObject rootObject
                 in scene.GetRootGameObjects())
        {
            Transform[] transforms =
                rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform transform
                     in transforms)
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject GetOrCreateUIObject(
        string name,
        Transform parent,
        Transform searchRoot,
        string legacyName = null)
    {
        Transform existing =
            FindDescendant(
                searchRoot,
                name);

        if (existing == null
            && !string.IsNullOrEmpty(legacyName))
        {
            existing =
                FindDescendant(
                    searchRoot,
                    legacyName);

            if (existing != null)
            {
                Undo.RecordObject(
                    existing.gameObject,
                    $"Rename {legacyName} to {name}");

                existing.gameObject.name =
                    name;
            }
        }

        if (existing != null)
        {
            if (existing.parent != parent)
            {
                Undo.SetTransformParent(
                    existing,
                    parent,
                    $"Move {name}");
            }

            return existing.gameObject;
        }

        return CreateUIObject(
            name,
            parent);
    }

    private static GameObject GetOrCreateDirectUIObject(
        string name,
        Transform parent)
    {
        Transform existing =
            parent.Find(name);

        if (existing != null)
            return existing.gameObject;

        return CreateUIObject(
            name,
            parent);
    }

    private static Transform FindDescendant(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform transform
                 in transforms)
        {
            if (transform.name == objectName)
                return transform;
        }

        return null;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject gameObject =
            new GameObject(
                name,
                typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            gameObject,
            $"Create {name}");

        gameObject.transform.SetParent(
            parent,
            false);

        return gameObject;
    }

    private static TextMeshProUGUI GetOrCreateText(
        string name,
        Transform parent,
        Transform searchRoot,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        GameObject textObject =
            GetOrCreateUIObject(
                name,
                parent,
                searchRoot);

        TextMeshProUGUI tmp =
            GetOrAddComponent<TextMeshProUGUI>(
                textObject);

        Undo.RecordObject(
            tmp,
            $"Configure {name}");

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font =
                TMP_Settings.defaultFontAsset;
        }

        return tmp;
    }

    private static T GetOrAddComponent<T>(
        GameObject gameObject)
        where T : Component
    {
        T component =
            gameObject.GetComponent<T>();

        if (component != null)
            return component;

        return Undo.AddComponent<T>(
            gameObject);
    }

    private static void AddOrConfigureLayoutElement(
        GameObject gameObject,
        float preferredHeight)
    {
        LayoutElement layoutElement =
            GetOrAddComponent<LayoutElement>(
                gameObject);

        Undo.RecordObject(
            layoutElement,
            $"Configure {gameObject.name} Layout");

        layoutElement.preferredHeight =
            preferredHeight;
    }

    private static void StretchToParent(
        RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        Undo.RecordObject(
            rectTransform,
            "Stretch UI Object");

        rectTransform.anchorMin =
            Vector2.zero;

        rectTransform.anchorMax =
            Vector2.one;

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            Vector2.zero;

        rectTransform.sizeDelta =
            Vector2.zero;
    }

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + $"Serialized field '{propertyName}' was not found "
                + "on StageVocabularyReviewController.");

            return;
        }

        property.objectReferenceValue =
            value;
    }

    private static void SetObjectReferenceArray<T>(
        SerializedObject serializedObject,
        string propertyName,
        T[] values)
        where T : Object
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName);

        if (property == null)
        {
            Debug.LogError(
                $"[{nameof(Stage2VocabularyReviewUIBuilder)}] "
                + $"Serialized array '{propertyName}' was not found "
                + "on StageVocabularyReviewController.");

            return;
        }

        property.arraySize =
            values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                values[i];
        }
    }
}

#endif
