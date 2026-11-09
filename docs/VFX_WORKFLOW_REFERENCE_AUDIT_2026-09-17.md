# VFX Workflow Reference Audit — 2026-09-17

## Verdict

Adopt the tutorial's **inspectable Shader Graph + VFX Graph authoring workflow**
for SPELLSTRIKE VFX: construct a simple shape, expose its controls, animate it with
curves, build named supporting systems, coordinate shared parameters, and inspect
the composition at normal and reduced playback speeds before gameplay integration.

The transferable lesson is a layered, tunable production process. The video's
lightning silhouette, downward travel, ground flash, sparks, and cleanup are
reference-specific. They do not replace David's current Basic charge design or
authorize its release/impact implementation.

## Source and inspection coverage

- Supplied file: `C:\Users\David\Downloads\videoplayback.mp4`.
- Size: 33,442,237 bytes.
- SHA-256: `7609120863D605A3D86A9D581E0820CAD780856EE12C5353A91679EAC2C4C7D4`.
- Container duration: approximately 16:43.22; 640 x 360 H.264, 30 fps; English AAC.
- Decoded and indexed: **30,096 video frames**, zero-based indices 0–30,095.
- Visually reviewed: **67 overview frames** at exact 15-second intervals,
  covering 00:00–16:30; four native-resolution graph details at 04:00, 05:30,
  07:45, and 12:30; and **300 consecutive frames** in the intervals below.
- Consecutive visual inspection: 00:00.000–00:05.967 (frames 0–179) and
  15:44.000–15:47.967 (frames 28,320–28,439), at the native 30 fps.
- Full spoken tutorial reviewed through a locally generated English ASR transcript;
  the final recognized speech ends at approximately 16:27.96, followed by the
  credits/background portion. The full source was passed to transcription.
- **This is not a claim that all 30,096 frames were individually visually inspected.**
  All were decoded/indexed; consecutive visual review targets the demonstrations,
  while the full spoken workflow and overview cover the longer editing tutorial.

Evidence is saved in [the reference folder](visual-references/vfx-workflow-2026-09-17/).
The [frame index](visual-references/vfx-workflow-2026-09-17/frame-index.csv) retains
source PTS/time, and the manifest records extraction coverage. Contact sheets have
timestamp/frame labels. ASR is machine-generated, not an exact quotation source:
it misrecognizes terms such as Lerp, width, and procedural, and some proper names.
The input resolution limits small Inspector-text certainty; unconfirmed tiny values
are not promoted to required project settings.

## Workflow breakdown across the full tutorial

| Source time | Observed demonstration / narration | Rule to carry into SPELLSTRIKE |
|---|---|---|
| 00:00–01:10 | Finished examples; compares drawn flipbooks with procedural shader generation, followed by setup context and promotion. | Decide how to construct the shape from the effect's needs; inspect the desired result first. Promotional calls to action are not task instructions. |
| 01:10–02:04 | Shader Graph and VFX Graph in Unity 2020.1.10 / URP; creates a shader intended for VFX output. | Verify shader/output compatibility in our installed 17.3.0 packages. Follow the technique, not obsolete menu names. |
| 02:04–02:34 | Starts with a rectangle/line and exposed width/height. | Prove the primary silhouette with the simplest shape before decorating it. |
| 02:34–03:14 | Simple Noise, scale, Time, Multiply, vector speed, and Tiling And Offset create moving UV noise. | Expose spatial scale and motion speed independently. Noise is purposeful shape variation, not a substitute for a designed silhouette. |
| 03:14–03:57 | Direct noise distortion is too strong; Lerp mixes undistorted UVs with distorted coordinates using an exposed amount. | Include a controllable deformation amount and inspect zero, moderate, and extreme settings. This is shader UV distortion, not necessarily screen refraction. |
| 03:57–04:18 | Adds HDR color and connects the shape mask to color/alpha. | Control mask, color, and brightness explicitly; keep silhouette readable before bloom. |
| 04:18–05:51 | Creates the VFX Graph, uses a plane mesh output and assigns the shader; fixes unreadable property/reference names. | Deliver an actual graph with clear artistic controls and a visible preview. Naming is part of usability. |
| 05:51–06:17 | Replaces constant spawn with a one-particle burst, removes unnecessary velocity, makes lifetime deterministic. | Remove template behavior that conflicts with the brief; choose spawn and lifetime deliberately. A one-particle lightning mesh is not a universal particle-count rule. |
| 06:17–07:59 | Uses normalized age/lifetime and Sample Curve for growth; fixes pivot so the bolt extends from its source; adjusts duration and curve timing. | Specify growth direction and origin, then verify both on the moving gameplay attachment. Expose timing instead of hiding it in code. |
| 07:59–08:39 | Animates width so the bolt narrows and disappears; tests and refines curves. | Give visual cleanup an authored curve; use effect-specific evolution. The Basic charge's revised buildup grows instead. |
| 08:39–10:41 | Groups the main lightning, adds a separate flash, exposes colors, switches flash blending to additive, and fixes size-over-lifetime overwriting base size. | Name and isolate layers. Check composition modes and make sure artist controls are not overridden downstream. |
| 10:41–11:05 | Copies flash into a smaller, longer-lived glow and reduces its intensity. | Supporting glow has its own lifetime/intensity; do not make every layer identical. |
| 11:05–13:02 | Adds impact flash at the endpoint, adjusts offset/delay, slows preview for timing, and shares impact parameters. | Inspect timing slowly and coordinate related layers through shared parameters. In gameplay, confirmed hits/events retain authority over when impact happens. |
| 13:02–15:05 | Adds a bounded spark burst, capacity, short randomized lifetimes/velocity, velocity alignment and stretched shape; fixes color-over-lifetime composition. | Secondary particles support the main read. Explicit capacity and functional color/size controls are required. Tutorial counts are not A05s budgets. |
| 15:05–15:35 | Adds impact glow using the same offset and delay, then reviews the composed result. | Reuse meaningful shared controls; review all layers together after solo checks. |
| 15:35–16:43 | Finished variants, repeated color/brightness examples, promotion and credits. | Preserve a common identity while exposing variation. Do not infer mobile performance or copy extra showcased systems that were not constructed in the tutorial. |

## Consecutive-frame observations

The opening title obscures part of the first demonstration. The unobscured repeat
at 00:04.233 is the clearer timing reference:

| Frames / source time | Visible change | Interpretation and limit |
|---|---|---|
| 0–12 / 00:00.000–00:00.400 | Title bars form before the effect montage. | Editorial overlay; not VFX anticipation. |
| 13–35 / 00:00.433–00:01.167 | Top glows, downward bolts, bright ground arrival, spray, then fading body. | Layer order is visible, but central title overlays limit silhouette reading. |
| 36–126 / 00:01.200–00:04.200 | Residual debris/ground marks and title dissolve; reset for a repeat. | Do not turn the title's duration into gameplay timing. |
| 127–129 / 00:04.233–00:04.300 | Large bright top glows establish the source; short bolt begins extending. | Source flash leads the body. |
| 130–132 / 00:04.333–00:04.400 | Irregular narrow bolts extend down toward the ground. | Primary motion reads before the destination flash. |
| 133–137 / 00:04.433–00:04.567 | Destination bloom/flash appears; the large soft discs quickly diminish while smaller base shapes persist. | Arrival and flash are coordinated, with different layer lifetimes. About 0.20 s separates first top glow from visible ground flash in this rendered montage; this is not a universal engine delay. |
| 138–143 / 00:04.600–00:04.767 | Sparks/debris spread while the main bolts and top glints remain. | Secondary motion follows the main read. |
| 144–151 / 00:04.800–00:05.033 | Bolt bodies thin and fade; ground energy and airborne detail remain. | Width/opacity cleanup is distinct from secondary-particle lifetime. |
| 152–179 / 00:05.067–00:05.967 | Sparse motes/debris and darker ground residue linger. | Showcase includes residual layers; not all are built in the narrated baseline. |
| 28,320–28,439 / 15:44.000–15:47.967 | Four variants pulse at differing moments; source/ground glow changes while narrow irregular bodies and sparse detail maintain their identity. | Parameterized variants and independent layer timing are visible. It is not evidence of one synchronized charge ramp. |

![Consecutive frames showing source, travel and arrival](visual-references/vfx-workflow-2026-09-17/consecutive-009.jpg)

![Consecutive frames showing thin-out and residual detail](visual-references/vfx-workflow-2026-09-17/consecutive-010.jpg)

## What becomes mandatory

1. Reference evidence and an explicit visual contract before implementation.
2. Inspectable Shader Graph appearance and VFX Graph motion/lifetime authoring.
3. A simple primary shape first, followed by separately named supporting layers.
4. Stable exposed controls and curves; correct pivot, simulation space, and block composition.
5. Solo/composed previews at normal speed, slow motion, and selected frames/progress values.
6. Reusable prefab/graph assets and a small gameplay presentation binding.
7. Separate graph import, C# compile, PC visual/gameplay, and physical A05s validation claims.

These are now written into `AGENTS.md` and `docs/VFX_PIPELINE.md`. The reusable
`docs/VFX_EFFECT_SPEC_TEMPLATE.md` makes the required evidence concrete.
No automated approval or runtime enforcement system was introduced.

## Application to the current Basic charge

- Latest accepted appearance stays **small/faint -> progressively larger/brighter**.
- Primary layer: irregular curved/converging cyan streaks; supporting wisps/motes,
  cyan-white core and exactly ONE ring around `MagicSpawnPoint`.
- All major layers coexist and share `Charge01`; the earlier final collapse is superseded.
- The currently authored charge uses C# mesh generation for motion plus Shader
  Graph for appearance. **It does not yet meet the graph-motion authoring workflow.**
- A subsequent implementation pass should move that motion into inspectable VFX
  Graph systems (or establish an evidence-backed exception) and produce an actual
  Unity visual review before calling it accepted. This audit does not perform that conversion.
- The current task changes documentation and evidence only. It does not add a laser,
  release gameplay, impact resolver, impact VFX, camera shake, or higher-tier effects.

## Validation limits

The source explains a Unity 2020 tutorial, not a benchmark of our Unity 6/17.3.0
project. No device timings, supported graphics-API result, or A05s performance
claim can be derived from it. The graph-first policy must be applied with current
project compatibility checks and measured mobile budgets.

No Unity runtime/scene/asset implementation was changed during this audit. The
existing Basic charge's pending visual acceptance and device testing remain pending.
