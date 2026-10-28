using UnityEngine;

public sealed class TutorialNavigationGuide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform guideVisual;
    [SerializeField] private Transform destinationMarker;
    [SerializeField] private TutorialDirectionalArrowTrail directionalArrowTrail;

    [Header("Guide Placement")]
    [SerializeField, Min(0f)] private float distanceInFrontOfPlayer = 2.25f;
    [SerializeField] private float groundHeightOffset = 0.08f;
    [SerializeField, Min(0f)] private float turnSpeed = 12f;
    [SerializeField] private bool hideLegacyGuideWhenTrailAssigned = true;

    private Transform target;

    public Transform CurrentTarget => target;

    private void Awake()
    {
        if (directionalArrowTrail == null)
            directionalArrowTrail = GetComponent<TutorialDirectionalArrowTrail>();

        SyncTrailEndpoints();
    }

    private void LateUpdate()
    {
        if (player == null || target == null)
            return;

        Vector3 direction = target.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        if (guideVisual != null && !ShouldHideLegacyGuide())
        {
            Vector3 desiredPosition =
                player.position
                + direction * distanceInFrontOfPlayer
                + Vector3.up * groundHeightOffset;

            guideVisual.position = desiredPosition;

            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            guideVisual.rotation = Quaternion.Slerp(
                guideVisual.rotation,
                desiredRotation,
                turnSpeed * Time.deltaTime);
        }

        if (destinationMarker != null)
        {
            destinationMarker.position =
                target.position + Vector3.up * groundHeightOffset;
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        SyncTrailEndpoints();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SyncTrailEndpoints();
        SetVisualsActive(target != null);
    }

    public void ClearTarget()
    {
        target = null;
        SyncTrailEndpoints();
        SetVisualsActive(false);
    }

    private void SetVisualsActive(bool active)
    {
        if (guideVisual != null)
            guideVisual.gameObject.SetActive(active && !ShouldHideLegacyGuide());
        if (destinationMarker != null)
            destinationMarker.gameObject.SetActive(active);
        if (directionalArrowTrail != null)
            directionalArrowTrail.SetVisible(active);
    }

    private bool ShouldHideLegacyGuide()
    {
        return hideLegacyGuideWhenTrailAssigned
               && directionalArrowTrail != null;
    }

    private void SyncTrailEndpoints()
    {
        if (directionalArrowTrail != null)
            directionalArrowTrail.SetEndpoints(player, target);
    }
}
