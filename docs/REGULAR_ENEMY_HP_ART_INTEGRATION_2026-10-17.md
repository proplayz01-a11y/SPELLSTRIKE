# Regular Enemy Overhead HP — approved sprite integration (2026-10-17)

Status: **sprite assets imported to project, runtime code integrated, C# compilation passed; Unity Play and Android visual QA pending.**

## Approved art and alignment

User-approved simplified gold/navy regular-enemy frame without the blue crystal; red fill is a **separate** image.

- `Assets/Resources/SpellstrikeHUD/RegularEnemyHP_Frame.png` — immovable transparent outer frame.
- `Assets/Resources/SpellstrikeHUD/RegularEnemyHP_Fill.png` — transparent red fill, independently HP-driven.
- Two distinct Unity .meta GUIDs, Single Sprite / Full Rect, alpha enabled, no mipmaps, 1024 import limit.
- Both files are 880×150 pixels and share identical crop geometry from the user's two verified original downloads (2172×724). Source hashes were compared before import, preventing asset swaps.
- Local alpha/composition samples were generated with 100%, 50%, 15%, and 0% fill:
  `Library/HUDImportStaging_20261017/preview_100.png`, `preview_50.png`,
  `preview_15.png`, `preview_0.png`. These **are offline mockups**, not Unity Game View screenshots.

## Unity runtime binding

Updated only `Assets/Scripts/EnemyScripts/RegularEnemyOverheadHealthBar.cs`.
The shared `EnemyEncounterHUD` and `EnemyHealth` owners from the prior overhead-HP implementation stay as they were.

Regular-enemy world-space Canvas layers in draw order:

1. `EmptyNavyTrack` — 136×11.5 UI units, visible as health is lost.
2. `DynamicRedHealthFill` — 160×27.25 UI units with
   `Image.Type.Filled` and horizontal left-to-right fill fraction equal to
   `Mathf.Clamp01(currentHealth/maxHealth)`; uses the approved red Sprite.
3. `ApprovedGoldNavyFrame` — 160×27.25 UI units, full static size in front.

No frame stretching while damage occurs. All images have `raycastTarget=false`
and the overhead Canvas has no GraphicRaycaster. If either Sprite is missing,
the component logs a warning and safely falls back to the previous colored
rectangles so gameplay HP functionality remains available.

The component continues to follow camera/model bounds, update via
`EnemyHealth.HealthChanged` and legacy direct HP writes, and hide on death.
Known bosses keep their separate screen-space HUD path.

## Vertical placement adjustment (2026-10-17 follow-up)

On request, regular-enemy overhead bars **follow enemy X/Z motion only**. At
first appearance, the component samples the enemy model's head/top bounds and
stores a fixed **world-space Y height** above it. Subsequent frames follow the
enemy's horizontal center using the initially captured local offset, but
never re-sample animated model bounds or follow the root's jumping/attack Y.
It resets that reference if the enemy is disabled and later reactivated at a
new spawn. This intentionally keeps the bar at one height even if the enemy
jumps or travels onto higher/lower terrain. HP, sprites, camera-facing size,
death hiding and boss HUD are unaffected. The change passed a background
Assembly-CSharp shadow-project build with **0 errors** and 22 pre-existing
warnings; live Play/Android visual tests remain pending.

## Verification

- Used the two originals actually downloaded by the user in Windows Downloads
  on 2026-10-17 at 19:30 (SHA256 matched the already generated files).
- Sprites are properly separated, true transparent RGBA, frame center hollow.
- Offline visual composition inspected at HP 100%, 50%, and 0%, including
  red-fill clipping against the fixed gold edge.
- Background `dotnet build` of a temporary, auto-cleaned shadow
  `Assembly-CSharp.csproj` (explicitly including the new overhead HP source)
  succeeded with **0 errors, 22 existing warnings**.
- No Unity Play, Inspector sprite reference/runtime test, or physical Android
  frame-time/readability check was performed. Unity was not brought to
  foreground. Test next in a regular enemy encounter at 2400×1080 and
  desktop 16:9.
- Protected scenes and original Stage Select/prefab were not edited. No
  progression/health-damage logic was changed.
