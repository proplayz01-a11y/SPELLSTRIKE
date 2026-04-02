using System.Collections;
using UnityEngine;

public class HollowKnightBossController : EnemyController
{
    [Header("Phase Change")]
    [Range(0.1f, 0.9f)]
    public float phaseTwoThreshold = 0.5f;
    public bool phaseTwoStarted = false;
    public GameObject summonPrefab;
    public Transform[] summonPoints;
    public int summonCount = 2;
    public float phaseTwoDamageMultiplier = 1.2f;

    public override void TakeDamage(float damage, int wordLength = 0)
    {
        float modifiedDamage = phaseTwoStarted ? damage * phaseTwoDamageMultiplier : damage;
        base.TakeDamage(modifiedDamage, wordLength);

        if (!phaseTwoStarted && currentHealth <= maxHealth * phaseTwoThreshold)
        {
            StartPhaseTwo();
        }
    }

    private void StartPhaseTwo()
    {
        phaseTwoStarted = true;
        if (animator != null)
            animator.SetTrigger("PhaseTwo");

        if (summonPrefab != null && summonPoints != null && summonPoints.Length > 0)
        {
            for (int i = 0; i < summonCount; i++)
            {
                Transform spawnPoint = summonPoints[i % summonPoints.Length];
                Instantiate(summonPrefab, spawnPoint.position, spawnPoint.rotation);
            }
        }

        Debug.Log("Hollow Knight boss entered Phase Two and summoned helpers.");
    }
}
