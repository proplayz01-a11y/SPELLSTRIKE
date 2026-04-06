using UnityEngine;

public class StoneSentinelController : EnemyController
{
    [Header("Shield")]
    public bool shieldActive = true;
    public float shieldReduction = 0.5f;
    public int minWordLengthToBreakShield = 5;
    public GameObject shieldBreakEffect;

    public override void TakeDamage(float damage, int wordLength = 0)
    {
        if (shieldActive)
        {
            if (wordLength >= minWordLengthToBreakShield)
            {
                shieldActive = false;
                if (shieldBreakEffect != null)
                    Instantiate(shieldBreakEffect, transform.position, Quaternion.identity);

                Debug.Log("Stone Sentinel shield broken by a long word!");
            }
            else
            {
                damage = Mathf.RoundToInt(damage * shieldReduction);
                Debug.Log("Stone Sentinel shield absorbed damage.");
            }
        }

        base.TakeDamage(damage, wordLength);
    }
}
