using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class Stage2ShallowWaterVolume : MonoBehaviour
{
    [SerializeField] private Transform waterSurface;
    [SerializeField] private Transform mapRoot;
    [SerializeField, Min(0.1f)] private float maxWalkableDepth = 2.5f;
    [SerializeField, Min(0.1f)] private float maxFootHeightAboveSurface = 1.2f;

    private readonly Dictionary<Stage2WaterInteraction, int> overlapCounts = new();
    private readonly RaycastHit[] raycastHits = new RaycastHit[32];

    public float WaterY => waterSurface != null ? waterSurface.position.y : transform.position.y;

    public void Configure(Transform water, Transform map, float shallowDepth)
    {
        waterSurface = water;
        mapRoot = map;
        maxWalkableDepth = Mathf.Max(0.1f, shallowDepth);
    }

    public bool CanSplashAt(Vector3 footWorldPosition, out Vector3 surfacePosition)
    {
        float waterY = WaterY;
        surfacePosition = new Vector3(footWorldPosition.x, waterY + 0.02f, footWorldPosition.z);

        if (footWorldPosition.y > waterY + maxFootHeightAboveSurface)
            return false;

        Vector3 origin = new Vector3(footWorldPosition.x, waterY + 0.5f, footWorldPosition.z);
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, raycastHits, maxWalkableDepth + 1.5f, ~0, QueryTriggerInteraction.Ignore);
        float highestSubmergedY = float.NegativeInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null)
                continue;
            if (mapRoot != null && !hit.collider.transform.IsChildOf(mapRoot))
                continue;
            if (hit.point.y <= waterY + 0.02f && hit.point.y > highestSubmergedY)
                highestSubmergedY = hit.point.y;
        }

        if (float.IsNegativeInfinity(highestSubmergedY))
            return false;

        float depth = waterY - highestSubmergedY;
        return depth >= -0.02f && depth <= maxWalkableDepth;
    }

    private void OnTriggerEnter(Collider other)
    {
        Stage2WaterInteraction interaction = other.GetComponentInParent<Stage2WaterInteraction>();
        if (interaction == null)
            return;

        overlapCounts.TryGetValue(interaction, out int count);
        overlapCounts[interaction] = count + 1;
        if (count == 0)
            interaction.EnterWaterVolume(this);
    }

    private void OnTriggerExit(Collider other)
    {
        Stage2WaterInteraction interaction = other.GetComponentInParent<Stage2WaterInteraction>();
        if (interaction == null || !overlapCounts.TryGetValue(interaction, out int count))
            return;

        count--;
        if (count <= 0)
        {
            overlapCounts.Remove(interaction);
            interaction.ExitWaterVolume(this);
        }
        else
        {
            overlapCounts[interaction] = count;
        }
    }

    private void OnDisable()
    {
        foreach (Stage2WaterInteraction interaction in overlapCounts.Keys)
        {
            if (interaction != null)
                interaction.ExitWaterVolume(this);
        }
        overlapCounts.Clear();
    }
}
