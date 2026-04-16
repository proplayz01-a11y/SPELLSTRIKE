using System.Collections;
using UnityEngine;

public class BrambleSpriteAbilities : MonoBehaviour
{
    [Header("Bramble Snare")]
    [SerializeField] private GameObject brambleSnarePrefab;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float spikeSpeed = 8f;
    [SerializeField] private float snareShakeIntensity = 0.3f;

    [Header("Thorn Toss")]
    [SerializeField] private GameObject thornTossPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float knockbackForce = 2.5f;
    [SerializeField] private int thornDamage = 5;

    [Header("Cooldown")]
    [SerializeField] private float abilityCooldown = 8f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private MovementofPlayer playerMovement;

    private bool abilityActive = false;
    private bool isEnabled = false;

    public void EnableAbilities()
    {
        isEnabled = true;
        StartCoroutine(AbilityLoop());
    }

    IEnumerator AbilityLoop()
    {
        while (isEnabled)
        {
            yield return new WaitForSeconds(abilityCooldown);

            if (!abilityActive)
            {
                yield return StartCoroutine(BrambleSnare());
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(ThornToss());
            }
        }
    }

    IEnumerator BrambleSnare()
    {
        abilityActive = true;

        // Spawn below player
        Vector3 spawnPos = player.position + Vector3.down * 2f;
        GameObject snare = Instantiate(brambleSnarePrefab, spawnPos, Quaternion.identity);

        // Shoot up fast
        float t = 0;
        Vector3 targetPos = player.position;
        while (t < 1f)
        {
            t += Time.deltaTime * spikeSpeed;
            snare.transform.position = Vector3.Lerp(spawnPos, targetPos, t);
            yield return null;
        }

        // Camera shake on hit
        //CameraShakeManager.Instance.Shake(snareShakeIntensity);

        // Stun player
        playerMovement.enabled = false;
        yield return new WaitForSeconds(stunDuration);
        playerMovement.enabled = true;
        Destroy(snare);

        abilityActive = false;
    }

    IEnumerator ThornToss()
    {
        abilityActive = true;

        transform.LookAt(player);

        Vector3 direction = (player.position - throwPoint.position).normalized;

        GameObject thornObj = Instantiate(thornTossPrefab, throwPoint.position, Quaternion.identity);
        ThornProjectile thorn = thornObj.GetComponent<ThornProjectile>();

        if (thorn != null)
        {
            thorn.knockbackForce = knockbackForce;
            thorn.Launch(direction, thornDamage);
        }

        abilityActive = false;
        yield return null; // ← DAGDAG TO
    }

    public void DisableAbilities()
    {
        isEnabled = false;
        StopAllCoroutines();
    }
}