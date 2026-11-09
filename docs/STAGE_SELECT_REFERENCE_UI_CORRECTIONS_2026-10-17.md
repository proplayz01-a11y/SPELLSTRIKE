# Stage Select Reference UI Corrections — 2026-10-17

The user requested visual fixes against `Spellstrike_ Enchanted World Map.png`, preserving its design. Changes are confined to an isolated scene and the existing editor builder.

## Changed

- Added `Assets/Scenes/StageSelectBook_ReferenceFix.unity` from the latest saved visual-review scene, with Unity-generated metadata.
- Extended `Assets/Editor/StageSelectReferencePreviewBuilder.cs` with an undoable in-place layout correction and a separate title-clipping correction. Neither Apply menu saves automatically or rebuilds the hierarchy. The original Build path uses the shared layout helper and binds the scene-owned Enter callback after prefab creation.
- Corrected artwork fitting, stage label/badge placement, route geometry, information-panel typography and Enter Stage text. Kept generated assets and later manual layout adjustments.
- Disabled the old route graphics and sampled existing connector artwork along four curves. Preserved the four route parents and manager order. Saved state has 90 bead objects, including one user duplicate, and five compass markers.
- Persisted exactly one callback from the new Enter button to the existing manager's `OnEnterPressed`.
- Increased all five selected-stage title boxes to 44px. The former 38px box was below the existing Alegreya 28pt line height (about 38.108px), causing TMP Ellipsis to suppress the first line. The title-only menu preserves user-adjusted artwork and positions.

## Evidence

- Editor compilation completed; compiled Editor DLL is newer than the final source, with no matching builder compiler errors found.
- David invoked the visible correction menus after Windows automation was unreliable, then saved. He confirmed the title is visible after the targeted correction.
- An enlarged Edit Mode Game view showed corrected route shapes, readable stage plaques and the dark Enter label. The final title correction was verified in the saved properties and by David's visibility confirmation.
- Final saved scene: 730,328 bytes; SHA-256 `3a4907936e12e390990c84a404141bc9dffc718967587f292b20d967034bb221`.
- Independent serialized audit: all five title heights 44; one persistent Enter callback; complete StageSelectManager serialization unchanged from the source review scene, including arrays and review-controller/profile references.
- Pre/post SHA-256 checks confirm production StageSelectScene, protected SampleScene and StageSelectManager.cs were unchanged by this work.

## Limits and manual validation

This is an isolated presentation correction, not production integration. The art remains a reference-guided recreation with different fine details. Current saved user customization is retained. No Unity MCP, hidden Editor C# execution, runtime component or duplicate review overlay was used.

Still unverified:

1. Fresh scene reload and normal selection across the currently unlocked stages.
2. Correct lock warning, checkmarks and route visibility under existing progress.
3. Enter routing through the existing ItemSelectionScene flow after a reload.
4. Mandatory pending vocabulary-review behavior.
5. Physical Android text readability, UI/texture cost and performance. The sampled route Graphics have not been profiled on a device.

Do not alter PlayerPrefs, progression authority or review gating to make the preview look complete. Do not run the full layout command merely to change heading height: use the title-only command, preserving manual geometry changes.

No architecture or gameplay contract changed. No production-ready/runtime-complete claim or Git commit was made.
