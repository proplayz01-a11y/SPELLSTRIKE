using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PersistentHUDRoot : MonoBehaviour
{
    private const int PersistentHudSortingOrder = 5;

    public static PersistentHUDRoot Instance { get; private set; }

    private Canvas[] ownedCanvases;
    private Slider healthSlider;
    private Slider enemyHealthSlider;
    private Button attackButton;
    private Transform tilePoolPanel;
    private Transform wordBarPanel;
    private TutorialResponsiveLayout responsiveLayout;
    private MobileStageControls mobileStageControls;
    private EnemyHealth boundEnemyHealth;
    private PotionSidebarUI persistentPotionSidebar;

    public Slider HealthSlider => healthSlider;
    public Slider EnemyHealthSlider => enemyHealthSlider;
    public EnemyHealth BoundEnemyHealth => boundEnemyHealth;
    public Button AttackButton => attackButton;
    public Transform TilePoolPanel => tilePoolPanel;
    public Transform WordBarPanel => wordBarPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        CacheReferences();
        RefreshPersistentPresentation();
    }

    private void Start()
    {
        // All scene Awake callbacks have executed at this point. Existing
        // PotionInventory / PotionSystem instances from a stage or loadout scene
        // take precedence over this direct-stage fallback.
        EnsureSharedPotionBag();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void CacheReferences()
    {
        ownedCanvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in ownedCanvases)
        {
            if (canvas == null)
                continue;

            // The editable Mage HUD places its existing bound HP Text on a
            // nested Canvas above the fixed ornate gold frame (order 20).
            // Forcing that artist-authored sub-Canvas down to the persistent
            // root's order 5 hides the live "100 / 100" behind the artwork.
            // Keep this existing uniquely-named Text on top even if a
            // scene transition cached it at the old global sort order.
            // All ordinary HUD canvases still use the shared order 5.
            if (canvas.gameObject.name == "HP_ActualValue" &&
                canvas.GetComponent<Text>() != null &&
                canvas.GetComponentInParent<PlayerStatusHudSkin>() != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 20;
                continue;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = PersistentHudSortingOrder;
        }

        healthSlider = GetComponentsInChildren<Slider>(true)
            .FirstOrDefault(slider => slider.name == "HealthBar");
        attackButton = GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.name == "AttackButton");
        tilePoolPanel = FindDescendant(transform, "TilePoolPanel");
        wordBarPanel = FindDescendant(transform, "WordBarPanel");

        Canvas combatCanvas = attackButton != null
            ? attackButton.GetComponentInParent<Canvas>(true)
            : null;

        enemyHealthSlider = GetComponentsInChildren<Slider>(true)
            .FirstOrDefault(slider => slider.name == "EnemyHealthBar");
        if (enemyHealthSlider == null)
            enemyHealthSlider = EnsurePersistentEnemyHealthSlider(combatCanvas);

        if (combatCanvas != null)
        {
            responsiveLayout = combatCanvas.GetComponent<TutorialResponsiveLayout>();
            if (responsiveLayout == null)
                responsiveLayout = combatCanvas.gameObject.AddComponent<TutorialResponsiveLayout>();

            mobileStageControls = combatCanvas.GetComponent<MobileStageControls>();
        }

        if (healthSlider == null)
            Debug.LogWarning("[PersistentHUDRoot] HealthBar slider was not found.", this);
        else
        {
            // Presentation only: reuse the authoritative live PlayerHealth
            // Slider and persistent Canvas; never spawn a second health owner.
            PlayerStatusHudSkin skin = healthSlider.GetComponent<PlayerStatusHudSkin>();
            if (skin == null)
                skin = healthSlider.gameObject.AddComponent<PlayerStatusHudSkin>();
            skin.Initialize();
        }
        if (enemyHealthSlider == null)
            Debug.LogWarning("[PersistentHUDRoot] EnemyHealthBar slider was not found.", this);
        if (attackButton == null)
            Debug.LogWarning("[PersistentHUDRoot] AttackButton was not found.", this);
        if (tilePoolPanel == null || wordBarPanel == null)
            Debug.LogWarning("[PersistentHUDRoot] TilePoolPanel or WordBarPanel was not found.", this);
    }

    private Slider EnsurePersistentEnemyHealthSlider(Canvas combatCanvas)
    {
        if (combatCanvas == null)
            return null;

        // Older Stage 1/2 scenes authored EnemyHealthBar outside the persistent
        // HUD root. Adopt that exact slider into the DDOL combat canvas so legacy
        // trigger references still point at the same object and its visual style is
        // preserved across scene loads.
        Scene activeScene = SceneManager.GetActiveScene();
        Slider authored = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(slider =>
                slider != null
                && slider.name == "EnemyHealthBar"
                && !slider.transform.IsChildOf(transform)
                && slider.gameObject.scene == activeScene);

        if (authored != null)
        {
            authored.transform.SetParent(combatCanvas.transform, false);
            authored.gameObject.SetActive(false);
            Debug.Log(
                $"[PersistentHUDRoot] Adopted scene EnemyHealthBar from '{activeScene.name}' into the persistent HUD.",
                authored);
            return authored;
        }

        // New stages may only contain the DDOL HUD bootstrap and no separately
        // authored enemy bar yet. Clone the proven player-health Slider hierarchy,
        // then let TutorialResponsiveLayout place it in the canonical top-right slot.
        if (healthSlider == null)
            return null;

        GameObject clone = Instantiate(healthSlider.gameObject, combatCanvas.transform, false);
        clone.name = "EnemyHealthBar";
        Slider generated = clone.GetComponent<Slider>();
        if (generated == null)
        {
            Destroy(clone);
            return null;
        }

        if (generated.fillRect != null)
        {
            Image fillImage = generated.fillRect.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = new Color(0.7843138f, 0.06666667f, 0.1254902f, 1f);
        }

        generated.interactable = false;
        generated.gameObject.SetActive(false);
        Debug.Log("[PersistentHUDRoot] Generated a persistent EnemyHealthBar from the canonical HealthBar shell.", generated);
        return generated;
    }

    public void SetGameplayVisible(bool visible)
    {
        if (ownedCanvases == null || ownedCanvases.Length == 0)
            CacheReferences();

        foreach (Canvas canvas in ownedCanvases)
        {
            if (canvas != null)
                canvas.enabled = visible;
        }
    }

    public void RefreshPersistentPresentation()
    {
        CacheReferences();
        EnsureSharedPotionBag();
        SetGameplayVisible(true);

        if (healthSlider != null)
            healthSlider.gameObject.SetActive(true);

        if (enemyHealthSlider != null)
            enemyHealthSlider.gameObject.SetActive(
                boundEnemyHealth != null && !boundEnemyHealth.IsDead
                && CanDisplayEnemyHealth(boundEnemyHealth));

        if (attackButton != null)
        {
            Transform attackRoot = attackButton.transform.parent;
            if (attackRoot != null)
                attackRoot.gameObject.SetActive(true);

            attackButton.gameObject.SetActive(true);
        }

        if (tilePoolPanel != null)
            tilePoolPanel.gameObject.SetActive(true);
        if (wordBarPanel != null)
            wordBarPanel.gameObject.SetActive(true);

        responsiveLayout?.ForceRefresh();
        mobileStageControls?.RefreshForCurrentScene();
    }

    private void EnsureSharedPotionBag()
    {
        if (!Application.isPlaying)
            return;

        if (PotionInventory.Instance == null &&
            FindFirstObjectByType<PotionInventory>(FindObjectsInactive.Include) == null)
        {
            // A direct Stage 3/4 launch has no ItemSelectionScene bootstrap.
            // Reuse PotionInventory's existing JSON save owner and defaults.
            new GameObject("PotionInventory_StageHUD_Fallback")
                .AddComponent<PotionInventory>();
        }

        if (PotionSystem.Instance == null &&
            FindFirstObjectByType<PotionSystem>(FindObjectsInactive.Include) == null)
        {
            GameObject host = new GameObject("PotionSystem_StageHUD_Fallback");
            PotionSystem system = host.AddComponent<PotionSystem>();
            system.potionInventory = PotionInventory.Instance;
        }

        if (persistentPotionSidebar != null)
            return;

        // Parent under the same persistent combat Canvas, never a transient
        // stage-owned HUDCanvas. One bag follows the canonical combat HUD.
        Canvas canvas = attackButton != null
            ? attackButton.GetComponentInParent<Canvas>(true)
            : null;
        if (canvas == null)
            return;

        persistentPotionSidebar = canvas.GetComponent<PotionSidebarUI>();
        if (persistentPotionSidebar == null)
            persistentPotionSidebar = canvas.gameObject.AddComponent<PotionSidebarUI>();

        persistentPotionSidebar.targetCanvas = canvas;
        persistentPotionSidebar.potionInventory = PotionInventory.Instance;
        persistentPotionSidebar.potionSystem = PotionSystem.Instance;
        persistentPotionSidebar.passiveItemInventory = PassiveItemInventory.Instance;
        persistentPotionSidebar.toggleButtonAnchoredPosition = new Vector2(24f, -245f);
        persistentPotionSidebar.sidebarAnchoredPosition = new Vector2(136f, -245f);
        persistentPotionSidebar.tooltipAnchoredPosition = new Vector2(136f, -348f);
        persistentPotionSidebar.startOpen = false;
    }

    public void ShowEnemyHealth(EnemyHealth enemy)
    {
        if (enemy == null || enemy.IsDead)
        {
            HideEnemyHealth();
            return;
        }

        // Titan is already present during Node 4 riddles. Never show its HP
        // before the Awakening animation actually enters AwakenedHoldV004,
        // even if StageNodeTrigger or another system requests it early.
        if (!CanDisplayEnemyHealth(enemy))
        {
            if (boundEnemyHealth == enemy)
                HideEnemyHealth(enemy);
            return;
        }

        if (enemyHealthSlider == null)
            CacheReferences();

        HideSceneOwnedEnemyHealthBars();

        if (enemyHealthSlider == null)
        {
            Debug.LogWarning("[PersistentHUDRoot] Cannot show enemy health because EnemyHealthBar is missing.", this);
            return;
        }

        if (boundEnemyHealth != null && boundEnemyHealth != enemy)
            boundEnemyHealth.Died -= HandleBoundEnemyDied;

        boundEnemyHealth = enemy;
        boundEnemyHealth.Died -= HandleBoundEnemyDied;
        boundEnemyHealth.Died += HandleBoundEnemyDied;
        boundEnemyHealth.BindHealthSlider(enemyHealthSlider);
        enemyHealthSlider.gameObject.SetActive(true);

        Debug.Log($"[PersistentHUDRoot] Enemy health shown for '{enemy.name}'.", enemy);
    }

    private static bool CanDisplayEnemyHealth(EnemyHealth enemy)
    {
        if (enemy == null || enemy.IsDead) return false;
        var titan = enemy.GetComponent<TitanEarthbreakerController>();
        return titan == null || titan.IsHealthBarUnlocked;
    }

    public void HideEnemyHealth(EnemyHealth enemy = null)
    {
        if (enemy != null && boundEnemyHealth != null && boundEnemyHealth != enemy)
            return;

        if (boundEnemyHealth != null)
            boundEnemyHealth.Died -= HandleBoundEnemyDied;

        boundEnemyHealth = null;

        if (enemyHealthSlider == null)
            CacheReferences();

        if (enemyHealthSlider != null)
            enemyHealthSlider.gameObject.SetActive(false);

        HideSceneOwnedEnemyHealthBars();
    }

    private void HandleBoundEnemyDied(EnemyHealth enemy)
    {
        HideEnemyHealth(enemy);
        Debug.Log($"[PersistentHUDRoot] Enemy health hidden because '{enemy.name}' died.", enemy);
    }

    private void HideSceneOwnedEnemyHealthBars()
    {
        // When a DDOL HUD survives into the next stage, that stage's authored
        // EnemyHealthBar can still enter active by default. Keep exactly one visual
        // authority: the persistent bar owned by this root.
        foreach (Slider slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider == null || slider == enemyHealthSlider || slider.name != "EnemyHealthBar")
                continue;
            if (slider.transform.IsChildOf(transform))
                continue;

            slider.gameObject.SetActive(false);
        }
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
