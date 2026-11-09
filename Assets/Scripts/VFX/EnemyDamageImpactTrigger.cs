using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Enemy-owned damage response. The trigger object is created lazily under
    /// the enemy on the first accepted hit, then reused for subsequent damage.
    /// The spawned visual is independent of the enemy so lethal hits still show.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyDamageImpactTrigger : MonoBehaviour
    {
        private const string TriggerObjectName = "Damage Impact VFX Trigger";
        private const string ResourcePath = "Spellstrike/VFX/BasicAttackImpactMeshBurst";
        private const float DefaultImpactScaleMultiplier = 6f;

        private static GameObject cachedImpactPrefab;
        private static bool resourceLookupAttempted;
        private static bool missingPrefabWarningLogged;
        private static float activeImpactScaleMultiplier = DefaultImpactScaleMultiplier;

        /// <summary>
        /// Temporarily scopes an impact scale to the synchronous damage call.
        /// Enemy damage receivers trigger PlayFor() inside that call, so this
        /// keeps attack-tier presentation out of their shared health logic.
        /// </summary>
        public static void WithImpactScale(float scaleMultiplier, Action applyDamage)
        {
            if (applyDamage == null)
                return;

            float previousScale = activeImpactScaleMultiplier;
            activeImpactScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            try
            {
                applyDamage();
            }
            finally
            {
                activeImpactScaleMultiplier = previousScale;
            }
        }

        /// <summary>Call only after the receiver confirms that health decreased.</summary>
        public static void PlayFor(Transform damageReceiver)
        {
            if (damageReceiver == null)
                return;

            EnemyDamageImpactTrigger trigger =
                damageReceiver.GetComponentInChildren<EnemyDamageImpactTrigger>(true);

            if (trigger == null)
            {
                Transform existingChild = damageReceiver.Find(TriggerObjectName);
                GameObject triggerObject = existingChild != null
                    ? existingChild.gameObject
                    : new GameObject(TriggerObjectName);

                if (triggerObject.transform.parent != damageReceiver)
                    triggerObject.transform.SetParent(damageReceiver, false);

                trigger = triggerObject.GetComponent<EnemyDamageImpactTrigger>();
                if (trigger == null)
                    trigger = triggerObject.AddComponent<EnemyDamageImpactTrigger>();
            }

            trigger.PlayImpact(activeImpactScaleMultiplier);
        }

        private void PlayImpact(float scaleMultiplier)
        {
            // Edit Mode diagnostics can move colliders without a physics step.
            // Runtime collision callbacks already have synchronized physics.
            if (!Application.isPlaying)
                Physics.SyncTransforms();

            if (!resourceLookupAttempted)
            {
                cachedImpactPrefab = Resources.Load<GameObject>(ResourcePath);
                resourceLookupAttempted = true;
            }

            if (cachedImpactPrefab == null)
            {
                if (!missingPrefabWarningLogged)
                {
                    Debug.LogWarning(
                        "[EnemyDamageImpactTrigger] Impact prefab was not found at Resources/" + ResourcePath + ".prefab",
                        this);
                    missingPrefabWarningLogged = true;
                }

                return;
            }

            Transform enemyRoot = transform.parent != null ? transform.parent : transform;
            Camera mainCamera = Camera.main;
            Vector3 impactPosition = GetCameraFacingSurface(enemyRoot, mainCamera, out Vector3 facing);

            GameObject impact = Instantiate(
                cachedImpactPrefab,
                impactPosition,
                Quaternion.LookRotation(facing.normalized));

            impact.name = "PlayerMagicOrb_EnemyDamageImpact";
            impact.transform.localScale = Vector3.one * Mathf.Max(0.01f, scaleMultiplier);
        }

        private static Vector3 GetCameraFacingSurface(
            Transform enemyRoot,
            Camera mainCamera,
            out Vector3 facing)
        {
            Collider[] colliders = enemyRoot.GetComponentsInChildren<Collider>(true);
            List<Collider> damageBodyColliders = new List<Collider>(colliders.Length);
            Bounds combinedBounds = default;
            bool hasBounds = false;

            foreach (Collider targetCollider in colliders)
            {
                if (targetCollider == null || !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy)
                    continue;

                // Ignore aggro/range/interaction trigger volumes. They can be
                // much larger than the enemy's body and would place the burst
                // floating outside the visible model.
                if (targetCollider.isTrigger)
                    continue;

                damageBodyColliders.Add(targetCollider);

                if (!hasBounds)
                {
                    combinedBounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetCollider.bounds);
                }
            }

            // Some enemies use trigger-only bodies. Fall back to those only if
            // no solid collider exists anywhere under the damage receiver.
            if (damageBodyColliders.Count == 0)
            {
                foreach (Collider targetCollider in colliders)
                {
                    if (targetCollider == null || !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy)
                        continue;

                    damageBodyColliders.Add(targetCollider);

                    if (!hasBounds)
                    {
                        combinedBounds = targetCollider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(targetCollider.bounds);
                    }
                }
            }

            Vector3 center = hasBounds ? combinedBounds.center : enemyRoot.position + Vector3.up;
            facing = mainCamera != null
                ? mainCamera.transform.position - center
                : -enemyRoot.forward;

            if (facing.sqrMagnitude <= 0.0001f)
                facing = Vector3.forward;

            Vector3 direction = facing.normalized;

            // Start outside the enemy and raycast toward its visible side. This
            // places the mesh burst on the collider surface instead of buried
            // at the bounds center where the enemy model can occlude it.
            if (mainCamera != null)
            {
                Ray viewRay = new Ray(mainCamera.transform.position, -direction);
                float nearestDistance = float.PositiveInfinity;
                Vector3 surfacePoint = center;
                Vector3 surfaceNormal = direction;
                bool foundSurface = false;

                foreach (Collider targetCollider in damageBodyColliders)
                {
                    if (targetCollider.Raycast(viewRay, out RaycastHit hit, 1000f)
                        && hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        surfacePoint = hit.point;
                        surfaceNormal = hit.normal;
                        foundSurface = true;
                    }
                }

                if (foundSurface)
                {
                    facing = surfaceNormal.sqrMagnitude > 0.0001f
                        ? surfaceNormal.normalized
                        : direction;
                    return surfacePoint + facing * 0.25f;
                }
            }

            if (hasBounds)
            {
                Vector3 extents = combinedBounds.extents;
                float supportDistance =
                    Mathf.Abs(direction.x) * extents.x +
                    Mathf.Abs(direction.y) * extents.y +
                    Mathf.Abs(direction.z) * extents.z;
                return center + direction * (supportDistance + 0.25f);
            }

            return center + direction * 0.5f;
        }
    }
}
