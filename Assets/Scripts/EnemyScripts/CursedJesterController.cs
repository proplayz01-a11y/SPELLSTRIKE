using UnityEngine;
using UnityEngine.AI;

public class CursedJesterController : EnemyController
{
    [Header("Evasion")]
    [Range(0f, 1f)]
    public float evadeChance = 0.25f;
    public float teleportRadius = 4f;

    public override void TakeDamage(float damage, int wordLength = 0)
    {
        if (Random.value < evadeChance && currentState != EnemyState.Dead)
        {
            TeleportAroundPlayer();
            Debug.Log("Cursed Jester evaded the attack!");
            return;
        }

        base.TakeDamage(damage, wordLength);
    }

    private void TeleportAroundPlayer()
    {
        if (player == null || agent == null) return;

        Vector3 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 destination = player.position + new Vector3(randomDirection.x, 0f, randomDirection.y) * teleportRadius;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            transform.position = hit.position;
        }
    }
}
