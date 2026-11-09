# Regular enemy HP bar unification — 2026-10-19

## Request and scope

Apply the **same approved gold/navy-framed, red-live-fill overhead HP bar that
Bramble Sprite uses** to every current playable **non-boss enemy**, without
altering combat damage, encounter flow, enemy animation, collision, prefabs,
or the separate top-right boss health HUD. Existing Stage1/2/3 boss HUDs
must not gain an overhead bar.

## Production runtime path

- The implementation is shared, **not nine separate prefab canvases**:
  `EnemyEncounterHUD.ShowFor(enemyRoot)` resolves the `EnemyHealth` on the
  encounter root or child and routes **regular** enemies to
  `RegularEnemyOverheadHealthBar.EnsureVisible`. It hides the persistent boss
  slider for those encounters.
- `RegularEnemyOverheadHealthBar` uses the same two approved sprites:
  `Assets/Resources/SpellstrikeHUD/RegularEnemyHP_Frame.png` (fixed frame)
  and `RegularEnemyHP_Fill.png` (independent horizontal fill). Its
  world-space Canvas follows the active camera, stays about 118 screen
  pixels wide, hides on death/offscreen, and does not intercept clicks.
- Regular `EnemyHealth.TakeDamage` updates live UI via `HealthChanged`,
  plus direct-field-change fallback in `LateUpdate` for the older Bramble
  and legacy damage routes. Existing prefabs/scenes remain authoritative.
- `EnemyHealth.UsesBossHud` detects Hollow Knight, Drowned Captain,
  Titan Earthbreaker and the anticipated finale boss controller names.
  Existing boss routing still calls `PersistentHUDRoot.ShowEnemyHealth`.

## Verified source/scene roster (runtime appearance pending)

| Stage | Regular enemies intended to share Bramble's overhead art | Wiring evidence |
| --- | --- | --- |
| Stage 1 | **Bramble Sprite, Stone Sentinel, Cursed Jester** | `Node1Controller`, `StoneSentinelTriggerEncounter`, and `CursedJesterTriggerEncounter` each call `EnemyEncounterHUD.ShowFor`; all three scene enemy roots have `EnemyHealth` |
| Stage 2 | **Barnacle Husk, Corsair Phantom, Tide Wraith** | Each `StageNodeTrigger` points to an actual scene enemy root with `EnemyHealth`; common `ShowFor` used before/after activation |
| Stage 3 | **Sunbound Warden, Stone Golem, Desert Archer** | `StageNodeTrigger` for nodes 1/2/3 points to a root containing `EnemyHealth`, using the same common `ShowFor` path, with imported prefabs on Golem/Archer |

The original Stage3 serialized progression order is **Warden Node1 →
Stone Golem Node2 → Desert Archer Node3 → Titan boss Node4**. The UI
rollout neither reorders nor rewires encounters.

**Boss exclusions:** Hollow Knight (Stage1), Drowned Captain (Stage2),
Titan Earthbreaker (Stage3). Their top-right HUD and special reveal/phase
handling stay intact. Future Vander/final-stage enemies are not yet authored
in a production scene and are excluded from claims of tested integration.

## Changes in this follow-up

Modified only `Assets/Scripts/EnemyScripts/RegularEnemyOverheadHealthBar.cs`:

1. The Bramble Sprite's controller owns its actual maximum health and mirrors
   its changing `currentHealth` to an `EnemyHealth` component that can have
   a **different serialized maxHealth**. The overhead presenter now derives
   the fill denominator from `BrambleSpriteController.maxHealth` only for
   Bramble, including event and legacy direct-change refresh. This corrects
   the visual percentage **without** changing any gameplay HP values or
   modifying the Bramble controller itself.
2. An enemy activated after `ShowFor` was called while its root was inactive
   (e.g., Corsair Phantom) now recaptures its model-top anchor in
   `OnEnable`. The visual remains stable after that capture, instead of
   anchoring permanently to an inactive model/collider fallback.

Pre-edit script backed up with matching SHA256 under
`Library/RegularEnemyHPUnification_20261019/`. No serialized scene,
prefab, boss controller, sprite, player HUD, or save changes were required.

## Verification and outstanding QA

- [x] Script/serialized scene audit for all nine regular enemies and three
  current bosses.
- [x] Both approved Resources Sprite assets present; use of world-space
  dynamic filled Sprite verified in source.
- [x] `git diff --check` on edited script.
- [x] Unity Editor observed importing/recompiling the updated shared HUD
  script; its log showed graph compilation/assembly reload with no reported
  `error CS` diagnostic for this change. This is **not** a visual test.
- [ ] Fresh Unity Play Mode: each of nine appears **above the correct model**,
  starts full, red fill shrinks at damage, hides on defeat, no duplicate.
- [ ] Stages 1–3 boss fights: **top-right boss-only**, Titan remains hidden
  until its authored unlock.
- [ ] Corsair Phantom enable-after-trigger positional QA and Bramble
  mismatched-health-value fractional fill validation.
- [ ] PC 16:9 and Galaxy A05s 20:9: readable at gameplay distance, no
  obstructed camera/word UI, acceptable frame time and no orphan UI objects.
- [ ] Summoned minions: no current Stage2/3 code instantiates enemy actors in
  the audited paths. Any future summon prefab must include `EnemyHealth`
  and explicitly call `EnemyEncounterHUD.ShowFor` on spawn to show before
  the first hit (the existing fallback only shows after a surviving hit).

Until live Unity and device checks are recorded, source/scene wiring is
verified but final gameplay visual/device acceptance is **pending**.
Visible Unity Desktop control was intermittently unavailable on this pass,
so no unverified Play Mode assertion was substituted for a live test.
