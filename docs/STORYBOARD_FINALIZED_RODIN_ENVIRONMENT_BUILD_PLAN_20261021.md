# SPELLSTRIKE — Finalized Storyboard to Rodin 3D Environments

**Date:** 2026-10-21  
**State:** Desktop storyboard visual audit and build blueprint, **not generated geometry or approved dimensions**.  
**Latest user direction:** Finish the complete, physically consistent library using Rodin-generated modular assets **before expanding Timeline animation/dialogue production**. Vander Poel's Humanoid rig and initial isolated idle Timeline were prepared separately; this plan does not alter them.

## 1. Evidence and sources, in authority order

- User's actual Desktop folder: `C:\Users\David\OneDrive\Desktop\FINALIZED STORYBOARD\Prologue_Panel_01.png` through `Prologue_Panel_42.png` (**42 images**) visually reviewed in seven contact sheets at `Library/StoryboardRodinAudit_20261021/contact_sheet_01.jpg` through `07.jpg`.
- **Physical library arrangement:** `Assets/Story/References/Library_TopDown_Layout_REFERENCE.png`, `docs/LIBRARY_ENVIRONMENT_BIBLE.md`. This overrides small AI-illustration inconsistencies in an individual perspective.
- **Specific shots and beats:** the Desktop's finalized 42 panels, read alongside `docs/STORY_PANEL_ASSET_MANIFEST.md`. Do not invent plot beats or change approved identities.
- **Style target:** Panel 11, painterly/cel-shaded fantasy with confident outlines, warm amber study light versus navy/cyan archive shadows, carved stone, dark wood, bronze/gold, cyan magic only where motivated.
- **Checksum comparison:** Desktop Panels **39/42** are byte-identical to one of the same-number approved repo source PNGs. Panels **04, 05 and 08 differ** from their repo copies (comparisons at `Library/StoryboardRodinAudit_20261021/compare_panel_{04,05,08}.jpg`). For this build use the **user-designated Desktop finalized version**: Panel 5's subdued outdoor light, Panel 8's tabletop without the extra lantern; Panel 4 is closely compositionally matched but its file is different. Do **not** overwrite repo approved references or publish this as a formal change to canonical documentation.
- Previous 28 runtime composite pages are historical playback, not the construction reference; the new presentation must use real Unity 3D animation/Timeline.

## 2. Scope: four reusable environments, not 42 separate models

| Panels | Physical set | 3D construction strategy |
|---|---|---|
| **01 and 04** | Young Van/Nevali family home | Same home footprint: warm furnished intact variant → damaged walls, broken furniture, books, block toys and blue bow/ribbon, smoke and cool rubble dressing after the war. Preserve grieving action. |
| **02–03** | Great War city ruins | One modular devastated fantasy city street with burnt buildings, shattered parapets, rubble and banners. Storm/fire/smoke and abstract violet Broken Language are **Unity VFX**, not fused to the Rodin architecture. The readable Word magic tiles are distinct from corruption. |
| **05–37** | **One complete Vander Poel library** | South exterior/front entry → front study chamber/central long table → Gothic arch and red-carpet aisle → rear archive and closed rear doors. Reuse the same walls, furniture and landmarks for all angles and both Van ages. |
| **38–42** | TutorialForest | Reuse/derive the actual game forest geometry: stylized trees, grass, rocks, sun and route/bridge context. Do not regenerate the entire forest as a disconnected, contradictory map. Check lighting/style transition from the library. |

**Library first:** 33/42 source panels are in or at its exterior. Home and ruins may be built later with the same Rodin module practice. The library is one scene/set; its hall, passage, and archive are **connected spaces** rather than separate floating shots.

## 3. Canonical library footprint and shot landmarks

View the reference with **north/rear archive at the top** and **south/front façade at the bottom**:

```text
            NORTH: END ARCHIVE WALL / REAR DOUBLE DOOR
  +-------------------------------------------------------+
  |     ARCHIVE SIDE SHELVES     ARCHIVE SIDE SHELVES      |
  |   tall shelves + desks         tall shelves + props   |
  |           CENTER RED RUNNER / WALKABLE AISLE          |
  +---------------------- GOTHIC ARCH --------------------+
  |                BLUE/GOLD ARCH COLUMNS                |
  |  BOOKSHELVES                                      SHELVES
  |       MAIN LONG WOOD STUDY TABLE, TWO CHAIRS         |
  |           LARGE RUG / CHANDELIER ABOVE              |
  |  BOOKSHELVES                              STUDY PROPS|
  |  SOUTH CENTER: AMBER STAINED GLASS / BLUE BANNERS    |
  |                                  SOUTH-RIGHT ENTRY DOOR
  +-------------------------------------------------------+
```

The drawing is **topological, not a substitute for the official top-down image**.

### Temporary measured greybox proposal (NOT locked dimensions)

Use 1 Unity unit = 1 meter, and a full-scale Vander model approximately 1.9 m tall as a human-scale check. Suggested starting values for a playable, camera-navigable draft (refine through screenshots **before** Rodin production):

| Area/object | Initial draft size | Important constraint |
|---|---|---|
| Front study | **14 m wide × 12 m deep × 5.5 m high** | Room for wide two-shots and camera dollies; exact 2D panel proportions are not measured geometry |
| Main table | **3.2 m × 1.4 m × 0.78 m high** | Two seated actors with hand reach and book over-the-shoulder shot clearance |
| Central gateway | **3.4–4.0 m clear width**, **4.3 m arch crown** | Deep-blue inset ribs/stone and flanking guardian pedestals; centered north of table |
| Central connector | **3.5 m aisle × 5–7 m deep** | One physical route; red carpet through portal, torches/statues outside the walking area |
| Rear archive | **14 m × 9 m × 5.5 m high** | Shelf bays on both sides, books can be retrieved with clear two-person blocking |
| Shelf module | ~**2.0 m × 0.55 m × 3.3 m** | Vary only a few inserts and book color patterns, not unique dense meshes per copy |

These are **engineering placeholders, not claims that the reference picture encodes meter measurements**. Verify silhouettes at cameras first. Position the main table in the *front* study; maintain the central path, south-right door, south-center stained window and rear archive doors.

### Essential continuity anchors actually visible in the final 42 panels

| Panel range | Lens/character action | Environment we must physically support |
|---|---|---|
| **05** | Young Van enters from a doorway at image right | Front-right exterior doorway, stone jambs, visible study table/rug, candlelit chandelier and stone Gothic passage farther inside. Exterior illumination is subdued in Desktop final. |
| **06–08** | Seated grief conversation/reactions | Two correct chairs across long wood table; shelf-and-banner background available for Vander's centered reaction in 08. **No added tabletop lantern for Desktop Panel 08.** |
| **09–10, 12–15** | Study conversation, era-change and thought shots | Continuous same table/wood tabletop, flanking shelves and arch; warm torches; a believable visible hallway axis, two shot-reverse-shot camera positions. |
| **11** | Nevali's magical appearance in the study | A camera-visible cool blue Gothic window zone to support the glowing silhouette while preserving the canonical front stained-glass façade location. Magical person/particles are separate Timeline/VFX. |
| **16–17** | Vander walks through arch and selects ancient book | True open passage, carpet physically leading to archive shelf; book can be pulled from one reachable hero shelf; actor with staff behind. |
| **18–19** | Vander carries/sets book on table | Same ancient volume reappears at exact table position; place chairs with actor reach clearances. |
| **20–26, 30** | Extreme book and tabletop close-ups | High-detail, riggable ancient **book** (separate covers and animated pages), clean page materials for story illustrations/message. Don't bake legible Nevali text into a generic Rodin texture. |
| **22–24, 27–33** | Reactions, accusations, choices | Keep bookshelf, blue-banner and arch landmarks constant across alternating camera compositions; allow standing up near chairs. |
| **34–35** | Vander heads into arch, returns carrying distinct staff | Same gateway and red runner; two symmetrical guardian statues on plinths, separate flanking wall torches, blue stone columns. Staff is a **separate canonical prop**, not part of the library wall. |
| **36–37** | Staff activates forbidden technique, Van drawn into living book | Unobstructed stage around table; practical VFX spawn above book; room-scale lighting change, camera/effect paths and enough clearance to animate Van being pulled inward. |

**Important:** The locked guardian statue design from the manifest is a **complete small robed figure with hands clasped at the chest, on a narrow carved pedestal**, not a disembodied bust or generic knight. The torch must be **separate and higher**, positioned further outward from the gateway. A 2D panel may visually obscure its lower body; use the locked statue specification.

## 4. Rodin strategy: **one module at a time, one assembled library**

### Why we should NOT ask Rodin for the whole 3D building in one generation

- Single-output image-to-3D is likely to merge furniture, stairs, walls, shelves and characters into one geometry blob, distort absolute scales and lose interior walkable space. A good exterior screenshot is not equivalent to useful room topology.
- 33 panel shots need editable table clearance, moving actors, camera routes, selectable doors/book/props, and occlusion separation.
- **Rodin generates the visual assets; Unity/Blender enforce the canonical floor-plan assembly.** This still uses Rodin for the complete visible environment, but doesn't delegate continuity logic to a single AI model guess.

### Production batches (generate modular families, then instance them)

| Batch | Unique asset **families** | Rodin priority / reuse |
|---|---|---|
| **A — Study shell** | cut-stone wall straight, wall corner, floor tile section, vaulted ceiling rib/beam, Gothic arch with navy band, blue/gold column, south-right wooden entry door, south stained glass with banners | Start with **arch**, column and wall modules to lock silhouette. Use repeats and mirrored instances. Straight floors/basic structure may be made in Blender with Rodin materials/details to maintain exact measurements. |
| **B — Study furniture** | main central long table, high-backed wooden chair, tall built-in filled bookshelf, half/shelf corner variation, desk/cabinet, book-stack inserts | Hero table and chairs get precise hand/leg clearance. Bookshelves should reuse modular insert meshes/atlases rather than hundreds of unique books. |
| **C — Study hero dressing** | golden chandelier, twin-wall torch holder, bronze/gold compass blue banner, guardian full-figure statue/pedestal, desktop candelabrum, astronomical globe, small research props | Torch light comes from **Unity Light**, not a permanent modeled fire; duplicate the same approved statue on both arch bays. |
| **D — Connected archive** | archive tall shelf variant, closed end double doors, narrow pointed windows, old research lectern, archive scattered books, wood/stone accents | Reuse wall/floor/arch/shelf modules from A/B; add only archive-specific depth, details and cooler lighting. |
| **E — Narrative hero props** | ancient leather-bound chronicle with compass/sigil cover, page mesh/covers/hinge, swappable page graphics, dedicated Vander staff (reference separate) | Book must have **separable cover/page geometry** for Page 20–26 and Page 36–37. Staff must not be fused to actor. Text and magic are separate Unity elements. |
| **F — Later sets** | intact home walls/window/chairs/books/plants/blocks; home damage modules; burning Gothic ruined city walls/columns/rubble/banner | Defer until library blocking and its proof shots work. The forest primarily reuses existing game geometry. |

**Initial target:** roughly 18–25 distinct Rodin generation jobs for the complete library, subject to testing the first assets and balancing modularity, not a fixed quota. Many instances are repeated. All models should be saved individually in the source folder with a stable name; no bulk generation is authorized before the first few are approved.

### Prompt wrapper for every Rodin asset

Add the right cropped environmental reference to each prompt. Don't feed a full character dialogue panel or collage to a generation intended only for furniture.

> Produce ONE isolated, full 3D modular environment asset for the SPELLSTRIKE Gothic wizard library. Stylized painterly/cel-shaded dark fantasy from the attached approved reference: deep blue-grey stone, dark walnut, aged bronze-gold, restrained navy/teal magical accents, sharply readable silhouette. Front/back/sides should have complete usable geometry. No people, characters, body parts, floating unrelated objects, text, logos, frames, camera, room/background, prebuilt floor or sky. Clean UV-friendly topology; high-detail ornament may use texture and normal maps. Export PBR GLB/FBX with texture maps. Maintain realistic human scale. The reference crop governs shapes and ornament, NOT the addition of unrelated architecture.

Then append ONE object-specific prompt below, **not all of them at once**:

1. **Gateway:** `ONE freestanding deep Gothic pointed stone arch portal with exactly two dark blue inset carved column supports, weathered slate blocks and thin aged-gold accents, open clear passage for two adult figures. Symmetrical construction, no sealed portal surface, no wall across opening. Approximate 3.6 m walkable clearance and 4.3 m crown once resized in Blender. Match the study-to-archive gateway in reference Panels 16 and 34. Do not include statues, torches, carpet, shelves or a door.`
2. **Straight stone wall:** `ONE rectangular 3 m wide, 5.5 m tall Gothic dark-grey coursed masonry wall section, flat clean endpoints for tiled repeats, no baked window or arch opening, medium-scale carved stone, complete back face, muted cold blue mortar. No decorative props attached. Designed to snap beside identical wall sections in a larger indoor library.`
3. **Bookshelf:** `ONE free-standing ornate dark-walnut library bookshelf approximately 2 m wide and 3.3 m tall, with aged brass fittings and built-in lower cabinet. Populate shelves with a small number of optimized grouped dark leather-bound books in burgundy, navy and olive. Exact symmetric outer frame, straight level foot and back, no floating books, no surrounding wall. Designed for repeated copies along both sides of the room.`
4. **Study table:** `ONE long solid rectangular walnut scholar's table, approximately 3.2 by 1.4 meters and 0.78 m tall after rescaling, ornate restrained carved legs and aged brass corner insets, smooth usable tabletop, enough clear knee space for two seated human actors on opposite sides. Absolutely no fused books, candles, props or chairs; this is a separate animated-interaction surface.`
5. **Chair:** `ONE tall-backed old Gothic wooden study chair matching the walnut table, carved peak finial and narrow aged-gold details, robust four legs, real seat space for an elderly robed man, no cushion/person/robe attached; height scaled for the table and human actors.`
6. **Guardian statue:** `ONE COMPLETE small old robed scholar guardian STATUE in weathered bronze-grey stone, hooded head and FULL TORSO, both hands clasped at chest, full sculpted robe and lower body, on a narrow tall rectangular ornamental stone pedestal with stepped base. Match the paired statues flanking the library gateway in storyboard Panel 34. No bust-only sculpture, no staff or book or weapons, no torch attached.`
7. **Wall torch:** `ONE mounted fantasy Gothic aged bronze wall sconce with a single flame cup, wall mount plaque, restrained curved metal detailing, complete backplate and orientation flat against a wall. NO fire plume or modeled light volume; separate Unity flame/light will be added. No statue, pedestal or wall chunk.`
8. **Banner:** `ONE vertical navy-blue fabric wizard academy wall banner with a centered symmetrical eight-point gold compass/star emblem and narrow gold hem, aged fabric drape and wooden/metal hanging bar, no readable letters or extra heraldry. Corresponds to the navy-and-gold banners behind Vander at the study bookshelf.`
9. **Front window:** `ONE pointed Gothic front-façade stained-glass window in grey stone frame with warm amber stained panels, designed to fit in a separate south wall opening, deep frame and glass kept separate/material-selectable. No door and no complete wall building. Flanked by separately modeled navy compass banners in the assembled library.`
10. **Archive door:** `ONE large aged walnut Gothic double-leaf archive door with embossed rectangular panels, blackened iron hinges and gold trims, openable left and right leaves, deep pointed stone surround only if separately selectable. No hallway or room geometry; must fit at the far end of the archive red-runner axis.`
11. **Chandelier:** `ONE suspended medieval brass ring chandelier with evenly arranged restrained candle holders, black iron chain and central hanging support, symmetrical balanced fantasy scholastic design, physically separate holders and chain, no entire ceiling, no baked light glare. Similar to the circular chandelier visible over the main study table in Panel 05.`
12. **Hero chronicle:** `ONE large heavy leather-bound ancient fantasy chronicle book with dark brown cover, embossed elegant gold compass/star motif, visibly thick page block, separate cover-ready and paper geometry for later Blender rigging, no illegible molded words and no magical particle effects. Shown being taken from the archive shelf and opened on the main table in Panels 17-26.`

**Input views:** Use cropped **Panel 05** for front study/entry, **06/08** for table and shelves, **16/34** for gateway/guardian/runner, **17** for archive shelf, **18/20** for the chronicle, and the approved **top-down** reference for spatial placement. For shapes with concealed sides use new consistent orthographic object-only views if needed, not multiple different scenes blended together.

## 5. Blender + Unity assembly contract

1. **Greybox before bulk generation:** in `Assets/Scenes/Cinematics/Storyboard3D_CinematicWorkbench.unity` reconcile the current generic placeholder hall against the canonical top-down. Do **not** accept existing hall/corridor as complete. Stage actual dimensions, door, table/chairs, connected arch/archive, and 6 representative static camera views before Rodin generation beyond prototypes.
2. **Single asset import:** export each Rodin result as textured GLB/FBX with source PBR maps, then validate all sides in Blender; remove duplicated faces, fused unneeded surfaces, floaters, ngons causing shading breaks, excessive internal faces, and stray objects. Keep a low-poly working copy. Do not alter original Rodin download.
3. **Standardize physical size and pivot:** apply Blender scale, Z-up asset convention as prepared for Unity Y-up import, set sensible pivot at floor/bottom-back for snapped wall/shelf props or center/base for furniture. Save exact asset name and dimension in a manifest. Do **not** expect a text prompt's numeric dimensions to produce meter-accurate output by itself.
4. **Material separation:** at least stone, wood, metal, banner/fabric, glass; atlas repeated book spines where practical. Convert to URP materials, validate color-space, normal orientation and cutout/transparent glass. Avoid a new huge texture set for each book.
5. **Blockout replacement:** one module family at a time, using prefabs and repeated/rotated/mirrored instances aligned to the single ground plan. Keep rear connector in the SAME building; disable corresponding blockout proxies rather than stacking duplicate walls/props.
6. **Interaction and cameras:** table/seat/stand targets, old book shelf/retrieval position and gateway walk path must be explicitly accessible to Vander/Van; the Hero Book page materials and staff are independent Timeline/VFX bindings, not embedded in a static set.
7. **Optimization:** mobile and PC profiles can share geometry with tailored LODs/materials. Create actual `LODGroup` with lower-detail meshes where meaningful; `LOD`-named roots alone do nothing. Prefer baked/static lighting and a few practical real-time lights, limit realtime shadows, frustum/distance culling and atlas repeated decorative objects. Occlusion is useful for the archive/doorway, but verify actual result before claiming a performance benefit. Exact A05s performance target of 30 FPS is an acceptance target **not measured here**.
8. **Safety:** production `StoryScene`, `Prologue_Sequence.asset`, `TutorialScene`, main menu, Stage Select, final-stage bosses, Build Settings, save database, PlayerPrefs and all gameplay systems remain unchanged. Build only in the isolated cinematic workbench and new art prefabs. Do not use Unity MCP; use normal visible Editor operations.

### Example controlled scene hierarchy, NOT yet created

```text
_LIBRARY_ONE_BUILDING
  FRONT_FACADE_ENTRY
    EntranceDoor_RightSouth
    CenterStainedGlass
    BlueBanners
  STUDY
    StructuralStoneWalls
    EastWestShelves
    MainTableAndSeats
    StudyPropsAndChandelier
    StudyCameraAndActorTargets
  GATEWAY_AND_CONTINUOUS_RED_RUNNER
    ArchBluePillars
    GuardianStatues
    WallTorches
  REAR_ARCHIVE
    EastWestTallShelves
    HeroChronicleShelf
    RearEndDoubleDoors
  ENVIRONMENT_LIGHTING
```

## 6. Camera acceptance gates and finish definition

| Gate | Evidence required | Pass means |
|---|---|---|
| **G0: layout** | top-down Unity/Blender view plus interior camera checks at Panels **05, 06, 08, 16, 17, 34, 36** | All show exactly **one believable continuous building**; correct front door/stained window/table/arch/runner/archive relations. |
| **G1: Rodin kit prototype** | one test Gothic arch, one bookshelf, one table, inspected from all sides and placed at true scale | No fused building, broken back geometry or unusable interaction clearance; matches art style. |
| **G2: study completed** | completed study shell/furniture/lighting, screenshot comparisons with 05–15 and 18–33 | Camera backgrounds and warm/cool lighting align without moving the entire room each shot. |
| **G3: archive completed** | walking route, book retrieval and staff return staged over 16–18 and 34–35 | Characters can travel through one arch; no occlusion blockage or teleporting props. |
| **G4: magic-ready** | book interaction geometry and separate VFX anchor verified against 20–26 and 36–37 | Book pages can animate and show controlled legible Nevali message; space for the portal effect. |
| **G5: runtime scene** | Unity Play Mode, actor animation and several camera frames, console and basic PC profiling | Set looks correct and animated in isolation; A05s device QA still separate. |

### Recommended first Rodin pilot

Build **one canonical arch module first**, using a clean crop of Panel 34/16. It is the strongest repeated geographic landmark, and its width/columns set corridor scale. Next do **one bookshelf** and the **hero study table**, place them in the correct greybox and check cameras. If any of those three fails the agreed style/topology, correct the pipeline before burning credits on twenty more objects.

**Not done by this audit:** no Rodin generations have been triggered, the current library greybox has not been spatially rebuilt, no movie/voice/dialogue shots have been finished, no new environment meshes/LODs/colliders/camera tracks imported, and PC/Android performance has not been measured.
