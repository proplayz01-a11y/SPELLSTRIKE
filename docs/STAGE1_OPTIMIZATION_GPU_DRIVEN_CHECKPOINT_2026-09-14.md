# Stage 1 Optimization GPU-Driven Checkpoint — 2026-09-14

## Scope / safety
- Work is isolated to `Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity` and test-only optimization assets/tools.
- Do **not** promote the optimized map or rendering changes to `Assets/Scenes/SampleScene.unity` until physical Galaxy A05s validation passes.
- Before Unity/Blender/build/profiling work, check whether the actual VALORANT game process is running. If it is, stop the dev work/process first and do not continue until VALORANT is no longer running. Do not kill Unity just because VALORANT is running unless explicitly asked.

## Occlusion culling status — PASS
- Baked occlusion is implemented on the optimization test scene.
- Occludees: `672/672`
- Occluders: `398/398`
- Gameplay `OcclusionArea`: about `2347.8 x 726.4 x 3375.0`
- Baked PVS: `23,352,896 bytes` (~22.3 MiB reported by validator)
- Saved occlusion asset: `Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test/OcclusionCullingData.asset`
- Visual traversal checks passed: hidden geometry repopulated correctly after moving out from behind large occluders; no obvious missing houses/walls from tested angles.
- Same-angle ON/OFF tests consistently showed materially lower render submission with baked occlusion enabled, roughly 22–26% fewer batches and ~19–26% fewer draw calls in the tested dense village angle. Editor FPS itself was noisy, so do not claim a fixed FPS gain.

## GPU-driven experiment assets — prepared, not promoted
Created under `Assets/Settings/OptimizationTests/`:
- `Stage1_ForwardPlus_Renderer.asset`
- `Stage1_GPUDriven_RPAsset.asset`
- `Stage1_GPUDrivenOcclusion_RPAsset.asset`

Test utility:
- `Assets/Editor/Stage1GpuDrivenExperimentMenu.cs`
- Menu items 12/13/14 switch the runtime quality pipeline in Play Mode only between:
  1. baseline Mobile Forward
  2. Forward+ + GPU Resident Drawer
  3. Forward+ + GPU Resident Drawer + GPU Occlusion
- A `Temp/Stage1GpuDrivenExperiment.trigger` watcher was added to the same utility for deterministic test-only switching.
- Production `Assets/Settings/Mobile_RPAsset.asset` and `Assets/Settings/Mobile_Renderer.asset` were not intentionally modified by the experiment.

## BRG prerequisite discovery
Unity warned that GPU Resident Drawer requires Graphics > Shader Stripping > Batch Renderer Group Variants = **Keep All**.

Confirmed enum values from Unity 6.3 itself:
- `Keep All = 2`
- `Strip if Entities Graphics Package is not installed = 0`
- `Strip All = 1`

The project original/current production-safe value is `m_BrgStripping: 0`.
During the unfinished benchmark it was temporarily changed to `2`; this checkpoint restores it to `0` before stopping.
A rollback snapshot exists in `Temp/GraphicsSettings_before_gpu_benchmark.asset` but should not be used to overwrite unrelated future changes blindly.

## Runtime grass removal — implemented
- Project decision: stop generating the decorative grass blades at runtime before continuing GPU-driven benchmarking.
- `Assets/Scripts/Map Scripts/GrassSpawner.cs` now has `generateGrassAtRuntime = false` by default.
- Both automatic `Start()` generation and `RespawnGrass()` return immediately while that flag is disabled.
- `Assets/Scenes/SampleScene.unity` and `Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity` explicitly serialize `generateGrassAtRuntime: 0`.
- The grass prefab, material, shader, mask, and static vegetation were **not** deleted. This is reversible without reconstructing assets.
- Runtime verification in the optimized test scene produced no `[GrassSpawner]` or `=== GRASS ===` log entries after entering Play Mode, even though `logSpawnStats` remains enabled. This confirms the generator did not enter its map-bounds/spawn path.

## Historical pre-grass-removal baseline benchmark
Scene: `Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity`
Capture: 8 seconds in Editor Play Mode

- Average FPS: `52.4`
- Average frame time: `19.08 ms`
- P95: `22.14 ms`
- P99: `24.81 ms`
- Triangles avg: `522,245`
- Batches avg: `10,430`
- Draw calls avg: `12,759`
- SetPass avg: `87`
- Shadow casters avg: `30`

Report source at the time: `Temp/Stage1RuntimeProfile.txt`

This historical capture used a different stressful camera view from the newer grassless baseline below. Do **not** attribute the FPS difference between the two captures solely to grass removal.

## New grassless baseline — valid for this exact view
Pipeline: baseline `Assets/Settings/Mobile_RPAsset.asset` (Forward)

- Average FPS: `113.1`
- Average frame time: `8.84 ms`
- P95: `13.07 ms`
- P99: `18.99 ms`
- Triangles avg: `408,334`
- Batches avg: `274`
- Draw calls avg: `274`
- SetPass avg: `65`
- Shadow casters avg: `35`

This is the canonical grassless baseline for the camera position used in this capture. Future Forward+ / Resident Drawer A/B/C captures must use the same stationary view if they are to be compared directly.

## GPU-driven fixed-camera benchmark — completed, not promoted
The earlier pre-`Keep All` Resident Drawer result remains invalid and must not be used. A new controlled pass was completed with:

- runtime grass disabled,
- baked camera occlusion enabled,
- `m_BrgStripping: 2` (`Keep All`) only for the benchmark,
- one Play Mode session,
- the same Main Camera transform for all variants,
- `CombatCamera` temporarily disabled so the benchmark view could not drift,
- a 10-second settle period after pipeline switches before the final 8-second captures.

Locked benchmark camera serialized transform:

- Position: approximately `(1068.989, -76.155945, -671.28)`
- Rotation quaternion: approximately `(0.20216654, 0.1475178, -0.015025046, 0.96806073)`
- Euler hint: approximately `(15.543, -314.412, 0.185)`

Settled fixed-camera results:

| Variant | Avg FPS | Avg ms | P95 ms | P99 ms | Triangles avg | Batches avg | Draw calls avg | SetPass avg |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Baseline Forward | 119.5 | 8.37 | 13.65 | 19.31 | 368,426 | 442 | 442 | 64 |
| Forward+ + Resident Drawer | 121.3 | 8.24 | 12.13 | 23.53 | 368,875 | 438 | 438 | 75 |
| Forward+ + Resident Drawer + GPU Occlusion | 128.8 | 7.77 | 10.19 | 19.94 | 149,175 | 793 | 792 | 107 |

The GPU Occlusion variant visibly rendered the fixed test view correctly and reported a large reduction in visible triangle count, from about `369k` to about `149k`. Unity also warns that triangle/vertex and submission counters may not directly represent indirect draw work, so these counters are evidence of changed culling behavior rather than exact GPU cost.

### Editor timing drift invalidates a promotion decision
A bracketing baseline was captured again after the GPU variants, using the same locked camera and unchanged baseline render stats:

- Average FPS: `167.6`
- Average frame time: `5.97 ms`
- P95: `7.87 ms`
- P99: `9.56 ms`
- Triangles avg: `368,657`
- Batches avg: `443`
- Draw calls avg: `442`
- SetPass avg: `64`

That bracketing baseline was materially faster than the earlier `119.5 FPS / 8.37 ms` baseline despite essentially identical render work and camera state. Therefore Editor timing variance/background load is larger than the apparent GPU-driven timing advantage.

Decision:

- **Do not promote GPU Resident Drawer or GPU Occlusion to production based on the Editor benchmark.**
- Keep the experiment assets as an optional candidate for later physical-device profiling.
- Resident Drawer alone showed no reliable, material advantage in this Editor test.
- GPU Occlusion remains interesting because it materially reduced visible geometry in the fixed view, but the performance benefit must be validated on the Galaxy A05s before adoption.
- Runtime pipeline was restored to baseline Forward after the experiment.
- `ProjectSettings/GraphicsSettings.asset` was restored byte-for-byte to the pre-benchmark snapshot with `m_BrgStripping: 0`.
- The temporary `CombatCamera` disable was restored to enabled after the benchmark.

Saved reports include:

- `Temp/Stage1RuntimeProfile_LOCKED_STABLE_baseline_20260914.txt`
- `Temp/Stage1RuntimeProfile_LOCKED_STABLE_resident_20260914.txt`
- `Temp/Stage1RuntimeProfile_LOCKED_STABLE_occlusion_20260914.txt`
- `Temp/Stage1RuntimeProfile_LOCKED_STABLE_baseline_bracket_20260914.txt`

## Exact next step
Proceed with the already-approved render-distance layer while keeping the production pipeline unchanged:

1. Test Main Camera far clip reduction from `1000` toward about `350` for Android.
2. Check for whole-building/landmark pop at known long LOD distances before accepting the value.
3. Profile the same representative views with baked occlusion and runtime grass still disabled.
4. Then add renderer-only distance culling bands for decorative/visual objects without disabling gameplay logic.
5. Keep GPU-driven rendering as a separate later A05s experiment rather than mixing it into the far-clip test.

## Physical device remains mandatory
No physical Galaxy A05s validation has been completed yet. Before production promotion, validate FPS/frame time, memory pressure, heat/thermal throttling, stutters, load spikes, crashes/OOM, touch, and Node 1 -> Node 4 traversal on-device.

## Shadow optimization pass — tested, no extra caster pruning adopted
Mobile URP was audited after far clip was set to 600 and wall culling was fixed.

Current Mobile_RPAsset shadow settings are already conservative:
- Main light shadows: ON
- Main shadow map: 1024
- Shadow distance: 50
- Cascades: 1
- Additional-light shadows: OFF
- Soft shadows: OFF

Optimized-map audit:
- 684 renderers total
- 672 enabled
- 672/672 enabled renderers initially allowed to cast shadows
- 168 wall renderers (56 wall groups × 3 LOD renderers)

Controlled experiment disabled shadow casting only for tiny/small non-wall visuals while leaving shadow receiving unchanged. 210 renderers were changed by the test utility, but at the fixed stressful camera the render workload did not materially change.

Fixed stress-camera baseline:
- Average FPS: 146.6
- Average frame time: 6.82 ms
- P95: 9.34 ms
- P99: 24.17 ms
- Triangles: 171,415
- Batches / draw calls: 139 / 139
- SetPass: 49
- Shadow casters: 27

Tiny/small shadow casting OFF:
- Average FPS: 152.6
- Average frame time: 6.55 ms
- P95: 9.91 ms
- P99: 12.82 ms
- Triangles: 171,444
- Batches / draw calls: 139 / 139
- SetPass: 49
- Shadow casters: 27

Decision: reject the extra tiny/small shadow-caster policy for now. The fixed-view render counters were unchanged, so the FPS difference is treated as Editor timing noise rather than a proven gain. No persistent Mobile_RPAsset shadow setting was changed. Temporary shadow audit/experiment/stress-view utilities were removed after the test.

If physical Galaxy A05s profiling later shows shadow GPU cost is still significant, the next bounded experiment should be main shadow-map resolution 1024 -> 512 or shadow distance 50 -> ~35–40, one variable at a time.
