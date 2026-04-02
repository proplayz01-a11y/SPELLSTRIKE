using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float beamDuration = 0.4f;
    public float maxDistance = 60f;
    public float beamWidth = 0.2f;
    public Gradient beamGradient;

    private int damage;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public void Launch(Transform origin, Transform target, int damageAmount)
    {
        if (origin == null)
        {
            Debug.LogWarning("LaserBeam launched without an origin.");
            Destroy(gameObject);
            return;
        }

        damage = Mathf.Max(1, damageAmount);
        Vector3 startPoint = origin.position;
        Vector3 endPoint = startPoint + origin.forward * maxDistance;

        if (target != null)
        {
            Vector3 direction = (target.position - startPoint);
            if (direction.sqrMagnitude > 0.01f)
                endPoint = startPoint + direction.normalized * maxDistance;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
            if (beamGradient != null)
                lineRenderer.colorGradient = beamGradient;
            lineRenderer.enabled = true;
        }

        StartCoroutine(FireRoutine(startPoint, endPoint));
    }

    private IEnumerator FireRoutine(Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 direction = (endPoint - startPoint).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            if (Physics.Raycast(startPoint, direction, out RaycastHit hit, maxDistance))
            {
                if (lineRenderer != null)
                    lineRenderer.SetPosition(1, hit.point);

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ApplyDamage(damage);
                }
                else
                {
                    hit.collider.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        yield return new WaitForSeconds(beamDuration);

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        Destroy(gameObject, 0.1f);
    }
}
