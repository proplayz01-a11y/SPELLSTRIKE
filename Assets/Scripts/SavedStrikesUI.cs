using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavedStrikesUI : MonoBehaviour
{
    [System.Serializable]
    public class StrikeRowUI
    {
        public Transform tileContainer;
        public Button useButton;
    }

    private class SavedStrike
    {
        public string word;
        public int damage;
        public bool occupied;
    }

    [Header("Panel")]
    public GameObject strikesCanvas;

    [Header("Rows (Top to Bottom)")]
    public StrikeRowUI strike1Row;
    public StrikeRowUI strike2Row;
    public StrikeRowUI strike3Row;

    [Header("Tile Rendering")]
    public Image tileImagePrefab;
    public TextMeshProUGUI missingSpriteLetterPrefab;
    public string tileResourcePrefix = "tile-";

    [Header("Enemy Targeting")]
    public string enemyTag = "Enemy";

    private readonly List<StrikeRowUI> rows = new List<StrikeRowUI>();
    private readonly List<SavedStrike> saved = new List<SavedStrike>();

    private void Awake()
    {
        rows.Add(strike1Row);
        rows.Add(strike2Row);
        rows.Add(strike3Row);

        for (int i = 0; i < 3; i++)
            saved.Add(new SavedStrike());
    }

    private void Start()
    {
        if (strikesCanvas != null)
            strikesCanvas.SetActive(false);

        for (int i = 0; i < rows.Count; i++)
        {
            int captured = i;
            if (rows[captured] != null && rows[captured].useButton != null)
            {
                rows[captured].useButton.onClick.RemoveAllListeners();
                rows[captured].useButton.onClick.AddListener(() => UseStrike(captured));
                rows[captured].useButton.interactable = false;
            }
        }

        Debug.Log("[SavedStrikesUI] Initialized.");
    }

    public void TogglePanel()
    {
        if (strikesCanvas == null)
        {
            Debug.LogWarning("[SavedStrikesUI] StrikesCanvas reference is missing.");
            return;
        }

        bool next = !strikesCanvas.activeSelf;
        strikesCanvas.SetActive(next);
        Debug.Log($"[SavedStrikesUI] Strikes panel toggled: {(next ? "Opened" : "Closed")}");
    }

    public void ClosePanel()
    {
        if (strikesCanvas == null)
        {
            Debug.LogWarning("[SavedStrikesUI] StrikesCanvas reference is missing.");
            return;
        }

        strikesCanvas.SetActive(false);
        Debug.Log("[SavedStrikesUI] Strikes panel closed.");
    }

    public bool CanAddStrike()
    {
        for (int i = 0; i < saved.Count; i++)
        {
            if (!saved[i].occupied) return true;
        }
        return false;
    }

    public void AddStrike(string word, int damage)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            Debug.LogWarning("[SavedStrikesUI] AddStrike blocked: empty word.");
            return;
        }

        word = word.Trim().ToUpperInvariant();

        if (word.Length >= 13)
        {
            Debug.Log("[SavedStrikesUI] AddStrike blocked: ultimate attacks (13+) cannot be saved.");
            return;
        }

        if (damage <= 0)
        {
            Debug.LogWarning("[SavedStrikesUI] AddStrike blocked: damage must be > 0.");
            return;
        }

        int slot = GetFirstEmptySlot();
        if (slot < 0)
        {
            Debug.Log("[SavedStrikesUI] AddStrike blocked: all 3 slots are occupied.");
            return;
        }

        saved[slot].word = word;
        saved[slot].damage = damage;
        saved[slot].occupied = true;

        RenderRow(slot);
        SetRowButtonInteractable(slot, true);

        Debug.Log($"[SavedStrikesUI] Strike saved in slot {slot + 1}: {word} (Damage {damage})");
    }

    public void ClearAllStrikes()
    {
        for (int i = 0; i < saved.Count; i++)
        {
            saved[i].word = string.Empty;
            saved[i].damage = 0;
            saved[i].occupied = false;
            ClearRowVisuals(i);
            SetRowButtonInteractable(i, false);
        }

        Debug.Log("[SavedStrikesUI] All saved strikes cleared.");
    }

    private void UseStrike(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= saved.Count) return;
        if (!saved[slotIndex].occupied) return;

        SavedStrike strike = saved[slotIndex];
        bool hit = DealDamageToMainEnemy(strike.damage);

        if (!hit)
        {
            Debug.Log("[SavedStrikesUI] USE pressed but no main enemy target found.");
            return;
        }

        saved[slotIndex].word = string.Empty;
        saved[slotIndex].damage = 0;
        saved[slotIndex].occupied = false;

        ClearRowVisuals(slotIndex);
        SetRowButtonInteractable(slotIndex, false);

        Debug.Log($"[SavedStrikesUI] Strike released from slot {slotIndex + 1}. Damage dealt: {strike.damage}");
    }

    private bool DealDamageToMainEnemy(int damage)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0) return false;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        GameObject mainEnemy = GetNearestActiveEnemy(enemies, player);
        if (mainEnemy == null) return false;

        EnemyHealth enemyHealth = mainEnemy.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            return true;
        }

        EnemyController enemyController = mainEnemy.GetComponentInParent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.TakeDamage(damage);
            return true;
        }

        BrambleSpriteController bramble = mainEnemy.GetComponentInParent<BrambleSpriteController>();
        if (bramble != null)
        {
            bramble.TakeDamage(damage);
            return true;
        }

        return false;
    }

    private GameObject GetNearestActiveEnemy(GameObject[] enemies, Transform player)
    {
        GameObject nearest = null;
        float best = Mathf.Infinity;
        Vector3 origin = player != null ? player.position : transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;

            float d = Vector3.Distance(origin, e.transform.position);
            if (d < best)
            {
                best = d;
                nearest = e;
            }
        }

        return nearest;
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < saved.Count; i++)
        {
            if (!saved[i].occupied)
                return i;
        }
        return -1;
    }

    private void RenderRow(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= rows.Count) return;
        if (rows[slotIndex] == null || rows[slotIndex].tileContainer == null) return;

        ClearRowVisuals(slotIndex);

        string word = saved[slotIndex].word;
        for (int i = 0; i < word.Length; i++)
        {
            char letter = word[i];
            string resourceName = tileResourcePrefix + letter;
            Sprite sprite = Resources.Load<Sprite>(resourceName);

            if (sprite != null && tileImagePrefab != null)
            {
                Image tile = Instantiate(tileImagePrefab, rows[slotIndex].tileContainer);
                tile.sprite = sprite;
                tile.gameObject.SetActive(true);
            }
            else if (missingSpriteLetterPrefab != null)
            {
                TextMeshProUGUI fallback = Instantiate(missingSpriteLetterPrefab, rows[slotIndex].tileContainer);
                fallback.text = letter.ToString();
                fallback.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[SavedStrikesUI] Missing tile sprite and fallback prefab for letter '{letter}'.");
            }
        }
    }

    private void ClearRowVisuals(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= rows.Count) return;
        if (rows[slotIndex] == null || rows[slotIndex].tileContainer == null) return;

        Transform container = rows[slotIndex].tileContainer;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private void SetRowButtonInteractable(int slotIndex, bool state)
    {
        if (slotIndex < 0 || slotIndex >= rows.Count) return;
        if (rows[slotIndex] == null || rows[slotIndex].useButton == null) return;
        rows[slotIndex].useButton.interactable = state;
    }
}
