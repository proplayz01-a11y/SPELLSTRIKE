using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class VocabularyBarrier : MonoBehaviour
{
    public event Action<VocabularyBarrier> BarrierBroken;

    [Header("Vocabulary")]
    [SerializeField] private StageVocabularyProfile profile;
    [Min(0)] [SerializeField] private int wordIndex;

    [Header("Combat Effect")]
    [SerializeField] private bool barrierActive = true;
    [SerializeField] private bool blockNonTargetDamageWhileActive;
    [Range(0f, 1f)] [SerializeField] private float nonTargetDamageMultiplier = 0.35f;
    [Min(1f)] [SerializeField] private float targetWordDamageMultiplier = 1.25f;
    [SerializeField] private GameObject barrierVisual;
    [Tooltip("Optional screen-space panel root. When assigned, the whole panel is shown and hidden with the challenge.")]
    [SerializeField] private GameObject combatCluePanel;
    [SerializeField] private TMP_Text combatClueText;
    [Tooltip("Keep enabled for existing encounters. Node 1 hides this while Bramble Sprite is dormant.")]
    [SerializeField] private bool revealChallengeOnStart = true;

    [Header("Tile Availability")]
    [SerializeField] private TileManager tileManager;
    [Min(1)] [SerializeField] private int maximumPoolTiles = 16;
    [SerializeField] private bool guaranteeTargetLettersOnStart = true;

    [Header("Runtime Records")]
    [SerializeField] private VocabularyPracticeSession session;

    [Header("Events")]
    [SerializeField] private UnityEvent onChallengeRevealed;
    [SerializeField] private UnityEvent onChallengeHidden;
    [SerializeField] private UnityEvent onBarrierBroken;
    [SerializeField] private UnityEvent onNonTargetWordHit;

    public bool BarrierActive => barrierActive;
    public bool ChallengeVisible => challengeVisible;
    private VocabularyWordData runtimeTargetWord;

    public VocabularyWordData TargetWord =>
        runtimeTargetWord ?? (profile != null ? profile.GetWord(wordIndex) : null);

    private bool challengeVisible;

    private void Awake()
    {
        challengeVisible = revealChallengeOnStart;
    }

    private IEnumerator Start()
    {
        if (tileManager == null)
            tileManager = FindFirstObjectByType<TileManager>();

        if (session == null)
            session = VocabularyPracticeSession.Instance != null
                ? VocabularyPracticeSession.Instance
                : FindFirstObjectByType<VocabularyPracticeSession>();

        ApplyBarrierVisual();
        UpdateClue();

        if (!challengeVisible || !guaranteeTargetLettersOnStart || TargetWord == null || tileManager == null)
            yield break;

        // TileManager fills its pool after the first layout frame.
        yield return null;
        yield return null;

        if (!challengeVisible)
            yield break;

        tileManager.EnsureWordLettersAvailable(TargetWord.Word, maximumPoolTiles);
    }

    public int ResolveHit(
        string submittedWord,
        int incomingDamage,
        bool isTargetChallengeCast = false)
    {
        VocabularyWordData targetWord = TargetWord;
        if (!barrierActive || targetWord == null)
            return incomingDamage;

        bool matchedTarget = targetWord.Matches(submittedWord);
        if (session != null)
            session.RecordCombatSubmission(profile, targetWord, submittedWord, matchedTarget);

        if (matchedTarget)
        {
            barrierActive = false;
            HideChallenge();
            BarrierBroken?.Invoke(this);
            onBarrierBroken?.Invoke();
            if (isTargetChallengeCast)
            {
                Debug.Log(
                    $"[VocabularyBarrier] {targetWord.Word} retrieved. Barrier broken with a barrier-only impact; enemy damage remains 0.",
                    this);
                return 0;
            }

            Debug.Log($"[VocabularyBarrier] {targetWord.Word} retrieved. Barrier broken.", this);
            return Mathf.Max(1, Mathf.RoundToInt(incomingDamage * targetWordDamageMultiplier));
        }

        onNonTargetWordHit?.Invoke();

        if (isTargetChallengeCast || blockNonTargetDamageWhileActive)
        {
            Debug.Log(
                $"[VocabularyBarrier] '{submittedWord}' was valid but not the target word. The active barrier absorbed the hit.",
                this
            );
            return 0;
        }

        int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * nonTargetDamageMultiplier));
        Debug.Log($"[VocabularyBarrier] '{submittedWord}' was valid but not the target word. Damage reduced to {reducedDamage}.", this);
        return reducedDamage;
    }

    public void SetBlockNonTargetDamageWhileActive(bool shouldBlock)
    {
        blockNonTargetDamageWhileActive = shouldBlock;
    }

    public void SetGuaranteeTargetLettersOnStart(bool shouldGuarantee)
    {
        guaranteeTargetLettersOnStart = shouldGuarantee;
    }

    public void SetRuntimeTargetWord(VocabularyWordData targetWord)
    {
        runtimeTargetWord = targetWord;
        UpdateClue();
    }

    public void ClearRuntimeTargetWord()
    {
        runtimeTargetWord = null;
        UpdateClue();
    }

    public void ResetBarrier()
    {
        barrierActive = true;
        ApplyBarrierVisual();
        UpdateClue();

        if (guaranteeTargetLettersOnStart && TargetWord != null && tileManager != null)
            tileManager.EnsureWordLettersAvailable(TargetWord.Word, maximumPoolTiles);
    }

    public void BeginChallenge()
    {
        challengeVisible = true;
        ResetBarrier();
        onChallengeRevealed?.Invoke();
    }

    public void HideChallenge()
    {
        challengeVisible = false;
        ApplyBarrierVisual();
        UpdateClue();
        onChallengeHidden?.Invoke();
    }

    private void ApplyBarrierVisual()
    {
        if (barrierVisual != null)
            barrierVisual.SetActive(challengeVisible && barrierActive);
    }

    private void UpdateClue()
    {
        bool shouldShowClue = challengeVisible && barrierActive;

        if (combatCluePanel != null)
            combatCluePanel.SetActive(shouldShowClue);

        if (combatClueText != null)
        {
            if (combatCluePanel == null)
                combatClueText.gameObject.SetActive(shouldShowClue);

            combatClueText.text = challengeVisible && TargetWord != null
                ? "BREAK THE BARRIER\nForm the target word, then STRIKE."
                : string.Empty;
        }
    }
}
