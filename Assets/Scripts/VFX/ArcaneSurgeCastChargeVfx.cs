using UnityEngine;
using UnityEngine.Rendering;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Small hand-bound Strong Attack charge, started exclusively by the
    /// OnStrongAttackCast animation event and removed at Zap release/end.
    /// Visual only: no collision, damage, or independent gameplay timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcaneSurgeCastChargeVfx : MonoBehaviour
    {
        private const int Segments = 24;
        private const float OuterRadius = 0.31f;
        private const float InnerRadius = 0.21f;
        private readonly Vector3[] outerPoints = new Vector3[Segments];
        private readonly Vector3[] innerPoints = new Vector3[Segments];
        private LineRenderer outerRing;
        private LineRenderer innerRing;
        private float startedAt;

        public static ArcaneSurgeCastChargeVfx Spawn(Transform hand)
        {
            if (hand == null) return null;

            Material material = Resources.Load<Material>("Spellstrike/VFX/ArcaneSurgeEnergy");
            if (material == null)
            {
                Debug.LogWarning("[ArcaneSurge] Strong cast charge material missing.");
                return null;
            }

            GameObject root = new GameObject("Arcane Surge - Strong Cast Charge");
            root.transform.SetParent(hand, false);
            ArcaneSurgeCastChargeVfx effect = root.AddComponent<ArcaneSurgeCastChargeVfx>();
            effect.outerRing = effect.CreateRing("Yellow energy halo", material, 0.036f,
                new Color(1f, 0.68f, 0.12f, 0.65f));
            effect.innerRing = effect.CreateRing("White-hot cast core", material, 0.019f,
                new Color(1f, 0.98f, 0.70f, 0.95f));
            effect.startedAt = Time.time;
            return effect;
        }

        private LineRenderer CreateRing(string label, Material material, float width, Color tint)
        {
            GameObject child = new GameObject(label);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = width;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.startColor = tint;
            line.endColor = tint;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return line;
        }

        private void LateUpdate()
        {
            if (outerRing == null || innerRing == null)
                return;

            Camera view = Camera.main;
            Vector3 right = view != null
                ? transform.InverseTransformDirection(view.transform.right).normalized
                : Vector3.right;
            Vector3 up = view != null
                ? transform.InverseTransformDirection(view.transform.up).normalized
                : Vector3.up;

            float elapsed = Time.time - startedAt;
            float buildup = Mathf.Clamp01(elapsed / 0.30f);
            float size = Mathf.Lerp(0.5f, 1f, buildup);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * (Mathf.PI * 2f / Segments);
                float outerAngle = angle + elapsed * 8.5f;
                float innerAngle = -angle + elapsed * 12.5f;
                float crackle = 1f + 0.10f * Mathf.Sin(i * 5.2f + elapsed * 42f);
                outerPoints[i] = (right * Mathf.Cos(outerAngle) + up * Mathf.Sin(outerAngle))
                    * (OuterRadius * size * crackle);
                innerPoints[i] = (right * Mathf.Cos(innerAngle) + up * Mathf.Sin(innerAngle))
                    * (InnerRadius * size);
            }

            outerRing.SetPositions(outerPoints);
            innerRing.SetPositions(innerPoints);
        }
    }
}
