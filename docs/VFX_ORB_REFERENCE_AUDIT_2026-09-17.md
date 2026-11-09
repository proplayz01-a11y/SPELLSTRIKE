# VFX Orb Tutorial Reference Audit — Revised 2026-09-30

## Verdict

Use this tutorial as a **technique library**, not as a finished Basic charge recipe.
Its strongest transferable contribution is the first layer: sphere-local trail
heads, turbulence, Conform to Sphere, inherited strip lifetime, and tapered strip
width. The tutorial later adds a soft beam/core and an extremely dense particle
cloud. Those layers must be rebuilt separately and approved one at a time for
SPELLSTRIKE. The finished cyan example is a steady looping orb with a closed shell;
it does not demonstrate David's attack buildup, inward convergence, one-ring rule,
release timing, or post-release fade.

Recommended decision: restart visual approval at **Layer 1 — primary curved
streaks only**. Do not hide weak streak motion behind the core, ring, bloom, or
motes. Integrate only after the isolated layers and final composition pass.

## Source and inspection coverage

- Supplied source: `C:\Users\David\Downloads\videoplayback (3).mp4`
- SHA-256: `F0130C995C1D3B683F338C059A22300ADE97DF4A8624BBCB8F66C5B4AC2D0853`
- 16:30.05, 640 x 360, 30 fps H.264 video with English AAC audio.
- **29,700 decoded/indexed frames.** On 2026-09-30 the full video was reviewed at
  **6 FPS**: 5,940 real source frames, every fifth frame, arranged sequentially
  into 42 labeled contact sheets covering 00:00.000–16:29.833. This supersedes
  the earlier 15-second overview as the whole-video visual review.
- **390 consecutive native-rate frames visually inspected** in four selected
  intervals: 00:19–00:22 (frames 570–659), 00:26–00:30 (780–899),
  14:45–14:48 (26,550–26,639), 15:24–15:27 (27,720–27,809).
- Full spoken tutorial locally transcribed into 136 timestamped ASR segments.
  The last recognized speech ends near 16:24; the ending contains promotion,
  thanks, and credits. The ASR is imperfect, especially node names and numbers;
  timing/technical claims below were cross-checked against the visual overview.
- David later supplied a separate timestamped transcript on 2026-09-30. It confirms
  the same construction order and the stated 1,000 trail/strip capacities,
  5,000-to-50,000 dense-particle rate increase, and GPU warning. A preserved copy
  is [included with the 6 FPS evidence](visual-references/vfx-orb-2026-09-30-six-fps/user-supplied-transcript.txt).
  It also contains speech-to-text errors (for example “purling curl noise,”
  “trails pound rate” and “beam”), so exact operator names must still be verified
  in the current Unity/VFX Graph UI rather than copied from transcript spelling.
- The 6 FPS review was visually inspected sheet by sheet. The four ranges above
  remain the detailed **30 FPS** inspection. The other 23,370 decoded frames were
  indexed, not individually watched; no claim of full 30 FPS inspection is made.

Extracted [overview and consecutive-frame sheets](visual-references/vfx-orb-2026-09-17/)
and the [full frame index](visual-references/vfx-orb-2026-09-17/frame-index.csv)
are saved locally with timestamps and source frame numbers.

The revised [full-video 6 FPS evidence](visual-references/vfx-orb-2026-09-30-six-fps/)
contains all 42 sheets, a [sample index](visual-references/vfx-orb-2026-09-30-six-fps/six-fps-index.csv),
and an extraction [manifest](visual-references/vfx-orb-2026-09-30-six-fps/six-fps-manifest.json).

## What the tutorial actually builds

| Source time | Observed step | Relevance to Basic charge |
|---|---|---|
| 00:00–00:40 | Shows several glowing orbs; selects a tutorial orb. The cyan example has a bright center and animated jagged boundary. | Good technique/shape reference, but the shown orbs have different ring/shell designs. Do not copy them wholesale. |
| 00:40–01:43 | Unity 2019.3.7 URP/package setup, creates a VFX Graph and exposes the blackboard. | Our project already has Shader Graph/VFX Graph 17.3.0. Adapt the workflow to current APIs; do not change package versions. |
| 01:43–02:09 | Deletes defaults and starts with the template trail system. | Correct first layer for our work: primary streaks before supporting visuals. |
| 02:09–04:49 | Exposes trail spawn rate, HDR color, shared size and trail lifetime; changes spawn to a sphere. | Use named artist controls and a local origin at `MagicSpawnPoint`. Tutorial capacities around 1,000 are demonstration values, not mobile budgets. |
| 04:49–07:26 | Turbulence plus Conform to Sphere shape the trail heads. Strip lifetime is inherited; strip size is randomized and tapered over lifetime. The nodes are grouped as Trails. | Strong basis for irregular curved streaks. Conform to Sphere attracts toward a spherical surface, so it does not by itself create the required pull into the staff crystal. Add a separate age/progress-driven inward term. |
| 07:26–09:57 | Builds a separate periodically spawned quad called Beam, reusing size/color; switches blend mode and adds a fade-in/out alpha curve. | Treat this as the core/glow layer. “Beam” here is a soft billboarded center, not authorization for the eventual attack laser. Author after streak approval. |
| 09:57–12:09 | Adds a third simple particle system, sphere position, additive blend, random small size and a very high exposed spawn rate. | Candidate technique for restrained wisps/motes. The demonstrated density is unsuitable for the A05s. |
| 12:09–15:30 | Adds turbulence and Conform to Sphere to the dense cloud; position/time feed a curl-noise operator, whose values are remapped to constrain motion. Shared size scales systems together. | Useful for coherent secondary motion. The high-density cloud becomes the dominant shell, conflicting with streak dominance unless heavily reduced. Node naming in the ASR is uncertain; verify the current VFX Graph equivalent in Unity 6. |
| 15:30–15:51 | Recolors trails and previews variants. | Cyan/cyan-white supports SPELLSTRIKE's palette. Additional colors are tutorial variations. |
| 15:39–16:30 | Promotion and credits over finished orb variants. | No implementation instructions or authorization come from this portion. |

The overlap at 15:39–15:51 is deliberate: color tuning continues while the video
begins its promotional ending.

## Layer audit

| Layer | Tutorial construction | SPELLSTRIKE decision | Main acceptance question |
|---|---|---|---|
| Primary streaks | Head particles in a sphere; turbulence; Conform to Sphere; strip output; inherited lifetime; tapered width. | **Adopt first**, at a much lower bounded capacity. Add true inward convergence and drive radius/speed/intensity with `Charge01`. | Can the streaks alone read as curved, irregular energy feeding the crystal rather than radial spikes or a circular spinner? |
| Core/glow | Separate periodic one-particle quad (“beam”), additive, shared size/color, alpha-over-life. | Adapt as a continuous cyan-white center controlled by `Charge01`. No attack laser behavior. | Does it support the streaks without hiding them or becoming a large featureless bloom? |
| Wisps/motes | Separate sphere-spawn particle system with turbulence and very high rate. | Use restrained counts and short lifetimes; build only after core approval. | Are they visible as detail without dominating overdraw or cluttering the gameplay camera? |
| ONE ring | Not taught by the constructed tutorial orb; multi-band variants only appear in the opening/ending showcase. | Author independently as one clean secondary ring. | Is there exactly one ring, subordinate to the streaks, with readable growth and fade? |
| Closed shell/cloud | Dense cloud constrained around a sphere and moved with curl noise/remapping. | Do not use as the dominant Basic charge silhouette. A faint wisp influence may be borrowed if budgets allow. | Does the result still read as converging streaks rather than a sealed plasma ball? |
| Release/fade | Tutorial layers loop autonomously; no gameplay release is demonstrated. | Use SPELLSTRIKE's existing release marker and the approved 0.16 clip-second shared fade. | Do all layers hold their release shape, dim together and clean up without affecting projectile timing? |

## Critical weaknesses and traps

1. **The reference is a looping orb tutorial, not a charge animation.** Its
   `Size` parameter scales systems, but the tutorial does not author a unified
   `Charge01` buildup, animation-event release, interruption or retrigger path.
2. **Conform to Sphere is not center convergence.** It tries to keep particles at
   a radius. Used alone, it will produce a shell/spinner rather than energy being
   consumed by the crystal.
3. **The particle budgets are desktop showcase values.** The tutorial discusses
   roughly 1,000 trail/strip capacities, then 5,000 and 50,000 particle spawn-rate
   settings. At approximately 11:08–11:19 the preview visibly saturates into a
   large white disc before being reduced. The narrator explicitly warns that the
   high rate can “melt your GPU.” None of these values are A05s evidence.
4. **The tutorial's final quality depends heavily on bloom and a dark uncluttered
   background.** The 6 FPS review shows several stages becoming a bright soft disc.
   Readability must be retested at staff scale in SPELLSTRIKE's gameplay camera.
5. **The showcased rings and closed cyan shell are different variants.** They are
   not consecutive construction stages and should not be combined as mandatory
   Basic charge layers.
6. **The tutorial targets Unity 2019.3.7.** Current package nodes, defaults,
   capacity behavior and mobile support must be checked in Unity 6 / VFX Graph
   17.3.0. Small Inspector text cannot be treated as verified from a 640×360 source.

## Required layer-by-layer implementation order

1. **Primary curved streaks only.** Hide/disable head dots, core, ring and motes.
   Tune local sphere spawn, low capacities, turbulence, strip taper, orbital
   component and a separate inward component. Review at `Charge01` 0, 0.5 and 0.96.
2. **Core/glow only, then with streaks.** Confirm small/faint to brighter/larger,
   while preserving streak hierarchy.
3. **ONE ring only, then with approved layers.** Confirm ring count and hierarchy.
4. **Wisps**, then **micro motes**, one system at a time, at desktop and mobile
   budgets.
5. **Composition pass.** Check every layer solo, normal speed, slow motion,
   gameplay camera, bright/dark backgrounds, bounds, overdraw and culling.
6. **Release fade.** Drive all output alpha/emission from the shared visibility
   envelope; verify release, half-fade and complete cleanup.
7. **Integration and device validation.** Attach to the real animated
   `MagicSpawnPoint`, then test interruption/retrigger/disable, PC and physical A05s.

Do not proceed to the next visual layer until David accepts the current layer.

## Frame-level observations

| Frames / exact time | Visible change | Design interpretation |
|---|---|---|
| 570–573 / 00:19.000–00:19.100 | A small pink point becomes a bright disc with several thin circular traces in one or two frames. | Fast appearance in this showcase is not a measured charge ramp. Its several rings conflict with David's ONE-ring rule. |
| 574–659 / 00:19.133–00:21.967 | The pink core flickers while circular bands and fine granular detail move around it. | Shows layered core/bands; the Basic charge should keep its curved streaks dominant. |
| 780–824 / 00:26.000–00:27.467 | A separate gold/pink orb shows several broad bands circling a tiny center. | An alternate look, not the final SPELLSTRIKE silhouette. Do not duplicate its ring count. |
| 825–867 / 00:27.500–00:28.900 | The showcase cuts to a compact cyan orb with bright center and animated jagged inner/outer lines. | Supports procedural irregular motion and cyan palette. This is a cut between variants, not evidence of one charge ramp. |
| 868–899 / 00:28.933–00:29.967 | A larger faint closed cyan shell appears around the compact orb, expands/brightens over the next frames, and becomes a dominant irregular boundary with a bright center. | This is the showcased orb's specific shell. David requested ONE clean supporting ring and streak-dominant charge, so use the motion technique selectively. |
| 26,550–26,639 / 14:45.000–14:47.967 | In the Unity preview, a bright cyan-white center persists as short jagged elements and outer fragments change around it. | Demonstrates separate luminous center and moving detail; not a laser or impact. |
| 27,720–27,809 / 15:24.000–15:26.967 | The mostly finished orb appears beneath a color picker. Its fragmented cyan boundary changes while core placement remains stable. | Color is tunable while geometry/motion stays coherent. No source footage proves a pre-release buildup tied to an attack animation. |

![Cyan orb in consecutive frames](visual-references/vfx-orb-2026-09-17/consecutive-019.jpg)

## Handoff to Astra for the Basic charge

The useful VFX Graph techniques are: **sphere-local spawn, trail/strip output,
turbulence, a bounded conforming force, age-driven trail width/opacity, and shared
exposed size/color/lifetime parameters.** These should be authored as visible,
named graph systems in an isolated Basic charge asset.

SPELLSTRIKE's approved design differs in specific ways:

1. The effect belongs under Van Aert's existing `MagicSpawnPoint` and uses the
   3–5-letter BasicAttack pre-release window. Its shared `Charge01` must produce
   **small/faint -> larger/brighter**, continuously.
2. Irregular **curved streaks are dominant**. They orbit and visibly converge
   toward the crystal. The tutorial's Conform to Sphere alone keeps particles on
   an orb; add a separate inward pull/age evolution so this is not a generic spinner.
3. Add subtle wisps, small motes, and a concentrated cyan-white core.
4. Use **ONE clean cyan ring** as a secondary layer. Do not duplicate the
   tutorial's multiple bands or closed jagged shell around the full charge.
5. Do not add a beam/laser, impact burst, release change, camera shake, or other
   attack tiers based on tutorial content.
6. The narrator raises particle rates as high as tens of thousands and jokes
   that this will melt the GPU. Those values are plainly unsuitable as an
   untested Galaxy A05s preset. Begin with bounded counts and controlled
   transparent coverage; measure on the actual device.
7. The tutorial was made with Unity 2019.3.7. Inspect node availability and
   graph/Shader Graph compatibility in the project's installed Unity 6 and
   packages before saving final assets. Do not change package versions.

This audit and its evidence precede Astra's Unity graph authoring. It is not
evidence that a SPELLSTRIKE graph now exists, imports, renders, or passes PC/A05s
validation. The existing C# charge prototype remains unaccepted until those
checks and the requested visual review occur.

## Current implementation comparison (2026-09-30)

`BasicChargeStreaks.vfx` now exists and imports. It has a Head & Trail base,
Local simulation, turbulence, an origin attractor, `Charge01`, corrected bounds,
and `ChargeVisibility` connected to final alpha multipliers. This is architectural
progress, not visual completion. Rendered VFX Graph particles and clean curved
inward streak motion have not been proven. The next graph pass should therefore
work only on Layer 1 and use this revised audit as the acceptance contract.

The current CPU fallback/prototype has separate visual evidence, but its later
compression fields conflict with David's accepted small/faint-to-larger/brighter
direction. Do not use that prototype's current buildup as the target appearance.
