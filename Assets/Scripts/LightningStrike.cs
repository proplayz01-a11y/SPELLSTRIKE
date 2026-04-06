using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningStrike : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float strikeHeight = 20f;
    public float boltWidth = 0.18f;
    public float duration = 0.35f;
    public Gradient lightningGradient;
    public float damageRadius = 1.6f;

    private int damage;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public void Launch(Transform target, int damageAmount)
    {
        if (target == null)
        {
            Debug.LogWarning("LightningStrike launched without a valid target.");
            Destroy(gameObject);
            return;
        }

        damage = Mathf.Max(1, damageAmount);

        Vector3 topPoint = target.position + Vector3.up * strikeHeight;
        Vector3 bottomPoint = target.position;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, topPoint);
            lineRenderer.SetPosition(1, bottomPoint);
            lineRenderer.startWidth = boltWidth;
            lineRenderer.endWidth = boltWidth;
            if (lightningGradient != null)
                lineRenderer.colorGradient = lightningGradient;
            lineRenderer.enabled = true;
        }

        StartCoroutine(StrikeRoutine(target, bottomPoint));
    }

    private IEnumerator StrikeRoutine(Transform target, Vector3 strikePoint)
    {
        yield return new WaitForSeconds(0.1f);

        if (target != null)
        {
            Collider[] hits = Physics.OverlapSphere(strikePoint, damageRadius);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ApplyDamage(damage);
                    break;
                }
                else
                {
                    hit.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        yield return new WaitForSeconds(duration);

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        Destroy(gameObject, 0.1f);
    }
}
