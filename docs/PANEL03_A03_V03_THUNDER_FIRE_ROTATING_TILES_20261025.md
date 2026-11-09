# SPELLSTRIKE Panel 03 A03 — Thunder, Rotating Word Tiles, Battlefield Fire and Embers

**Date:** October 25, 2026. **Status:** Unity 6.3 Editor authored, visually reviewed and Play Mode tested; short prototype preview exported. **Not final VFX Graph-compliant production particles, native 30 FPS Recorder export, or Samsung Galaxy A05s device validation.**

## User intent / scene to open

User requests: inherit the three subtle thunder illumination pulses from approved **Panel 02 v05**; bring **A–G Word Magic tiles** nearer to Younger Van's staff while leaving some spatial gap; make those tiles rotate *flat in their own plane* like a slow fan, without three-axis tumbling; spread fire across the war zone; add warm drifting atmospheric fire embers. Keep the static UltimateAttack release pose, the canonical staff, low rear angle, ruined city, oversized faceless shadow, and original 0.32m camera push.

- **v03 scene:** `Assets/Scenes/Cinematics/Prologue_P03_A03_UltimateRelease_v03_ThunderFireTiles.unity`
- **Own native v03 Timeline:** `Assets/Cinematics/Storyboard3D/Timelines/Panel03_A03_v03/A03_UltimateRelease_ThunderFireTiles_v03.playable`
- **Video review:** `ArtSource/Cinematics/Panel03_v03/Reviews/P03_A03_ThunderFireSpinningTiles_7p6s_UnityPreview.mp4`
- **True Game View at thunder peak:** `ArtSource/Cinematics/Panel03_v03/Reviews/P03_A03_v03_ThunderFlash_GameView_2p05s.png`
- **True Game View quiet-versus-flash:** `ArtSource/Cinematics/Panel03_v03/Reviews/P03_v03_Quiet_vs_ThunderFlash_Comparison.png`
- **Decoded MP4 contact sheet:** `ArtSource/Cinematics/Panel03_v03/Reviews/P03_A03_ThunderFireSpinningTiles_ContactSheet.png`
- **Storyboard target versus shot:** `ArtSource/Cinematics/Panel03_v03/Reviews/P03_ApprovedStoryboard_vs_ThunderFireTiles_v03.png`

The original Panel 03 v02 scene and all Panels 01/02 scenes remain unchanged.

## Layer 1: inherited Panel 02 thunder

Copied the approved visual timings **2.05, 5.12, 7.20 seconds** from Panel 02's native `A02_v05_ThunderLight_3SoftPulses.anim` and `A02_v05_StormSkyFlash_3SoftPulses.anim`. These remain *shared read-only animation assets*; the v03 scene has its **own storm sky material copy** `P03_A03_StormSky_Independent_v03.mat` and a dedicated scene-only, shadowless violet-blue Directional Light. Peak intensity is 0.78 / 0.62 / 0.34. Storm returns to its dark baseline between pulses. No thunder rumble audio or moving geometric lightning bolt was added.

## Layer 2: close A–G tiles with slow flat spin

- Rebuilt the imported **seven paired tile/letter meshes** as 7 scene-local animated pivots within `P03_A03__WORDMAGIC_FLAT_SPIN_ROOT`; original imported word-tiles remain present but disabled to avoid duplicates.
- First tile pivot sits approximately **3.22m from the staff renderer's bounds center**, with the other six forming an upward/outward spell arc toward the faceless corruption rather than starting at the enemy's face. The furthest pivot is about 8.92m from the staff.
- Authored one native `A03_SlowFlatSpinningWordTiles_v03.anim` AnimationClip with independently keyframed **Z-axis local quaternion rotation only** for each pivot, alternating clockwise/counterclockwise at **7.3–9.1 degrees per second**. Both tile and letter spin together coherently; there is **no X tilt or Y yaw animation**.
- Each letter was moved slightly forward off the tile face and enlarged to remove the old coplanar/edge-on appearance. New **scene-only readable amber unlit material** `P03_ArcaneLetters_ClearAmber_v03.mat` keeps all A–G glyphs readable over cyan-blue boards.
- User-requested rotating *visual pose* is not a rotating hitbox or gameplay mechanic.

## Layer 3: distributed animated flames and lighting

- Added **18 scene-only fire locations** around the warzone: 10 near visible ground/road edges, and 8 on higher/deeper ruined structures.
- Each fire uses **three named independently flickering flames** (54 total **35-vertex** tapered meshes): gold core, orange middle flame and crimson edge. A copied, valid imported URP **Shader Graph** `P03_FlameTongues_UnlitTransparent.shadergraph` provides feathered transparent appearance, with three separate adjustable materials `P03_FIRE_GOLD_CORE.mat`, `P03_FIRE_ORANGE_BODY.mat`, `P03_FIRE_CRIMSON_EDGE.mat`.
- Motion is driven by **native Unity Timeline AnimationClip** `A03_WarZone_18Fires_Flicker_v03.anim` with 162 local-scale curves (width/height oscillations, differing per fire) for the static scene-only flame mesh silhouettes. They do not represent evolving/despawning gameplay fire, so no damage/hit/spawn gameplay logic is added.
- **Six local orange point lights** with no shadows add modest warm bounce near burning ruins, preserving readability.
- The first camera pass had too many fire locations hidden behind the buildings; ten floor-level fires were explicitly moved to the **visible street edges** and flame material brightness adjusted so they read as warm fire rather than large white flares.

## Layer 4: airborne drifting embers

- Added two scene-only **CPU ParticleSystem** ember layers beneath `P03_A03__ATMOSPHERIC_EMBERS`:
  - Midfield atmosphere: up to **220** particles, 30 emitted/second, maximum lifetime 4.1s.
  - Foreground embers: up to **70** particles, 9 emitted/second, maximum lifetime 3.1s.
- Camera proof shows tiny warm orange/red glowing motes floating with low turbulence and upward drift. A custom feathered radial texture `P03_EmberRadialSoft.png` and `P03_Embers_URP_ParticlesUnlit.mat` supplies the particle appearance.
- Runtime diagnostic at one representative sample: 101 and 21 living particles, respectively; **290 maximum concurrently configured**, 39/second combined emission. No particle lights, collisions, damage, collision callbacks or scenewide physics queries.
- An initial Unity Console warning about mixed velocity curve modes was traced to the two ember systems and fixed in v03: the X/Y/Z velocity-over-lifetime channels are now **all Constant-mode** (Y upward 0.55). A fresh Play Mode inspection confirmed both emitters alive, with approximately 100 and 22 particles and matching XYZ curve modes. Earlier pre-fix warning text may remain in Editor Console history.

**VFX workflow exception:** The team's `docs/VFX_PIPELINE.md` requires graph-first **Visual Effect Graph** for production timed particles. The present scene's **ember particles use the built-in ParticleSystem instead of an authored .vfx graph** as a defense-only quick prototype, deliberately reusing Unity's stable Billboard renderer. This is a **known unresolved production-compliance item**; replace these two ember systems with an actual named VFX Graph Spawn/Initialize/Update/Output effect and review asset controls and mobile parity **before calling this VFX production complete**. The three flame shapes themselves use Shader Graph with deterministic Timeline geometry flicker. No unsupported performance or Android compatibility claim is made.

## Unity Play Mode evidence

- **5 serialized native Timeline tracks:** camera slow push, thunder-light pulses, sky brightness flashes, seven flat spinning tiles, fire flicker.
- **7.6 seconds** total with **0.320 m** camera push, correct initial camera at roughly `(-2.60,1.55,13.60)`. An inherited copied Timeline `removeStartOffset` bug initially reset the camera to origin during Play Mode; **v03 only** was corrected by setting the copied camera clip's `AnimationPlayableAsset.removeStartOffset=false`. The 0.320 m full-camera motion was reverified after the correction.
- Actual tile A rotation at **0 / 2.05 / 3.8 / 5.12 / 7.2 / 7.6s:** 0 / 14.97 / 27.74 / 37.38 / 52.56 / 55.48 degrees. **X=0 and Y=0 throughout**.
- Thunder directional intensity at those timestamps: 0 / 0.78 / 0 / 0.62 / 0.34 / 0; all pulses act on the sky and environment. Quiet-versus-flash Game View central-region mean RGB increases **63.84 to 84.70** (screenshot measurement, not calibrated photometry).
- Van's original `UltimateAttack` clip was baked from the exact 1.000s `OnAttackRelease` event in the earlier v02 scene. The character's original Animator and skinned body renderer remain disabled, and the scene renders the approved **10,069-vertex** static Younger Van mesh. No pose or staff transform edits were performed in this v03 VFX pass.
- Fire root has **18 sites / 54 individual rendered flame meshes**, and two ember systems were actually running during Play Mode. The camera frame shows visible warm fires down the ruined street and orange embers at several depths, while the A–G words remain readable.
- **64 genuine 2400×1080 Unity Game View capture frames** encoded into a decoded/verified **7.73-second, silent 1920×1080** MP4 at **~8.28 screenshot-capture FPS**. The full 20:9 framing is preserved using letterboxing (not cropped). This number is **not the measured gameplay frame rate**.
- This remains a **PC Editor visual review only**, no physical Android/Galaxy A05s benchmark or real-time production profiling. Shader Graph compilation was confirmed by Unity; some ambient Unity Remote / COS connection diagnostic errors in the wider console remain unrelated to this scene, so avoid claiming the whole Console was clean.

## Explicit future polish, not yet done

The enormous faceless corruption and its violet columns remain simplified placeholder geometry. Final movie art may need better shadow-tendril sculpt, fire/smoke layering, a VFX Graph replacement for the prototype embers, independent UI-free high-framerate capture, war-zone audio, and a physical device graphics benchmark. Keep user-approved storyboard and the established one canonical staff / frozen UltimateAttack pose.
