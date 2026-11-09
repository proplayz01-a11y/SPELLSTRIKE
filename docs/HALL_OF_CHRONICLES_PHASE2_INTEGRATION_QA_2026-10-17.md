# Hall of Chronicles Phase 2 — Isolated Unity Integration QA

Date: 2026-10-17 (Asia/Manila)
Scene: `Assets/Scenes/StageSelectBook_HallOfChronicles_Preview.unity`
Status: **layered static composition and ambient implementation saved; Editor Play visual checks passed. Physical Android not tested.**

## Source verification

- ZIP: `C:\Users\David\Documents\Codex\2026-10-15\cou\outputs\Spellstrike_Hall_of_Chronicles_Animated_Asset_Pack_v1.zip`
- Verified source ZIP SHA-256: `7A24B358A94F3118DD05542BF0DF13469119A9E533D9792BFCF8D4126B45D61F`.
- Freshly extracted under `C:\Users\David\Documents\Codex\2026-10-15\cou\work\COS_HallOfChronicles_Staging_20261017\`.
- Separately inspected `Spellstrike_Hall_of_Chronicles_FINALIZED`; its root/nested copies were byte-matched against the ZIP's 53 packaged files by read-only audit.
- Read `README.md`, `asset_manifest.json`, `Unity_Assembly_Guide.md`, `Animation_Recommendations.md`, `QUALITY_NOTES.md`, and QA documentation before assembly. Manifest remains the alignment authority.

## New assets and runtime wiring

- Imported all **42 runtime PNGs only** to `Assets/Sprites/HallOfChroniclesV1/`: base1, warm/blue glows20, flames19, particle textures2. Offline preview images are not runtime assets.
- Imported as Single UI Sprites, Full Rect, 100 PPU, sRGB on, Bilinear/Clamp, no mipmaps or read/write. Base maximum 4096, full alpha for effects, uncompressed reference imports (including Android override RGBA32) before any ASTC experiment.
- Placed one `HallOfChroniclesBackgroundCanvas` with `sortingOrder=-50`, `CanvasScaler` reference 2400x1080/Match Height, **without** GraphicRaycaster, under the unchanged main Stage Select book canvas.
- `LibraryBackground` is 2400x1080 with `HallOfChroniclesCoverFit`, keeping all sprites in a **single cover-scaled group**. The environment crops at narrower aspect ratios; it does not stretch.
- 40 active ordinary uGUI Images: base order0; 20 glows order10–29; 19 flames order30–48. All decorative `raycastTarget=false`. Two particle textures are imported but emitters were intentionally not created.
- Exact crop positions follow `(x + w*pivot.x - 1200, 540 - y - h*(1-pivot.y))`; the resulting centered placement matches the source `(x+w/2-1200,540-y-h/2)`. Near flame pivots are located at the original wick position so scale does not detach them.
- `HallOfChroniclesAmbient` serialized 39 bindings, independent phases and periods, alpha from the manifest limits, near-flame scale 0.98–1.02, diffuse flames alpha-only, glows static transforms and small alpha modulation. Optional particles off. Updates stop when the guarded book UI is hidden; defaults restore on disable.
- The isolated scene instance's obsolete `StageSelectReferenceRoot/PreviewBackdrop` is disabled, preventing a second flat image from covering the library. Its source book prefab is not modified or overrides applied.

## Editor verification completed

| Check | Evidence/status |
| --- | --- |
| ZIP hash and asset inventory | PASS. Exact ZIP hash and 42 PNGs; no preview PNG imported. |
| Compilation | PASS after correction to use `TextureImporterSettings.spriteMeshType`. Final Unity Tundra build succeeded. |
| Static composition | PASS visual at 2400x1080. Library, depth blur hierarchy, small localized glows, table/books/globe and sharp foreground book all visible without apparent major seams. |
| Runtime animation enabled | PASS source serialization: `animate: 1`, 39 Image bindings, Unity menu confirmation. Play Mode rendered without a background-script exception. Sub-pixel flicker amplitude and exact min/max pose were not instrumented. |
| Min/max pose commands | Added isolated Unity menu options `5 - Preview Minimum Ambient Pose` and `6 - Preview Maximum Ambient Pose` plus `4 - Restore Manifest Static Pose`. The source compiled, but command execution/visual inspection remained unverified because the COS Unity MCP plugin server entered an error state after the earlier successful Play tests. These are QA helpers, not required for runtime animation. |
| 20:9 | PASS actual Unity Game View at 2400x1080, static and Play screenshots. |
| 16:9 | PASS actual Unity Game View at 1920x1080 in Play. Sides cropped, book stayed proportionate; no independent overlay stretching. |
| Input/clickability | PASS: Stage 2 icon selected in actual Play, lower Stage Information Panel updated to `Stage 2: Sunken Seas`; Stage5 remained LOCKED. Enter button intentionally not activated; no stage completion or PlayerPrefs mutation testing. |
| Console | One error visible unrelated to Hall: Unity Remote requirements/ADB forward failed. Two MCP bridge port warnings. No Hall runtime exception or fresh C# compiler error found in inspected Editor log tail. |
| Protected source files | PASS, hashes below retained after preview implementation/Play. |

Screenshots stored on user's local machine in:
`Library/HallOfChroniclesReview/Unity_Static_20x9_GameView.png`,
`Unity_Play_20x9_GameView.png`,
`Unity_Play_16x9_GameView.png`.
Copies can be placed under the Codex output directory for longer-term retention.

## Protected SHA-256 baselines retained

- `Assets/Scenes/SampleScene.unity`: `7AC9BC6C17456DA80A9F05FF69D99E396E0745854DA599B8BB093BD178CCDCC3`
- `Assets/Scenes/StageSelectScene.unity`: `EB75B478CAEDFFD104ACBA214CE2A83E206198A4601BDA40B9D826C8B0BA4521`
- `Assets/Scenes/StageSelectBook_ReferenceFix.unity`: `2413C9B6551F93D4CB22CDFE74D38531F458B70691598333F18EC235D4A8FD4E`

The source book prefab is pre-existing, untracked in Git and was **not written by the Hall builder**. Do not delete or discard it because of Git status.

## Remaining limitations

- Exact minimum/maximum flicker pose, flame-by-flame brightness deltas and wick edges were not instrumented numerically in Editor; crop/pivot math and observed default/Play visuals provide a conservative baseline.
- When Unity MCP becomes available again, open the *already saved isolated preview in Edit Mode* and run the two new min/max pose menu items, inspect the candle bases at both extremes, then use `4 - Restore Manifest Static Pose` before leaving. This must not change production or the source book prefab.
- Device ASTC 6x6/4x4 appearance, physical Galaxy A05s GPU time, overdraw, draw-call count, memory and thermal/performance costs remain **unverified**. Current RGBA32 import is a deliberately high-fidelity comparison state, not final Android compression optimization.
- A separate existing Stage Select book route/review wiring issue may remain. The visual work does not change `StageSelectManager`, the Enter listener, review gate or progression; their complete end-to-end routing was not tested to avoid state mutations.
- User approval is still required before any production promotion. The isolated preview is not part of the enabled Build Settings scene list.

## 2026-10-17 ambient visibility adjustment (v2)

- User feedback: original candles/glows felt static at normal Game View scale.
- Modified only `Assets/Scripts/HallOfChronicles/HallOfChroniclesAmbient.cs`, no new Unity scene/prefab art binding and no production scene edit.
- More visible, independently phased flame flutter: combines slow wave, faster sub-flicker and low-frequency Perlin variation. Near candle flames now fade ~0.49–1.00, stretch ~0.84–1.18 vertically, and sway about ±4.5 degrees around their existing wick-centered pivots. Far diffuse flames continue alpha-only, with warm and blue illumination pulsing locally rather than moving the room.
- Adds runtime-only `NearFlameFlickerAccent` Image children on the near flames (same sprite, alpha-only normal blending, non-raycast). These are never authored into or applied to the original book prefab.
- Verified Unity Tundra C# compile success, isolated Play renders; the source StageSelectScene SHA-256 `EB75B478CAEDFFD104ACBA214CE2A83E206198A4601BDA40B9D826C8B0BA4521` and reference scene SHA-256 `2413C9B6551F93D4CB22CDFE74D38531F458B70691598333F18EC235D4A8FD4E` remain unchanged. Inspector retained `Animate=true` and 39 bindings.
- In Edit Mode, executed the existing minimum and maximum pose QA menus; Unity logged both. Restored the exact saved normal manifest pose afterward. The saved isolated scene hash remained `B6A60CA84AB5D7BC8BE4409F364705FD8C22FB3E89554EE2AB0FE59174FF49D0`.
- Existing Unity Remote/ADB command invocation error is unrelated to the new art script. Device frame-rate/performance still not measured.

## 2026-10-17 floating atmospheric dust (preview only)

- The supplied `particle_dust_soft.png` is a 64x64 sprite whose bright center occupies only about 6x6 pixels. Using it as a small UI Image makes the dust almost invisible in downscaled Game View.
- Added `Assets/Scripts/HallOfChronicles/HallOfChroniclesFloatingDust.cs`; the existing `HallOfChroniclesAmbient.Awake()` adds/configures this component automatically on the **isolated preview's** `LibraryBackground` at runtime, with no saved scene/prefab or navigation binding modifications.
- The gold dust sprite is byte-identical to the imported original, re-imported as a Unity Resource at `Assets/Resources/HallOfChronicles/particle_dust_soft.png` with a **new** Unity GUID, so runtime loading works in both Editor and player builds. Original pack assets stay unchanged.
- A fixed pool of 36 soft golden non-raycast UI Images (76–137 source-pixel quads) drifts upward at 13–29 source pixels per second with asynchronous lateral sway, softly breathing brightness and edge fade. The visible sprite core is enlarged sufficiently to be noticed while staying dust-like, not confetti.
- Particles stay in left/right atmospheric lanes that remain partly exposed at 16:9 and 20:9, under the existing sharp Stage Select book. They are shown only when the book is visible and the ambient animation flag is enabled; no particles spawn per frame and no physics, collision, lights or expensive full-screen overlays are added.
- **QA limitation:** The user was watching a VALORANT championship and explicitly requested that Unity **not be foregrounded**, including during compilation. All changes were made using COS filesystem tools while Unity stayed minimized. Existing preview/production scene hashes were unchanged. Unity MCP plugin was unavailable on this pass, and no reliable script-compilation or Game View test has yet been observed after the dust addition. Do not claim live visual/device QA passed until a background compile and Play inspection are available.
