# Mage HUD Reference V1 - Isolated Unity Assembly and QA

**Date:** 2026-10-18  
**Editor:** Unity 6000.3.10f1, Android target  
**Result:** Isolated preview assembly complete; production integration **not** performed.

## Deliverables

- `Assets/Scenes/MageHud_Reference_Preview.unity`: saved preview scene cloned from the current authored scene, with live HUD assembled into its existing `PersistentHUDRoot/Canvas/HealthBar` hierarchy.
- `Assets/UI/MageHUDReferenceV1/`: ten source PNGs extracted from `Spellstrike_Mage_HUD_Asset_Pack_v1.zip` and imported as UI Sprites with their own `.meta` files. PNG bytes matched `asset_manifest.json` SHA-256 values.
- `Assets/Editor/MageHudReferencePreviewBuilder.cs`: scoped editor assembly and QA menus (`SPELLSTRIKE > HUD > Mage Reference`). It will refuse to overwrite an existing preview and must not be used to change production scenes.
- `docs/MageHUDReferenceV1/screenshots/`: Unity Game View captures, listed below.
- `Library/MageHUDReferenceV1_SourcePack/`: full source-pack copy, in-editor assembly path audit, original hashes, captured unsaved source opacity changes and restoration audit. `Library` is intentionally outside production version control.

## Retained hierarchy and live components

The preview retains the original Slider at:

`PersistentHUDRoot/Canvas/HealthBar`

Its visual hierarchy remains under `HealthBar/SpellstrikePlayerStatusSkin` with these **exact** binding paths:

- `HEALTH/HP_LiveFill` (`Image.Type.Filled`, left-to-right)
- `HEALTH/HP_ActualValue` (**existing live** `UnityEngine.UI.Text` component, rendered above art)
- `EXP/EXP_LiveFill` (`Image.Type.Filled`, left-to-right)

The frame does not resize as the fills change. The mage portrait uses a separate shaped `Mask`; both bar fills use shaped masks. Gold framing, navy backing, and portrait/badges retain their own Unity sprite layers, with no baked numeric text. The preview attaches the existing `PlayerStatusHudSkin` component to its cloned HealthBar Slider for Edit Mode QA only; no replacement health manager or gameplay XP provider was introduced.

## Verification results

| Check | Result |
| --- | --- |
| Ten images and manifest SHA-256 | PASS |
| Preserve original UI Slider and all three exact paths | PASS |
| Masked portrait with gold/badge overlap | PASS visually in Game View |
| Both bar fills 0%, 50%, 60%, 100% | PASS visually in Android-target 20:9 Game View |
| Existing Slider -> real `PlayerStatusHudSkin` update | PASS at 0, 50, 60, 100 with matching HP fill and live HP text (`0 / 100` through `100 / 100`) |
| Slider maximum = 0 | PASS; HP fill zero, no gameplay death triggered |
| Fixed frame independent of fill lengths | PASS visually |
| Transparent surround and alpha-cut mask art | PASS visually; sprite import settings inspected |
| 2400 x 1080 (20:9) | PASS in Unity Game View |
| 1920 x 1080 (16:9) | PASS for HUD sizing/legibility in Unity Game View; see caveat below |
| Production XP default | PASS; saved `EXP_LiveFill.fillAmount = 0` |
| Level/XP numeric UI | Not displayed: no real level/XP data source |
| Source `SampleScene.unity`, original Canvas prefab, HP and responsive scripts, Build Settings | SHA-256 unchanged after completing the task |

## Screenshots

Captured from Unity Game View on October 18. These are **Editor Game View** captures, not device screenshots.

- [20:9 default, real HP text, XP empty](screenshots/mage-hud_20x9_default_hp-live_xp-empty.png)
- [20:9 0% HP/XP fixture](screenshots/mage-hud_20x9_0percent.png)
- [20:9 50% HP/XP fixture](screenshots/mage-hud_20x9_50percent.png)
- [20:9 60% HP/XP fixture](screenshots/mage-hud_20x9_60percent.png)
- [20:9 100% HP/XP fixture](screenshots/mage-hud_20x9_100percent.png)
- [16:9 1920 x 1080, real HP text, XP empty](screenshots/mage-hud_16x9_1920x1080_default_hp-live_xp-empty.png)
- [20:9 isolated preview, legacy HUD overlap removed](screenshots/mage-hud_20x9_no-overlapping-legacy-hud.png)

### Legacy HUD overlap resolved (follow-up)

The old wizard portrait, second HP bar and second `1 / 1` label were **not**
duplicate saved objects in the mage preview. Unity had both the original
`SampleScene` and its `MageHud_Reference_Preview` clone loaded additively,
so both scenes rendered their respective persistent HUDs. On October 18, the
original `SampleScene` was **unloaded** (still listed as `not loaded` in the
Hierarchy), leaving only the isolated preview rendering. The resulting Game
View shows one mage portrait, one HP bar, one EXP bar and one live HP label.
`SampleScene.unity`, the preview scene and original Canvas prefab all retained
their before-follow-up SHA-256 hashes. No scene or prefab contents needed
changing to remove the duplicate; the fix was scene-view isolation.

The fixture scenes/screenshots are QA-only; the **saved preview scene has HP full (1 / 1) and XP empty**.

## Original source preservation

The source `SampleScene.unity` was dirty before the task began. During Unity's Save-As-Copy the Editor unexpectedly serialized two pre-existing unsaved Image alpha adjustments into the original scene file. A byte-for-byte capture was saved at:

`Library/MageHUDReferenceV1_SourcePack/captured_unsaved_opacity_tweaks_original_autoserialized.unity`

The original `SampleScene.unity` bytes were restored to their pre-task SHA-256 (`a1d84f1a5ef2e2d8fab19d778d580eeeecf88066a6ac350049ffd3a7373a7e7a`). The original Canvas prefab and all protected scripts were hash-verified unchanged. The separate captured changes remain available for reapplication. Do **not** casually close/discard an unsaved source scene or copy the fixture values back into production.

## Limitations and next integration gate

1. The preview is a **separate copy of a gameplay scene**, not a minimal HUD-only scene. Other gameplay environment and HUD visuals may be visible, especially when the source is additively loaded for preservation. A stray old bar was visible during the 16:9 QA capture. This does not represent an added duplicate HealthBar within the mage preview hierarchy.
2. Physical Galaxy A05s/Android run, desktop PC-wide responsive validation beyond the 16:9 Editor preset, and end-to-end damage/progression testing remain to be done before production deployment.
3. Runtime XP has no authoritative progression provider yet; preserve `EXP_LiveFill.fillAmount = 0` until one is implemented. QA fixtures explicitly simulate XP and should be reset afterward.
4. The Unity MCP bridge reported no Editor instance during the task. The connected COS Desktop controlled the actual Unity Editor menus, and the Unity Editor script assembled/verified the scene. The bridge limitation did not prevent the on-disk preview assembly.
5. Unity's Editor incurred several unusually long compilation/domain reloads and pre-existing Relay connection warnings. Editor-only `SendMessage(LateUpdate)` test assertions were eliminated by a direct edit-mode invocation of the same skin refresh. This was QA tooling only; gameplay scripts were not changed.

**Approval gate:** Review the isolated preview visually. Do not apply it to `SampleScene`, prefabs, or production progression until separately authorized.

## 2026-10-18 preview-only visual polish

The current mage HUD retained the approved portrait, heart/star ornaments,
bevel masks, gold frame, navy backing, identical placement/scaling and all
three required HP/EXP paths. The same bound `HEALTH/HP_ActualValue` Unity UI
Text now uses the **already-present**
`Assets/Fonts/SPELLSTRIKE/Alegreya-Variable.ttf` font, with a cream-white
19pt bold manuscript treatment. The existing dark outline remains in place
for contrast over red HP. No numeric values are baked into sprites.

The invisible text RectTransform was widened from approximately 82 to 174
local units while its horizontal midpoint stayed the same. This provides
space for future authentic multi-digit HP counts without pushing against
the right frame. An asset-font measurement at the authored 19pt size
estimated a width of 68 units for `120 / 120` and 86 for `9999 / 9999`,
both within the 174-unit text box. This is a typography sizing estimate,
not a live gameplay damage/heal test.

The corresponding preview-only Builder authoring values were updated, so
new assembly uses the same manuscript font and text box. Unity recompiled
the Builder and reloaded the changed preview; the **isolated** scene was
visually checked in Game View at 2400 x 1080 (20:9) and 1920 x 1080 (16:9).
The art and full red/empty blue bars remained aligned; no duplicate legacy
HUD appeared. The previously completed 0/50/60/100 percent fill and live
Slider tests remain valid because fill components and bindings were not
modified in this polish. Physical Android and live death/heal validation
are still pending.

New polish captures:

- [20:9 after polish](screenshots/mage-hud_polish_v2_20x9.png)
- [16:9 after polish](screenshots/mage-hud_polish_v2_16x9.png)
- [Close-up of updated serif HP text and preserved gold artwork](screenshots/mage-hud_polish_v2_closeup.png)
