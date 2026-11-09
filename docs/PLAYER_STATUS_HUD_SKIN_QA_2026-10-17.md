# SPELLSTRIKE Player Status HUD — 2026-10-17

## Editable authoring workflow (latest)

The original Scene View overlay was a temporary hidden `DontSave` preview and
could **not** be selected or edited. This is resolved with a deliberate,
non-destructive conversion action:

1. Open `SampleScene` in **Edit Mode** (do not enter Play).
2. Use `SPELLSTRIKE > HUD > Make Player Status Editable`.
3. The command removes the temporary preview, makes a normal **editable**
   `PersistentHUDRoot > Canvas > HealthBar > SpellstrikePlayerStatusSkin`
   hierarchy, selects it, and disables only the three existing legacy
   HealthBar image components. The conversion is undoable with Ctrl+Z.
4. Expand the new root and adjust each child RectTransform and Image in the
   Inspector: `VanAert_StorybookPortrait`, `HP_FixedGoldFrame`,
   `HP_LiveFill`, `EXP_FixedGoldFrame`, `EXP_LiveFill`,
   `HP_ActualValue` and their navy tracks. Use the Rect Tool (T).
5. Move the parent to reposition the complete HUD; move children to tune
   frame alignment; change the Image sprite/color or font styling normally.
   **Preserve the three runtime binding names**
   `HP_LiveFill`, `EXP_LiveFill`, `HP_ActualValue`, because runtime looks
   them up to update HP and later XP.
6. The command **does not automatically save** `SampleScene`. The user
   decides whether to save after positioning it. The existing unsaved user
   scene work is preserved until then.

Gameplay `PlayerStatusHudSkin.Initialize()` first looks for this authored
hierarchy and reuses it **without resetting any RectTransforms** or spawning
duplicate art. Live HP still fills the existing red Image and updates the
existing numeric Text; XP remains a subdued placeholder until a true XP
controller is wired. If an editable hierarchy is incomplete, the runtime
reports a warning without overwriting the user's layout. The automatic,
non-editable preview stops drawing when the real authored hierarchy exists.

**Verification:** both runtime and Unity Editor shadow assemblies compiled
with 0 errors, 47 existing warnings. The physical Editor menu invocation
and 20:9/16:9 Play Mode visual checks have not been run. No .unity scene
or prefab was modified or saved by this code-only implementation.

Status: **integrated in project code, five player-named art assets imported, C# compilation passed. Actual Unity Play Mode and physical Android QA remain pending.**

## User-approved direction

Top-left storybook-style Van Aert portrait in a gold hexagon, ornate red HP bar
above a narrower blue EXP bar, with navy/gold frames. The original HP design
references and the five renamed source PNGs were found in
`C:\Users\David\Downloads`.

## Assets imported

All files in `Assets/Resources/SpellstrikePlayerHUD/`, with Sprite `.meta`
files; original Downloads files are untouched.

| Resource sprite | Origin in Downloads | Pixel dimensions |
|---|---|---|
| VanAert_Portrait.png | Golden Hex Wizard Avatar.png | 512 x 506 |
| PlayerHP_Frame.png | WIZARD HEALTH FRAME.png | 1024 x 184 |
| PlayerHP_Fill.png | WIZARD HEALTH BAR.png | 1024 x 121 |
| PlayerEXP_Frame.png | WIZARD EXP FRAME.png | 1024 x 140 |
| PlayerEXP_Fill.png | WIZARD EXP BAR.png | 1024 x 72 |

Each file was tightly cropped to its alpha content and downsampled for UI use.
The original source hashes/bounds and output dimensions are captured in
`Library/SpellstrikePlayerHudAssets.json`.

## Runtime code and ownership

- **New** `Assets/Scripts/PlayerPersistence/PlayerStatusHudSkin.cs`: a
  presentation-only MonoBehaviour on the existing `HealthBar` Slider.
- **Targeted wiring** in
  `Assets/Scripts/PlayerPersistence/PersistentHUDRoot.cs`:
  `CacheReferences` ensures the skin exists on the current player-health
  Slider; it does not create another Slider, health system or Canvas.
- Five independent images make the approved layered layout, plus a live
  HP-number `Text`. Decorative images do not capture pointer raycasts.
- The existing legacy Slider's `Graphic` renderers are disabled only after
  successful Sprite loading, while the actual Slider and its binding remain
  intact; their enabled state is restored if the skin is destroyed.
- Red HP fill uses the **real existing Slider value and maxValue**, and
  matches healing/damage/restore on the following LateUpdate. Text is live
  (e.g. 100 / 100 or 50 / 100), not baked in.
- A future `SetExperienceFraction(float)` hook exists, **but there is no
  gameplay XP progression owner today**. The EXP track displays only muted
  translucent blue artwork so that the visual frame is present without a
  misleading XP progress value. No fictitious XP/level is saved or awarded.

## Layout and verification

### Explicit top-left placement from user Scene View reference

The user supplied a Scene View image showing the old heart/red player HUD at
the upper-left, with the boss bar at the upper-right, and requested the new
Van Aert layered status HUD appear **in that same player slot**.

Audited `SampleScene.unity` serialized hierarchy: `PersistentHUDRoot ->
Canvas -> HealthBar (Slider) -> Image / Border / Fill`. The existing
`TutorialResponsiveLayout` anchors that very Slider to the top-left safe
area at `(44,-18)`. The new skin remains **a child of that exact Slider**,
now offset `(-27,-12)` relative to its top-left pivot. This places its
effective top-left at approximately `(17,-30)` of the safe-area legacy
logical Canvas, keeping the portrait's pointed hat from clipping the screen
edge. The boss bar, joystick and tile HUD anchoring are unchanged.

**Editor distinction, resolved in a subsequent pass:** The screenshot was
Unity **Scene View in Edit Mode**, and the previous implementation only
constructed this art at Play. Added
`Assets/Editor/PlayerStatusHudScenePreview.cs` to create the **same exact
five-layer composition in the Scene View** while inspecting
`SampleScene` outside Play. It shares
`PlayerStatusHudSkin.CreateVisualLayers()` rather than duplicating art
geometry. The preview root and all descendants are flagged
`HideInHierarchy | DontSaveInEditor | DontSaveInBuild`, constructed detached,
and cleared on scene closing, scene saving, assembly reload, and before
entering Play. The scene's original heart/border/fill graphics are visually
suppressed via transient `CanvasRenderer.SetAlpha(0)`, **not** by editing
their serialized `Graphic.enabled` properties. Alpha state is restored
when clearing the preview.

This means the design is visible in Edit Mode after Unity imports/reloads the
new Editor script, while the runtime `PersistentHUDRoot` still owns the real
HUD in Play. The manual refresh command is
`SPELLSTRIKE/HUD/Refresh Player Status Scene Preview`.

The editor was **minimized** during this implementation, in compliance
with the user's no-foreground request, and did not log a fresh asset import
after the new Editor script was written. Consequently, this Editor preview
has passed a background `Assembly-CSharp` + `Assembly-CSharp-Editor`
source build with **0 errors**, but direct in-Editor visual verification
must still occur after Unity processes the changed source files.
Original scene hashes remained unchanged; no production scene/prefab was
serialized by this implementation.

Scoped shadow C# build after the placement change passed with **0 errors**.
Live Editor Play positioning and 2400x1080/16:9 visuals remain unverified,
notwithstanding the static hierarchy and logical-coordinate checks.

- Independent full-size frames sit above their dynamic fills; portrait covers
  the left-hand joins, matching the supplied full composite reference.
- Kept the authoritative mobile/player HUD position and safe-area anchoring
  controlled by `TutorialResponsiveLayout` unchanged.
- Offline compositing visually inspected at 100% and 50% HP:
  `Library/PlayerHUDQA_20261017/PlayerHud_HP_100.png` and
  `PlayerHud_HP_050.png`. These previews are **not Unity Game View screenshots**.
- Shadow Unity-generated Assembly-CSharp C# build, explicitly including the
  new source file: **0 errors, 22 existing unrelated warnings**. Temporary
  shadow project was deleted afterwards.
- `SampleScene.unity`, `StageSelectScene.unity`, and
  `StageSelectBook_ReferenceFix.unity` original hash baselines remain intact.
- No battle logic, potions, enemy/boss HUD, TilePool, WordBar, tutorial layouts,
  XP database progression, or serialized scenes/prefabs were edited.
- Unity Editor was deliberately not foregrounded. This was a filesystem/C#
  integration, not a claimed verified in-Editor presentation.

## Required real QA before user sign-off

1. In **SampleScene** Play at 2400x1080 landscape, confirm imported sprites
   render instead of the old HP bar; inspect top-left visibility and safe area.
2. Press H / take normal damage: text and red fill should shrink together
   and the portrait/frame should remain static. Heal and death/retry should
   restore the correct amounts.
3. At a 16:9 Game View, verify portrait and frames remain readable and do not
   overlap other panels or potion bag. Check both PC and Android layout branches.
4. Check stage transitions to Stage 2/3 and the Stage Select/death overlay:
   no duplicated player HUD and no UI on menu scenes.
5. EXP remains a visual unfilled placeholder until actual runtime XP data is
   defined. Do not display the decorative blue track as earned XP.
6. Test on a physical Galaxy A05s. No device performance or touch claims were
   made in this implementation pass.
