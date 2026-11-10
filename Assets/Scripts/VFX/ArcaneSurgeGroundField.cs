using UnityEngine;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// The Cast animation event creates a visible, twice-as-large AoE
    /// circle. Its ground center follows the designated enemy until that
    /// enemy is disabled/despawned; all lightning endpoints and damage use
    /// this same moving radius. No extra damage authority lives here.
    /// </summary>
    // After ordinary enemy movement Update, before the descending projectile.
    [DefaultExecutionOrder(450)]
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgeGroundField : MonoBehaviour
    {
        private const float CircleBaseRadius = 1.5f;
        private const float MagicCircleScaleMultiplier = 2f;
        private const float LifetimeSeconds = 6f;
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private Transform trackedEnemy;
        private Collider trackedCollider;
        private Vector3 colliderLocalCenter;
        private float colliderFeetOffsetY;
        private static readonly Vector2[] StrikeOffsets =
        {
            new Vector2(-0.40f, 0.16f),
            new Vector2(0.43f, 0.24f),
            new Vector2(-0.10f, -0.48f)
        };

        public Vector3 Center { get; private set; }
        public float Radius { get; private set; }

        public Vector3 StrikePoint(int index)
        {
            Vector2 offset = StrikeOffsets[Mathf.Clamp(index, 0, StrikeOffsets.Length - 1)];
            return Center + new Vector3(offset.x * Radius, 0.10f, offset.y * Radius);
        }

        public static ArcaneSurgeGroundField Spawn(Transform enemy, ArcaneSurgeZapLibrary library)
        {
            if (enemy == null || library == null || !library.HasAreaEffects)
            {
                Debug.LogWarning("[ArcaneSurge] Cast circle requires a valid target and all Hovl AoE prefabs.");
                return null;
            }

            Collider collider = FindEnemyCollider(enemy);
            if (collider == null)
            {
                Debug.LogWarning("[ArcaneSurge] No active enemy collider for a ground AoE center.");
                return null;
            }

            Bounds bounds = collider.bounds;
            float radius = Mathf.Clamp(
                Mathf.Max(bounds.extents.x, bounds.extents.z) + 3.5f, 6f, 18f)
                * MagicCircleScaleMultiplier;
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            // Terrain and walkable mesh floors may be below a raised enemy
            // collider. Avoid probing other enemy colliders as the ground.
            RaycastHit[] hits = Physics.RaycastAll(
                new Vector3(center.x, bounds.min.y + 2f, center.z), Vector3.down,
                60f, ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
            float bestY = float.NegativeInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.normal.y < 0.55f ||
                    hit.point.y > bounds.min.y + 0.6f)
                    continue;
                if (hit.collider.GetComponentInParent<EnemyController>() != null ||
                    hit.collider.GetComponentInParent<BrambleSpriteController>() != null ||
                    hit.collider.GetComponentInParent<EnemyHealth>() != null)
                    continue;
                bestY = Mathf.Max(bestY, hit.point.y);
            }
            if (!float.IsNegativeInfinity(bestY))
                center.y = bestY;

            GameObject root = new GameObject("Arcane Surge - Animated Cast Ground AoE");
            root.transform.position = center;
            ArcaneSurgeGroundField field = root.AddComponent<ArcaneSurgeGroundField>();
            field.Center = center;
            field.Radius = radius;
            field.trackedEnemy = enemy;
            field.trackedCollider = collider;
            field.colliderLocalCenter =
                collider.transform.InverseTransformPoint(bounds.center);
            field.colliderFeetOffsetY =
                bounds.min.y - collider.transform.position.y;

            GameObject circle = Instantiate(library.MagicCircle,
                center + Vector3.up * 0.12f, Quaternion.identity, root.transform);
            circle.name = "Hovl Magic Circle - OnStrongAttackCast";
            float relativeScale = radius / CircleBaseRadius;
            circle.transform.localScale *= relativeScale;
            foreach (Light light in circle.GetComponentsInChildren<Light>(true))
                light.enabled = false; // Mobile-safe URP, particles remain.

            Destroy(root, LifetimeSeconds); // Safety cleanup if events are interrupted.
            return field;
        }

        private void Update()
        {
            // Keeping the actual ground circle homing also keeps each AoE
            // damage check aligned with what the player sees. Its Y is
            // projected onto terrain rather than following the enemy's head.
            if (trackedEnemy == null || !trackedEnemy.gameObject.activeInHierarchy ||
                trackedCollider == null || !trackedCollider.enabled ||
                !trackedCollider.gameObject.activeInHierarchy)
                return; // Freeze at the last enemy location on despawn/death.

            // Collider.bounds can remain at the previous physics step after
            // the enemy Transform moves (autoSyncTransforms is normally off).
            // Using the collider's LIVE transform makes the circle truly
            // home immediately without a per-frame Physics.SyncTransforms().
            Vector3 currentCenter = trackedCollider.transform.TransformPoint(colliderLocalCenter);
            float feetY = trackedCollider.transform.position.y + colliderFeetOffsetY;
            Vector3 next = new Vector3(currentCenter.x, feetY, currentCenter.z);
            Vector3 origin = new Vector3(next.x, feetY + 2f, next.z);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
                60f, ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
            float floor = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.collider == null || hit.normal.y < 0.55f ||
                    hit.point.y > feetY + 0.6f ||
                    hit.collider.GetComponentInParent<EnemyController>() != null ||
                    hit.collider.GetComponentInParent<BrambleSpriteController>() != null ||
                    hit.collider.GetComponentInParent<EnemyHealth>() != null)
                    continue;
                floor = Mathf.Max(floor, hit.point.y);
            }
            if (!float.IsNegativeInfinity(floor))
                next.y = floor;
            Center = next;
            transform.position = next;
        }

        private void LateUpdate()
        {
            // Animator root motion may move a generic monster AFTER Update.
            // Keep the visible circle aligned in that same rendered frame.
            Update();
        }

        public void Release()
        {
            if (this != null && gameObject != null)
                Destroy(gameObject, 0.9f);
        }

        public void Cancel()
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
        }

        private static Collider FindEnemyCollider(Transform enemy)
        {
            Transform root = enemy;
            Component receiver = enemy.GetComponentInParent<EnemyController>();
            if (receiver == null) receiver = enemy.GetComponentInParent<BrambleSpriteController>();
            if (receiver == null) receiver = enemy.GetComponentInParent<EnemyHealth>();
            if (receiver != null) root = receiver.transform;

            Collider firstTrigger = null;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;
                if (!collider.isTrigger)
                    return collider;
                if (firstTrigger == null)
                    firstTrigger = collider;
            }
            return firstTrigger;
        }
    }
}
