using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class VocabularyBarrier : MonoBehaviour
{
    [Header("Vocabulary")]
    [SerializeField] private StageVocabularyProfile profile;
    [Min(0)] [SerializeField] private int wordIndex;

    [Header("Combat Effect")]
    [SerializeField] private bool barrierActive = true;
    [Range(0f, 1f)] [SerializeField] private float nonTargetDamageMultiplier = 0.35f;
    [Min(1f)] [SerializeField] private float targetWordDamageMultiplier = 1.25f;
    [SerializeField] private GameObject barrierVisual;
    [SerializeField] private TMP_Text combatClueText;

    [Header("Tile Availability")]
    [SerializeField] private TileManager tileManager;
    [Min(1)] [SerializeField] private int maximumPoolTiles = 16;
    [SerializeField] private bool guaranteeTargetLettersOnStart = true;

    [Header("Runtime Records")]
    [SerializeField] private VocabularyPracticeSession session;

    [Header("Events")]
    [SerializeField] private UnityEvent onBarrierBroken;
    [SerializeField] private UnityEvent onNonTargetWordHit;

    public bool BarrierActive => barrierActive;
    public VocabularyWordData TargetWord => profile != null ? profile.GetWord(wordIndex) : null;

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

        if (!guaranteeTargetLettersOnStart || TargetWord == null || tileManager == null)
            yield break;

        // TileManager fills its pool after the first layout frame.
        yield return null;
        yield return null;
        tileManager.EnsureWordLettersAvailable(TargetWord.Word, maximumPoolTiles);
    }

    public int ResolveHit(string submittedWord, int incomingDamage)
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
            ApplyBarrierVisual();
            onBarrierBroken?.Invoke();
            Debug.Log($"[VocabularyBarrier] {targetWord.Word} retrieved. Barrier broken.", this);
            return Mathf.Max(1, Mathf.RoundToInt(incomingDamage * targetWordDamageMultiplier));
        }

        onNonTargetWordHit?.Invoke();
        int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * nonTargetDamageMultiplier));
        Debug.Log($"[VocabularyBarrier] '{submittedWord}' was valid but not the target word. Damage reduced to {reducedDamage}.", this);
        return reducedDamage;
    }

    public void ResetBarrier()
    {
        barrierActive = true;
        ApplyBarrierVisual();
        UpdateClue();

        if (TargetWord != null && tileManager != null)
            tileManager.EnsureWordLettersAvailable(TargetWord.Word, maximumPoolTiles);
    }

    private void ApplyBarrierVisual()
    {
        if (barrierVisual != null)
            barrierVisual.SetActive(barrierActive);
    }

    private void UpdateClue()
    {
        if (combatClueText != null)
            combatClueText.text = TargetWord != null ? TargetWord.CombatClue : string.Empty;
    }
}
