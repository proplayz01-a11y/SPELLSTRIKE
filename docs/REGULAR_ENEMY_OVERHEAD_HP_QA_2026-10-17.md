# Regular enemy overhead HP — 2026-10-17

Status: **code integrated, C# compile verified, runtime visual QA pending**.

## Existing ownership / integration

- `Assets/Scripts/EnemyScripts/EnemyHealth.cs`: existing HP/death authority,
  new serialized `HudDisplayRole` field (Auto, RegularOverhead, BossHud).
- `Assets/Scripts/EnemyScripts/EnemyEncounterHUD.cs`: existing encounter
  presentation bridge; regular calls `RegularEnemyOverheadHealthBar.EnsureVisible`
  and clears top-right persistent HP, while boss calls existing
  `PersistentHUDRoot.ShowEnemyHealth`.
- `Assets/Scripts/EnemyScripts/RegularEnemyOverheadHealthBar.cs`: new
  noninteractable World Space uGUI Canvas per active regular enemy.

No new manager, material, prefab, scene, collision or damage script. No
existing Stage Select, SampleScene or gameplay HUD positioning edits.

## Behavior

- Auto boss detection based on major controller components, checking enemy
  ancestors/descendants: HollowKnightController, Stage1BossController,
  DrownedCaptainController, TitanEarthbreakerController and anticipated
  Master Wizard / finale controller names. Inspector override remains
  authoritative when a controller name differs.
- Normal encounter trigger routes activate the overhead bar; spawned enemies
  missing a trigger get a bar after their first **surviving** EnemyHealth hit.
- The thin red HP fill uses actual health fraction. It refreshes through
  `EnemyHealth.HealthChanged` as well as a cheap fallback comparison each frame,
  because BrambleSpriteController and older EnemyController write the public
  currentHealth field without invoking that event.
- Camera-facing bar uses bounds of active skinned/mesh renderers with collider
  fallback and fixed approximate pixel size in 1080p, independent of giant
  model hierarchy scale. It hides off-screen, behind camera, at 0 HP,
  when the enemy is inactive/dead, or if no camera is found.
- All decorative graphics have `raycastTarget=false`, no GraphicRaycaster.
  Bar cleanup unsubscribes events and destroys the world-space canvas on
  enemy destruction. Existing persistent HUD boss controls remain unchanged.

## Static / compilation checks

- Compiled via `dotnet build` against a **temporary shadow copy** of the
  Unity-generated `Assembly-CSharp.csproj`, explicitly including new source.
  Build returned exit code 0, zero errors and existing unrelated project
  warnings. Shadow project cleaned; original generated project unchanged.
- Verified all known `EnemyEncounterHUD.ShowFor` callers use the shared bridge.
- Verified production scene SHA256:
  - SampleScene: `7AC9BC6C17456DA80A9F05FF69D99E396E0745854DA599B8BB093BD178CCDCC3`
  - StageSelectScene: `EB75B478CAEDFFD104ACBA214CE2A83E206198A4601BDA40B9D826C8B0BA4521`
  - StageSelectBook_ReferenceFix: `2413C9B6551F93D4CB22CDFE74D38531F458B70691598333F18EC235D4A8FD4E`

## QA not yet performed

The user had requested no Unity foreground interruptions. No active Unity
instance was available during this coding pass, so neither Edit/Play Game
View, per-enemy 3D placement, touch/UI overlap nor device GPU/performance
testing is claimed.

### Targeted acceptance checklist

1. Stage 1 Bramble trigger: only overhead HP appears; damage via Bramble's
   direct HP updates shrinks red fill, bar disappears on death.
2. Stage 1 Stone Sentinel/Jester: bar hovers above moving head and follows
   animation/locomotion, no top-right bar. Background must not block clicks.
3. Stage 1 Hollow Knight: **top-right boss HUD only**, no overhead.
4. Stage 2 Barnacle Husk, Corsair Phantom, Tide Wraith: overhead health at
   reasonable scale; Drowned Captain: boss HUD only.
5. Stage 3 Archer/Golem, including scaled model and distant camera: bar stays
   aligned and legible. Titan: no bar until existing boss reveal gate.
6. Health at 100%, roughly 50%, near zero and zero, plus enemy disable,
   stage transitions and multiple enemies. Verify no orphan canvases.
7. Android Galaxy A05s: readable without overlap with player HUD, tiles,
   joystick or buttons; compare UI and GPU/frame-time overhead against
   existing physical baseline before declaring performance validated.

### Known limitations

- World Space uGUI relies on active camera's depth rendering for environmental
  occlusion. Mesh sorting/depth, camera tag and landscape view need actual Play
  evidence. Bars near overlapping enemies may occlude one another; no stacking.
- Legacy enemies that never call `EnemyEncounterHUD.ShowFor` display their HP
  on first surviving `EnemyHealth.TakeDamage`, not immediately on spawn.
- Only known boss controllers are auto-detected; new bosses require explicit
  role marking or classifier update.
