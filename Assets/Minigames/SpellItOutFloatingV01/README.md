# Spell It Out: five V11 tiles and gentle motion, v01

Prepared 2026-11-02. This is an isolated integration deliverable. The shared `Assets/Scenes/MinigameModeSelect_Integration_v01.unity` and `Assets/Editor/ModeSelectIsolatedIntegrationV01.cs` have not been edited by this task. Do not run the broad minigame builder to install this feature.

## Files

All new project files live in `Assets/Minigames/SpellItOutFloatingV01/`:

- `Runtime/ModeSelectSpellItOutTileMotion.cs`: one presentation component, five separately bound slot transforms, independent periods/phases, rest-relative bob/rotation/depth.
- `Editor/ModeSelectSpellItOutInstallerV01.cs`: explicit SIO-only installer and owned QA commands. It has no automatic installation hook.
- `QA/MinigameModeSelect_SIO_SPELL_QA_v01.unity`: separately GUIDed copy of the saved integration scene. Only SIO presentation is changed.
- `QA/SIO_SPELL_Preview512_v01.renderTexture`: owned QA clone so additive Editor scenes cannot write to the same RenderTexture.
- `QA/ModeSelectSpellItOutQAProbe.cs`: Editor-only evidence collector; exclude this QA scene from player builds. The production installer does not add the probe.
- This README and Unity-generated `.meta` files.

## Integrate into the shared scene

1. Check VALORANT and the shared scene's saved/unsaved state. Coordinate ownership of any existing dirty changes. The installer does not save or overwrite that scene.
2. Activate the intended mode-select scene with its existing `V11_SPEL_3D_AssetReuse` gallery, `V11_SP_E_L_DedicatedPreviewCamera`, and `V11_Actual3DTiles_SPEL_RawImage`.
3. Run the visible menu **SPELLSTRIKE > Minigames > SIO SPELL > 2 Apply To Active Scene - Unsaved**. It validates the existing setup, creates only the second L, configures five slot poses/motion, and fixes only the SIO preview's drawing order.
4. Inspect the result and then save the intended target manually under the integration owner's workflow. This task has not run the installer on the shared scene. Undo is available. Reapplying recognizes the owned fifth L and does not add more tiles.

Production only needs the runtime motion component plus the existing camera, RT and RawImage. Do not copy the QA probe or add its scene to Build Settings. Existing navigation, gold-hover reflection, pressed tint, button dimensions and artwork are preserved.

## Open the isolated QA

Open `QA/MinigameModeSelect_SIO_SPELL_QA_v01.unity` additively using the visible Editor. Run **3 Use QA As Play Start**, then enter Play Mode. The start-scene override runs only this saved QA scene while retaining the other open scenes' Edit Mode state. After testing, exit Play and run **4 Restore Previous Play Start**. This restores the previous active/start scene and closes only the clean owned QA scene; dirty scenes are retained. **5 Save Owned QA Only** is available when saving this specific QA scene is necessary.

**1 Create Isolated QA Scene** refuses to overwrite an existing QA asset. It is for first creation only. The installed deliverable already contains the QA scene.

## Layout and tuning

The retained root/gallery/camera names use the original SPEL naming for compatibility; the composition now contains S, P, E, L, L. The original four slots stay in place as objects, with adjusted local poses. The new independent slot is `V11_L2_Slot`.

| Slot | Rarity | Rest position (x,y,z) | Rest Z rotation | Bob amplitude | Period | Phase (cycles) |
|---|---|---|---:|---:|---:|---:|
| S | Bronze | -2.12, 0.10, 0.01 | 10 deg | 0.045 | 3.7 s | 0.05 |
| P | Silver | -1.05, -0.16, 0.04 | -8 deg | 0.038 | 3.1 s | 0.31 |
| E | Bronze | 0.02, 0.17, -0.03 | 5 deg | 0.052 | 3.9 s | 0.58 |
| L | Silver | 1.09, -0.12, 0.06 | -9 deg | 0.041 | 2.9 s | 0.79 |
| L2 | Silver | 2.15, 0.08, 0.00 | 12 deg | 0.047 | 3.4 s | 0.93 |

Local rotation variation is (1.2,1.5,2.4) deg for S/E/L2 and (1.2,1.5,-2.8) deg for P/L. Depth amplitude is 0.012. Motion is continuous, sinusoidal and uses unscaled time. The model holder's existing 0.67 scale and -0.59161 Y centering are retained. No aggressive scaling, orbiting or material changes occur.

The serialized `tiles` array on `ModeSelectSpellItOutTileMotion` is the artist-facing binding/tuning entry point. On disable it restores original local poses. To change bound targets/rest layout in an installer, call `RestoreAndClearBindings()`, set the new layout, then `Configure(bindings)`. `PauseAndRestore()`, `Resume()` and `SampleAt(seconds)` support inspection. Runtime Update does not allocate arrays, materials, meshes or RenderTextures.

## Rendering and the diagnosed layering issue

One existing orthographic preview camera remains, on the existing layer-30 culling channel. Orthographic size is 1.65 for the validated arrangement, with transparent clear. The source RT's 512x256, AA1 settings are retained; visual review at 2400x1080 did not justify increasing resolution. No extra camera, particles, postprocessing or shadow pass was added.

The old builder inserts the RawImage at fixed sibling index 2, but the card retains inactive legacy text children before its artwork. This placed the RawImage behind `SIO_01_Base`, hiding otherwise correctly rendered tiles. The installer uses relative ordering instead: base, glow, 3D RawImage, text, arrow, gold reflection. It moves only the existing SIO RawImage; it never edits the shared builder or deletes the retained children.

Each L is a separate slot/instance while sharing the imported Silver base, L meshes and materials. The source GLBs under `Assets/MenuV2/RodinV11/` are unchanged. There are 15 model renderers and 28,396 triangles across all five tile instances, including their canonical front/back glyphs. This geometry count is not a measured draw-call or FPS result. The QA readbacks/PNG encoding are Editor-only and are not part of production motion cost.

## Verification and limits

Real Windows Unity Editor Play Mode at 2400x1080 showed all five letters, readable title/description, transparent preview surroundings and the corrected stacking. Clicking directly over the tile preview opened `SpellItOutScene`. Runtime samples verify distinct L targets, shared imported resources, independent motion, rest-pose restoration and sampled bounds inside the preview camera.

The project has an unrelated Unity Remote / adb no-device exception. It is reported separately rather than treated as a SIO failure or suppressed through project-settings changes. No physical Android/A05s or standalone player-build validation was performed. No mobile FPS, GC profiler or exact draw-call result is claimed. Held-press feedback was preserved by leaving the existing component/references intact; its full timing curve was not separately measured.

See the external `SpellItOut_Integration_Report.md`, `SpellItOut_QA_Summary.json`, actual Game View PNG and sampled RT motion GIF for final evidence and capture coverage. GIFs are sampled evidence, not native-frame recording or a seamless-loop asset.

## API references

The isolated Play start uses Unity's documented [playModeStartScene](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/scenemanagement/editorscenemanager/playmodestartscene) override. Actual Game View evidence uses [ScreenCapture.CaptureScreenshot](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/screencapture/capturescreenshot), not an offline recreation.
