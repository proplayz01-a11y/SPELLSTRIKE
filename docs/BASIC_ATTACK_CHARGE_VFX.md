# Basic Attack Charge VFX

The additional orb tutorial `videoplayback (3).mp4` was audited before further
graph authoring. See [the timestamped orb audit](VFX_ORB_REFERENCE_AUDIT_2026-09-17.md)
for applicable VFX Graph trail/force techniques and the specific visual elements
that must not be copied into the Basic charge.

## Current visual direction

David explicitly revised the direction on 2026-09-17: begin small and faint,
then continuously become larger and brighter before release. This replaces the
original contraction/compression requirement. Curved, irregular cyan streaks,
subtle wisps, micro motes, cyan-white core, and ONE clean cyan ring coexist.
Individual streaks still curve inward while the overall envelope grows.

On 2026-09-30 David approved a **short intensity fade after release** instead of
an immediate hide. Default duration is 0.16 clip-time seconds (0.16 seconds at
normal animation speed). Charge progress stays at 1 while visibility eases from
1 to 0; the effect stays attached to the staff, then is hidden. This extends
presentation only; projectile/release/hit/damage authority remains unchanged.

**Resolved on 2026-11-07:** the CPU renderer now follows the saved growth direction.
`startSize = 0.18` grows continuously toward full size using `growthPower = 1.35`;
the stale `finalCompression`/`compressionStart` collapse was removed. Energy also
increases with charge progress, so the envelope becomes larger and brighter before
the existing release marker.

## Implementation and tuning

- `Assets/Scripts/VFX/BasicAttackChargeVfx.cs`: one bounded mesh with all layers.
- `Assets/Scripts/VFX/BasicAttackChargeBinding.cs`: reads BasicAttack animation
  progress and its existing release marker; attaches the reusable effect under
  the canonical player's `MagicSpawnPoint`.
- `Assets/VFX/BasicAttackCharge/BasicAttackCharge.prefab`: reusable charge with
  appearance, movement, and desktop/mobile budgets exposed in the Inspector.
- `BasicChargeEnergy.shadergraph` in that folder: URP unlit additive appearance;
  UV/vertex-color inputs and a custom mask function. The currently integrated
  charge motion is still authored in C#.
- `Assets/VFX/BasicAttackCharge/BasicChargeStreaks.vfx`: isolated Unity-authored
  VFX Graph checkpoint. Its exposed `Charge01` drives sphere spawn radius through
  a 0.08-to-0.30 remap; a separate 0.25-to-1.0 remap multiplies strip alpha.
  Heads and strips are Local, with turbulence and center attraction. The
  2026-09-30 streak-only pass has actual rendered evidence in
  [the Layer 1 record](BASIC_CHARGE_LAYER1_STREAKS.md). It is **not** yet the
  canonical charge effect.
- `BasicChargeStreakAppearance.shadergraph`: newly authored native URP Unlit
  appearance for the isolated streak layer, with analytic soft edges, particle
  color/alpha preservation, and Charge01-driven HDR intensity. Its assignment
  to the strip output and actual HDR/bloom rendering remain pending; see the
  Layer 1 record for current verification status.
- `Assets/VFX/BasicAttackCharge/BasicChargeGraphPreview.prefab`: isolated Visual
  Effect preview surface for the graph, not player or scene integration.
- `Assets/Scripts/VFX/BasicAttackChargeTiming.cs` shares the fade timeline between
  runtime and preview. `postReleaseFadeSeconds` is exposed on the canonical player
  binding and serialized as 0.16. Set it to 0 for immediate hiding at release.
- `Charge01` stays at 1 during the tail. `ChargeVisibility` is the graph's shared
  output multiplier; the CPU prototype multiplies every layer's additive intensity
  by the same envelope. It holds the release geometry throughout the short fade.
- The release remains at 0.8333333 seconds, frame 25 at 30 fps, in the currently
  referenced BasicAttack clip. The visual binding starts its fade at that marker.
  It stops graph source emission once at release and hides the instance after
  the fade; interruption/disable still clears immediately. A fresh cast resets
  graph playback/visibility. Enabling the binding after release does not create
  a fresh charge tail. The fade follows animator pause/speed.

No laser, impact, camera shake, damage, projectile, targeting, vocabulary,
Target Challenge, or higher-tier attack behavior is implemented by this effect.
No production scene edit is required by this correction.

## Preview and acceptance

Open **SPELLSTRIKE > VFX > Basic Attack Charge Preview**. After a code reload,
close and reopen the preview so it creates a fresh copy of the prefab.

1. Inspect the current pre-release shape and resolve the growth/compression
   discrepancy above before treating the full charge as visually accepted.
2. On the preview timeline, 1 is release. Compare 1, approximately 1.096
   (half fade), and 1.193 (past fade completion). Shape/progress must hold at
   release while all layers dim together and disappear.
3. Enable Loop preview and Show Van Aert; inspect staff/crystal attachment.
4. Toggle Mobile budget. Check the same identity with fewer ribbons/motes.
5. Run checks, then verify actual 3-, 4-, and 5-letter attacks in Play Mode,
   interruption/disable cleanup, and absence of charge on higher tiers.
6. Validate actual visibility/overdraw/frame time on the Samsung Galaxy A05s.

The fade duration is authored on the binding's **Post-release presentation**
Inspector section. Reopen the preview after changing the prefab value. Its
timeline samples animation and does not fire projectile events.

Editor preview is sampled animation, not gameplay or physical Android proof.
PC visual acceptance, actual gameplay regression, and A05s validation are pending.

### Graph checkpoint verification (2026-09-17)

**Historical checkpoint, superseded for rendering/appearance by the 2026-09-30
Layer 1 work.** The new **SPELLSTRIKE > VFX > Layer 1 - Streak Graph Preview**
renders the actual graph without gizmos. It captures Charge01 0/0.5/0.96 at
equal age and fixed framing, and supports normal/quarter-speed review. See
[the current layer record](BASIC_CHARGE_LAYER1_STREAKS.md); full effect and
gameplay/device acceptance remain pending.

Unity imported the `.vfx` and preview prefab; the Visual Effect Inspector showed
the graph and `Charge01` slider. The previously reported tiny colored mark in
Prefab Mode may have been the VisualEffect gizmo; actual particles have not yet
been confirmed with gizmos disabled. Readable irregular trails, inward motion, and
0/0.5/0.96 growth were **not** visually verified. The graph retains template
bounds centered at `(0,2,0)` with size `(4.5,7,4.5)` and template appearance.
It lacks the cyan streak styling, supporting layers, one ring, and pre-release
gameplay integration. The older C# preview above continues to preview the
existing prototype, not this new graph.

## Verification for the growth correction

The renderer, binding, and preview C# files compiled together successfully with
Unity 6000.3.10f1's bundled compiler and Unity/.NET Standard reference assemblies.
Only expected serialized-field assignment warnings were reported. This targeted
check is not a full project build or Shader Graph import/compilation check.

## Post-release fade verification (2026-09-30)

`Tools/Test-BasicChargeFade.ps1` passed 1,647 timing assertions across 15/30/60/120
fps samples, including release continuity, monotonic dimming, endpoint, paused
sample time, fresh cast samples, zero duration, and invalid inputs. The helper,
runtime binding/renderer and existing preview compiled against Unity 6000.3.10f1
reference assemblies. These checks do not establish actual Animator interruption,
graph rendering, or physical-device performance; live preview results are recorded
separately. CPU preview Run checks now checks all vertex colors dim by half at the
fade midpoint in both quality modes, then hide at completion.

**Fresh live Editor result:** Run checks passed on 2026-09-30 at 00:53:38 +08:00:
imported shader, release marker, sampled staff attachment, finite geometry,
all-layer fade and disabled-render guard. Desktop preview uses 780 vertices and
mobile-budget preview 406. The initial Edit Mode test incorrectly expected the
runtime OnDisable callback; the harness now explicitly tests the disabled preview
render guard and restores preview state after failures. Runtime lifecycle remains
an actual Play Mode test, not a claim from this Editor check.

Fresh rendered captures (00:55:28–29 +08:00) visibly show full release intensity,
reduced intensity halfway through the fade, and a blank frame at completion:

| Preview | Release | Half fade | Fade complete |
|---|---|---|---|
| Desktop | [PNG](visual-references/basic-charge-fade-2026-09-30/pc-1.00.png) | [PNG](visual-references/basic-charge-fade-2026-09-30/pc-fade-half.png) | [PNG](visual-references/basic-charge-fade-2026-09-30/pc-fade-complete.png) |
| Mobile budget in Editor | [PNG](visual-references/basic-charge-fade-2026-09-30/mobile-1.00.png) | [PNG](visual-references/basic-charge-fade-2026-09-30/mobile-fade-half.png) | [PNG](visual-references/basic-charge-fade-2026-09-30/mobile-fade-complete.png) |

[Live validation result](visual-references/basic-charge-fade-2026-09-30/unity-validation.txt)
and [timing test result](visual-references/basic-charge-fade-2026-09-30/timing-result.txt)
are saved with the images. Export framing is small/off-center; these captures prove
the fade behavior, not polished composition or full-charge visual acceptance.

**VFX Graph:** Astra authored `ChargeVisibility` (default 1, range 0–1) and connected
it to final Multiply Alpha blocks in both Heads Quad and Strip Quad outputs.
Unity imported the saved graph, and static inspection confirmed both connections.
The graph's existing particles therefore have output alpha multiplied every render;
an actual graph 1/0.5/0 render comparison remains unverified. The canonical player
still uses the CPU prototype; its graph reference is unassigned.

## BasicAttack Animation Event integration (2026-11-07)

- `Assets/VAN AERT/ANIMS/BasicAttack.anim` now contains `OnAttackChargeStart` at
  frame 0 (0.0s). The existing `OnAttackRelease` remains at frame 25
  (0.8333333s), and `OnAttackAnimationFinished` remains at frame 68 (2.25s).
- The canonical `VanAertPlayer.prefab` already contained a
  `BasicAttackChargeBinding`; its references are configured to the player Animator,
  existing `MagicSpawnPoint`, `BasicAttackCharge.prefab`, and the canonical
  BasicAttack clip. Its `OnAttackChargeStart()` event method opens the visual
  window. `LateUpdate` then samples Animator progress, so the animation remains
  the timing authority and no gameplay call is added to the charge event.
- The charge remains attached to `MagicSpawnPoint` through the frame-25 release,
  then uses the pre-existing 0.16 clip-time fade. It is hidden on interruption or
  when the player leaves the BasicAttack state. Higher attack tiers do not receive
  a charge-start event from this change.
- A CPU geometry check in a temporary Preview Scene passed at progress 0 / 0.5 / 1:
  780 vertices were generated at each sample and bounds grew from about 0.11 to
  0.30 to 0.67 units. A 50% visibility sample remained enabled and a 0% sample
  hid the renderer. Unity script validation passed with zero errors.
- Live gameplay Animator-event dispatch and visual appearance in the authored
  gameplay scene remain pending. The current scene was already dirty, so it was
  deliberately not entered into Play Mode during this pass. Android validation
  also remains pending.
