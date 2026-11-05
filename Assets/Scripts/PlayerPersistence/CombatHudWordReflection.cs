using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a short, UI-only rim sweep from the existing STRIKE availability
/// signal. It does not validate words or invoke any gameplay/attack methods.
/// </summary>
[DisallowMultipleComponent]
public sealed class CombatHudWordReflection : MonoBehaviour
{
    [Header("Valid Word Reflection")]
    [SerializeField, Min(0.1f)] private float sweepDuration = 1.05f;
    [SerializeField, Min(0f)] private float strikeDelay = 0.16f;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.28f;
    [SerializeField, Range(0.1f, 0.8f)] private float sweepWidthFraction = 0.34f;

    private const string ResourceRoot = "CombatHUDFrameFill_v02/";
    private static readonly Color SweepTint = new(1f, 0.81f, 0.43f, 0f);

    private sealed class SweepLayer
    {
        public RectTransform MaskRect;
        public RectTransform Rect;
        public Image Image;
        public float Travel;
        public float MaxAlpha;
        public bool FillParent;
        public float Aspect;
    }

    private PersistentHUDRoot hudRoot;
    private TileManager boundTileManager;
    private Button attackButton;
    private Image attackButtonImage;
    private Transform wordBarPanel;
    private Canvas combatCanvas;
    private SweepLayer wordBarSweep;
    private SweepLayer strikeSweep;

    private Sprite wordBarMaskSprite;
    private Sprite strikeMaskSprite;
    private Sprite approvedStrikeSprite;
    private Sprite sweepSprite;
    private Texture2D sweepTexture;

    private string previousWord = string.Empty;
    private bool previousActionable;
    private bool wordChanged;
    private bool pulseActive;
    private float pulseElapsed;
    private bool initialized;

    private void Start()
    {
        TryInitialize();
    }

    private void LateUpdate()
    {
        if (!initialized && !TryInitialize())
            return;

        RefreshLayerGeometry(wordBarSweep);
        RefreshLayerGeometry(strikeSweep);
        BindTileManager();

        string currentWord = boundTileManager != null
            ? boundTileManager.GetCurrentWord()
            : string.Empty;

        bool hudVisible = IsHudVisible();
        bool modeCorrect = boundTileManager != null
            && (boundTileManager.CurrentMode != TilePoolMode.TargetChallenge
                || boundTileManager.IsActiveTargetChallengeAnswer(currentWord));
        bool actionable = hudVisible
            && modeCorrect
            && attackButton != null
            && attackButton.IsInteractable()
            && !string.IsNullOrWhiteSpace(currentWord)
            && Time.timeScale > 0f;

        bool enteredActionableWord = actionable
            && (!previousActionable || currentWord != previousWord || wordChanged);

        if (!actionable)
        {
            StopPulse();
        }
        else if (enteredActionableWord)
        {
            StartPulse();
        }

        if (pulseActive)
            UpdatePulse(Time.unscaledDeltaTime);

        previousWord = currentWord;
        previousActionable = actionable;
        wordChanged = false;
    }

    private void OnDisable()
    {
        if (boundTileManager != null)
            boundTileManager.WordChanged -= HandleWordChanged;

        boundTileManager = null;
        previousActionable = false;
        wordChanged = true;
        StopPulse();
    }

    private void OnDestroy()
    {
        if (boundTileManager != null)
            boundTileManager.WordChanged -= HandleWordChanged;

        DestroyRuntimeObject(sweepSprite);
        DestroyRuntimeObject(sweepTexture);
    }

    private bool TryInitialize()
    {
        hudRoot = GetComponent<PersistentHUDRoot>();
        if (hudRoot == null)
            hudRoot = PersistentHUDRoot.Instance;
        if (hudRoot == null)
            return false;

        attackButton = hudRoot.AttackButton;
        wordBarPanel = hudRoot.WordBarPanel;
        if (attackButton == null || wordBarPanel == null)
            return false;

        Transform wordBarFrame = wordBarPanel.Find("WordBar_OrnateFrame");
        if (wordBarFrame == null)
            return false;

        attackButtonImage = attackButton.GetComponent<Image>();
        if (attackButtonImage == null)
            return false;

        combatCanvas = attackButton.GetComponentInParent<Canvas>(true);

        wordBarMaskSprite = Resources.Load<Sprite>(ResourceRoot + "Wordbar_ReflectionGold_Crop");
        strikeMaskSprite = Resources.Load<Sprite>(ResourceRoot + "Strike_ReflectionGold_Crop");
        approvedStrikeSprite = Resources.Load<Sprite>(ResourceRoot + "Strike_Approved_Crop");
        if (wordBarMaskSprite == null || strikeMaskSprite == null || approvedStrikeSprite == null)
        {
            Debug.LogWarning("[CombatHudWordReflection] One or more v02 reflection assets could not be loaded.", this);
            return false;
        }

        // Replace only the visual on the existing Button's Image. Its Button,
        // RectTransform, target graphic, input bounds and UnityEvent are untouched.
        attackButtonImage.sprite = approvedStrikeSprite;
        attackButtonImage.type = Image.Type.Simple;
        attackButtonImage.preserveAspect = true;
        attackButtonImage.color = Color.white;

        CreateSweepSprite();
        if (sweepSprite == null)
            return false;

        wordBarSweep = CreateMaskedSweep(
            wordBarFrame,
            wordBarMaskSprite,
            "WordBar_ValidReflectionMask",
            fillParent: true,
            maxAlpha: maxOpacity);

        strikeSweep = CreateMaskedSweep(
            attackButton.transform,
            strikeMaskSprite,
            "Strike_ValidReflectionMask",
            fillParent: false,
            maxAlpha: maxOpacity * 0.92f);

        if (wordBarSweep == null || strikeSweep == null)
            return false;

        initialized = true;
        BindTileManager();
        StopPulse();
        return true;
    }

    private void BindTileManager()
    {
        TileManager current = TileManager.Instance;
        if (current == boundTileManager)
            return;

        if (boundTileManager != null)
            boundTileManager.WordChanged -= HandleWordChanged;

        boundTileManager = current;
        if (boundTileManager != null)
            boundTileManager.WordChanged += HandleWordChanged;

        previousActionable = false;
        wordChanged = true;
    }

    private void HandleWordChanged(string word)
    {
        wordChanged = true;
    }

    private bool IsHudVisible()
    {
        return hudRoot != null
            && hudRoot.enabled
            && hudRoot.gameObject.activeInHierarchy
            && attackButton != null
            && attackButton.enabled
            && attackButton.gameObject.activeInHierarchy
            && wordBarPanel != null
            && wordBarPanel.gameObject.activeInHierarchy
            && combatCanvas != null
            && combatCanvas.isActiveAndEnabled;
    }

    private void CreateSweepSprite()
    {
        const int width = 128;
        const int height = 8;
        sweepTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "CombatHUD_ReflectionSweep_Gradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0,
            hideFlags = HideFlags.DontSave
        };

        for (int x = 0; x < width; x++)
        {
            float normalized = x / (float)(width - 1);
            float distance = Mathf.Abs(normalized * 2f - 1f);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.8f);
            Color pixel = new(1f, 1f, 1f, alpha);
            for (int y = 0; y < height; y++)
                sweepTexture.SetPixel(x, y, pixel);
        }

        sweepTexture.Apply(false, true);
        sweepSprite = Sprite.Create(
            sweepTexture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect);
        sweepSprite.name = "CombatHUD_ReflectionSweep_GradientSprite";
        sweepSprite.hideFlags = HideFlags.DontSave;
    }

    private SweepLayer CreateMaskedSweep(
        Transform parent,
        Sprite maskSprite,
        string objectName,
        bool fillParent,
        float maxAlpha)
    {
        if (parent is not RectTransform parentRect)
            return null;

        GameObject maskObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        maskObject.transform.SetParent(parent, false);
        maskObject.transform.SetAsFirstSibling();

        RectTransform maskRect = (RectTransform)maskObject.transform;
        maskRect.localScale = Vector3.one;
        maskRect.localRotation = Quaternion.identity;
        if (fillParent)
        {
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.pivot = new Vector2(0.5f, 0.5f);
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
        }
        else
        {
            float aspect = maskSprite.rect.width / Mathf.Max(1f, maskSprite.rect.height);
            float width = parentRect.rect.width;
            float height = parentRect.rect.height;
            if (width / Mathf.Max(1f, height) > aspect)
                width = height * aspect;
            else
                height = width / aspect;

            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
            maskRect.pivot = new Vector2(0.5f, 0.5f);
            maskRect.anchoredPosition = Vector2.zero;
            maskRect.sizeDelta = new Vector2(width, height);
        }

        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.sprite = maskSprite;
        maskImage.color = Color.white;
        maskImage.type = Image.Type.Simple;
        maskImage.preserveAspect = false;
        maskImage.raycastTarget = false;

        Mask mask = maskObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject sweepObject = new("ReflectionSweep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sweepObject.transform.SetParent(maskRect, false);
        RectTransform sweepRect = (RectTransform)sweepObject.transform;
        sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
        sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
        sweepRect.pivot = new Vector2(0.5f, 0.5f);
        sweepRect.localScale = Vector3.one;
        sweepRect.sizeDelta = new Vector2(maskRect.rect.width * sweepWidthFraction, maskRect.rect.height);

        Image sweepImage = sweepObject.GetComponent<Image>();
        sweepImage.sprite = sweepSprite;
        sweepImage.type = Image.Type.Simple;
        sweepImage.preserveAspect = false;
        sweepImage.color = SweepTint;
        sweepImage.raycastTarget = false;

        return new SweepLayer
        {
            MaskRect = maskRect,
            Rect = sweepRect,
            Image = sweepImage,
            Travel = (maskRect.rect.width + sweepRect.rect.width) * 0.5f,
            MaxAlpha = maxAlpha,
            FillParent = fillParent,
            Aspect = maskSprite.rect.width / Mathf.Max(1f, maskSprite.rect.height)
        };
    }

    private void RefreshLayerGeometry(SweepLayer layer)
    {
        if (layer == null || layer.MaskRect == null || layer.Rect == null)
            return;

        if (!layer.FillParent && layer.MaskRect.parent is RectTransform parentRect)
        {
            float width = parentRect.rect.width;
            float height = parentRect.rect.height;
            if (width / Mathf.Max(1f, height) > layer.Aspect)
                width = height * layer.Aspect;
            else
                height = width / Mathf.Max(0.001f, layer.Aspect);

            layer.MaskRect.sizeDelta = new Vector2(width, height);
        }

        float maskWidth = layer.MaskRect.rect.width;
        float maskHeight = layer.MaskRect.rect.height;
        if (maskWidth <= 1f || maskHeight <= 1f)
            return;

        layer.Rect.sizeDelta = new Vector2(maskWidth * sweepWidthFraction, maskHeight);
        layer.Travel = (maskWidth + layer.Rect.rect.width) * 0.5f;
    }

    private void StartPulse()
    {
        pulseElapsed = 0f;
        pulseActive = true;
        ResetLayer(wordBarSweep);
        ResetLayer(strikeSweep);
    }

    private void UpdatePulse(float deltaTime)
    {
        pulseElapsed += deltaTime;

        float wordBarProgress = Mathf.Clamp01(pulseElapsed / Mathf.Max(0.1f, sweepDuration));
        UpdateLayer(wordBarSweep, wordBarProgress);

        float strikeElapsed = pulseElapsed - strikeDelay;
        if (strikeElapsed >= 0f)
        {
            float strikeProgress = Mathf.Clamp01(strikeElapsed / Mathf.Max(0.1f, sweepDuration));
            UpdateLayer(strikeSweep, strikeProgress);
        }
        else
        {
            SetLayerAlpha(strikeSweep, 0f);
        }

        if (pulseElapsed >= sweepDuration + strikeDelay)
            StopPulse();
    }

    private void StopPulse()
    {
        pulseActive = false;
        pulseElapsed = 0f;
        ResetLayer(wordBarSweep);
        ResetLayer(strikeSweep);
    }

    private static void UpdateLayer(SweepLayer layer, float progress)
    {
        if (layer == null || layer.Rect == null || layer.Image == null)
            return;

        float eased = Mathf.SmoothStep(0f, 1f, progress);
        float x = Mathf.Lerp(-layer.Travel, layer.Travel, eased);
        layer.Rect.anchoredPosition = new Vector2(x, 0f);
        SetLayerAlpha(layer, Mathf.Sin(progress * Mathf.PI) * layer.MaxAlpha);
    }

    private static void ResetLayer(SweepLayer layer)
    {
        if (layer == null || layer.Rect == null)
            return;

        layer.Rect.anchoredPosition = new Vector2(-layer.Travel, 0f);
        SetLayerAlpha(layer, 0f);
    }

    private static void SetLayerAlpha(SweepLayer layer, float alpha)
    {
        if (layer == null || layer.Image == null)
            return;

        layer.Image.color = new Color(SweepTint.r, SweepTint.g, SweepTint.b, Mathf.Clamp01(alpha));
    }

    private static void DestroyRuntimeObject(Object runtimeObject)
    {
        if (runtimeObject == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeObject);
        else
            DestroyImmediate(runtimeObject);
    }
}
