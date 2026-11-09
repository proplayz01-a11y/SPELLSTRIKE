# Magic Orb Basic Attack impact hook: implementation record

> The original implementation specification is retained below for traceability. DeepSeek is no longer part of the workflow; the hook has been authored, integrated, and validated directly.

> **Superseded on 2026-11-07:** the projectile-owned ParticleSystem hook described below is no longer the production path. The current implementation is enemy-owned: `EnemyHealth`, `EnemyController`, and `BrambleSpriteController` invoke `EnemyDamageImpactTrigger` after confirmed HP loss. It creates/reuses a child GameObject named `Damage Impact VFX Trigger` and spawns the procedural mesh effect from `Assets/Resources/Spellstrike/VFX/BasicAttackImpactMeshBurst.prefab`. See `README.md` for the current architecture and validation status.

## Goal
Finish the Basic Attack VFX integration by spawning the already-authored Hovl Electro one-shot when the production homing projectile reaches an accepted target. Keep the existing projectile movement, damage, vocabulary rules, attack timing, and Spellbook recording unchanged.

## Relevant project files
- Script to patch: `Assets/Scripts/HomingProjectile.cs`
- Projectile prefab to receive the serialized reference: `Assets/Prefabs/PlayerProjectile_MagicOrb.prefab`
- Impact prefab: `Assets/VFX/MagicOrb/Impact/PlayerMagicOrb_ElectroHit_OneShot.prefab`
- Current canonical travel visual: `Assets/VFX/MagicOrb/MagicOrb_HovlBasicAttack.prefab` (already nested in the projectile prefab)
- Player reference: `Assets/Prefabs/VanAertPlayer.prefab` -> `AttackController.playerProjectile` -> `Assets/Prefabs/PlayerProjectile_MagicOrb.prefab`

## Current `HomingProjectile` behavior that must be preserved
- `Launch(int incomingDamage, int incomingWordLength = 0, AttackController owner = null, string incomingWord = "", bool isTargetChallengeCast = false, Transform explicitTarget = null)` stores damage, word length, owner, challenge mode, submitted word, and optional explicit target.
- `Update()` homes to its target; Titan Earthbreaker uses `SpellAimPosition`.
- `OnTriggerEnter(Collider other)` accepts recognized `EnemyController`, `BrambleSpriteController`, `EnemyHealth`, or a valid `VocabularyWorldTarget` for Target Challenge casts. It ignores other colliders.
- Once accepted, it sets `hasHit = true`, resolves `VocabularyBarrier`, applies the correct damage branch, notifies Target Challenge resolution or successful normal combat word landing, and destroys the projectile.
- `OnCollisionEnter(Collision collision)` currently delegates to the trigger handler. Do not introduce duplicate resolution or duplicate VFX when Unity invokes more than one callback.
- Do not alter `AttackController`, `SpellbookManager`, the word submission flow, damage multipliers, or Target Challenge barrier semantics.

## Required implementation
1. Add an optional serialized `GameObject` reference on `HomingProjectile` for the impact effect. A null reference must preserve existing behavior and must never throw.
2. Add a single private impact-spawn helper. It should instantiate `PlayerMagicOrb_ElectroHit_OneShot.prefab` at the accepted impact point and start the prefab's ParticleSystems. The prefab's root ParticleSystem has `playOnAwake = false`, so explicitly call `Play(true)` on the root system (or safely start all child systems once if no root ParticleSystem is present).
3. Trigger path: derive the impact point from `Collider.ClosestPoint(transform.position)` and use a stable normal fallback such as `-transform.forward` if the normal cannot be derived reliably.
4. Collision path: use `Collision.GetContact(0).point` and `.normal` when `contactCount > 0`; otherwise fall back to the trigger-path calculation.
5. Refactor both callbacks through one shared impact-resolution method or equivalent so `hasHit` guarantees exactly one damage resolution, notification, impact spawn, and projectile destruction.
6. Spawn the effect only after the collider passes the current accepted-target checks. Do not spawn VFX for ignored colliders. For accepted targets whose vocabulary barrier absorbs damage or rejects an unrelated Target Challenge barrier, keep the existing resolution behavior; the visual should reflect that a projectile physically struck the accepted target.
7. Do not parent the impact effect to the projectile or target. It must finish independently after impact. The prefab is already tuned as a one-shot: root lifetime 0.42 seconds, `loop = false`, `playOnAwake = false`, `Stop Action = Destroy`; GlowSphere lifetime 0.24 seconds; 16 sparks with lifetime 0.40 seconds. Do not add a new cleanup manager or coroutine unless a proven issue requires it.
8. Do not change `FindNearestEnemy`, `Launch` parameter order/defaults, `VocabularyBarrier.ResolveHit`, damage values, owner notifications, or the final `Destroy(gameObject)` behavior beyond the minimal shared callback structure needed to pass impact location data. A later visibility adjustment may add a short post-release movement gate without changing the homing calculation after that gate.

## Prefab assignment after script patch
- Assign the serialized impact reference on the `HomingProjectile` component inside `Assets/Prefabs/PlayerProjectile_MagicOrb.prefab` to `Assets/VFX/MagicOrb/Impact/PlayerMagicOrb_ElectroHit_OneShot.prefab`.
- Do not change the `VanAertPlayer.prefab` projectile reference. It must continue pointing to `Assets/Prefabs/PlayerProjectile_MagicOrb.prefab`.
- Do not edit the original package prefab `Assets/Hovl Studio/Magic effects pack/Prefabs/Hits and explosions/Electro hit.prefab`.

## Required tests before accepting the patch
- Normal `EnemyController` hit: exactly one impact, unchanged damage and `NotifySuccessfulAttackLanded` behavior.
- `EnemyHealth` fallback hit: exactly one impact, unchanged damage.
- `BrambleSpriteController` hit: exactly one impact, unchanged damage.
- Target Challenge exact-word barrier hit: exactly one impact, unchanged `ResolveHit` and `NotifyTargetChallengeProjectileResolved` behavior.
- Target Challenge wrong-word/absorbed hit: exactly one impact on the physically accepted barrier, unchanged zero-damage result and owner notification.
- Unrelated/non-target collider: no damage, no impact, projectile not consumed.
- Trigger and collision callback both observed for the same contact: one resolution and one impact only.
- Impact instance visibly plays once and is destroyed after the particle systems finish.
- Run existing compile/tests and inspect Unity Console for new errors. Do not claim Android is verified without testing on the connected Galaxy A05s.

## Final implementation and validation record
- Added `[SerializeField] private GameObject impactEffectPrefab` to `HomingProjectile`.
- Routed trigger and collision callbacks through `ResolveAcceptedImpact`, preserving the existing accepted-target checks and `hasHit` de-duplication guard.
- Trigger contacts raycast the incoming projectile segment against the exact collider to obtain a surface point/normal, including for non-convex MeshColliders. `Collider.ClosestPoint` is only used as a fallback for BoxCollider, SphereCollider, CapsuleCollider, and convex MeshCollider; unsupported types fall back to a collider-bounds ray intersection. This avoids Unity's `Physics.ClosestPoint` warning and an unreliable/hidden impact position.
- Collision contacts use `Collision.GetContact(0).point` and `.normal` when available, otherwise the same safe fallback.
- The impact is instantiated unparented and the root ParticleSystem is explicitly started with `Play(true)`; child systems are started individually only when no root ParticleSystem exists.
- Assigned `PlayerMagicOrb_ElectroHit_OneShot.prefab` on the production `PlayerProjectile_MagicOrb.prefab` component.
- Unity script validation: 0 errors. Editor domain reload completed without a new C# compile error.
- Play Mode checks passed: EnemyHealth trigger damage 25 + one impact; EnemyHealth collision damage 30 + one impact; subsequent trigger callback did not duplicate damage/effect; EnemyController damage 20 + one impact; BrambleSpriteController damage 15 + one impact; accepted Target Challenge world-target collision applied zero damage but spawned one impact; unrelated collider spawned no impact and did not consume the projectile.
- `AttackController.ShootMagicProjectile()` was exercised directly in a Play Mode harness: production projectile spawned, `Launch()` received the pending payload, pending damage cleared, duplicate release was blocked, 25 damage landed, the normal-combat owner event fired once for `HELLO`, and one impact spawned.
- The four authored attack clips were inspected: `OnAttackRelease` is at Basic 0.833s, Medium 0.633s, Strong 1.267s, and Ultimate 1.000s; each also has `OnAttackAnimationFinished`. A live gameplay log showed the release event invoking `ShootMagicProjectile()` and launching the production projectile.
- After the user reported that no orb was visible at the staff, the visual child was enlarged to 2.5 while leaving the root collider unchanged. `HomingProjectile` now holds movement for 0.12 seconds after `Launch()` so the orb remains perceptible at the animation-event release point before homing begins. The homing vector/targeting logic is otherwise unchanged.
- After gameplay logs showed damage landing alongside Unity's `Physics.ClosestPoint` unsupported-collider warning at `HomingProjectile.OnTriggerEnter`, trigger impact positioning was changed to raycast the incoming segment against the accepted collider. An isolated non-convex MeshCollider probe returned the expected entry-face point and normal without calling the unsupported closest-point API. This fixes the observed positioning warning path; authored gameplay visual confirmation is still pending.
- Remaining: visual replay of all four attack tiers in the authored gameplay scene, full Spellbook submission/history, exact-word Target Challenge owner-notification end-to-end regression, and physical Galaxy A05s Android validation.
