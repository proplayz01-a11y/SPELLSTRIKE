using System.Collections;
using UnityEngine;

public sealed class TutorialTrainingDummy : MonoBehaviour
{
    [SerializeField] private TutorialDirector tutorialDirector;
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "BeingHit";
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField, Min(0f)] private float hideDelay = 1.25f;

    [Header("Unrigged Dummy Reaction")]
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0f)] private float hitTiltDegrees = 10f;
    [SerializeField, Min(0.01f)] private float hitReactionDuration = 0.18f;
    [SerializeField, Min(0f)] private float deathTiltDegrees = 70f;
    [SerializeField, Min(0.01f)] private float deathReactionDuration = 0.45f;

    private bool defeated;
    private Quaternion restingLocalRotation;
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        restingLocalRotation = visualRoot.localRotation;
    }

    public void TriggerBeingHit()
    {
        if (defeated)
            return;

        if (animator != null && !string.IsNullOrWhiteSpace(hitTrigger))
        {
            animator.SetTrigger(hitTrigger);
            return;
        }

        StartReaction(AnimateHitReaction());
    }

    public void TriggerDeath()
    {
        if (defeated)
            return;

        defeated = true;

        if (animator != null && !string.IsNullOrWhiteSpace(deathTrigger))
            animator.SetTrigger(deathTrigger);
        else
            StartReaction(AnimateDeathReaction());

        if (tutorialDirector != null)
            tutorialDirector.NotifyTrainingDummyDefeated();
        else
            Debug.LogError("[TutorialTrainingDummy] TutorialDirector reference is missing.");

        float destroyDelay = animator == null
            ? Mathf.Max(hideDelay, deathReactionDuration)
            : hideDelay;

        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
        else
            Destroy(gameObject);
    }

    private void StartReaction(IEnumerator reaction)
    {
        if (visualRoot == null)
            return;

        if (reactionCoroutine != null)
            StopCoroutine(reactionCoroutine);

        reactionCoroutine = StartCoroutine(reaction);
    }

    private IEnumerator AnimateHitReaction()
    {
        float elapsed = 0f;

        while (elapsed < hitReactionDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / hitReactionDuration);
            float tilt = -hitTiltDegrees * Mathf.Sin(normalizedTime * Mathf.PI);
            visualRoot.localRotation =
                restingLocalRotation * Quaternion.Euler(tilt, 0f, 0f);
            yield return null;
        }

        visualRoot.localRotation = restingLocalRotation;
        reactionCoroutine = null;
    }

    private IEnumerator AnimateDeathReaction()
    {
        Quaternion startRotation = visualRoot.localRotation;
        Quaternion endRotation =
            restingLocalRotation * Quaternion.Euler(-deathTiltDegrees, 0f, 0f);
        float elapsed = 0f;

        while (elapsed < deathReactionDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / deathReactionDuration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            visualRoot.localRotation =
                Quaternion.Slerp(startRotation, endRotation, easedTime);
            yield return null;
        }

        visualRoot.localRotation = endRotation;
        reactionCoroutine = null;
    }
}
