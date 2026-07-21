using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    public Button attackButton;
    public TileManager tileManager;
    public Animator playerAnimator;
    public MovementofPlayer movementController;
    public GoalsPanelUI goalsPanelUI;

    [Header("Stage Tile Debuffs")]
    public TileDebuffManager tileDebuffManager;
    public bool reduceTileDebuffsOnSuccessfulAttack = true;

    // Projectile
    public GameObject playerProjectile;   // Assign your PlayerProjectile prefab
    public Transform magicSpawnPoint;     // Assign hand spawn point
    private int pendingDamage = 0;
    private int currentWordLength = 0;
    private string pendingWord = string.Empty;
    private string currentAttackTrigger = "BasicAttack";
    private Coroutine attackCoroutine;
    private bool attackInputLocked;

    // Rarer letters bonus multiplier
    private readonly HashSet<char> rareLetters = new HashSet<char>() { 'X', 'Z', 'Q', 'K', 'J', 'V' };

    private readonly Dictionary<char, int> letterDamage = new Dictionary<char, int>()
    {
        // Common = 1
        {'A',1},{'E',1},{'I',1},{'O',1},{'U',1},
        {'S',1},{'T',1},{'R',1},{'N',1},{'L',1},
        
        // Uncommon = 2
        {'B',2},{'C',2},{'D',2},{'F',2},{'G',2},
        {'H',2},{'M',2},{'P',2},{'W',2},{'Y',2},
        
        // Rare = 4
        {'J',4},{'K',4},{'Q',4},{'V',4},{'X',4},{'Z',4}
    };

    private float GetLengthMultiplier(int wordLength)
    {
        if (wordLength <= 5) return 1.0f;
        else if (wordLength <= 9) return 1.5f;
        else if (wordLength <= 13) return 2.0f;
        else return 3.0f;
    }

    private string GetAttackTrigger(int wordLength)
    {
        if (wordLength >= 3 && wordLength <= 5)
            return "BasicAttack";
        else if (wordLength >= 6 && wordLength <= 9)
            return "BasicAttack";
        else if (wordLength >= 10 && wordLength <= 13)
            return "BasicAttack";
        else
            return "UltimateAttack";
    }

    private int CalculateDamage(string word)
    {
        int baseDamage = 0;

        foreach (char c in word)
        {
            if (letterDamage.ContainsKey(c))
                baseDamage += letterDamage[c];
            else
                baseDamage += 1; // fallback
        }

        float multiplier = GetLengthMultiplier(word.Length);
        float potionMultiplier = PotionSystem.Instance != null ? PotionSystem.Instance.GetDamageMultiplier() : 1f;
        float passiveMultiplier = GetPassiveDamageMultiplier();
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier * potionMultiplier * passiveMultiplier);

        if (PotionSystem.Instance != null && potionMultiplier > 1f)
            PotionSystem.Instance.ConsumePowerUpAttackCharge();

        Debug.Log($"Word: {word} | Base: {baseDamage} | Multiplier: {multiplier}x | Potion Multiplier: {potionMultiplier} | Passive Multiplier: {passiveMultiplier} | Final Damage: {finalDamage}");

        return finalDamage;
    }

    private float GetPassiveDamageMultiplier()
    {
        PassiveItemInventory passiveInventory = PassiveItemInventory.Instance;
        if (passiveInventory == null)
            passiveInventory = FindFirstObjectByType<PassiveItemInventory>();

        if (passiveInventory != null && passiveInventory.IsEquipped(PassiveItemId.SkySword))
            return 1.10f;

        return 1f;
    }

    void Start()
    {
        if (attackButton != null)
        {
            attackButton.interactable = false;
            attackButton.onClick.AddListener(ExecuteAttack);
        }

        
        

        if (tileManager != null)
            tileManager.attackController = this;

        if (movementController == null)
            movementController = GetComponent<MovementofPlayer>();

        if (goalsPanelUI == null)
            goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();

        if (tileDebuffManager == null)
            tileDebuffManager = FindFirstObjectByType<TileDebuffManager>();
    }

    public void CheckWord()
    {
        if (attackButton == null || tileManager == null)
            return;

        string word = tileManager.GetCurrentWord();
        Debug.Log("Current word: " + word + "'Upper: '" + word.ToUpper() + "'");
        Debug.Log("Is valid Word? " + DictionaryManager.Instance.IsValidWord(word));

        attackButton.interactable = !attackInputLocked
                                    && DictionaryManager.Instance.IsValidWord(word)
                                    && word.Length >= 3
                                    && word.Length <= 16;
    }

    public void ExecuteAttack()

    {
        
        if (tileManager == null || DictionaryManager.Instance == null)
            return;

        if (attackCoroutine != null)
            return;

        string word = tileManager.GetCurrentWord().ToUpper();

        if (!DictionaryManager.Instance.IsValidWord(word))
            return;

        SpellbookManager.Instance.RecordWord(word);

        int wordLength = word.Length;
        int destroyedCount = tileManager.GetWordTileCount();

        if (wordLength >= 6 && goalsPanelUI != null)
        {
            goalsPanelUI.IncrementWordCount();
        }

        int finalDamage = CalculateDamage(word);
        pendingDamage = finalDamage;
        currentWordLength = wordLength;
        pendingWord = word;

        currentAttackTrigger = GetAttackTrigger(wordLength);
        Debug.Log("Attack tier: " + currentAttackTrigger);

        tileManager.DestroyUsedTiles();
        List<char> refillLetters = tileManager.GenerateRefillLettersFromBudget(destroyedCount);

        if (movementController != null)
            movementController.BeginAttack();

        // Trigger attack animation
        if (playerAnimator != null)
            playerAnimator.SetTrigger(currentAttackTrigger);
        Debug.Log($"[Animation] Trigger fired: {currentAttackTrigger} | Animator valid: {playerAnimator != null}");

        if (attackButton != null)
            attackButton.interactable = false;

        if (TileSpawner.Instance != null)
        {
            TileSpawner.Instance.SpawnSpecificTiles(refillLetters);
        }

        attackCoroutine = StartCoroutine(EndAttackAfterAnimation());
    }

    // Called via Animation Event
    public void ShootMagicProjectile()
    {
        Debug.Log($"[ShootMagicProjectile] obj={gameObject.name} id={GetInstanceID()} pendingDamage={pendingDamage} wordLength={currentWordLength}");
        Debug.Log($"[AttackController START] obj={gameObject.name} id={GetInstanceID()} animatorObj={(playerAnimator != null ? playerAnimator.gameObject.name : "NULL")}");
        if (playerProjectile == null || magicSpawnPoint == null)
        {
            Debug.LogWarning("Projectile or spawn point not assigned.");
            return;
        }

        Transform target = GetNearestEnemy();
        if (target == null)
        {
            Debug.Log("No enemy found.");
            return;
        }

        if (pendingDamage <= 0)
        {
            Debug.LogWarning($"ShootMagicProjectile blocked. pendingDamage = {pendingDamage}, currentWordLength = {currentWordLength}");
            return;
        }

        GameObject proj = Instantiate(playerProjectile, magicSpawnPoint.position, Quaternion.identity);
        HomingProjectile projectileScript = proj.GetComponent<HomingProjectile>();

        if (projectileScript != null)
        {
            Debug.Log($"Launching projectile with damage {pendingDamage} and word length {currentWordLength}");
            projectileScript.Launch(pendingDamage, currentWordLength, this, pendingWord);
        }

        pendingDamage = 0;
        currentWordLength = 0;
        pendingWord = string.Empty;
    }

    public void SetAttackInputLocked(bool locked)
    {
        attackInputLocked = locked;

        if (attackButton == null)
            return;

        if (locked)
            attackButton.interactable = false;
        else
            CheckWord();
    }

    public void NotifySuccessfulAttackLanded()
    {
        if (!reduceTileDebuffsOnSuccessfulAttack)
            return;

        if (tileDebuffManager == null)
            tileDebuffManager = FindFirstObjectByType<TileDebuffManager>();

        if (tileDebuffManager == null)
            return;

        tileDebuffManager.ReduceTileDebuffCountersOnSuccessfulAttack();
    }

    Transform GetNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float shortestDistance = Mathf.Infinity;
        GameObject nearest = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = enemy;
            }
        }

        return nearest != null ? nearest.transform : null;
    }

    private IEnumerator EndAttackAfterAnimation()
    {
        float duration = 0.8f;

        if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
        {
            foreach (var clip in playerAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == currentAttackTrigger)
                {
                    duration = clip.length;
                    break;
                }
            }
        }

        yield return new WaitForSeconds(duration + 0.05f);

        if (movementController != null)
            movementController.EndAttack();

        attackCoroutine = null;
    }
}
