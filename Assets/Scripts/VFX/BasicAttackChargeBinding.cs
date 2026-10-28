using UnityEngine;
using UnityEngine.VFX;

namespace Spellstrike.Vfx
{
    /// <summary>Reads the existing BasicAttack state and release event. Never sends gameplay events.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class BasicAttackChargeBinding : MonoBehaviour
    {
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Transform magicSpawnPoint;
        [SerializeField] private BasicAttackChargeVfx chargePrefab;
        [Tooltip("Optional VFX Graph replacement. Assign only after isolated visual review.")]
        [SerializeField] private VisualEffect chargeGraphPrefab;
        [SerializeField] private AnimationClip basicAttackClip;
        [SerializeField] private string statePath = "Base Layer.BasicAttack";
        [SerializeField] private int animatorLayer;

        [Header("Post-release presentation")]
        [Tooltip("Fade length in clip-time seconds. Follows animation pause/speed. Zero hides at release.")]
        [Min(0f)] [SerializeField] private float postReleaseFadeSeconds = BasicAttackChargeTiming.DefaultFadeSeconds;

        public float PostReleaseFadeSeconds => Mathf.Max(0f, postReleaseFadeSeconds);

        private BasicAttackChargeVfx instance;
        private VisualEffect graphInstance;
        private Camera view;
        private int stateHash;
        private float releaseSeconds;
        private static readonly int Charge01Id = Shader.PropertyToID("Charge01");
        private static readonly int ChargeVisibilityId = Shader.PropertyToID("ChargeVisibility");
        private bool chargeStarted;
        private bool graphEmissionStopped;
        private float lastAnimationSeconds = -1f;

        public static float FindReleaseTime(AnimationClip clip)
        {
            if (clip == null) return -1f;
            float earliest = float.PositiveInfinity;
            foreach (AnimationEvent marker in clip.events)
                if (marker.functionName == "OnAttackRelease" || marker.functionName == "ShootMagicProjectile")
                    earliest = Mathf.Min(earliest, marker.time);
            return float.IsInfinity(earliest) ? -1f : earliest;
        }

        private void Awake()
        {
            stateHash = Animator.StringToHash(statePath);
            releaseSeconds = FindReleaseTime(basicAttackClip);
            if (playerAnimator == null || magicSpawnPoint == null ||
                (chargePrefab == null && chargeGraphPrefab == null) ||
                basicAttackClip == null || releaseSeconds <= 0f || animatorLayer < 0 ||
                animatorLayer >= playerAnimator.layerCount)
            {
                Debug.LogWarning("Basic charge VFX needs Animator, MagicSpawnPoint, a charge prefab, and the BasicAttack clip with its existing release marker.", this);
                enabled = false;
                return;
            }
            if (chargeGraphPrefab != null)
            {
                graphInstance = Instantiate(chargeGraphPrefab, magicSpawnPoint, false);
                graphInstance.transform.localPosition = Vector3.zero;
                graphInstance.transform.localRotation = Quaternion.identity;
                if (!graphInstance.HasFloat(Charge01Id) || !graphInstance.HasFloat(ChargeVisibilityId))
                {
                    Debug.LogWarning("Basic charge VFX Graph must expose Floats named Charge01 and ChargeVisibility.", this);
                    Destroy(graphInstance.gameObject);
                    graphInstance = null;
                    enabled = false;
                    return;
                }
                graphInstance.gameObject.SetActive(false);
            }
            else
            {
                instance = Instantiate(chargePrefab, magicSpawnPoint, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.Hide();
            }
        }

        private void LateUpdate()
        {
            if (instance == null && graphInstance == null) return;
            if (playerAnimator == null || !playerAnimator.isActiveAndEnabled || magicSpawnPoint == null)
            {
                HideCharge();
                return;
            }
            AnimatorStateInfo state = playerAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
            if (playerAnimator.IsInTransition(animatorLayer))
            {
                AnimatorStateInfo next = playerAnimator.GetNextAnimatorStateInfo(animatorLayer);
                // Follow the incoming Basic state; cancel immediately on interruption into another state.
                if (next.fullPathHash != stateHash)
                {
                    HideCharge();
                    return;
                }
                state = next;
            }
            float seconds = state.normalizedTime * basicAttackClip.length;
            if (state.fullPathHash != stateHash || seconds < 0f)
            {
                HideCharge();
                return;
            }
            // A restarted state must never inherit the previous cast's stopped emission/fade.
            if (seconds < lastAnimationSeconds) HideCharge();
            lastAnimationSeconds = seconds;
            BasicAttackChargeTiming.Sample sample = BasicAttackChargeTiming.Evaluate(
                seconds, releaseSeconds, PostReleaseFadeSeconds);
            // Do not create a new charge if first observed/enabled after release.
            if (!sample.IsVisible || (seconds >= releaseSeconds && !chargeStarted))
            {
                HideCharge();
                return;
            }
            chargeStarted = true;
            if (graphInstance != null)
            {
                graphInstance.SetFloat(Charge01Id, sample.Charge01);
                graphInstance.SetFloat(ChargeVisibilityId, sample.Visibility);
                if (!graphInstance.gameObject.activeSelf)
                {
                    graphInstance.gameObject.SetActive(true);
                    graphInstance.Reinit();
                }
                if (seconds >= releaseSeconds && !graphEmissionStopped)
                {
                    graphInstance.Stop();
                    graphEmissionStopped = true;
                }
                return;
            }
            if (view == null || !view.isActiveAndEnabled) view = Camera.main;
            // Keep the release shape; no further growth or procedural respawn during its short fade.
            instance.RenderCharge(sample.Charge01, Mathf.Min(seconds, releaseSeconds), view, sample.Visibility);
        }

        private void HideCharge()
        {
            chargeStarted = false;
            graphEmissionStopped = false;
            lastAnimationSeconds = -1f;
            if (instance != null) instance.Hide();
            if (graphInstance != null && graphInstance.gameObject.activeSelf)
                graphInstance.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            HideCharge();
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance.gameObject);
            if (graphInstance != null) Destroy(graphInstance.gameObject);
        }
    }
}
