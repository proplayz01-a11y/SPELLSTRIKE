using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Connects the tutorial's existing gameplay logic to the SAME persistent HUD
/// prefab as the stages. The tutorial owns its TileManager and TileSpawner;
/// only the visual/input HUD becomes persistent. No new damage or HP rules.
/// </summary>
[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public sealed class TutorialPersistentHudBridge : MonoBehaviour
{
    private const string TutorialSceneName = "TutorialScene";
    private Canvas debugOverlay;
    private bool debugOverlayWasEnabled;
    private GameObject potionBagButton;
    private bool potionBagWasActive;
    private bool bound;
    private float nextOverlayCheck;

    /// <summary>Expose the real persistent Bag only for the guided potion lesson.</summary>
    public bool UnlockPotionBagForLesson()
    {
        PersistentHUDRoot hud = PersistentHUDRoot.Instance;
        if (hud == null)
            return false;
        if (potionBagButton == null)
        {
            Transform bag = FindDescendant(hud.transform, "PotionBagButton");
            if (bag == null)
                return false;
            potionBagButton = bag.gameObject;
        }
        PotionSidebarUI sidebar = hud.GetComponentInChildren<PotionSidebarUI>(true);
        if (sidebar == null || !sidebar.enabled)
            return false;
        potionBagButton.SetActive(true);
        sidebar.CloseSidebar();
        sidebar.Refresh();
        return potionBagButton.GetComponent<Button>() != null;
    }

    public Button GetPotionBagButton()
    {
        return potionBagButton != null ? potionBagButton.GetComponent<Button>() : null;
    }

    public Button GetHealthPotionButton()
    {
        PersistentHUDRoot hud = PersistentHUDRoot.Instance;
        Transform slot = hud != null ? FindDescendant(hud.transform, "HPotionSlot") : null;
        return slot != null ? slot.GetComponent<Button>() : null;
    }

    private IEnumerator Start()
    {
        if (gameObject.scene.name != TutorialSceneName)
            yield break;
    
        // Keep only the canonical objects after a scene or script-domain reload.
        PruneDuplicatePersistentRoots();
    
        // A direct Tutorial launch has the prefab in-scene. Story-to-Tutorial
        // transitions can instead reuse an already-surviving singleton.
        for (int attempts = 0; attempts < 6 && !bound; attempts++)
        {
            bound = BindExistingGameplayToSharedHud();
            if (!bound)
                yield return null;
        }
        if (!bound)
            Debug.LogError("[TutorialPersistentHudBridge] Canonical persistent HUD was unavailable; legacy gameplay HUD was left untouched.", this);
    }

    private bool BindExistingGameplayToSharedHud()
    {
        PersistentHUDRoot hud = PersistentHUDRoot.Instance;
        if (hud == null)
            return false;
    
        var scene = gameObject.scene;
        TileManager tiles = FindInScene<TileManager>(scene);
        TileSpawner spawner = FindInScene<TileSpawner>(scene);
        AttackController attack = PersistentPlayerRoot.Instance != null
            ? PersistentPlayerRoot.Instance.Attack
            : FindFirstObjectByType<AttackController>();
        PlayerHealth health = attack != null ? attack.GetComponent<PlayerHealth>() : null;
    
        if (tiles == null || spawner == null || attack == null || health == null)
            return false;
    
        hud.CacheReferences();
        if (hud.HealthSlider == null || hud.AttackButton == null ||
            hud.WordBarPanel == null || hud.TilePoolPanel == null)
            return false;
    
        // Tutorial is a guided word lesson, NOT Stage 1 normal combat. Keep
        // its pool empty until TutorialDirector explicitly spawns lesson tiles.
        tiles.prefillTilePoolOnStart = false;
        tiles.RebindPersistentHud(
            hud.TilePoolPanel, hud.WordBarPanel, attack, clearExistingTiles: false);
        spawner.tilePoolPanel = hud.TilePoolPanel;
        attack.RebindSceneDependencies(hud.AttackButton, tiles);
        health.BindHealthSlider(hud.HealthSlider);
    
        // Hide duplicated authored gameplay controls, including the old
        // scene-owned pause button. The persistent HUD is the single pause owner.
        string[] oldVisuals =
        {
            "HealthBar", "EnemyHealthBar",
            "TilePoolPanel", "TilePoolPanelDesign", "TilePoolPanelDesign (1)",
            "WordBarPanel", "UI_Buttons", "MobileMovementJoystick", "PauseButton"
        };
        foreach (Transform t in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
        {
            if (oldVisuals.Contains(t.name))
                t.gameObject.SetActive(false);
        }
    
        // The full persistent prefab includes stage-specific potion controls;
        // keep the tutorial uncluttered, then restore the button on scene exit.
        Transform bag = FindDescendant(hud.transform, "PotionBagButton");
        if (bag != null)
        {
            potionBagButton = bag.gameObject;
            potionBagWasActive = bag.gameObject.activeSelf;
            bag.gameObject.SetActive(false);
        }
    
        hud.RefreshPersistentPresentation();
        if (potionBagButton != null)
            potionBagButton.SetActive(false);
    
        SuppressTutorialDebugOverlay();
        Debug.Log("[TutorialPersistentHudBridge] Tutorial HP, attack, word bar, tiles, joystick and pause now use the canonical persistent HUD.", this);
        return true;
    }

    private void LateUpdate()
    {
        if (!bound || Time.unscaledTime < nextOverlayCheck)
            return;
        nextOverlayCheck = Time.unscaledTime + 1f;
        SuppressTutorialDebugOverlay();
    }

    private void SuppressTutorialDebugOverlay()
    {
        if (debugOverlay == null)
        {
            debugOverlay = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c != null &&
                    c.name == "TileGenerationDebugOverlayCanvas");
            if (debugOverlay != null)
                debugOverlayWasEnabled = debugOverlay.enabled;
        }
        if (debugOverlay != null)
            debugOverlay.enabled = false;
    }

    private void OnDestroy()
    {
        // This HUD survives into later stages, where both the potion bag and
        // optional developer overlay should work as they did before tutorial.
        if (debugOverlay != null)
            debugOverlay.enabled = debugOverlayWasEnabled;
        if (potionBagButton != null)
            potionBagButton.SetActive(potionBagWasActive);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform match = FindDescendant(child, name);
            if (match != null) return match;
        }
        return null;
    }


    private static void PruneDuplicatePersistentRoots()
    {
        PersistentHUDRoot canonicalHud = PersistentHUDRoot.Instance;
        if (canonicalHud != null)
        {
            PersistentHUDRoot[] hudRoots = FindObjectsByType<PersistentHUDRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (PersistentHUDRoot candidate in hudRoots)
            {
                if (candidate == null || candidate == canonicalHud ||
                    candidate.gameObject == canonicalHud.gameObject)
                    continue;
    
                GameObject duplicate = candidate.gameObject;
                duplicate.SetActive(false);
                Destroy(duplicate);
                Debug.LogWarning("[TutorialPersistentHudBridge] Removed a duplicate persistent gameplay HUD.", canonicalHud);
            }
        }
    
        PersistentPlayerRoot canonicalPlayer = PersistentPlayerRoot.Instance;
        if (canonicalPlayer == null)
            return;
    
        PersistentPlayerRoot[] playerRoots = FindObjectsByType<PersistentPlayerRoot>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PersistentPlayerRoot candidate in playerRoots)
        {
            if (candidate == null || candidate == canonicalPlayer ||
                candidate.gameObject == canonicalPlayer.gameObject)
                continue;
    
            GameObject duplicate = candidate.gameObject;
            duplicate.SetActive(false);
            Destroy(duplicate);
            Debug.LogWarning("[TutorialPersistentHudBridge] Removed a duplicate persistent player root.", canonicalPlayer);
        }
    }
}
