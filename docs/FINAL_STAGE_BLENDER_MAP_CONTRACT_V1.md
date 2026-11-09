# SPELLSTRIKE Final Stage — Blender Map Contract (v1 Draft)

**Purpose:** execution brief for a Sol Ultra Blender map-building session. This
is a **design/input contract**, **not** an existing map, approved environment
art or a passed Unity implementation.

## Source-of-truth scope

- Four playable Adventure stages: Stage 1 / Stage 2 / Stage 3 / **Final Stage**.
  The old stand-alone Cursed Gothic stage is removed from the current scope.
- The Final Stage reprises the **Hollow Knight**, **Drowned Captain** and
  **Titan** as sequential remnant boss encounters, culminating in
  **Vander Poel**. Keep one boss encounter active at a time.
- The sequence uses one connected, optimizable dark-fantasy environment, not
  several independent large unculled worlds. The exact art direction is
  proposed below and must be reviewed against user reference art before lock.
- Sol Ultra owns the authored **.blend environment and inspected .glb export**;
  COS owns a **new, backed-up Unity scene**, physics/NavMesh, boss prefab
  integration, vocab gates, performance and full PC + Android QA.
- Preserve original Stage 1–3 scenes and original characters, including
  Vander's canonical staff and Van Aert's canonical player rig.

## Environment proposal: The Fractured Grand Archive

One decaying spellbook citadel arranged as a **contiguous, four-encounter
journey**. Theme: parchment stone, fractured book spines, aged bronze, dark
navy, thin cyan arcane cracks, and restrained red corruption. Boss arenas echo
past stages without importing entire old terrain maps.

| Segment | Visual identity | Functional geometry |
| --- | --- | --- |
| Entry / checkpoint | Broken royal library threshold, floating manuscript fragments | Protected player entry and neutral preparation ground; no boss can hit player here |
| Arena A — Knight's Court | Thornbound dark-royal courtyard; echoes Enchanted Kingdom | Flat central fighting plane, blocking perimeter, boss entry, fragment/reward site, single gated exit |
| Arena B — Drowned Gallery | Fossilized ship ribs inside an indoor library-flood hall; echoes Sunken Seas | Traversable dry ground with shallow **visual-only** water accents; no unnecessary full-surface realtime water simulation |
| Arena C — Titan Reliquary | Huge runic stone chamber; echoes Mythic Ruins | Wide, unobstructed, locally level collision ground; safe real Titan root/capsule/foot IK clearance; separate upper architecture for silhouette |
| Final Sanctum — Vander Poel | Cracked celestial reading room with shattered ritual book at center | Distinct large boss arena, dialogue/cutscene viewing positions, final victory point and ending handoff |

**Route:** Entry → Arena A → controlled corridor → Arena B → controlled
corridor → Arena C → long narrative ascent → Final Sanctum. Doors/walls must
break cross-arena visibility to support frustum and occlusion culling. This is
a navigation design proposal, not a guaranteed implemented scene flow.

## Blender production rules

1. Create an **editable native .blend** file with named collections for:
   `ENV_Static`, `ENV_Floor`, `ENV_Decor`,
   `COLLISION_Static`, `ARENA_Bounds`, `VISIBILITY_Doors`,
   `LIGHTING_Guides`, `MARKERS_Gameplay`.
2. Name entry and encounter marker empties:
   `PLAYER_Spawn_Entry`,
   `ARENA_A_PlayerEntry`, `ARENA_A_BossSpawn`, `ARENA_A_Exit`,
   similarly `ARENA_B_*` and `ARENA_C_*`,
   and `FINAL_PlayerEntry`, `FINAL_VanderSpawn`,
   `FINAL_VictorySpot`. Empty markers carry **no game logic**.
3. Avoid applying hidden transforms to different groups inconsistently. The
   Blender→GLB→Unity calibration must preserve a common axis/up convention
   and uniform scale. Record the exact export settings in the manifest.
   **Do not assume the player or Titan uses a 1-unit human height**;
   project prefabs have nonstandard world scales. A Unity first-import scale
   proof is required before locking actual arena diameters.
4. Make every floor usable by Unity NavMesh with isolated, simple collision
   meshes. No overlapping hidden walkable upper platforms or accidental
   navmesh shortcuts. Include real solid walls/doorway clearances.
5. Keep geometry procedural/modular and freely editable. Avoid baking
   playable door, fragment or boss art into static meshes when Unity needs
   runtime visibility, collision or animation. Do not embed boss character
   rigs in the environment GLB.
6. Design for Samsung Galaxy A05s: tightly controlled materials/draw calls,
   repeated instanced meshes, atlas-friendly UVs, lightmapped static surfaces
   where practical, small transparent/water areas, LOD-ready heavy shapes,
   shadow-caster selection. Do not claim 30 FPS without physical measurement.
   Compare against recorded Stage 2 19.51 FPS baseline only as a caution,
   not a transferable final-stage benchmark.
7. Create a visibility/LOD plan so only the active arena's major meshes
   require full-detail rendering. No giant high-detail terrain mesh for
   decorative hills; segmented grounded meshes are preferred.
8. Titan's four-stomp procedure has physically strict 94%-reach and real
   grounded contact in its original Stage 3 QA. **Do not change the animation,
   weaken its 0.35 m contact gate, or place the final Titan on slopes/steps**
   before independently validating natural stance on this map.

## Required handoff artifacts

Under an isolated versioned output directory, supply:

- `SPELLSTRIKE_FinalStage_v001.blend`: original editable source with
  organized collections, real meshes and meaningful materials.
- `SPELLSTRIKE_FinalStage_v001.glb`: inspected export with mesh names and
  hierarchy retained as far as the exporter supports.
- `FinalStage_MapManifest_v001.json`: collection/object inventory, marker
  world transforms, arena floor bounds, corridor/doorway dimensions,
  approximate vertex/triangle counts by arena and total, material/texture
  counts, Blender version and GLB export settings.
- Bird's-eye overview and one in-Blender solid/rendered screenshot per
  arena. Include clear scale/door marker screenshots; beautiful renders
  alone cannot certify import/navigation.
- Explicit list of missing assets, fallback material choices, intended
  Unity-only VFX and geometry known not to be collision-ready.

## COS integration acceptance gates

- First copy/import the map into **an isolated staging location**. Inspect
  mesh units/orientation, apply URP materials, and confirm all marker positions
  against the real canonical player and the three reused boss physical
  prefabs before touching any production scene.
- Create the **new** Final Stage scene with backup/hash evidence; register
  its stable scene name only after it loads. Rewire Stage Select from five
  to four logical stages with migration-safe progress/checkmark logic and a
  stage4-complete final marker.
- Add NavMesh, rigid collision per arena, one encounter activation at a
  time, boss cleanup, reset on death, safe player entry and transition.
- Add Vander Poel and ending progression only after the three gauntlet
  encounters are independently repeatable. Game win requires genuine
  clear/recording and the mandatory vocabulary review policy.
- Verify a real PC end-to-end run and a **physical A05s build**. Measure FPS,
  stability, navigation, touch controls and no soft-locks; no pass from GLB
  stats/Unity compile alone.

## Unknowns to lock before final art production

- Exact silhouette/composition of Vander's final room and any desired
  cinematic reveal; the user's approved story references control character
  identity, not yet this new arena.
- Whether the three remnant bosses reuse original models unchanged or gain
  narrative visual effects/skins.
- Where **Identity** and **Soul** are recovered within this compressed
  four-stage narrative; the older story planned five fragments and must not
  silently lose either. Candidate: Identity after the gauntlet, Soul after
  Vander, pending narrative approval.

This file is a reproducible handoff for map creation, **not a claim** that
Sol Ultra has already generated a playable level.
