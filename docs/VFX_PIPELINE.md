# SPELLSTRIKE VFX Pipeline

> **Purpose**
>
> This document teaches Codex and team members how to design, implement, and discuss visual effects in SPELLSTRIKE.
>
> In SPELLSTRIKE, VFX are **layered compositions**, not single particle systems.
>
> A good VFX effect is built from multiple small visual layers that combine into one readable gameplay moment.

---

# Core Rule

Never treat a major VFX as one object only.

Instead, think:

```text
Final VFX
= Shape / Core
+ Glow / Flare
+ Motion / Spread
+ Detail Particles
+ Impact / Cleanup
+ Gameplay Timing
```

Different VFX types use different layers, but the principle is always the same:

```text
Readable first.
Layered second.
Polished third.
Optimized always.
```

---

# VFX Implementation Rules for Codex

Before implementing any VFX:

1. Identify the gameplay event.
2. Identify where the VFX starts.
3. Identify where the VFX travels, if it moves.
4. Identify where the VFX impacts.
5. Identify when damage/debuff/gameplay logic happens.
6. Build the VFX in small layers.
7. Keep gameplay timing separate from visual lifetime.
8. Use placeholder visuals first if needed.
9. Avoid heavy mobile-unfriendly effects unless explicitly approved.
10. Finish with a testing checklist.

---

# Important Gameplay Sync Rule

VFX should support gameplay timing, not control it.

Good:

```text
Projectile hits enemy
↓
Damage resolves
↓
Impact VFX spawns
```

Good:

```text
Animation event calls ResolveAttack()
↓
Damage/debuff applies
↓
Impact VFX spawns
```

Bad:

```text
Particle lifetime ends
↓
Damage happens
```

Do not make damage, stun, knockback, tile cracking, or death depend only on particle lifetime.

---

# Universal Impact VFX Stack

Use this for basic hits, magic impacts, projectile impacts, tile impacts, water impacts, and boss impacts.

## Final Impact VFX

```text
Impact VFX
= MainHit
+ Flare
+ SpikyShapes
+ Spread
+ Sparkles
```

Recommended prefab hierarchy:

```text
Impact_VFX
├── MainHit
├── Flare
├── SpikyShapes
├── Spread
├── Sparkles
└── AutoDestroy
```

---

## 1. MainHit

### Purpose
The bright center of the impact.

It tells the player:

```text
The hit happened here.
```

### Visual Style
- bright flash
- small star/core
- short lifetime
- highest brightness at the center
- usually appears immediately on impact

### Implementation Options
- small particle burst
- billboard sprite
- quad with additive material
- short animated sprite
- VFX Graph burst if available

### Timing
Appears at frame 0 of the impact.

Suggested lifetime:

```text
0.08s – 0.20s
```

---

## 2. Flare

### Purpose
Adds glow, bloom, and magical energy.

It makes the impact feel stronger and more readable.

### Visual Style
- soft radial glow
- cross flare
- bloom burst
- cyan/blue/gold/magic-colored aura

### Implementation Options
- additive sprite
- transparent quad
- particle with soft texture
- optional point light for PC only

### Timing
Appears with MainHit or slightly after.

Suggested lifetime:

```text
0.15s – 0.40s
```

### Mobile Note
Avoid too many real-time lights. Prefer glow sprites/materials.

---

## 3. SpikyShapes

### Purpose
Adds sharp force and aggression.

It makes impacts feel less soft.

### Visual Style
- sharp triangular spikes
- slash-like shards
- starburst points
- directional energy shards

### Implementation Options
- burst particles using sharp textures
- mesh shards
- sprite sheet
- small additive quads

### Timing
Appears immediately on hit, then fades quickly.

Suggested lifetime:

```text
0.10s – 0.30s
```

---

## 4. Spread

### Purpose
Shows energy expanding outward.

It gives the effect motion and scale.

### Visual Style
- expanding ring
- circular shockwave
- misty burst
- splash expansion
- ripple

### Implementation Options
- scaling ring mesh
- expanding particle texture
- shader graph ring
- simple animated sprite

### Timing
Starts near impact time and expands outward.

Suggested lifetime:

```text
0.25s – 0.70s
```

---

## 5. Sparkles

### Purpose
Adds small detail particles.

It prevents the effect from feeling flat.

### Visual Style
- tiny energy fragments
- small droplets
- magic sparks
- dust bits
- glowing motes

### Implementation Options
- particle system burst
- random velocity particles
- short trails
- small additive sprites

### Timing
Starts on impact and trails slightly after the main flash.

Suggested lifetime:

```text
0.30s – 1.20s
```

---

# Player Basic Attack Impact VFX

Used when Van Aert's magic projectile hits an enemy.

## Design

```text
PlayerBasicHit_VFX
= MainHit
+ Flare
+ SpikyShapes
+ Spread
+ Sparkles
```

Recommended hierarchy:

```text
PlayerBasicHit_VFX
├── MainHit
├── Flare
├── SpikyShapes
├── Spread
├── Sparkles
└── AutoDestroy
```

## Gameplay Sync

Correct flow:

```text
Player submits valid word
↓
Attack animation plays
↓
Projectile spawns
↓
Projectile hits enemy
↓
Damage applies
↓
PlayerBasicHit_VFX spawns at hit point
```

Do not spawn the impact VFX when the attack animation starts unless the attack is melee.

## Scaling by Word Strength

```text
3–4 letters:
small impact, subtle flare, few sparkles

5–7 letters:
medium impact, stronger spread, more sparkles

8+ letters:
large impact, brighter flare, larger spread

Ultimate / rare word:
large impact + extra flare + stronger particles + optional camera/UI feedback
```

---

# Projectile Trail VFX

Used for magic bolts, spectral bolts, water bolts, enemy projectiles, and boss projectiles.

## Recommended Layers

```text
ProjectileTrail_VFX
= CoreProjectile
+ Trail
+ SmallParticles
+ Glow
+ ImpactVFX
```

Recommended hierarchy:

```text
ProjectilePrefab
├── CoreProjectile
├── TrailRenderer
├── TrailParticles
├── Glow
└── ImpactVFXReference
```

## Layer Definitions

### CoreProjectile
The visible body of the projectile.

Examples:
- magic orb
- water bolt
- ghost flame
- energy shard

### Trail
Shows motion direction.

Options:
- TrailRenderer
- particle trail
- stretched sprite

### SmallParticles
Adds magical breakup and movement detail.

Examples:
- sparks
- droplets
- spectral mist
- dust

### Glow
Makes the projectile readable during movement.

### ImpactVFX
Spawns when projectile hits a target or expires.

---

# Warning / Telegraph VFX

Used before enemy attacks, boss AoEs, tidal waves, cone attacks, and ground impacts.

## Purpose

Telegraphs tell the player:

```text
Danger will happen here soon.
```

## Recommended Layers

```text
Warning_VFX
= WarningShape
+ EdgeGlow
+ Pulse
+ OptionalRunes
+ FadeOut
```

Recommended hierarchy:

```text
WarningArea_VFX
├── WarningShape
├── EdgeGlow
├── Pulse
├── OptionalRunes
└── AutoFade
```

## Common Shapes

```text
Circle = AoE impact
Cone = scream/breath attack
Rectangle/Line = wave or charge attack
Ring = shockwave
Path strip = dash/charge warning
```

## Rules

- Warning must appear before damage.
- Warning duration should match attack windup.
- Warning should disappear or transform when attack resolves.
- Warning should be readable even on mobile screens.

Suggested duration:

```text
0.5s – 1.2s
```

---

# Tidal Wave VFX

Used for Tide Wraith's signature ability.

## Design Summary

Tidal Wave is a layered water attack, not one particle system.

The main visible wave is:

```text
MainWave
= WaveMesh
+ WaterBody
+ FoamCrest
```

Supporting detail:

```text
+ SpawnSplash
+ ImpactSplash
+ SourceDroplets
+ TrailDroplets
+ OptionalShockwaveRipple
+ OptionalDistortion
```

Recommended hierarchy:

```text
TidalWave_VFX
├── MainWave
│   ├── WaveMesh
│   ├── WaterBody
│   └── FoamCrest
├── SpawnSplash
├── ImpactSplash
├── SourceDroplets
├── TrailDroplets
├── ShockwaveRipple
├── OptionalDistortion
└── AutoDestroy
```

---

## Tidal Wave Layer Definitions

### WaveMesh
The base shape of the wave.

Purpose:
- defines the silhouette
- gives the attack its large moving form

Implementation:
- mesh renderer
- stylized wave mesh
- low-poly mesh preferred for Android

### WaterBody
The blue water material over the wave.

Purpose:
- gives the wave its main color and volume

Implementation:
- water shader
- scrolling texture
- simple blue transparent material if needed

### FoamCrest
The white foam layer on top/sides.

Purpose:
- makes the wave look like crashing water
- adds readability and motion

Implementation:
- second material layer
- foam mask texture
- separate mesh/overlay
- particle strip on top edge if needed

### SpawnSplash
Splash when wave appears.

Purpose:
- sells the cast origin

Implementation:
- particle burst
- short water splash sprite
- foam burst

### ImpactSplash
Splash when wave hits player or reaches endpoint.

Purpose:
- sells the collision

Implementation:
- water impact VFX stack
- droplets + foam + spread ring

### SourceDroplets
Small water drops near the caster/source.

Purpose:
- adds detail and motion

### TrailDroplets
Small droplets following the wave.

Purpose:
- prevents the moving wave from feeling static

### ShockwaveRipple
Optional expanding ring on impact.

Purpose:
- adds force/readability

### OptionalDistortion
Optional water refraction/ripple.

Purpose:
- extra polish

Mobile note:
- Keep distortion optional.
- Avoid expensive screen-space effects unless tested.

---

# Tidal Wave Gameplay Sync

Correct flow:

```text
Tide Wraith starts cast
↓
WarningArea appears
↓
Cast windup finishes
↓
TidalWave_VFX spawns and moves forward
↓
TidalWave hitbox touches player
↓
Damage + knockback + short stun apply
↓
ImpactSplash spawns
↓
Wave expires and cleans up
```

Do not tie stun/knockback to particle lifetime.

---

# Tidal Wave Impact VFX

When Tidal Wave hits the player or environment:

```text
TidalWaveImpact_VFX
= MainHit
+ Flare
+ WaterSplash
+ FoamSpread
+ Droplets
+ Sparkles
```

Recommended hierarchy:

```text
TidalWaveImpact_VFX
├── MainHit
├── Flare
├── WaterSplash
├── FoamSpread
├── Droplets
├── Sparkles
└── AutoDestroy
```

---

# Tile Cracking VFX

Used when Corsair Phantom or other enemies crack/break tiles.

## Recommended Layers

```text
TileCrack_VFX
= CrackFlash
+ CrackOverlay
+ SmallShards
+ Dust/Sparkles
+ StateChange
```

Recommended hierarchy:

```text
TileCrack_VFX
├── CrackFlash
├── CrackOverlayPulse
├── SmallShards
├── DustParticles
└── AutoDestroy
```

## Layer Definitions

### CrackFlash
A brief flash when the tile changes state.

### CrackOverlay
Visible crack texture on the tile.

This should remain while the tile is Cracked.

### SmallShards
Tiny pieces or particles when the tile cracks/breaks.

### Dust/Sparkles
Small detail particles.

### StateChange
The actual tile state change must be controlled by TileState / TileDebuffManager, not the VFX.

---

# Tile Broken VFX

Used when a Cracked tile becomes Broken.

## Recommended Layers

```text
TileBreak_VFX
= BreakFlash
+ Shards
+ Dust
+ FadeOut
```

Recommended hierarchy:

```text
TileBreak_VFX
├── BreakFlash
├── Shards
├── Dust
└── AutoDestroy
```

Gameplay sync:

```text
Tile state changes to Broken
↓
TileBreak_VFX spawns
↓
Tile hides/deactivates
```

If needed, VFX can spawn before tile deactivation.

---

# Ghost / Spectral VFX

Used for Corsair Phantom, Tide Wraith, ghost enemies, spectral projectiles, and soul effects.

## Recommended Layers

```text
GhostVFX
= CoreShape
+ Mist
+ Glow
+ Wisps
+ Fade
```

Examples:

```text
GhostGlide_VFX
├── BodyTrail
├── SpectralMist
├── EdgeGlow
├── Wisps
└── FadeOut
```

```text
SpectralBolt_VFX
├── CoreOrb
├── MistTrail
├── Glow
├── SmallWisps
└── ImpactVFXReference
```

## Style Notes

- use cyan, teal, blue-green, pale green, or ghostly white
- avoid overly solid shapes
- use transparent/fading layers
- ghost effects should feel light and fading, not heavy

---

# Slash / Melee Impact VFX

Used for sword hits, claw hits, spectral slash, melee enemies, and boss slashes.

## Recommended Layers

```text
SlashImpact_VFX
= SlashArc
+ MainHit
+ SpikyShapes
+ Sparkles
+ OptionalTrail
```

Recommended hierarchy:

```text
SlashImpact_VFX
├── SlashArc
├── MainHit
├── SpikyShapes
├── Sparkles
└── AutoDestroy
```

## Rules

- SlashArc shows motion direction.
- MainHit appears where contact happens.
- SpikyShapes sell force.
- Sparkles/debris sell polish.

---

# Boss AoE VFX

Used for boss stomps, slams, novas, circles, rune attacks, shockwaves.

## Recommended Layers

```text
BossAoE_VFX
= WarningArea
+ ChargeGlow
+ ImpactCore
+ ShockwaveSpread
+ Debris/Particles
+ Aftermark
```

Recommended hierarchy:

```text
BossAoE_VFX
├── WarningArea
├── ChargeGlow
├── ImpactCore
├── ShockwaveSpread
├── DebrisParticles
├── Aftermark
└── AutoDestroy
```

## Gameplay Sync

```text
Warning appears
↓
Boss animation reaches impact frame
↓
Damage applies
↓
Impact VFX spawns
↓
Shockwave/debris play
↓
Aftermark fades
```

---

# Explosion / Magic Burst VFX

Used for magic impacts, boss spells, ultimate word attacks, and powerful enemy casts.

## Recommended Layers

```text
MagicBurst_VFX
= MainHit
+ Flare
+ SpikyShapes
+ Spread
+ Sparkles
+ Smoke/Mist
```

Recommended hierarchy:

```text
MagicBurst_VFX
├── MainHit
├── Flare
├── SpikyShapes
├── Spread
├── Sparkles
├── SmokeOrMist
└── AutoDestroy
```

---

# UI Feedback VFX

Used for word validation, STRIKE button, tile selection, ultimate word activation, potion use, rewards.

## Recommended Layers

```text
UIFeedback_VFX
= Highlight
+ Pulse
+ Glow
+ Particles
+ Fade
```

Examples:

```text
ValidWord_VFX
├── WordBarGlow
├── Pulse
└── Sparkles
```

```text
StrikeButtonReady_VFX
├── ButtonGlow
├── BorderPulse
└── SmallParticles
```

```text
RewardUnlock_VFX
├── ItemGlow
├── Burst
├── Sparkles
└── Shine
```

## Mobile Note

UI VFX should be readable but not distracting.
Avoid huge particle counts over UI.

---

# Cleanup Rules

Every temporary VFX must clean itself up.

Options:
- AutoDestroy script
- ParticleSystem Stop Action = Destroy
- Coroutine cleanup
- Object pooling later if performance requires it

Avoid leaving unused VFX objects in the scene.

---

# Performance Rules for PC and Android

## Prefer

- mesh/material for large stable shapes
- particles only for details
- short lifetimes
- object pooling for repeated effects later
- simple transparent materials
- compressed textures
- low particle counts on Android
- fewer real-time lights

## Avoid

- too many overlapping transparent particles
- expensive distortion everywhere
- long-living particle systems
- unbounded particle emission
- heavy screen-space effects on mobile
- spawning many VFX every frame

---

# Codex Checklist Before Implementing VFX

Codex should answer these before editing:

```text
1. What gameplay event triggers this VFX?
2. Where does it spawn?
3. Does it move?
4. When does it impact?
5. Which layers are needed?
6. Which layers are optional polish?
7. What object owns cleanup?
8. Does gameplay timing depend on animation event, collision, or scripted resolve?
9. Is this Android-friendly?
10. What testing is required?
```

---

# Stylized Orb VFX

Used for magic projectiles, spell charges, ghost bolts, boss casts, and ultimate word attacks.

## Layer Formula

StylizedOrb_VFX
= DarkFlare
+ BrightCore
+ FloatingParticles
+ OuterSphere
+ ShaderErosion
+ TextureScroll
+ Trail / Impact

## Recommended Hierarchy

StylizedOrb_VFX
├── DarkFlare
├── BrightCore
├── FloatingParticles
├── OuterSphere
├── Trail
├── ImpactVFXReference
└── AutoDestroy

## Layer Notes

DarkFlare:
A darker background glow that gives the orb depth.

BrightCore:
The bright center that makes the orb readable.

FloatingParticles:
Small particles orbiting or floating around the orb.

OuterSphere:
A sphere mesh shell, preferably made/exported from Blender.

ShaderErosion:
Uses alpha clipping/noise/Voronoi texture to create a magical broken surface.

TextureScroll:
Moves the texture over time so the orb does not feel static.

Trail:
Used only if the orb moves as a projectile.

Impact:
Spawned when the orb hits an enemy/player/environment.

# VFX Implementation Template

Use this when planning a new effect:

```md
## VFX Name

### Gameplay Event
What triggers it?

### Layers
- Main/core:
- Glow/flare:
- Motion/spread:
- Detail particles:
- Impact:
- Cleanup:

### Prefab Hierarchy
EffectName_VFX
├── ...
└── AutoDestroy

### Timing
- Spawn:
- Peak:
- Impact:
- Fade:
- Destroy:

### Gameplay Sync
What method/event/collision applies gameplay logic?

### Performance Notes
What should be simplified for Android?

### Testing Checklist
- [ ] Spawns correctly
- [ ] Plays at correct timing
- [ ] Cleans up
- [ ] Does not cause console errors
- [ ] Does not obscure gameplay
- [ ] Works on Android/mobile view
```

---

# Current SPELLSTRIKE VFX Priorities

1. PlayerBasicHit_VFX
2. Tide Wraith TidalWave_VFX
3. Tide Wraith TidalWaveImpact_VFX
4. Corsair Phantom PiercingScream_VFX
5. Corsair Phantom TileCrack_VFX
6. Boss AoE warning + impact VFX
7. UI word/STRIKE feedback VFX

---

_Last Updated: 2026-07-08_
