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

    // Projectile
    public GameObject playerProjectile;   // Assign your PlayerProjectile prefab
    public Transform magicSpawnPoint;     // Assign hand spawn point
    private int pendingDamage = 0;
    private int currentWordLength = 0;
    private string currentAttackTrigger = "BasicAttack";
    private Coroutine attackCoroutine;

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
            return "MediumAttack";
        else if (wordLength >= 10 && wordLength <= 13)
            return "StrongAttack";
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
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        if (PotionSystem.Instance != null)
            finalDamage = Mathf.RoundToInt(finalDamage * PotionSystem.Instance.GetDamageMultiplier());

        Debug.Log($"Word: {word} | Base: {baseDamage} | Multiplier: {multiplier}x | Potion Multiplier: {PotionSystem.Instance?.GetDamageMultiplier() ?? 1f} | Final Damage: {finalDamage}");

        return finalDamage;
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
    }

    public void CheckWord()
    {
        if (attackButton == null || tileManager == null)
            return;

        string word = tileManager.GetCurrentWord();
        Debug.Log("Current word: " + word + "'Upper: '" + word.ToUpper() + "'");
        Debug.Log("Is valid Word? " + DictionaryManager.Instance.IsValidWord(word));

        attackButton.interactable = DictionaryManager.Instance.IsValidWord(word)
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

        int wordLength = word.Length;
        int destroyedCount = tileManager.GetWordTileCount();

        int finalDamage = CalculateDamage(word);
        pendingDamage = finalDamage;
        currentWordLength = wordLength;
        Debug.Log($"Word: {word} | Length: {wordLength} | Final Damage: {finalDamage}");

        currentAttackTrigger = GetAttackTrigger(wordLength);
        Debug.Log("Attack tier: " + currentAttackTrigger);

        tileManager.DestroyUsedTiles();

        if (movementController != null)
            movementController.BeginAttack();

        // Trigger attack animation
        if (playerAnimator != null)
            playerAnimator.SetTrigger(currentAttackTrigger);
        Debug.Log($"[Animation] Trigger fired: {currentAttackTrigger} | Animator valid: {playerAnimator != null}");

        if (attackButton != null)
            attackButton.interactable = false;

        TileSpawner.Instance.SpawnTiles(destroyedCount);

        attackCoroutine = StartCoroutine(EndAttackAfterAnimation());
    }

    // Called via Animation Event
    public void ShootMagicProjectile()
    {
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

        GameObject proj = Instantiate(playerProjectile, magicSpawnPoint.position, Quaternion.identity);
        HomingProjectile projectileScript = proj.GetComponent<HomingProjectile>();
        if (projectileScript != null)
        {
            int damageToUse = pendingDamage;
            if (damageToUse <= 0 && tileManager != null)
            {
                string word = tileManager.GetCurrentWord().ToUpper();
                if (!string.IsNullOrEmpty(word))
                    damageToUse = CalculateDamage(word);
            }

            if (damageToUse <= 0)
            {
                Debug.LogWarning($"ShootMagicProjectile() has non-positive pendingDamage ({pendingDamage}). Did ExecuteAttack() run?");
            }
            else
            {
                projectileScript.Launch(damageToUse, currentWordLength);
            }
        }
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
