# Stage 1 Galaxy A05s 30 FPS Optimization Audit — 2026-09-15

## Locked constraints
- Camera far clip: **600**. User explicitly requires this to remain unchanged.
- Render scale: **0.60** unless a later bounded experiment is explicitly accepted.
- User-managed ground collider: **do not touch**.
- Mobile controls: preserve validated Tutorial joystick + right-side camera touch behavior.
- Goal: **30 FPS consistent**; user accepts **27–28 FPS consistent** if 30 cannot be reached without unacceptable regression.
- Physical Galaxy A05s measurements decide KEEP/REVERT, not Editor FPS.

## Baseline before this push
- Prior validated SurfaceFlinger result: **24.148 FPS**, 289 frames, approximately 146 × 33 ms and 143 × 50 ms frames.

## Move 1 — establish current-device state before another optimization
- Safety gate: PASS; `VALORANT-Win64-Shipping` not running.
- Device: Galaxy A05s `R7AWA09VE1D`, Unity app focused.
- Scene serialized far clip verified: **600**.
- Scene serialized culling bands verified still at prior values: tiny 70 / small 120 / medium 180 / structure 300 / landmark 450.
- LOD setup report: 22 standard + 3 extra + 56 wall = **81 LODGroups**; distance culler present.
- 12-second SurfaceFlinger sample on currently installed build: **28.568 FPS**, avg **35.004 ms**, 126 measured intervals: **114 × 33 ms, 12 × 50 ms**.
- Interpretation: this sample already lands inside the user-accepted 27–28+ range and is close to 30, but it must be repeated under controlled gameplay/traversal before calling it consistent. No new visual sacrifice is justified yet.
- Decision: **HOLD / VALIDATE**, do not stack another optimization until repeatability is established.

### Validation A — Node 1 stationary, 20 seconds
- User positioned the player at Node 1 and kept the game stationary for the capture.
- SurfaceFlinger: **21.316 FPS**, avg **46.913 ms**, 125 measured intervals.
- P95: **50.138 ms**; P99: **50.154 ms**.
- Frame classes: **24 × ~33 ms**, **101 × ~50 ms**, no intervals >=59 ms.
- This does **not** reproduce the earlier 28.568 FPS sample. Node 1 stationary is currently dominated by the ~50 ms presentation bucket.
- Decision: the earlier 28.568 result is **not yet accepted as consistent**. Keep the build unchanged and continue controlled location/workload testing before making another optimization change.

### Validation B — Node 1 movement + camera, 20 seconds
- User ran/moved with the joystick while rotating the camera from the right-side touch region.
- SurfaceFlinger: **20.841 FPS**, avg **47.982 ms**, 125 measured intervals.
- P95: **50.163 ms**; P99: **66.826 ms**.
- Frame classes: **22 × ~33 ms**, **97 × ~50 ms**, **6 × >=59 ms**.
- Compared with stationary Node 1 (21.316 FPS), movement + camera costs only about **0.475 FPS** in this capture. The dominant problem remains the Node 1 rendering/view workload rather than a large extra cost from joystick/camera input.
- Decision: **VALIDATED BOTTLENECK AREA / NO SETTING CHANGE YET**. Next optimization work should target visible environment/render workload and preserve the already-validated controls.

## Move 2 — audit culling configuration before changing the build
- Source audit found a configuration mismatch: `Stage1MobileLodCullingSetupOnce.cs` is authored to apply the proposed tighter bands **55 / 95 / 145 / 240 / 400**, but the saved test scene still serializes **70 / 120 / 180 / 300 / 450**.
- The runtime culler is renderer-only (`Renderer.forceRenderingOff`). Stone-wall LOD renderers and terrain/ground/road/path/floor/landscape/plane/water/wall matches are excluded; it does not disable GameObjects or touch colliders/gameplay logic.
- Runtime A05s profile source verified: render scale **0.60**, LOD bias **0.80**, HDR off, environment cast/receive shadows off, occlusion on, and far clip locked at **600**.
- Attempted to invoke the setup trigger to serialize the tighter bands. Unity reported setup completion, but post-run scene inspection still showed **70 / 120 / 180 / 300 / 450**. Therefore the proposed tighter culling is **not considered applied** and no APK/device result will be attributed to it.
- Decision: **DO NOT CLAIM / INVESTIGATE SETUP MISMATCH** before building. This preserves the one-variable audit trail and avoids benchmarking a change that did not actually land.
### Move 2 physical A05s validation after direct test-scene serialization
- The dedicated test scene was subsequently verified with the tighter bands **55 / 95 / 145 / 240 / 400**, far clip **600**, **81 LODGroups**, and no collider diff.
- The in-Unity A05s benchmark logger replaced the no-longer-reliable SurfaceFlinger latency endpoint for subsequent controlled measurements.
- Stationary automatic windows measured **22.484 FPS** and **23.912 FPS** versus the earlier controlled Node 1 stationary baseline of **21.316 FPS**.
- Movement + right-side camera automatic windows were approximately **22.396–23.033 FPS**, with a nearby window at **23.713 FPS**, versus the earlier controlled movement + camera baseline of **20.841 FPS**.
- P95 remained approximately **50.12 ms**, so the build improved throughput but did not remove the dominant ~50 ms frame-time bucket.
- Decision: **KEEP** tighter culling. Measurable physical-device gain, controls remained usable, and locked far clip/render-scale/collider constraints were preserved.

## Move 3 - Main-light shadow map 1024 -> 512

- Before: validated tighter distance culling **55 / 95 / 145 / 240 / 400**, far clip **600**, render scale **0.60**; Android Mobile RP main-light shadow map **1024**, shadow distance **50**, one cascade, additional-light shadows off, soft shadows off.
- Intended change: use test-only `Assets/Settings/OptimizationTests/Stage1_A05s_Shadow512_RPAsset.asset` with only `m_MainLightShadowmapResolution` changed from 1024 to 512.
- Build-system finding: Unity was executing a stale `Assembly-CSharp-Editor.dll`; the source-side temporary pipeline override did not execute and those attempts did **not** produce a valid 512 APK. No performance conclusion is attributed to those failed attempts.
- Guarded fallback: recorded production `Mobile_RPAsset.asset` SHA256 **3E1AD0B0D6F5B7F8D4A8897148D9D3005A907FF75903EDBA98E5E12DD1513B11**, temporarily changed only `m_MainLightShadowmapResolution: 1024 -> 512` for the build input, then restored the project asset to **1024** after the build.
- Physical A05s stationary: first captured window **21.217 FPS**, avg **47.132 ms**, P95 **50.128 ms**, P99 **66.822 ms**, approximate 1% low **14.965 FPS**. A later non-movement window reached **23.433 FPS**.
- Physical A05s movement + right-side camera: **20.151 FPS**, avg **49.624 ms**, P95/P99 approximately **66.824/66.829 ms**, approximate 1% low **14.964 FPS**.
- Comparison: the validated 1024-shadow tighter-culling build had approximately **22.4-23.0 FPS** during movement, with a nearby **23.713 FPS** window. The 512 test did not improve physical-device performance.
- Visual/gameplay impact: no user-reported visual/gameplay benefit; lower shadow resolution would also reduce shadow sharpness.
- Decision: **REVERT / REJECT**. Keep main-light shadow resolution **1024**.

## Combined Move 4-6 - 35 m shadows + GPU Resident Drawer + GPU occlusion

- Reason for combining: user explicitly preferred one larger build/test cycle rather than three more isolated Android builds. This sacrifices per-variable attribution in exchange for finishing device validation faster.
- Candidate changes: shadow distance **50 -> 35 m**; main-light shadow resolution stayed **1024**; GPU Resident Drawer **ON**; GPU Resident Drawer camera occlusion **ON**; Forward+ experiment renderer used; BRG stripping temporarily set to **Keep All (2)** so the GPU-driven path could be built.
- Locked settings preserved: far clip **600**, render scale **0.60**, culling **55 / 95 / 145 / 240 / 400**, **81 LODGroups**; controls, gameplay, and user-managed collider were not intentionally changed.
- Build integrity issue: an initial build reported success while the output APK timestamp remained stale. That APK was **not** accepted or benchmarked as the combined candidate.
- Fresh-build safeguard: stale final APK and stale Gradle release APK were deleted before retriggering. The verified fresh output was created **2026-09-15 19:28:26 +08:00**, size **216,156,596 bytes**, SHA256 **6DB2225408C1DD10ECEAD54320711F02923AD4D2E2C8D35128C464FC1CF6A869**. Unity reported **Succeeded**, duration **47.099 s**, warnings **2**, errors **0**.
- Install: exact fresh APK installed successfully on Galaxy A05s `R7AWA09VE1D`; Unity activity focus was confirmed.
- Physical A05s stationary: window 1 **21.784 FPS**, avg **45.906 ms**, P95 **50.135 ms**, P99 **66.833 ms**, 1% low **14.963 FPS**. Window 2 **22.764 FPS**, avg **43.929 ms**, P95 **50.127 ms**, P99 **66.825 ms**, 1% low **14.964 FPS**.
- Physical A05s movement + right-side camera: **20.717 FPS**, avg **48.269 ms**, P95 **66.825 ms**, P99 **66.829 ms**, 1% low **14.964 FPS**.
- Result: combined GPU-driven + shorter-shadow candidate did not beat the simpler known-best tighter-culling configuration and worsened the heavy movement window.
- Visual/gameplay impact: no user-reported gameplay/visual improvement sufficient to justify the regression.
- Decision: **REVERT / REJECT combined Move 4-6**.

## Final known-best Stage 1 Android configuration

- Distance culling: **55 / 95 / 145 / 240 / 400** - **KEEP**.
- Far clip: **600** - locked.
- Render scale: **0.60** - keep; no further image-quality sacrifice required for this validation pass.
- LOD bias: **0.80**; **81 LODGroups**.
- Main-light shadow map: **1024**; shadow distance **50 m**; one cascade; additional-light shadows off; soft shadows off.
- GPU Resident Drawer: **OFF** for the accepted configuration.
- GPU Resident Drawer camera occlusion: **OFF** for the accepted configuration.
- BRG stripping: restored to production baseline **0**.
- Runtime environment shadow casting/receiving remains disabled by the validated A05s runtime profile; HDR off; occlusion culling on.
- Mobile controls remain the validated Tutorial joystick + right-side camera ownership behavior; user-managed collider remains untouched.
- Known-best same-logger physical-device performance after tighter culling: stationary **22.484-23.912 FPS**; movement + right-side camera approximately **22.396-23.033 FPS**, with a nearby **23.713 FPS** window. P95 remained approximately **50.12 ms**.
- User acceptance was later simplified: if the practical maximum is only around **25-27 FPS**, stop chasing 30 and accept the device limitation. Current heavier Node 1 measurements remain below that range, so the audit records the A05s performance limitation rather than claiming a sustained 25-30 FPS result.
- Optimization conclusion: further tested shadow/GPU-driven changes did not improve the A05s. **Stop the iterative Stage 1 performance-build loop and proceed to final Android regression/stability validation using the known-best configuration.**
