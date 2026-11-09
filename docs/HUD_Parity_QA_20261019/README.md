# Android combat HUD: Edit Mode / Play Mode parity

**Date:** 2026-10-19
**Production scene:** `Assets/Scenes/SampleScene.unity`
**Editor-only menu:** `SPELLSTRIKE > HUD > Sync Android Edit Preview`

## Root cause and correction

The 2400x1080 Game View originally showed an oversized Mage HUD, WordBar,
TilePool and Attack button in Edit Mode because the persistent combat/health
Canvases used `CanvasScaler.ScaleWithScreenSize` with `Match Width` (`0`).
The runtime responsive system instead switches to `Match Height` (`1`). With
the 800x600 reference Canvas, that is 3.0x vs 1.8x on 2400x1080. Simply
serializing the mobile runtime RectTransforms into the scene without matching
CanvasScaler values caused the previous huge-UI regression.

The fix reuses `TutorialResponsiveLayout.ApplyLegacyCombatLayout` and
`ApplyMobileGridLayout` as **one source of truth** through an explicit
Edit Mode authoring API; `AndroidCombatHudEditPreviewSync.cs` exposes the
menu action. It updates the existing CanvasScalers, WordBar, TilePool,
Attack, player HealthBar and (inactive) EnemyHealthBar so their transforms
correspond to the Android runtime. No duplicate runtime manager, new UI
prefab, replacement HealthBar Slider or alternate HP/EXP binding was made.

The enemy bar is now serialized inactive in `SampleScene`, consistent with
its no-enemy Play Mode state. Its original Slider is preserved; the existing
`PersistentHUDRoot.ShowEnemyHealth` can activate it during combat.

## Editing this HUD again

1. Open `SampleScene` with Android as the active Build Target. Exit Play.
2. Save other changes first; the sync command refuses a dirty scene so that
   it does not trample unrelated authored work.
3. Click **SPELLSTRIKE > HUD > Sync Android Edit Preview**. The menu runs
   the runtime mobile geometry method on existing objects, including
   `Match Height` CanvasScaler settings. It leaves the HUD's approved gold
   art and tile logic in place.
4. Check Game View using `2400x1080 A05s` or `1920x1080 Landscape`, then
   save `SampleScene` (Ctrl+S). The command can be re-run after future
   intentional layout changes.

**Authoring direction:** This menu is a **runtime-to-Edit preview sync**,
not a way to save arbitrary Edit Mode drags back to the runtime script.
For intentional new mobile HUD positions or scales, update the canonical
values in `TutorialResponsiveLayout.cs` (or capture Play Mode values and
apply them there), then run the menu and save again. Otherwise Play Mode
will correctly override inspector-only moves with its mobile layout rules.

This previews a **full-screen safe area**. Real devices with cutouts or
nontrivial safe insets can differ and require device QA. Play Mode camera
and tile spawn behavior are dynamic, so environment/background and active
tiles are not expected to remain identical to Edit Mode.

## Verified

- **Edit Mode 2400x1080 (20:9):** fixed gold/navy WordBar and frame mask,
  Mage portrait/HP number, empty XP track, TilePool ornaments, centred
  Attack button; oversized-UI regression removed.
- **Play Mode 2400x1080:** same authored HUD geometry and mask placement;
  live HP reads gameplay values (`100 / 100` in that test). UI comes from
  persistent production HUD; no duplicate legacy player HP graphics.
- **Edit and Play 1920x1080 (16:9):** side-by-side visual inspection passed
  for Mage HUD, WordBar, TilePool, Attack placement and borders without
  clipping. Play Mode displayed a live 8x2 tile pool, without a second bar.
- **After returning to Edit Mode:** `EnemyHealthBar` stayed hidden by default
  without deleting/deactivating the persistent HealthBar Slider; the Mage
  HUD remained visible. `SampleScene` was saved clean and the Game View
  preset restored to `2400x1080 A05s`.
- Unity loaded the new menu and its action was exercised at 16:9. The
  Android-only menu reports usable runtime positions and `Match Height`
  Canvas scaling. The gameplay C# assembly built with **0 errors** (22
  existing warnings). Unity successfully compiled the editor menu too.
- No change to TilePool cell sizes (`71.17 x 66.2`), WordBar-only sizing
  multiplier (`0.83`), existing WordBar tile click/reparent layering,
  health/experience bindings, camera, or original `Canvas.prefab`.

The Edit Mode health number in captures (`1 / 1`) is a saved fixture,
whereas Play Mode displayed the real gameplay HP (`100 / 100`). This is
expected data behavior and not a visual-layout mismatch. The Attack button
may be non-interactable when no valid word is available; its presence and
position are intact.

## Evidence

- `editmode_20x9_2400x1080.png`
- `editmode_16x9_1920x1080.png`
- `playmode_16x9_1920x1080.png`

All three images are cropped Unity Game View screenshots in this folder.

## Recoverability / pending tests

- Original authored geometry before the faulty runtime-value bake:
  `Library/HudEditPlayParity_20261019/SampleScene_before.unity`.
- Immediately before the five-field rollback:
  `Library/HudEditPlayParity_20261019/SampleScene_before_safe_revert_20261019.unity`.
- Before the enemy bar idle visibility change:
  `Library/HudEditPlayParity_20261019/SampleScene_BeforeIdleEnemyHealthHidden.unity`.
- Still required for final acceptance: verify real taps and the Attack
  button after a valid word, damage/heal UI changes, enemy bar visibility
  on encounter/death, a physical Galaxy A05s run, and any non-full safe area.
  Other existing SQLite initialization and Unity Remote diagnostics seen
  during Play testing are unrelated and were not modified here.
