using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class Tile : MonoBehaviour
{
    private const int DefaultDebuffCounter = 2;

    public char letter;
    public Button button;              // button on the UI prefab
    public TextMeshProUGUI textTMP;    // TMP text
    public Text letterText;            // fallback Text
    [FormerlySerializedAs("currentState")] public TileState CurrentState = TileState.Normal;
    public int DebuffCounter;

    [Header("Tile State Visuals")]
    public GameObject LockedOverlay;
    public GameObject CrackedOverlay;
    public TextMeshProUGUI DebuffCounterText;

    private TileManager tileManager;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (textTMP == null)
            textTMP = transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();

        if (LockedOverlay == null)
            LockedOverlay = transform.Find("LockedOverlay")?.gameObject;

        if (CrackedOverlay == null)
            CrackedOverlay = transform.Find("CrackedOverlay")?.gameObject;

        if (DebuffCounterText == null)
            DebuffCounterText = transform.Find("DebuffCounterText")?.GetComponent<TextMeshProUGUI>();

        PrepareDebuffCounterText();
    }

    void Start()
    {
        tileManager = Object.FindAnyObjectByType<TileManager>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning($"[Tile] No Button component found on tile '{name}'. This tile cannot be clicked.");
        }

        UpdateVisual();
        ApplyStateVisual();
    }

    // call this every time the letter changes
    public void SetLetter(char newLetter)
    {
        letter = newLetter;
        UpdateVisual();
    }

    public void SetTileState(TileState newState, int counter = 0)
    {
        CurrentState = newState;
        DebuffCounter = UsesDebuffCounter(newState) ? Mathf.Max(1, counter == 0 ? DefaultDebuffCounter : counter) : 0;
        Debug.Log($"[TileState] Tile set to {CurrentState}.");
        ApplyStateVisual();
    }

    public void SetState(TileState newState)
    {
        SetTileState(newState);
    }

    public void SetDebuffCounter(int value)
    {
        DebuffCounter = Mathf.Max(0, value);
        UpdateDebuffCounterVisual();
    }

    public void ClearDebuffVisuals()
    {
        if (LockedOverlay != null)
            LockedOverlay.SetActive(false);

        if (CrackedOverlay != null)
            CrackedOverlay.SetActive(false);

        if (DebuffCounterText != null)
            DebuffCounterText.gameObject.SetActive(false);
    }

    public bool IsSelectable()
    {
        return CurrentState == TileState.Normal || CurrentState == TileState.Cracked;
    }

    private bool UsesDebuffCounter(TileState state)
    {
        return state == TileState.Locked || state == TileState.Cracked;
    }

    private void UpdateVisual()
    {
        if (textTMP != null)
            textTMP.text = letter.ToString();
        if (letterText != null)
            letterText.text = letter.ToString();
    }

    private void PrepareDebuffCounterText()
    {
        if (DebuffCounterText == null)
            return;

        RectTransform counterRect = DebuffCounterText.GetComponent<RectTransform>();
        if (counterRect != null)
        {
            counterRect.anchorMin = new Vector2(1f, 0f);
            counterRect.anchorMax = new Vector2(1f, 0f);
            counterRect.pivot = new Vector2(1f, 0f);
            counterRect.anchoredPosition = new Vector2(-4f, 4f);
            counterRect.sizeDelta = new Vector2(28f, 28f);
        }

        DebuffCounterText.transform.SetAsLastSibling();
        DebuffCounterText.alignment = TextAlignmentOptions.Center;
        DebuffCounterText.fontStyle = FontStyles.Bold;
        DebuffCounterText.raycastTarget = false;
        DebuffCounterText.color = Color.red;
    }

    private void ApplyStateVisual()
    {
        ClearDebuffVisuals();

        if (CurrentState == TileState.Broken)
        {
            if (button != null)
                button.interactable = false;

            gameObject.SetActive(false);
            return;
        }

        if (button != null)
            button.interactable = IsSelectable();

        if (CurrentState == TileState.Locked)
        {
            if (LockedOverlay != null)
                LockedOverlay.SetActive(true);

            UpdateDebuffCounterVisual();
        }
        else if (CurrentState == TileState.Cracked)
        {
            if (CrackedOverlay != null)
                CrackedOverlay.SetActive(true);

            UpdateDebuffCounterVisual();
        }
    }

    private void UpdateDebuffCounterVisual()
    {
        if (DebuffCounterText == null)
            return;

        bool shouldShowCounter = UsesDebuffCounter(CurrentState);
        DebuffCounterText.gameObject.SetActive(shouldShowCounter);
        DebuffCounterText.text = DebuffCounter.ToString();
    }

    private void OnClick()
    {
        if (!IsSelectable())
        {
            Debug.Log($"[Tile] Click ignored. State: {CurrentState}");
            return;
        }

        if (tileManager != null)
        {
            tileManager.ToggleTilePanel(this);
        }
        else
        {
            Debug.LogWarning($"[Tile] TileManager not found. Click ignored for tile '{name}'.");
        }
    }
}
