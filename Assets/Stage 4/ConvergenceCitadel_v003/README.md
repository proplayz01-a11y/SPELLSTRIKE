# Stage 4 - Convergence Citadel v003

Four-stage adventure: this is **Stage 4**, not the removed stand-alone Cursed Gothic stage. Linear route: Start -> Hollow Knight -> Drowned Captain -> Titan Colossus -> Vander Poel throne courtyard. **Node 3 (Titan) is the largest island.**

`Environment/` contains bytewise copies of the approved v003 GLB exports, texture atlases and top-down reference. `Validation/` holds the Blender-produced technical provenance; this is not a Unity Play Mode acceptance pass.

Stage4Scene and an isolated validation companion are generated only via the visible Unity Editor menu `Stage 4 > Convergence Citadel v003 > Create Isolated Scenes (Preserve Open Scenes)`. The menu adds/temporarily saves only its own additive scenes; it never edits an existing SampleScene. Do not run a second time over existing scenes.

No stage-selection, unlock/save IDs, objective gating, boss mechanics, or story clips are wired in this validation pass. The validation scene contains an **inactive** canonical player and an **inactive** unmodified Titan candidate for scale inspection. The production Stage4Scene remains a visual/collision scaffold only until QA gates pass.

`ConvergenceCitadel_Final.blend` remains the editable Blender source in the user's original external v003 handoff directory; the Unity project intentionally imports `.glb` rather than `.blend`.