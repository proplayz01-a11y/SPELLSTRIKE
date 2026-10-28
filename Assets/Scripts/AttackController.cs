using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class AttackController : MonoBehaviour
{
    public event Action<string> WordSubmitted;
    public event Action<string> WordSubmissionRejected;
    public event Action<string> SuccessfulNormalCombatWordLanded;
    public bool IsAttackInProgress => attackCoroutine != null;
    public bool IsAttackInputLocked => attackInputLocked;

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
    private bool projectileReleasedForCurrentAttack;
    private string currentAttackTrigger = "BasicAttack";
    private Coroutine attackCoroutine;
    private bool attackInputLocked;
    private Func<string, bool> wordSubmissionFilter;
    private bool targetChallengeCastInProgress;
    private float stage3RiddleDamageBonusFraction;

    public float Stage3RiddleDamageBonusFraction => stage3RiddleDamageBonusFraction;

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

    private int CalculateDamage(string word, bool applyCombatModifiers = true)
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
        float potionMultiplier = applyCombatModifiers && PotionSystem.Instance != null
            ? PotionSystem.Instance.GetDamageMultiplier()
            : 1f;
        float passiveMultiplier = applyCombatModifiers ? GetPassiveDamageMultiplier() : 1f;
        float riddleMultiplier = applyCombatModifiers
            ? 1f + Mathf.Max(0f, stage3RiddleDamageBonusFraction)
            : 1f;
        int finalDamage = Mathf.RoundToInt(
            baseDamage * multiplier * potionMultiplier * passiveMultiplier * riddleMultiplier);

        if (applyCombatModifiers && PotionSystem.Instance != null && potionMultiplier > 1f)
            PotionSystem.Instance.ConsumePowerUpAttackCharge();

        Debug.Log($"Word: {word} | Base: {baseDamage} | Multiplier: {multiplier}x | Potion Multiplier: {potionMultiplier} | Passive Multiplier: {passiveMultiplier} | Riddle Multiplier: {riddleMultiplier} | Final Damage: {finalDamage}");

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

    public void SetStage3RiddleDamageBonus(float bonusFraction)
    {
        stage3RiddleDamageBonusFraction = Mathf.Max(0f, bonusFraction);
        Debug.Log(
            $"[AttackController] Stage 3 riddle damage bonus set to +{stage3RiddleDamageBonusFraction * 100f:0}%.",
            this);
    }

    public void ClearStage3RiddleDamageBonus()
    {
        SetStage3RiddleDamageBonus(0f);
    }

    void Start()
    {
        if (attackButton != null)
        {
            attackButton.interactable = false;
            attackButton.onClick.RemoveListener(ExecuteAttack);
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

    public void RebindSceneDependencies(Button persistentAttackButton, TileManager sceneTileManager)
    {
        if (attackButton != null && attackButton != persistentAttackButton)
            attackButton.onClick.RemoveListener(ExecuteAttack);

        attackButton = persistentAttackButton;
        tileManager = sceneTileManager;

        if (attackButton != null)
        {
            attackButton.onClick.RemoveListener(ExecuteAttack);
            attackButton.onClick.AddListener(ExecuteAttack);
        }

        if (tileManager != null)
            tileManager.attackController = this;

        goalsPanelUI = FindFirstObjectByType<GoalsPanelUI>();
        tileDebuffManager = FindFirstObjectByType<TileDebuffManager>();
        CheckWord();
    }

    public void CheckWord()
    {
        if (attackButton == null || tileManager == null)
            return;

        // ClearAllTiles can run during Tutorial/bootstrap frames before the dictionary
        // singleton is ready. Treat that state as "not attackable" instead of throwing.
        if (DictionaryManager.Instance == null)
        {
            attackButton.interactable = false;
            return;
        }

        string word = tileManager.GetCurrentWord();

        bool isValidWord = DictionaryManager.Instance.IsValidWord(word);


        if (tileManager.CurrentMode == TilePoolMode.TargetChallenge)
        {
            attackButton.interactable = !attackInputLocked
                                        && !targetChallengeCastInProgress
                                        && !tileManager.HasPendingTargetChallengeSubmission
                                        && isValidWord
                                        && word.Length >= 3
                                        && word.Length <= 16;
            return;
        }

        attackButton.interactable = !attackInputLocked
                                    && isValidWord
                                    && word.Length >= 3
                                    && word.Length <= 16;
    }

    public void ExecuteAttack()

    {
        
        if (tileManager == null)
            return;

        if (attackCoroutine != null)
            return;

        string word = tileManager.GetCurrentWord().ToUpper();

        if (tileManager.CurrentMode == TilePoolMode.TargetChallenge)
        {
            // Cyan riddle targets are authored challenge answers, not normal
            // dictionary-combat words (e.g. WIND). The target barrier checks
            // the actual submitted letters on projectile impact. A wrong
            // answer must reach that check to trigger its red-laser penalty.
            if (attackInputLocked)
                return;

            TargetChallengeSubmissionResult challengeResult =
                tileManager.TryBeginTargetChallengeSubmission(word);

            if (challengeResult == TargetChallengeSubmissionResult.PendingImpact)
                BeginTargetChallengeAttack(word);

            return;
        }

        // Preserve dictionary validation for NORMAL combat only.
        if (DictionaryManager.Instance == null || !DictionaryManager.Instance.IsValidWord(word))
            return;

        if (attackInputLocked)
            return;

        if (wordSubmissionFilter != null && !wordSubmissionFilter(word))
        {
            WordSubmissionRejected?.Invoke(word);
            tileManager.ClearWordBar();
            return;
        }

        WordSubmitted?.Invoke(word);
        if (SpellbookManager.Instance != null)
        {
            SpellbookManager.Instance.RecordWord(word);
        }

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
        projectileReleasedForCurrentAttack = false;
        Debug.Log("Attack tier: " + currentAttackTrigger);

        tileManager.DestroyUsedTiles();
        List<char> refillLetters = tileManager.GenerateRefillLettersFromBudget(destroyedCount, word);

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

    private void BeginTargetChallengeAttack(string word)
    {
        targetChallengeCastInProgress = true;

        WordSubmitted?.Invoke(word);
        if (SpellbookManager.Instance != null)
            SpellbookManager.Instance.RecordWord(word);

        int wordLength = word.Length;
        pendingDamage = CalculateDamage(word, false);
        currentWordLength = wordLength;
        pendingWord = word;
        currentAttackTrigger = GetAttackTrigger(wordLength);
        projectileReleasedForCurrentAttack = false;

        if (wordLength >= 6 && goalsPanelUI != null)
            goalsPanelUI.IncrementWordCount();

        if (movementController != null)
            movementController.BeginAttack();

        if (playerAnimator != null)
            playerAnimator.SetTrigger(currentAttackTrigger);

        if (attackButton != null)
            attackButton.interactable = false;

        Debug.Log(
            $"[AttackController] Target Challenge cast started for '{word}'. "
            + "Temporary tiles remain frozen until projectile impact.",
            this);
        attackCoroutine = StartCoroutine(EndAttackAfterAnimation());
    }

    // Called via Animation Event
    public void ShootMagicProjectile()
    {
        if (projectileReleasedForCurrentAttack)
        {
            Debug.LogWarning($"[AttackController] Duplicate projectile release ignored for {currentAttackTrigger}.", this);
            return;
        }

        Debug.Log($"[ShootMagicProjectile] obj={gameObject.name} id={GetInstanceID()} pendingDamage={pendingDamage} wordLength={currentWordLength}");
        Debug.Log($"[AttackController START] obj={gameObject.name} id={GetInstanceID()} animatorObj={(playerAnimator != null ? playerAnimator.gameObject.name : "NULL")}");
        if (playerProjectile == null || magicSpawnPoint == null)
        {
            Debug.LogWarning("Projectile or spawn point not assigned.");
            CancelTargetChallengeCast("Projectile prefab or spawn point is missing.");
            return;
        }

        Transform target = GetCurrentAttackTarget();
        if (target == null)
        {
            Debug.Log("No enemy found.");
            CancelTargetChallengeCast("No enemy target was available.");
            return;
        }

        if (pendingDamage <= 0)
        {
            Debug.LogWarning($"ShootMagicProjectile blocked. pendingDamage = {pendingDamage}, currentWordLength = {currentWordLength}");
            CancelTargetChallengeCast("Pending projectile damage was invalid.");
            return;
        }

        GameObject proj = Instantiate(playerProjectile, magicSpawnPoint.position, Quaternion.identity);
        HomingProjectile projectileScript = proj.GetComponent<HomingProjectile>();

        if (projectileScript != null)
        {
            Debug.Log($"Launching projectile with damage {pendingDamage} and word length {currentWordLength}");
            projectileScript.Launch(
                pendingDamage,
                currentWordLength,
                this,
                pendingWord,
                targetChallengeCastInProgress,
                target);
            projectileReleasedForCurrentAttack = true;
        }
        else
        {
            Destroy(proj);
            CancelTargetChallengeCast("Projectile prefab has no HomingProjectile component.");
            return;
        }

        pendingDamage = 0;
        currentWordLength = 0;
        pendingWord = string.Empty;
    }

    // Animation Event: place on the exact casting release frame.
    public void OnAttackRelease()
    {
        ShootMagicProjectile();
    }

    // Animation Event: place on the final active frame of every attack clip.
    public void OnAttackAnimationFinished()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (movementController != null)
            movementController.EndAttack();
    }

    public bool CanResolveTargetChallengeAgainst(VocabularyBarrier barrier)
    {
        return targetChallengeCastInProgress
               && barrier != null
               && barrier.BarrierActive
               && barrier.ChallengeVisible
               && tileManager != null
               && tileManager.DoesActiveTargetChallengeMatch(barrier.TargetWord);
    }

    public void NotifyTargetChallengeProjectileResolved(
        string submittedWord,
        VocabularyBarrier barrier,
        bool matchedActiveBarrier)
    {
        if (!targetChallengeCastInProgress)
            return;

        if (!matchedActiveBarrier)
        {
            CancelTargetChallengeCast(
                barrier == null
                    ? "The projectile did not hit an active vocabulary barrier."
                    : "The projectile hit a barrier assigned to a different or inactive target challenge.");
            return;
        }

        TargetChallengeSubmissionResult result =
            tileManager.ResolveTargetChallengeImpact(submittedWord);

        if (result == TargetChallengeSubmissionResult.IncorrectTarget)
            WordSubmissionRejected?.Invoke(submittedWord);

        if (result == TargetChallengeSubmissionResult.NotActive)
        {
            CancelTargetChallengeCast("The confirmed impact did not match the pending challenge submission.");
            return;
        }

        targetChallengeCastInProgress = false;
        CheckWord();
    }

    private void CancelTargetChallengeCast(string reason)
    {
        if (!targetChallengeCastInProgress)
            return;

        targetChallengeCastInProgress = false;
        pendingDamage = 0;
        currentWordLength = 0;
        pendingWord = string.Empty;

        if (tileManager != null)
            tileManager.CancelPendingTargetChallengeSubmission(reason);

        CheckWord();
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

    public void SetWordSubmissionFilter(Func<string, bool> submissionFilter)
    {
        wordSubmissionFilter = submissionFilter;
        CheckWord();
    }

    public void ClearWordSubmissionFilter()
    {
        wordSubmissionFilter = null;
        CheckWord();
    }

    public void NotifySuccessfulAttackLanded(string submittedWord = "")
    {
        string normalizedWord = string.IsNullOrWhiteSpace(submittedWord)
            ? string.Empty
            : submittedWord.Trim().ToUpperInvariant();

        if (!string.IsNullOrEmpty(normalizedWord))
            SuccessfulNormalCombatWordLanded?.Invoke(normalizedWord);

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

    private Transform GetCurrentAttackTarget()
    {
        if (targetChallengeCastInProgress)
        {
            VocabularyWorldTarget worldTarget =
                VocabularyWorldTarget.FindClosestActive(transform.position);
            if (worldTarget != null)
                return worldTarget.TargetPoint;
        }

        return GetNearestEnemy();
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
