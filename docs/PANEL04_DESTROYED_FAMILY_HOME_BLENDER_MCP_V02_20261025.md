# SPELLSTRIKE Panel 04 environment: destroyed family home v02 (Blender MCP)

## Production status

**Working 3D environment blockout, not an approved final cinematic.** Built directly in the connected Blender 5.0.1 UI session through Blender MCP `execute_blender_code`, then examined with two actual camera renders. Unity was closed by the user during this pass, so no Unity scene was opened or changed.

The live Blender MCP execution check returned Blender 5.0.1 and an existing Panel 01 home scene with 186 objects. The MCP add-on was updated via `uvx blender-mcp install-addon`, Blender was launched with the original safe home source, and `execute_blender_code` succeeded. The older `get_addon_status` checker reported a protocol mismatch/no connection despite the actual successful Blender commands; rely on live execute/scene tests, not that checker alone, for this specific connection.

## Deliverables

- Editable Blender source: `ArtSource/LibraryProps/DefenseSets/Prologue_Home_04_Ruined_v02_FromFamilyHome.blend`
- New GLB for later Unity import: `Assets/Cinematics/Storyboard3D/Environment/DefenseSets/Prologue_Home_04_Ruined_v02_FromFamilyHome.glb`
- Interior camera review: `ArtSource/Cinematics/Panel04_v02/Reviews/P04_v02_CoolRuins_QA.png`
- Exterior ruin review: `ArtSource/Cinematics/Panel04_v02/Reviews/P04_v02_Exterior_Ruins_QA.png`
- Blender MCP build script: `Library/Panel04Audit/build_p04_home_v02.py`
- Blender MCP initial camera polish script: `Library/Panel04Audit/polish_p04_home_v02.py`

## Architecture and story continuity

The v02 ruined set is directly derived by **Save As** from the existing `Prologue_Home_01_Peaceful_v03.blend`, not an unrelated house design. The original home continues to exist unmodified, complete with its roof, walls, front doorway, household furniture, rug, and Nevali's original 20 wooden toy blocks. The separate ruin has manually torn back/front wall geometry, a collapsed/slate-fragmented roof, burned/fallen timber, smashed shelves, ash patches, splinters, stone rubble, cold lighting, drifting static ash flecks, and a modeled blue hair ribbon. The breached rear wall opens into a separately modeled outer ruined street with abandoned buildings and distant siege towers. The scene has explicit empty markers for Van's kneeling placement, wife's resting position and the absence of Nevali; no generic character model has been invented or imported.

Separate design collections, `P04_01` through `P04_07`, hold the broken shell, fallen roof, home debris, exterior city, story clues, cameras/staging markers and atmosphere. The exterior is geometry, not a flat backdrop. The Blender build contained 854 scene objects at the last live count. GLB export was configured to select only visible non-hidden meshes, omitting lights/cameras and hidden source geometry.

## Shot review

- Preview camera: `P04_A04_CANONICAL_GRIEF_MEDIUM_WIDE`
- Secondary outdoor overview: `P04_EXTERIOR_RUINS_WIDE`
- Storyboard location: cool, desaturated ruined home, outside war ruin extension, left-side grieving family staging, original blocks and blue ribbon as Nevali's absence cues
- Renderer: Blender Eevee; screenshots are diagnostic environment previews, **not final Unity Game View**
- The final interior preview keeps the floor and original rug visible for actor blocking, with a rear wall aperture; exterior preview confirms a reachable-looking ruined street and skyline beyond the house

## Known limitations / next pass

1. **No wife character asset or Younger Van actor is placed** in this Blender environment. Rodin asset creation/import and animation/rig posing remain separate work.
2. The distant towers, roof breaks and stone rubble are low-poly blockout silhouettes. The first camera images showed too-intact book cabinetry and blocking beams; those were selectively hidden/repositioned in subsequent edits, but additional art polish is needed.
3. No smoke VFX Graph, final atmospheric sound, hero lighting review, 1920x1080 export, cutscene Timeline, Unity render QA, Android performance or collision setup was performed.
4. The `.glb` export exists on disk but has not been visually re-imported into Unity. Its URP material appearance is unverified.
5. No original Panel 01 .blend, ruined-home v01, or approved Unity Panel 01/02/03 file was modified. The original intact-home file's modification timestamp remained unchanged at 2026-10-25 08:48:25 in the final file audit.

## Validation observed

- VALORANT-Win64-Shipping.exe safety check returned absent before Blender automation and file export.
- Live Blender MCP executed Python, saved the new file and rendered actual camera frames.
- New `.blend`: 915,780 bytes; `.glb`: 2,238,184 bytes as measured after export.
- Both final QA images exist at the paths listed above.
- No user-specific scene approvals claimed for this environment; requires visual director review before a final Panel 04 Timeline.
