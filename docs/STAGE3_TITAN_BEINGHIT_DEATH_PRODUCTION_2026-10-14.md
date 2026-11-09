# Stage 3 Titan V017: BeingHit overlay and death integration

## Scope

Only the isolated `Animation_V017/Titan_ProductionIntegration` production Animator clone, production physical-combat candidate prefab, and `TitanEarthbreakerController` plus corresponding Editor helper code are modified. No source Golem clips, Astra animation, original Titan V015 death animation, or saved Stage3 scene are modified.

## BeingHit

Titan reuses the **Stone Golem's two-key architecture** of keeping the original combat base animation running while playing a separately weighted/masked hit overlay. The Golem's exact `Step Backward` and `Head Hit` curves are NOT copied: those paths target the Golem's skeleton and would damage the giant's real foot/IK contacts. The Titan-specific `Titan_V017_BeingHit_UpperBodyAdditive.anim` keys only `mixamorig:Spine2` and `mixamorig:Head` through paths discovered on the physical prefab. A Titan-specific AvatarMask and new **additive** `Titan Upper Body BeingHit` Animator layer (with `Empty` and `BeingHit` states) are installed in `Titan_V017_Stage3Boss_WDOn_ProductionClone.controller`. Runtime weight smoothly blends a brief 0.52-second recoil at up to 0.65 weight, with 0.6 seconds between reactions.

`EnemyHealth.TakeDamage` already calls `TriggerBeingHit` on the boss's own GameObject after every **nonlethal** actual health reduction. Titan implements that callback. While a real Hammerfist, Slam, Awakening or V017 is committed, damage applies normally but visible recoil is **queued until safe Chasing/hold**, never interrupting a physical hit event, animation rigging, or 94%-IK foot contact. An active recoil automatically fades to an empty layer and restores full base-layer control. Death clears the overlay immediately; missing additive layer fails visibly in the log without blocking damage.

The Titan additionally stops its ordinary **Chasing locomotion for the exact 0.52-second visible recoil** (like the Golem's hit-stop), then resumes NavMesh replanning. This is never run in an authored attack state, where it would risk invalidating the genuine foot/fist physics. Multiple spell hits inside the 0.6s cooldown are not allowed to infinitely stun-lock the boss.

## Death

The production Animator's existing `TitanDeathFromRightPreview` state now uses a **separate copied** V015 `Titan_V017_DeathWithFinishEvent_Production.anim` with a final-frame `OnDeathFinished` event at 3.22s. The untouched original V015 clip has 3.3s of authored movement. `EnemyHealth.Died` calls `TriggerDeath` exactly once; combat, NavMesh, wave sequences, V017 IK, hit overlay and projectile targeting are disabled, while Titan's solid physical corpse is preserved. The base Animator crossfades into the authored V015 death. An event reports defeat to the `Node 4` StageNodeController once, and invokes `onTitanDefeated` once. If an event is missed, a conservative `deathClip.length + 0.6s` fallback prevents a permanent Node 4 soft-lock; it does not invent contact, cause damage, or destroy the corpse.

## Tooling and test boundaries

- Explicit installer: `SPELLSTRIKE/Stage 3/Titan V017/Install Isolated BeingHit Layer and Death Event` (`Ctrl+Shift+Alt+V`). It is idempotent, and installs pending requested assets only when Unity exits busy compilation callbacks. It logged **PASS** with actual production Mixamo spine/head paths and length=3.300s.
- Play-only nonlethal QA: `Ctrl+Shift+Alt+G`. Injects a QA `EnemyHealth.TakeDamage` event only while genuinely Chasing/holding; **does not** claim the player landed a spell projectile.
- Play-only death QA: `Ctrl+Shift+Alt+Q`. Requires vulnerable, living production Titan, then applies lethal `EnemyHealth.TakeDamage` for event/animation/callback validation. This is not player-created combat damage.
- Play-only arena setup (`Ctrl+Shift+Alt+O`) hides the runtime vocabulary-practice overlay and restores time scale after using its riddle bypass, never touching the saved scene or treating the riddle as completed.

**Important validation distinction:** Source compilation and isolated asset installation are verified. Successful rendering of a nonlethal recoil, exact end of death motion, and a one-time Node 4 runtime notification must be independently confirmed in actual Unity Play. Do not mistake a generator PASS or edited controller YAML for a full gameplay acceptance test.

## Live original Stage3 result

**Authoritative V015 Death PASS:** After the production scene genuinely bootstrapped its NavMesh and the original Awakening completed, the editor-only **lethal EnemyHealth QA** ran. Unity recorded `S3N4_Titan_EarthbreakerV017_BOSS died.`, then `[TITAN DEATH] Genuine 0 HP: V015 death animation playing`, then `[TITAN DEATH] FINISHED event=True Node4Notified=True once. Corpse and solid colliders retained.` The actual clip event arrived at its final authored frame, before the 3.9-second safety fallback, and one `PersistentHUDRoot` health-bar removal was recorded. This is actual Play-mode proof of death playback/completion/callback, not a rendered cinematic-quality sign-off. Original death V015 source remained unchanged.

The first nonlethal QA shortcut refused to inject damage because Titan was in the middle of an authored attack. To exercise the **intended queued-reaction behavior**, the QA shortcut now accepts nonlethal damage in vulnerable combat, even during an authored attack; its isolated visual reaction is delayed to Chasing. Physical spell-projectile verification is still distinct from an injected QA `EnemyHealth.TakeDamage` event.
