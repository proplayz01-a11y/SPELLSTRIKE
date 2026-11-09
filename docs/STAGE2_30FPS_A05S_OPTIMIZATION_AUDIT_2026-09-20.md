# Stage 2 Galaxy A05s 30 FPS Optimization Audit — 2026-09-20

## Goal

- Optimize production `Stage2Scene` toward a practical **stable ~30 FPS** target on the Samsung Galaxy A05s.
- Use physical-device measurements to decide KEEP/REVERT. Editor FPS is not sufficient evidence.
- Current clean production baseline: **19.51 FPS**.

## Locked constraints

- Stage 1 is out of scope for this Stage 2 pass.
- `Assets/Scenes/Stage2Scene/Stage2Scene.unity` must remain at **exactly 409 serialized `MeshCollider` components**.
- The user-managed ground `MeshCollider` must not be removed, replaced, merged, or otherwise altered.
- Preserve the device-validated Stage 2 enemy fixes: upright presentation, chase, attacks, fresh NavMesh data, and Barnacle Water Burst range gating.
- Preserve the fresh Stage 2 NavMesh rebake and its existing asset GUIDs.
- Canonical Android production settings remain IL2CPP + ARM64 + GPU skinning / `GPUBatched` mesh deformation.
- Do not use the earlier Mono/ARMv7 CPU-skinning diagnostic APK for performance conclusions.
- Before disruptive build/editor automation, verify `VALORANT-Win64-Shipping.exe` is not running.

## Stage 2 gameplay checkpoint before optimization

Physical Samsung Galaxy A05s testing confirmed all four Stage 2 enemies are currently working:

- Barnacle Husk: upright, chases, attacks.
- Corsair Phantom: upright, chases, attacks.
- Tide Wraith: upright, chases, attacks.
- Drowned Captain: upright, chases, attacks.

The Android-only visual fix is `Assets/Scripts/Debug/Stage2AndroidEnemyUprightGuard.cs`. The final chase repair also depends on the fresh rebaked Stage 2 NavMesh assets and `Assets/Scripts/Environment/Stage2NavMeshRuntimeBootstrap.cs`. Do not reopen the earlier sideways/chase debugging unless a new regression is reproduced.

Barnacle Husk also keeps the authored Water Burst range gate in `Assets/Stage 2/Barnacle Husk/Scripts/BarnacleHuskController.cs`:

- `waterBurstCastRange = 14.2f`
- Water Burst cannot preempt chase while the player is outside that range.

## Fresh NavMesh rebake

The old committed Stage 2 NavMesh was stale relative to later scene/map changes. Pre-rebake runtime telemetry showed several enemies receiving `PathPartial` and moving only to the edge of reachable islands.

The two Stage 2 `NavMeshSurface` data assets were rebuilt while preserving their existing GUIDs:

- `Assets/Scenes/Stage2Scene/Stage2Scene/NavMesh-Plane.002.asset`
  - GUID retained: `e25c071c9d4f67d47a733bf0cd8dd2a3`
  - rebuilt size: `4,734,476` bytes
  - SHA-256: `9DA986AE225479F7669C7A4483104B9C0B374090EF0DCF9C3EDD03D35C3A64FB`
- `Assets/Scenes/Stage2Scene/Stage2Scene/NavMesh-STAGE2MAP.asset`
  - GUID retained: `de0d7e9b6dae41b49b17a32d2a463ac7`
  - rebuilt size: `10,297,280` bytes
  - SHA-256: `FF16C2EE0E2C3190B8B44E2FBA48B85561A121358E1DDFF00DF73F5EE2BCD9FC`

Rollback backup:

`RecoveryBackups/Stage2NavMesh_before_rebake_20260920_001304`

Post-rebake Stage 2 remained at **409 serialized MeshColliders**.

## Clean production Android baseline

The clean production Stage 2 regression build was generated from the canonical Android settings and installed on the A05s.

- APK: `Builds/Android/SPELLSTRIKE-Stage2Regression-dev.apk`
- timestamp: `2026-09-20 01:18:56`
- APK size: `1,015,859,479` bytes
- Unity BuildReport: `Succeeded`
- BuildReport output size: `3,088,302,310` bytes
- duration: `00:22:19.0298363`
- warnings: `20`
- errors: `0`
- streamed ADB install: `Success`
- runtime graphics API: Vulkan
- GPU: Qualcomm Adreno 610
- runtime target resolution observed: `2400 x 1080` landscape

### SurfaceFlinger baseline

Measured over 118 frame intervals from the Unity `SurfaceView`:

| Metric | Actual present | Frame ready |
| --- | ---: | ---: |
| FPS | **19.51** | **19.52** |
| Average | **51.25 ms** | **51.23 ms** |
| Median | **50.12 ms** | **49.78 ms** |
| P95 | **66.84 ms** | **58.30 ms** |
| P99 | **83.56 ms** | **71.61 ms** |

This is the reference baseline for subsequent Stage 2 optimization passes.

### Thermal and memory baseline

- Thermal status: `0` — no thermal throttling active during the sample.
- TOTAL PSS: `560,393 KB`
- TOTAL RSS: `240,444 KB`
- TOTAL SWAP PSS: `373,769 KB`
- Graphics PSS: `31,020 KB`
- Native Heap PSS: `7,968 KB`
- Unknown PSS: `87,649 KB`

Memory pressure is improved versus older Stage 2 runs, but swap remains heavy.

## Render-cost audit

The Stage 2 render audit reported:

- renderers total: `786`
- active/enabled renderers: `769`
- `MeshRenderer`: `763`
- `SkinnedMeshRenderer`: `6`
- particle renderers: `0`
- active geometry instances: **926,837 triangles / 844,744 vertices**
- unique meshes: `740`
- unique mesh triangles: `920,209`
- unique materials: `85`
- material slots: `867`
- shadow casters: `768`
- shadow receivers: `768`
- static-batching-flagged renderers: `750`
- LODGroups: **0**
- realtime lights: `1`
- active shadow lights: `1`
- reflection probes: `0`
- volumes: `1`
- Stage 2 camera occlusion culling flag: enabled
- baked occlusion data on `Stage2Scene`: **none** (`m_OcclusionCullingData={fileID:0}`)

The editor audit loaded the PC quality context even when invoked with `-buildTarget Android`; that audit artifact is **not** evidence that the Android player uses `PC_RPAsset`. The actual mobile render asset was inspected separately.

## Current mobile URP profile

`Assets/Settings/Mobile_RPAsset.asset` already uses a conservative low-end profile:

- render scale: `0.60`
- HDR: off
- MSAA: `1`
- depth texture: off
- opaque texture: off
- main-light shadows: on
- main shadow map: `1024`
- shadow distance: `50`
- cascades: `1`
- soft shadows: off
- additional-light shadows: off
- SRP Batcher: on
- dynamic batching: off
- GPU Resident Drawer: off
- GPU-resident occlusion: off
- adaptive performance: on

Because render scale is already `0.60`, structural scene/render cost should be attacked before sacrificing more image quality.

## Dominant geometry finding

The single largest renderer is:

`_STAGE2_ROOT/_Environment_PLACEHOLDER/STAGE2MAP/Plane.002`

- triangles: **526,334**
- vertices: **338,039**
- material slots: `1`
- shadow casting: on in the clean baseline
- shadow receiving: on
- approximate world bounds: `1947 x 317 x 1989`

This one renderer represents more than half of the audited Stage 2 triangle workload. Its very large bounds also make frustum/occlusion rejection coarse because the whole renderer remains submitted whenever any part intersects the view.

The next-largest active character meshes are only in the ~14k–36k triangle range. Stage 2 currently has **zero LODGroups**.

### STAGE2MAP source structure

`Assets/Maps/STAGE2MAP.glb` is approximately `43.7 MB` and contains:

- `746` mesh nodes/meshes
- `798,121` source triangles
- `71` materials
- `845` primitive/material slots
- approximately **86.1%** of the audited whole-scene triangle count
- approximately **97.5%** of the audited material-slot count

`Plane.002` is approximately 65.9% of the map's source triangles by itself. Its
material is double-sided. Across the map, **60 of 71 materials are double-sided**,
which can increase fragment/triangle work on mobile. One BLEND material spans 44
primitives / ~35,948 triangles.

Repeated source groups include palms, coral, kelp, docks, and `Node.*` meshes, but
Unity's unique-mesh triangle total is ~99.3% of its instance-triangle total, so the
current imported scene gets very little effective mesh reuse/instancing benefit.

## Water finding

`Stage2_WaterSurface` spans approximately `2447 x 2506` world units and uses the custom `SPELLSTRIKE/Stage2/CoastalWater` shader. Because it can cover a large portion of the screen, transparency/overdraw remains a candidate GPU cost. No water change has been accepted yet.

## Performance pass 1 — static environment shadow-caster removal

New runtime profile:

`Assets/Scripts/Environment/Stage2AndroidPerformanceProfile.cs`

Scope:

- Android player only.
- `Stage2Scene` only.
- Finds `STAGE2MAP`.
- Disables **shadow casting** on non-skinned static-map renderers.
- Disables shadow casting on `Stage2_WaterSurface`.
- Preserves shadow receiving so dynamic character shadows can still land on the environment.
- Does not alter player/enemy `SkinnedMeshRenderer` shadow casting.
- Does not alter colliders, NavMesh, gameplay, PC/editor visuals, or Stage 1.

Reason for this first pass: the clean scene has 768 shadow casters, including the 526k-triangle `Plane.002`. Removing static-environment shadow-map rendering is a lower-risk experiment than immediately splitting meshes, generating LODs, or reducing render scale.

### Build status at documentation checkpoint

The first production build attempt containing this profile did **not** reach a final Unity BuildReport before its headless process stopped. The log ends during the incremental Bee player build and contains no final `Succeeded` or explicit `BuildFailedException` line.

- prior batch Unity PID `15268`: no longer running
- `Logs/Stage2PerfPass1Build.log`: ends mid incremental player build
- output APK remains the clean-baseline artifact from `2026-09-20 01:18:56`
- therefore **no pass-1 FPS or visual conclusion is valid yet**

Do not install/benchmark the stale APK as if it contains pass 1. The next build must produce a fresh APK timestamp before device measurement.

## Next controlled steps

1. Rebuild the Stage 2-only production regression APK with the pass-1 profile and verify a fresh artifact timestamp.
2. Install on the physical Galaxy A05s and confirm the `[Stage2Perf]` runtime log.
3. Repeat the same SurfaceFlinger measurement and compare directly with **19.51 FPS / 51.25 ms average / 66.84 ms P95 / 83.56 ms P99**.
4. Recheck thermal and memory.
5. Verify all four enemies remain upright, chase, and attack; verify water/environment presentation remains acceptable.
6. Reconfirm **409 serialized MeshColliders** after any scene-editing pass.
7. If pass 1 is useful but insufficient, test one structural change at a time:
   - mobile visual LOD/decimated `Plane.002` while preserving the existing collision/NavMesh contract;
   - bake and A/B test occlusion because the camera flag is on but the scene currently has no baked occlusion data;
   - add LOD/distance culling for repeated props such as palms, coral, kelp, and docks;
   - test water overdraw/shader cost;
   - consider mobile shadow receiving and Bloom A/B tests only after the larger structural costs;
   - keep GPU Resident Drawer off unless an actual device A/B proves a gain.

## Current conclusion

Stage 2 gameplay is device-validated and stable enough to optimize. The clean A05s production baseline is **19.51 FPS**, and the main audited render problem is a large static environment with ~927k active triangle instances, 768 baseline shadow casters, no LODGroups, no baked occlusion data, widespread double-sided map materials, and a single 526k-triangle world-spanning renderer. Pass 1 is authored but remains **unvalidated** until a fresh production APK is successfully built and measured.
