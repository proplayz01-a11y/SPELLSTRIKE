# SPELLSTRIKE — 42-Panel Narration-Led 3D Cinematic: Camera Direction v01

**Date:** 2026-10-23
**Status:** Direction baseline / camera shot selection finalized for blocking. Exact positions, framing and shot durations must be validated in Unity Game View against the user-finalized images; no claim of 42 animated shots or final voice timings.
**Goal:** Preserve all 42 plot beats with a concise narration-led 3D cinematic, authentic character actions and essential direct dialogue. Reuse cameras; do NOT build 42 unique camera prefabs.

## Evidence and precedence

1. Visually examined all seven contact sheets from Library/StoryboardRodinAudit_20261021/contact_sheet_01.jpg through 07.jpg, depicting the user-finalized Desktop Prologue_Panel_01.png through 42.png.
2. docs/STORY_PANEL_ASSET_MANIFEST.md defines approved shot logic, character identities, props, staging, camera matching and mandatory dialogue. User-finalized Desktop images take priority over earlier repository alternatives for Panels 04, 05 and 08.
3. docs/LIBRARY_ENVIRONMENT_BIBLE.md and the canonical top-down reference govern the actual connected library architecture, overriding inconsistent AI-illustrated background details.
4. The production StoryScene image prologue and StorySequenceController stay untouched until a full 3D version passes integration gates.
5. User's new approach: narrated 3D action; e.g. Van watches through a doorway as Nevali plays, animated with local Mixamo LLM Mocap if suitable. Narration supplements motion, never replaces meaningful 3D character action.

## Cinematography rules

- **Screen geography:** At the study table Van stays **screen-left**, Vander **screen-right**, across a consistent 180-degree dialogue axis. Dirty OTS of Van is **from behind Vander**; reverse OTS of Vander is **from behind Van**. Only motivated inserts and clear transition shots break this baseline.
- **Building geography:** Front study with the table, south-right external entry and amber stained glass; northward Gothic gateway opening directly onto the rear archive red runner. The library is ONE connected building, never a fabricated separate corridor.
- **Camera motion:** Prefer locked-off frames with slow, motivated push-ins (0.2–0.7 m over a full beat), a single rear tracking move through the gateway, and a smooth final handoff to the gameplay camera. No unmotivated drone orbits, camera shake or constant motion. War magic may have a stronger but short move.
- **Exposure / color:** Family home warm gold, battlefield charcoal with violet Broken Language, grief cool/desaturated, study amber versus archive blue, book revelation restrained cyan-white, forest hopeful daylight. Maintain facial readability in dark shots.
- **Lens reference:** 28 mm equivalent for geography, 35 mm for wide two-shots and moving action, 50 mm for ordinary conversations and book interactions, 70–85 mm for sensitive face close-ups. These are FULL-FRAME equivalents, not Unity fieldOfView values. For a standard 36×24 mm virtual sensor, rough Unity vertical FoV: 28 mm=46°, 35 mm=38°, 50 mm=27°, 70 mm=19°, 85 mm=16°. Actual crop depends on framing, aspect, sensor and scene scale; tune in Editor.
- **Image composition:** Target 16:9, keep eyes near upper third, leave lower-quarter subtitle-safe margin, maintain a clear eyeline and silhouette, do not cover key book messages with captions. Mild DOF only when the storyboard focus requires it.
- **Custom mocap:** Source reference *plate* must have a locked camera and unobstructed performer; the eventual Unity render camera may push, track or cut separately. For contact-heavy book motions record/retarget two-hand performance and correct attachment separately. Loop in-place Mixamo walking while a separate transform controls world travel; verify actual travel speed vs gait and foot sliding.
- **Narration:** Narrator bridges time, setting and unseen background. Stop or lower narration for critical physical acting, name revelation, confrontation and the forbidden spell. Keep approved words/messaging intact. Final AudioClip length is the authority for shot durations.
- **Production deadline:** With Stage 5 also pending before Wednesday Oct 28, prioritize first a story-complete rough edit, using reused 3D shots and optional approved-art narration bridges where full home/war 3D assets are not ready. That fallback is a disclosed scope compromise, never described as fully 3D.

## Finalized shot selection: 28 reusable camera setups, 42 beats

**Codes identify camera setups, not separate Timeline files.** Reuse the same named camera when a later panel says the same ID. Camera movement is a track within that setup; the shot can continue across two or more narrative panel beats.

### Act 1 — Home and Great War (Panels 01–04)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 01 | A01, **doorway layered medium-wide**, 35 mm | Van's shoulder/torso at frame-left in doorway foreground; Nevali at mid-right playing with colored wooden blocks. Van watches quietly, she visibly builds and shifts blocks. Gentle 0.4 m push toward her. No additional wife/characters. | Warm narration, block Foley, music |
| 02 | A02, **high-wide ruined-city establishing**, 28 mm | Young Van small in left/near foreground against destroyed city and abstract violet corruption. Slow lateral reveal 0.5 m of collapsed architecture; no invented named monster or readable corrupted letters. | War narration and restrained ambience |
| 03 | A03, **low rear three-quarter hero action**, 28–35 mm | From behind Van and staff, eye height ~1 m. True readable A–G word tiles lift into frame toward overwhelming shadow; short forward push and slight tilt, never hide the threat. | Casting sound takes over narration |
| 04 | A04, **still low/medium three-quarter grief**, 50 mm | Wider composition holds kneeling Van, his wife, collapsed house, blue hair ribbon and scattered toys. Start modestly wide then very slowly approach, ending static on exhausted loss; no gore. | Narration falls away into silence |

### Act 2 — Library refuge and passing decades (Panels 05–15)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 05 | B01, **inside-looking-out entrance wide**, 28–35 mm | Front-right entry door to frame-right; young Van steps inside with staff, lit by subdued afternoon; deep-frame table and central gateway remain legible. One inward dolly / reverse reveal. | Narrator introduces refuge |
| 06 | B02, **clean two-shot across study table**, 50 mm | Young Van left, Vander right, eye-level; Van speaks/gestures and Vander listens. Gentle static shot; do not invent a tabletop lantern. | Short essential dialogue plus VO |
| 07 | B03, **Young Van tight medium**, 70 mm | Van's head bows and hand reaches chest. Tight from his front-left, background soft, no other actor needed. Hold static. | Breathing, silence; narrator brief |
| 08 | B04, **Vander frontal portrait**, 70 mm | Vander centered before original shelves/navy-gold banner, contemplative hand near chin. Locked tripod, no extra props. | Quiet musical resolution |
| 09 | B05, **older Van foreground + gateway-depth composition**, 35 mm | Match-cut passage of years to same study table; older white-haired Van foreground-left, mentor just emerged through the cool archive portal deep background. Soft rack-focus from Van to Vander. | Years-later narration |
| 10 | B02, **older-era table two-shot**, 50 mm | Reuse B02 with current Older Van visual. Van speaks of dream, Vander listens, no physical apparition within Panel 10. | Van's key dialogue |
| 11 | B06, **Van-to-Nevali dirty OTS**, 50 mm | Over older Van's navy shoulder/back at frame-left, Nevali center-right near cool Gothic window, gentle golden/cyan manifestation and reaching gestures. 0.25 m careful push. Not an actual escape from spellbook. | Narration/ethereal tone, no invented words |
| 12 | B07, **Vander gateway-side medium portrait**, 70 mm | Vander front/three-quarter, chin gesture, gateway softly behind; use Panel 10/12 background, not B04 shelf/banner background. Static. | Silence / restrained room tone |
| 13 | B07, **tighter Vander contemplative crop**, 85 mm | Exact B07 camera axis, slight optical push to close-up. Eyes carry the beat; no new camera-side room. | Quiet pause |
| 14 | B02, **established same-side conversational two-shot**, 50 mm | Van left listening, Vander right explaining palm-up, neither standing. Only slight background gateway DOF. | Vander essential reply |
| 15 | B08, **wider table and arch bridge shot**, 35 mm | Van still left, Vander rises right and gestures toward the central gateway. Hold arch in deep center, enough floor to judge clear path; avoid crossing the dialogue axis. | Narrator bridges action |

### Act 3 — Archive retrieval and illustrated chronicle (Panels 16–26)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 16 | C01, **rear follow/track through actual gateway**, 35 mm | Camera from behind the pair in front study, moving physically up the red runner: Vander leads; Van follows at short distance holding exactly one canonical staff. Keep their backs readable and statues/torches off the walk aisle. | Footsteps / narrated transition |
| 17 | C02, **shelf-side medium-close with soft watcher**, 50 mm | Vander on frame-right foreground, using both hands to withdraw one heavy *closed* non-glowing volume from the real archive shelf; Older Van and his staff softly visible behind. Static then 0.2 m push toward the book. | Book handling, VO gap |
| 18 | B02, **return to exact study geography / low table two-shot**, 50 mm | Vander right lowers the same closed book to center of empty table with both hands; Van left watches; Van's staff now rests upright beside/behind his left chair. Brief dolly toward landed book. | Heavy book-set-down Foley |
| 19 | B02, **table explanation two-shot**, 50 mm | Keep previous camera and book position; Vander palm-up explanation; do not cut to a reenacted Great War. | Vander history dialogue |
| 20 | C03, **elevated book-dominant OTS insert**, 50 mm | From slightly behind Van/above tabletop, open illustrated chronicle occupies 65–70% of image. Vander's right hand points to left Great War page. Stay on this exact optical camera for Panel 21. | Page Foley, narration/dialogue |
| 21 | C03, **identical held book insert**, 50 mm | No camera move or background switch: only Vander's same pointing hand shifts to the right-page faceless entity; other hand passive. | Entity explanation |
| 22 | C04, **dirty OTS behind Vander onto Van**, 70 mm | Vander blurred small screen-right shoulder, Van sharp seated screen-left, narrowed attentive eyes; crop hides book and preserve upright Van staff by the left chair when visible. | Suspicious pause |
| 23 | B02, **two-shot on SAME 180-degree line**, 50 mm | Van left makes a restrained question gesture, Vander right listens uneasily. The *open* book is on Vander's half, not center. | Van question |
| 24 | B02, **same two-shot and geography**, 50 mm | Vander replies guardedly from right. Same camera, no dramatic whip or opposing seating swap. | Vander reply |
| 25 | C03, **return to the exact high-angle page insert**, 50 mm | Historical illustrations dissolve into clean parchment via restrained cyan-white/gold surface motes; Vander's active hand withdraws but does not point. | Narrator quiet; magic SFX |
| 26 | C03, **hold exact insert as writing appears**, 50 mm | Pages clean, illumination soft. Exact text is physically legible on pages: "Please help me. I've been trapped in this book for so long. My name is Nevali." Hold 2–3s after writing completes; NO camera shake or floating projection. | Soft page magic, silence on Nevali |

### Act 4 — Doubt, confrontation and the decision (Panels 27–33)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 27 | C04, **return to same dirty OTS as Panel 22**, 85 mm | Very slow 0.2 m push on Van; blue eyes widen and breath catches on Nevali's name. Vander only blurred right-edge cue. | Catch of breath; narrator stops |
| 28 | D01, **table-level low dirty OTS onto Van**, 50 mm | Camera behind Vander on SAME axis, only small right foreground sliver; Van rises, leans and plants both palms across table, face in sharp focus. Book message orientation faces Van, so from Vander side it reads upside-down. | Van angry demand |
| 29 | D02, **reverse dirty OTS onto Vander**, 70 mm | Camera now behind Van toward Vander and correct banner/bookshelf side wall (not gateway). Vander guarded, regretful, explains with restrained hand gesture. | Vander confession |
| 30 | D03, **motived table/message insert + Van reaction**, 70 mm | Cut book corner/detail then Van's expression from his side of axis; grief shifts toward realization. No invented vision of prison-world; let voice and book communicate it. | Reveal dialogue |
| 31 | D04, **clean single Van face**, 85 mm | Slow controlled eye-level push as fear becomes resolve. Do not rotate around his head. | Van demands a way in |
| 32 | B08, **wide 2-shot with arch readable**, 35 mm | Vander stands screen-right, partly turns toward gateway; seated Van left. Reintroduce travel destination. | Forbidden warning |
| 33 | B08, **same-side standing two-shot**, 50 mm | Both now upright across the table, mutually facing. Do not switch sides; hold 1s after Van's acceptance. | Van's decision |

### Act 5 — Staff retrieval, spell and book entry (Panels 34–37)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 34 | E01, **locked symmetrical one-point gateway master**, 35 mm | Camera from study facing gateway; Van foreground left, the book open on table. Empty-handed Vander walks from right toward the gateway 1–3 steps beyond threshold. KEEP EXACT camera transform for Panel 35. | Vander's staff-retrieval line; footsteps |
| 35 | E01, **EXACT SAME camera, matched return**, 35 mm | Vander approaches from gateway carrying his own crown-height gold-cage / pale oval staff. Van foreground-left unchanged, own staff beside chair. No portal or casting yet. | Footsteps; tense music |
| 36 | E02, **ceremonial low three-quarter master / action triangle**, 35 mm | Screen-right Vander's gold cage staff, tabletop open book central, screen-left Van gripping his DIFFERENT cyan-diamond staff. Camera slowly pushes into staff→book→Van arc, filament white/cyan/gold, compact gutter portal. No purple circles or early transport. | Warning line, casting sound |
| 37 | E03, **slightly higher three-quarter to portal**, 35–50 mm | Preserve Vander solid at right and Van shocked left; aim his downward/right gaze toward center gutter. A continuous navy/gold/cyan ribbon draws 60–75% of Van + separate rigid staff into book; rack focus to portal; cut at peak. | One strong magical pull, transition |

### Act 6 — Forest wake-up and gameplay handoff (Panels 38–42)

| Panel | Camera ID / framing / lens | Direction, action and camera move | Sound |
|---|---|---|---|
| 38 | F01, **low ground-level three-quarter**, 50 mm | Existing TutorialForest grass clearing. Van on forearm, sunlight catches face and eyes; his intact tall staff lies nearby. Start at low camera height ~0.6 m; minimal push. | Breeze, birds, waking breath |
| 39 | F02, **true supine first-person POV**, 35 mm | Camera exactly from Van's eyes looking upward; two hands rise, one shielding sunlight; conifers/clouds frame sky. No staff within POV. | Soft wind, cloth |
| 40 | F03, **standing medium three-quarter**, 50 mm | Van stands, dusts off robe with two hands; staff still on grass. Framing holds hands, robe and staff simultaneously. | Brush cloth and grass |
| 41 | F04, **low close / grip insert**, 50 mm | From near grass at 0.7 m, Van bends, braces hand on knee and retrieves one full-length staff. Lens isolates grip and correct cyan diamond; no floating staff or truncated scale. | Pickup / wood Foley |
| 42 | F05, **rear wide follow to route**, 28–35 mm | Van left of center, staff held, modest forest route toward partly obscured low bridge at center-right. Camera gently settles behind his shoulder and visually matches the eventual gameplay third-person camera; transfer control after Timeline ends. | Narration resolves; music carries into Tutorial |

## Shot count and reusable Timeline camera anchors

- 42 **story beats**, approximately **28 camera setups**: A01–A04 (4), B01–B08 (8), C01–C04 (4), D01–D04 (4), E01–E03 (3), F01–F05 (5).
- Most valuable camera reuses: **B02** study-table two-shot (Panels 06, 10, 14, 18, 19, 23, 24), **B08** arch-visible two-shot (15, 32, 33), **C03** book insert (20, 21, 25, 26), **C04** Van reaction (22, 27), and **E01** departure/return (34, 35).
- Use one camera per shot ID only in an *isolated cinematic scene*; for zero-drift shot continuity reuse its exact Transform rather than visually matching a second independent camera.
- Shots can share a single Timeline Asset or be arranged in small chapters. Never create both a Camera activation track and a competing camera selection system simultaneously.

## Practical production priority through Wednesday Oct 28

1. **Lock book and late-story shots first:** C01–C04, D01–D04, E01–E03, F01–F05 use existing StudyArchive / TutorialForest and shared Van/Vander art. Physical staff hand contacts and the single moving book are biggest blockers.
2. **Lean early library:** B01–B08 using the one existing library; keep narration-led pauses, reuse frames and camera crops.
3. **Home/war opening are asset-constrained:** A01–A04 are creative specifications, not verified built locations. If the deadline forces an approved-art narrated montage, mark it explicitly as a temporary hybrid. Do not falsely claim the whole prologue is fully 3D.
4. **Stage 5 remains more critical than optional shot polish.** Prioritize working prologue→tutorial handoff, playable final encounter and build validation over adding more dramatic camera moves.

## Next actual Unity-camera validation

- Set up camera markers in a separate layer under the isolated Storyboard3D cinematic scene; do not move canonical furniture, roof or gameplay scenes.
- For each reference, capture a 16:9 Game View comparison to the user-finalized original and check actor silhouettes, staff count, book orientation and environment continuity.
- Validate **B02, B08, C01–C04, E01–E03** first; do not fabricate exact camera positions without seeing the imported actor rig heights / model pivots in Unity.
- Record one locked-camera actor reference plate for Mixamo LLM Mocap only for motions stock Mixamo cannot supply (e.g. Nevali stacking toys, both hands taking/setting book, Van grief); export usable retargets first, then tune Unity camera movement afterward.
- Run full shot playback after narration timing is available. Recheck roof clipping, spring arm collisions, one active AudioListener, subtitle margins, no black screen when PlayableDirector finishes, skip/replay, Android memory, and no premature gameplay unlock.
