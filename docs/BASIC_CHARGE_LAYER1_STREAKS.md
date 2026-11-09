# Basic Charge — Layer 1: Primary Streaks

Status: motion checkpoint rendered/reviewed; HDR appearance pass authored, Unity wiring and visual approval pending.

## Scope accepted for this pass

David requested one visual layer at a time, and approved Astra medium for
hands-on Unity VFX Graph authoring. This pass is **primary curved cyan streaks
only**, isolated from gameplay. Keep head simulation/GPU events but hide head
dot output. No core, haze/cloud, sphere shell, ring, wisps, motes or gameplay
integration is part of this layer. On 2026-09-30 David approved a further streak
appearance pass: Shader Graph HDR intensity, soft edges, and controlled bloom in
the isolated preview. This supersedes the earlier no-bloom constraint for that
preview; production camera/volume settings remain outside this pass.

The streaks must have irregular curved paths with an orbital component and a
visible inward pull toward the future staff crystal/origin. A generic radial
spray, straight collapsing lines, rigid circular spinner, or point cluster fails
the layer's visual target.

`Charge01` must read as small/faint at 0, more active at 0.5, and larger/brighter
at 0.96. Individual trajectories can converge while the overall charge envelope
grows. `ChargeVisibility` stays available for the separately implemented tail.

## Reference evidence

- Source: `C:\Users\David\Downloads\videoplayback (3).mp4`, SHA-256
  `F0130C995C1D3B683F338C059A22300ADE97DF4A8624BBCB8F66C5B4AC2D0853`.
- Full-video audit: 5,940 real frames sampled at 6 FPS, plus selected native 30 FPS
  sequences, detailed in `VFX_ORB_REFERENCE_AUDIT_2026-09-17.md`.
- Relevant narrated section: **01:43–07:20**, building and naming the trail layer.
- Native frame **06:30 / source frame 11,700** shows primary streaks alone before
  the core system. Use it for a fair layer-to-layer comparison; the completed
  orb at 12:41 contains other systems and is not this layer's acceptance image.
- The shorter `videoplayback (1).mp4` remains the specific motion reference for
  curved energy feeding a point. Avoid copying only the orb tutorial's spherical
  constraint; Conform to Sphere alone is not center convergence.

## Implementation plan and controls

| Part | Required behavior | Evidence required |
|---|---|---|
| Head simulation | Bounded local sphere spawn; short life; sensible speed; no gravity/upward template drift | Visible trails develop instead of becoming invisible points |
| Strip output | Cyan, tapered thin strips; stable GPU event/lifetime inheritance | Head dots are absent while strips remain visible |
| Motion | Irregular orbital curves plus inward pull, not rigid rotation | Normal and slow motion capture, readable individual paths |
| Charge01 | Small/faint to larger/brighter/more active | Same framing at 0, 0.5, 0.96 |
| Budget | Start near 8–12 desktop heads; consider 4–8 on mobile | Actual graph capacities/rates documented; no unmeasured FPS claims |
| Lifetime/bounds | Local origin, correctly padded bounds, predictable reset | No clipping or trails left behind after restart |

Expose meaningful artist controls for spawn radius/rate, strip width, life,
orbital strength/speed, inward attraction, turbulence, and cyan intensity where
needed. Shared properties must affect existing output consistently, with graph
blocks and connections visible to the artist.

## Isolated real-graph preview

### Saved graph controls in this pass

- `Charge01` is exposed, range 0–1: remaps spawn radius **0.08 → 0.30**
  and final strip alpha multiplier **0.25 → 1.0**.
- `ChargeVisibility` remains a separate exposed multiplier. The charge-alpha
  multiplier follows the lifetime alpha and visibility blocks on Strip Quad;
  neither is lost to the earlier alpha-over-life overwrite.
- Head output is hidden by a final Set Alpha = 0; head simulation/GPU events
  still generate strips. No separate bright head dots are drawn.
- Graph-node tuning: head size **0.006–0.012**, lifetime **0.8–1.5 s**,
  source rate **10/s**, GPU trail sampling **60/s**; attraction speed **0.45**,
  force **2**, stick force **1**; turbulence frequency **5**.
- Head capacity **13**; strip capacity **16 × 100 = 1,600** particles. These are
  capacity limits, not measured live counts or a validated Android budget.
- Radius/intensity remap endpoints and motion settings remain editable nodes;
  only `Charge01` and `ChargeVisibility` are Blackboard-exposed in this draft.
  A separate mobile quality control is not implemented in this pass.
- The saved Initialize velocity is `cross(position, float3(0, 0, 2))`, using
  Get Position Current, Cross Product and Set Velocity ordinary VFX Graph nodes.
  It gives sideways starting motion; Update sphere attraction and turbulence
  produce inward curved paths without a rigid circular spinner.
- Strip width has a smooth two-ended taper: 0 at normalized strip index 0,
  approximately 0.5 at 0.647, and 0 again at 1. Flat leading tips were removed.

Open **SPELLSTRIKE > VFX > Layer 1 - Streak Graph Preview**. This new window uses
the actual `BasicChargeStreaks.vfx` in a private preview scene; it creates no
gameplay object and draws no gizmos. It is distinct from the earlier CPU
**Basic Attack Charge Preview** window.

- Charge 0 / 0.5 / 0.96 buttons restart with seed 137, settle 48 rendered steps,
  and pause at 1.60 seconds for equal-age comparisons.
- Restart and Play allow free-running inspection; the displayed simulated time
  and GPU alive count make an empty preview distinguishable from a stopped graph.
- Camera distance changes framing; use the same distance for comparisons.
- Play, 0.25x and Step 1/30s support motion review.
- Save frame exports the actual graph render to `tmp/basic-charge-layer1/`.
- Record 2s / 60 frames exports consecutive renders simulated at 1/30-second
  steps. Encode at 30 fps for normal playback or 7.5 fps for quarter speed.
- A screenshot mockup or the older CPU preview cannot count as graph evidence.

## Validation and delivery

- [x] Actual graph rendering established with gizmos absent in the isolated Editor preview.
- [x] Primary cyan strips clearly visible; no supporting layers/head dots.
- [x] Curved tangential motion with inward attraction reviewed in the isolated preview.
- [x] Charge01 0/0.5/0.96 comparison captured in consistent framing.
- [x] Normal and slow motion clips captured from the actual graph.
- [x] Source controls/capacity/width/lifetime recorded.
- [ ] David approves Layer 1 before any new visual layer begins.

Preview C# compilation is a tooling check, not proof of VFX Graph rendering.
This isolated layer does not claim animated staff integration, full-charge
acceptance, a game build, or physical Galaxy A05s validation.

## Saved motion checkpoint — 2026-09-30

Actual final renders and node screenshots are in
`docs/visual-references/basic-charge-layer1-2026-09-30/`:

- `charge-0.png`, `charge-0.5.png`, `charge-0.96.png`: same camera distance 1.25,
  seed 137 and 1.60-second simulation maturity, 1200 × 900 rendered pixels.
- `charge-0.96-bright.png`: the same charge against a bright background.
- `motion-normal.mp4` / `motion-quarter-speed.mp4`: the same 60 consecutive actual
  graph renders, advancing 1/30 second per frame, encoded at 30 FPS / 7.5 FPS.
  Source sequence: `tmp/basic-charge-layer1/20260930-150726-350-q0.96/`.
- All 60 frames were visually inspected across six indexed sheets, and the live
  Editor preview was inspected at normal and quarter speed. This is a fixed-charge
  motion review, not the 0.833-second BasicAttack timing/attachment test.
- `graph-tangential-motion.png`, `graph-strip-taper.png`,
  `graph-growth-width.png`, `graph-strip-output.png` show the authoring nodes.
- `reference-trails-06m30s.jpg` is the matching tutorial layer. Our checkpoint is
  sparser and has smoother, longer arcs; the tutorial has denser, more irregular
  curls and stronger glow. Do not claim a complete reference match.

Motion-checkpoint graph SHA-256:
`9BE09E58721C8161889F8153FF56C61A3CC121C79F16C70F179F607642513C39`.
A transient Strip Initialize warning icon was observed, then disappeared on
inspection; its message/cause was not established. No graph compilation error
surfaced during these actual rendering checks. Do not equate this with a clean
full-project Console or a player build.

## HDR appearance pass — prepared, not visually validated

- New `BasicChargeStreakAppearance.shadergraph`: URP Unlit with Support VFX Graph,
  transparent additive blending, both faces, ordinary native nodes only. No custom
  HLSL wrapper, texture lookup, light, refraction or distortion.
- UV0.y feeds Subtract/Absolute/Multiply/One Minus/Saturate/Power to soften the
  ribbon width. Particle alpha multiplies that mask, retaining lifetime/visibility.
- `Charge01` is clamped and lerps **Emission Low 0.35 → Emission High 6**. This HDR
  multiplier and **Streak Tint** multiply particle RGB into unlit Base Color.
  Unlit energy uses HDR color rather than a lighting-dependent emission surface.
- **Edge Softness 1.4** adjusts the cross-width falloff. Shader Blackboard controls
  are `Charge01`, `Emission Low`, `Emission High`, `Edge Softness`, `Streak Tint`.
- The visible preview tool now has HDR controls and a preview-only URP Bloom
  toggle/intensity (0.35), threshold 1, scatter 0.4, clamp 12, high-quality filtering
  off. Its temporary Volume is enabled only during its camera render and destroyed
  on close; production volumes/renderer assets are not edited.
- Pending: assign this Shader Graph to the VFX strip output, link the existing
  Charge01 and expose EmissionLow/EmissionHigh/EdgeSoftness on the VFX Blackboard,
  verify native shader import, actual HDR/bloom behavior, control response and
  dark/bright-background captures. The `.vfx` motion checkpoint has not yet been
  changed to use this appearance asset.
- Preview C# passed targeted compilation against installed Unity/URP assemblies.
  Shader Graph serialization/reference checks passed; import/render remain pending.
