# Arcane Surge — Strong Attack (10–13 letters)

> **2026-11-09 change of direction:** The user imported the free **Vefects Zap VFX – URP** package and explicitly stopped Blender/custom VFX Graph development. The implementation below is the earlier prototype history, **not the current active renderer**. The Strong-only adapter now uses `ArcaneSurgeZapVisual` and a Resource ScriptableObject catalog referencing the vendor particle prefabs. See `docs/ARCANE_SURGE_ZAP_INTEGRATION.md` for the current implementation, tests and limitations.

Status (2026-11-09): **Strong-only C# integrated, full assembly compile passed; temporary visual renderer authored. NOT production VFX-Graph-complete, gameplay-reviewed or Android-accepted.**

## Accepted visual and gameplay contract

- Cast uses the existing StrongAttack animation and **existing OnAttackRelease** event (1.267s in the currently referenced StrongAttack clip).
- The lightning comes **from directly above the live enemy**, rather than traveling from the staff. Source = target head/valid target point + 11 world units; final point = live head.
- The visible downward descent has a hard **0.20-second clip-independent runtime duration** starting at projectile release. Each Update evaluates an endpoint attached to the *current* moving target, so no stale world-space impact lock.
- Strong-only palette: bright white/cyan core and violet halo/branches.
- Impact: high-contrast contact, three small forks, short lingering bright bolt, expanding ground ring, eight sparks, a 0.5-second synthesized electric thunder/crack/rumble, and a small 0.12s camera render impulse.
- Logic: original `AttackController -> HomingProjectile.Launch -> ResolveAcceptedImpact` owns damage, enemy receivers, barrier, hit notifications and cleanup **once**. VFX lifetime NEVER grants damage. Exactly one explicit accepted-contact attempt is made at the descent endpoint; premature trigger/collision callbacks are gated during Strong flight.
- **Target Challenge is deliberately exempt from the new overhead path** until that mode has its own end-to-end acceptance. Its validated STRIKE/dictionary/valid-but-incorrect/penalty behavior and existing projectile path are unchanged.
- No modifications to Basic/Medium VFX, Main Menu V11, production scenes, Stage 2 collider baseline, health/dictionary manager or animation clips.

## Tutorial evidence (COS Watch, original job recovered)

Source: [Unity VFX - Lightning Strike Effect Tutorial](https://www.youtube.com/watch?v=XPh0jiqf0iQ), 19:02. Existing COS Watch job `job_9c50f0d0841a` **succeeded** with video index `6f14e08b90c16058`; no duplicate analysis requested or started.

What was directly confirmed in timestamped Watch transcript and retrieved frames:

- **01:27–01:31** — creator explicitly describes the effect's core as a **zigzag mesh, lightning texture, and simple shader**.
- **05:05–05:14** — source lightning texture is assigned; Unity Graph shows an **Output Particle Mesh** context.
- **09:24–09:30** — mesh orientation/culling discussed; the Output Particle Mesh Inspector has culling disabled for two-sided display.
- **10:31–10:38** — **blur output ordered before bright output** in the VFX Graph output render order.
- **14:38–14:47** — output **LIGHTNING BRIGHT**, Shader Graph appearance (`DissolveShader_tut` name visible in indexed image), **Set Size**, a sampled color gradient, and alpha/clip controls are shown. Creator copies gradient to the blur output.

COS Watch's broad answer confidence was low (~0.20–0.25). **The exact internal source connections for mesh generation, detailed branching, and actual descent/collision gameplay were not recoverable with high confidence from this index. Do not attribute our homing logic, exactly 0.20-second movement, thunder or camera impulse to the tutorial.** Those are specifically SPELLSTRIKE design additions.

## Implemented files and components

- `Assets/Scripts/AttackController.cs`: Strong-only activation immediately after the existing `Launch` call.
- `Assets/Scripts/HomingProjectile.cs`: separate vertical Strong presentation switch; all accepted damage, collision receiver and vocabulary logic remains in `ResolveAcceptedImpact`.
- `Assets/Scripts/VFX/ArcaneSurgePresentation.cs`: target collider and current head selection, live tracking, timed descent and explicit confirmed-contact bridge.
- `Assets/Scripts/VFX/ArcaneSurgeVisual.cs`: **explicit interim implementation**, standalone render hierarchy; bright + blur lines, forks, shockwave, sparks, single cached synthesized thunder clip and URP camera render impulse.
- `Assets/Resources/Spellstrike/VFX/ArcaneSurgeEnergy.mat`: local material references existing `SPELLSTRIKE/VFX/BasicChargeEnergyMobileSafe` URP additive shader. Shader/Basic/Medium source assets remain unchanged.
- All new assets/scripts have dedicated Unity `.meta` GUID files.

The new strong visual currently relies on **LineRenderer procedural positions, not a newly authored `.vfx` graph**. This is a transparent **exception to `docs/VFX_PIPELINE.md`**, chosen because the connected Unity MCP bridge reported **`No Unity Editor instances found`** despite the local Unity process being present. Do not promote the visual to a final, approved effect based only on this fallback. No new shader code, mesh generator, scene change or speculative VFX Graph YAML was introduced. The graph-first target remains outstanding.

### Next required graph-first art pass

In Unity 6000.3.10f1 / VFX Graph 17.3.0:

1. Author/import a real lightning zigzag mesh with proper UVs, plus crisp and blurred lightning textures in an isolated `Assets/VFX/ArcaneSurge/` folder, with consistent local head-end pivot.
2. Author an inspectable Shader Graph for soft cyan/violet emissive lightning with visible transparency/mask, exposed brightness/clip/width; two-sided in URP as needed.
3. Author a named `Spawn -> Initialize -> Update -> Output Particle Mesh` VFX Graph with independent **Blur** and **Bright** outputs, matching confirmed tutorial layering. Add separately controlled branching and impact VFX Graph systems (burst, shockwave, sparks) with bounded capacity.
4. Expose `StrikeStart`, `StrikeEnd`, `Descent01`, `Impact01`, `Brightness`, `MobileQuality` and named color controls. Animate the strike toward **the moving live enemy head** through the existing tiny timing/target adapter, not a self-contained VFX particle timer that owns damage.
5. Build a reusable graph prefab and isolated graph preview. Verify graph import, output render order, and bright/dark gameplay camera rendering. Then replace/retire this interim LineRenderer visual only after a working graph has been rendered and accepted; no silent renderer switch.

## Validation evidence and limits

- Prior baseline before editing: `backend-system`, HEAD `a6e9d2b`, **0 tracked changes**, **401 currently displayed untracked Git-status entries**; previous estimate of 771 untracked files may count their contents. Protected these paths without cleaning or bulk staging.
- VALORANT game process absent before Windows/Unity automation attempts; only Riot Client services observed.
- Source assembly compilation: Unity-generated `Assembly-CSharp.csproj` with a **temporary, isolated** `Temp/ArcaneSurgeCompile.targets` include for the two new scripts (Unity had not regenerated its project yet). Result **0 C# errors**, 34 pre-existing compiler warnings. This is a C# type/assembly check, not Unity asset import/Shader Graph/Play Mode evidence.
- `git diff --check` reported no whitespace errors on modified tracked scripts.
- No Unity Play Mode combat regression or VFX preview was run. MCP-for-Unity editor bridge said no instances; a read-only desktop inspection showed the Editor minimized in `SampleScene`. It was not saved, altered, or entered into Play Mode.
- COS Watch capture of **actual new gameplay**, moving-target contact accuracy, runtime cleanup, high-contrast screenshot evidence and physical Samsung A05s/Adreno 610 performance **remain pending**.
- Provisional CPU renderer budget (configured, not measured): two major lines, three forks, one 33-point ring, eight two-point sparks; no real-time lights, world-screen refraction or persistent particles. Each strike creates and destroys render objects. Draw calls/overdraw/mobile FPS have **not** been profiled.

## Manual test flow when Unity Editor/MCP is operational

1. Let Unity import the new scripts and material; inspect Console for errors. Use **sample/isolated gameplay** and preserve any dirty production scene.
2. STRIKE a normal enemy with valid 9-, **10-**, **13-**, **14-letter words**: only 10–13 triggers the vertical Arcane Surge; the other bands retain existing attacks.
3. Observe the source above the enemy; test target strafing/turning mid-descent and compare the contact tip at **0.00, 0.05, 0.10, 0.15, 0.20 seconds**. Check no accidental old staff orb remains visible.
4. Verify a single receiver damage call and existing hit notification on contact, once only; no early trigger callback hit. Test target deactivation during descent, enemy death on impact, repeat casts, interruption, and scene unload without orphaned visuals.
5. Verify active vocabulary barriers retain `ResolveHit` damage behavior; separately test existing Target Challenge exactly as accepted. Do not change dictionary/STRIKE activation or valid-wrong penalty.
6. Inspect core/forks, brief impact flash, ring at feet, sparks, thunder, and camera impulse. Confirm camera location is restored after each URP render and Cinemachine remains stable.
7. Capture live PC gameplay with COS Watch/visual QA. Compare with tutorial, then author/accept the actual VFX Graph layers.
8. Build/test on Galaxy A05s: graphics API, visible VFX, GPU overdraw, memory, repeated-concurrent casts and frame-time; optimize secondary layers before reducing lightning readability.
