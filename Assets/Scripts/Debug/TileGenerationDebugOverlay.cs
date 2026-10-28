using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight temporary testing overlay for the dictionary-weighted Tile Pool.
/// It is created on demand so scenes/prefabs do not need Inspector wiring.
/// </summary>
public static class TileGenerationDebugOverlay
{
    // Keep the normal Stage 3 batch display independent from temporary vocabulary challenges.
    private static GameObject canvasRoot;
    private static TextMeshProUGUI wordText;

    /// <summary>
    /// Remove the temporary combat diagnostic when leaving gameplay so its
    /// DDOL canvas does not remain above the Stage Select menu.
    /// The next Show call may safely rebuild the display.
    /// </summary>
    public static void Hide()
    {
        if (canvasRoot != null)
            Object.Destroy(canvasRoot);
        canvasRoot = null;
        wordText = null;
    }

    public static void Show(
        string mainWord,
        string remainderWord,
        int stageNumber = 1,
        bool stage3RiddleWordsActive = false,
        bool stage3VocabularyWordsActive = false)
    {
        EnsureCreated();

        if (wordText == null)
            return;

        string safeMain = string.IsNullOrEmpty(mainWord)
            ? "(none)"
            : mainWord.ToUpperInvariant();
        string safeRemainder = string.IsNullOrEmpty(remainderWord)
            ? "(none)"
            : remainderWord.ToUpperInvariant();

        if (stageNumber == 2)
        {
            wordText.text =
                $"<b>Stage 2</b> | Main word: {safeMain} | " +
                $"<color=#59D9FF>Aquatic remainder: {safeRemainder}</color>";
        }
        else if (stageNumber == 3)
        {
            if (stage3RiddleWordsActive)
            {
                wordText.text =
                    $"<b>Stage 3</b> | <color=#FFD66B>Riddle word: {safeMain}</color> | " +
                    $"Riddle remainder: {safeRemainder}";
            }
            else if (stage3VocabularyWordsActive)
            {
                wordText.text =
                    $"<b>Stage 3</b> | <color=#FFD66B>MainBatch: {safeMain}</color> | " +
                    $"RemBatch: {safeRemainder}";
            }
            else
            {
                wordText.text =
                    $"<b>Stage 3</b> | <color=#FFD66B>MainBatch: {safeMain}</color> | " +
                    $"RemBatch: {safeRemainder}";
            }
        }
        else
        {
            wordText.text =
                $"<b>Stage {Mathf.Max(1, stageNumber)}</b> | " +
                $"Main word: {safeMain} | Remainder word: {safeRemainder}";
        }
    }

    private static void EnsureCreated()
    {
        if (canvasRoot != null && wordText != null)
            return;

        canvasRoot = new GameObject(
            "TileGenerationDebugOverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        Object.DontDestroyOnLoad(canvasRoot);

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        GameObject panelObject = new GameObject(
            "WordDebugPanel",
            typeof(RectTransform),
            typeof(Image));
        panelObject.transform.SetParent(canvasRoot.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -82f);
        panelRect.sizeDelta = new Vector2(780f, 48f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.035f, 0.055f, 0.82f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject(
            "WordDebugText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);

        wordText = textObject.GetComponent<TextMeshProUGUI>();
        wordText.text = "MainBatch: ... | RemBatch: ...";
        wordText.richText = true;
        wordText.fontSize = 17f;
        wordText.fontStyle = FontStyles.Bold;
        wordText.color = new Color(0.94f, 0.97f, 1f, 1f);
        wordText.alignment = TextAlignmentOptions.MidlineLeft;
        wordText.textWrappingMode = TextWrappingModes.NoWrap;
        wordText.overflowMode = TextOverflowModes.Ellipsis;
        wordText.enableAutoSizing = true;
        wordText.fontSizeMin = 11f;
        wordText.fontSizeMax = 17f;
        wordText.raycastTarget = false;
    }
}

/// <summary>
/// Temporary one-shot runtime diagnostic for the Android-only Stage 2 creature rig issue.
/// It is embedded in this already-imported script so Unity's AssetDatabase cannot skip it.
/// </summary>
public sealed class Stage2AndroidRigRuntimeProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject probe = new GameObject("Stage2AndroidRigRuntimeProbe");
        DontDestroyOnLoad(probe);
        probe.AddComponent<Stage2AndroidRigRuntimeProbe>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(4f);

        Debug.Log($"[Stage2RigAndroidProbe] platform={Application.platform} graphics={SystemInfo.graphicsDeviceType} device={SystemInfo.graphicsDeviceName} skinWeights={QualitySettings.skinWeights}");

        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.gameObject.scene.IsValid())
                continue;

            string fullName = GetHierarchyPath(animator.transform);
            string lower = fullName.ToLowerInvariant();
            if (!lower.Contains("barnacle") &&
                !lower.Contains("corsair") &&
                !lower.Contains("tidewraith") &&
                !lower.Contains("drownedcaptain") &&
                !lower.Contains("drowned captain"))
                continue;

            Transform hips = null;
            if (animator.isHuman)
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (hips == null)
            {
                Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name.ToLowerInvariant().Contains("hips"))
                    {
                        hips = candidate;
                        break;
                    }
                }
            }

            string hipsInfo = hips == null
                ? "hips=NULL"
                : $"hips={hips.name} hipsLocal={hips.localEulerAngles:F2} hipsWorld={hips.eulerAngles:F2}";

            Debug.Log(
                $"[Stage2RigAndroidProbe] animator={fullName} enabled={animator.enabled} human={animator.isHuman} avatarValid={(animator.avatar != null && animator.avatar.isValid)} " +
                $"local={animator.transform.localEulerAngles:F2} world={animator.transform.eulerAngles:F2} rootRot={animator.rootRotation.eulerAngles:F2} rootPos={animator.rootPosition:F3} {hipsInfo}");

            SkinnedMeshRenderer[] renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Transform rootBone = renderer.rootBone;
                string rootBoneInfo = rootBone == null
                    ? "rootBone=NULL"
                    : $"rootBone={rootBone.name} rootBoneLocal={rootBone.localEulerAngles:F2} rootBoneWorld={rootBone.eulerAngles:F2}";

                Debug.Log(
                    $"[Stage2RigAndroidProbe] renderer={GetHierarchyPath(renderer.transform)} enabled={renderer.enabled} quality={renderer.quality} " +
                    $"bones={renderer.bones.Length} updateWhenOffscreen={renderer.updateWhenOffscreen} {rootBoneInfo} boundsCenter={renderer.bounds.center:F3} boundsSize={renderer.bounds.size:F3}");
            }
        }

        Destroy(gameObject);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
