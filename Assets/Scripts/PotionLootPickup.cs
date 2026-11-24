using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>One scene-owned world loot item, backed by the existing potion inventory.</summary>
public sealed class PotionLootPickup : MonoBehaviour
{
    public PotionType Type { get; private set; }
    public int Remaining { get; private set; }
    private Transform bottle;
    private Transform recipient;
    private Vector3 flightStart;
    private float scale, born, collectedAt, nextAttempt;
    private bool collected;
    private Material ringMaterial;
    private Transform particleHalo;
    private SphereCollider pickupTrigger;
    private float bottleGroundOffset;

    public static string ModelPath(PotionType type) => "PotionLoot/Potion_" +
        (type == PotionType.Health ? "Health" : type == PotionType.Cleansing ? "Purify" : "PowerUp");

    public static PotionLootPickup Spawn(Transform source, PotionType type, int amount, int index = 0)
    {
        if (source == null || amount <= 0) return null;
        var model = Resources.Load<GameObject>(ModelPath(type));
        if (model == null)
        {
            Debug.LogError("[PotionLoot] Missing model: " + ModelPath(type));
            return null;
        }
        var player = PersistentPlayerRoot.Instance;
        var cc = player != null ? player.GetComponent<CharacterController>() : null;
        float playerHeight = cc != null ? cc.height * Mathf.Abs(cc.transform.lossyScale.y) : 6f;
        float height = Mathf.Clamp(playerHeight * .30f, .8f, 6f);
        Vector3 direction = source.forward;
        direction.y = 0;
        if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
        direction = Quaternion.Euler(0, index * 70f, 0) * direction.normalized;
        Vector3 origin = source.position + direction * height * .8f;
        // NavMesh chooses a reachable XZ location, not the visible floor height.
        // Like world tiles, the physical ground raycast is authoritative.
        bool onNavMesh = NavMesh.SamplePosition(origin, out var hit,
            Mathf.Max(2f, height * 3f), NavMesh.AllAreas);
        if (onNavMesh) origin = hit.position;
        if (TryGround(origin, height, out var floor))
            origin.y = floor.point.y;
        else if (TryGround(source.position, height, out floor))
            origin = floor.point;
        else
            Debug.LogWarning("[PotionLoot] No physical ground hit; retaining spawn fallback.");
        var go = new GameObject("PotionLoot_" + type);
        SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        go.transform.position = origin;
        var pickup = go.AddComponent<PotionLootPickup>();
        pickup.Type = type;
        pickup.Remaining = amount;
        pickup.scale = height;
        pickup.born = Time.time;
        // Match WorldTile's physical pickup behavior. A kinematic Rigidbody
        // ensures trigger messages even when the player uses CharacterController.
        var body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        pickup.pickupTrigger = go.AddComponent<SphereCollider>();
        pickup.pickupTrigger.isTrigger = true;
        pickup.pickupTrigger.center = Vector3.up * height * .65f;
        pickup.pickupTrigger.radius = height * 1.05f;
        // Preserve the FBX root's axis-conversion rotation and imported scale.
        // Animate a separate upright holder rather than rotating the FBX root.
        pickup.bottle = new GameObject("FloatingBottle").transform;
        pickup.bottle.SetParent(go.transform, false);
        pickup.bottle.localPosition = Vector3.up * height * .23f;
        pickup.bottle.localRotation = Quaternion.identity;
        pickup.bottle.localScale = Vector3.one;
        var instance = Instantiate(model, pickup.bottle, false);
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            pickup.bottle.localScale = Vector3.one * height / Mathf.Max(.01f, bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // Correct the actual imported mesh bottom, not just its pivot.
            pickup.bottleGroundOffset = pickup.bottle.position.y - bounds.min.y;
        }
        pickup.CreateRing();
        pickup.bottle.localPosition = Vector3.up * (pickup.bottleGroundOffset + height * .23f);
        pickup.CreateParticleHalo();
        return pickup;
    }

    private static bool TryGround(Vector3 reference, float height, out RaycastHit best)
    {
        int mask = LayerMask.GetMask("Ground", "WalkableEnvironment", "ShipDeckSpawn");
        if (FindSurface(reference, height, mask, out best)) return true;
        // Stage 1 imported walkable geometry also uses Default, as in TileSpawner.
        return FindSurface(reference, height, LayerMask.GetMask("Default"), out best);
    }

    private static bool FindSurface(Vector3 reference, float height, int mask, out RaycastHit best)
    {
        best = default;
        float span = Mathf.Max(8f, height * 4f);
        float distance = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(reference + Vector3.up * span, Vector3.down,
            span * 2f, mask, QueryTriggerInteraction.Ignore))
        {
            var c = hit.collider;
            if (c == null || hit.normal.y < .5f || c.CompareTag("Player") || c.CompareTag("Enemy")
                || c.GetComponentInParent<PlayerHealth>() != null
                || c.GetComponentInParent<EnemyHealth>() != null
                || c.GetComponentInParent<WorldTile>() != null
                || c.GetComponentInParent<PotionLootPickup>() != null
                || (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic)) continue;
            float delta = Mathf.Abs(hit.point.y - reference.y);
            if (delta >= distance) continue;
            best = hit;
            distance = delta;
        }
        return best.collider != null;
    }

    private void CreateRing()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        Color color = Type == PotionType.Health ? new Color(1f,.12f,.13f)
            : Type == PotionType.Cleansing ? new Color(.12f,.55f,1f) : new Color(.2f,1f,.12f);
        ringMaterial = new Material(shader);
        if (ringMaterial.HasProperty("_BaseColor")) ringMaterial.SetColor("_BaseColor", color);
        if (ringMaterial.HasProperty("_Color")) ringMaterial.SetColor("_Color", color);
        var ring = new GameObject("LootGlowRing");
        ring.transform.SetParent(transform, false);
        var line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 32;
        line.widthMultiplier = scale * .035f;
        line.sharedMaterial = ringMaterial;
        line.startColor = line.endColor = color;
        float groundRise = 0f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            var point = new Vector3(Mathf.Cos(angle)*scale*.48f, 0f,
                Mathf.Sin(angle)*scale*.48f);
            // Conform the whole ring to the actual terrain, and keep the
            // bottle above the highest sampled ground within its footprint.
            float surface = 0f;
            if (TryGround(transform.position + point, scale, out var hit))
                surface = hit.point.y - transform.position.y;
            point.y = surface + scale*.06f;
            line.SetPosition(i, point);
            // Store the maximum separately so the base clearance never sinks.
            groundRise = Mathf.Max(groundRise, surface);
        }
        bottleGroundOffset += groundRise;
        pickupTrigger.center = Vector3.up * (groundRise + scale*.65f);
    }

    private void CreateParticleHalo()
    {
        // Original spark/flare textures, using the same external hollow-shell
        // emission and fade behavior as our fragment effects.
        var spark = Resources.Load<Material>("PotionLoot/PotionSpark");
        var flare = Resources.Load<Material>("PotionLoot/PotionFlare");
        if (spark == null || flare == null)
        {
            Debug.LogWarning("[PotionLoot] Particle materials not available.");
            return;
        }
        particleHalo = new GameObject("PotionParticleHalo").transform;
        particleHalo.SetParent(transform, false);
        UpdateParticleAnchor();
        Color tint = Type == PotionType.Health ? new Color(1f,.075f,.10f)
            : Type == PotionType.Cleansing ? new Color(.075f,.42f,1f)
            : new Color(.16f,1f,.065f);
        AddParticles("Sparks", spark, tint, false);
        AddParticles("Flares", flare, tint, true);
    }

    private void AddParticles(string objectName, Material material, Color tint, bool flare)
    {
        var go = new GameObject(objectName);
        go.SetActive(false);
        go.transform.SetParent(particleHalo, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = flare ? 12 : 28;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.85f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(scale*.015f, scale*.065f);
        main.startSize = new ParticleSystem.MinMaxCurve(
            scale*(flare ? .13f : .055f), scale*(flare ? .23f : .115f));
        main.startColor = tint;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI*2f);
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = scale * (flare ? .66f : .60f);
        shape.radiusThickness = 0f;
        shape.scale = new Vector3(1f, 1.08f, 1f);
        var emission = ps.emission;
        emission.rateOverTime = flare ? 3f : 11f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),
            new GradientColorKey(Color.white,1)}, new[]{new GradientAlphaKey(0,0),
            new GradientAlphaKey(1,.2f),new GradientAlphaKey(.9f,.72f),new GradientAlphaKey(0,1)});
        fade.color = new ParticleSystem.MinMaxGradient(gradient);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.SetActive(true);
        ps.Play();
    }

    private void UpdateParticleAnchor()
    {
        if (particleHalo != null && bottle != null)
            particleHalo.position = bottle.position + Vector3.up * scale*.5f;
    }

    private void Update()
    {
        if (bottle == null) return;
        if (collected)
        {
            float t = Mathf.Clamp01((Time.time-collectedAt)/.24f);
            if (recipient != null)
                bottle.position = Vector3.Lerp(flightStart, recipient.position + Vector3.up * scale, t*t);
            UpdateParticleAnchor();
            if (t >= 1f) Destroy(gameObject);
            return;
        }
        float age = Time.time-born;
        bottle.localPosition = Vector3.up * (bottleGroundOffset + scale * (.23f + .10f*Mathf.Sin(age*2.7f)));
        bottle.Rotate(0f, 55f*Time.deltaTime, 0f, Space.World);
        UpdateParticleAnchor();
    }

    private void OnTriggerEnter(Collider other) => TryContactPickup(other);
    private void OnTriggerStay(Collider other) => TryContactPickup(other);

    private void TryContactPickup(Collider other)
    {
        if (collected || Time.time - born < .15f || Time.time < nextAttempt || Time.timeScale <= 0f)
            return;
        var health = other.GetComponentInParent<PlayerHealth>();
        var player = PersistentPlayerRoot.Instance;
        if (health == null || player == null || health != player.Health || health.currentHealth <= 0)
            return;
        if (player.Movement == null || !player.Movement.isActiveAndEnabled || player.Movement.IsMovementDisabled)
            return;
        nextAttempt = Time.time + .3f;
        TryCollect(health);
    }

    public bool TryCollect(PlayerHealth player)
    {
        if (collected || Remaining <= 0 || player == null || player.currentHealth <= 0) return false;
        var inventory = PotionInventory.EnsureInstance();
        if (inventory == null) return false;
        int capacity = inventory.maxPotionCount-inventory.GetCount(Type);
        if (capacity <= 0) return false;
        int added = inventory.AddPotion(Type, Mathf.Min(capacity, Remaining));
        if (added <= 0) return false;
        Remaining -= added;
        Debug.Log($"[PotionLoot] Collected {added} {PotionInventory.GetDisplayName(Type)}.");
        if (Remaining == 0)
        {
            collected = true;
            if (pickupTrigger != null) pickupTrigger.enabled = false;
            collectedAt = Time.time;
            recipient = player.transform;
            flightStart = bottle.position;
            var ring = transform.Find("LootGlowRing");
            if (ring != null) ring.gameObject.SetActive(false);
        }
        return true;
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
    }
}
