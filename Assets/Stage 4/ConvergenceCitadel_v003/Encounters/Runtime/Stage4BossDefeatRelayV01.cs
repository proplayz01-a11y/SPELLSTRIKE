using System.Collections;
using UnityEngine;

/// <summary>
/// Stage 4-only adapter for bosses without a scene-local death-completion callback.
/// Lives on the encounter node, so destroying the actor cannot cancel its callback.
/// </summary>
public sealed class Stage4BossDefeatRelayV01 : MonoBehaviour
{
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private StageNodeController node;
    [SerializeField, Min(0f)] private float postDeathSeconds = 2.5f;
    private bool dispatched;

    public void Configure(EnemyHealth health, StageNodeController receiver, float delaySeconds)
    {
        if (isActiveAndEnabled && bossHealth != null)
            bossHealth.Died -= OnBossDied;
        bossHealth = health;
        node = receiver;
        postDeathSeconds = Mathf.Max(0f, delaySeconds);
        if (isActiveAndEnabled && bossHealth != null)
            bossHealth.Died += OnBossDied;
    }

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.Died += OnBossDied;
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.Died -= OnBossDied;
    }

    private void OnBossDied(EnemyHealth health)
    {
        if (dispatched || health != bossHealth || node == null)
            return;
        dispatched = true;
        StartCoroutine(CompleteAfterPresentation());
    }

    private IEnumerator CompleteAfterPresentation()
    {
        if (postDeathSeconds > 0f)
            yield return new WaitForSeconds(postDeathSeconds);
        if (node != null)
            node.OnEnemyDefeated();
    }
}
