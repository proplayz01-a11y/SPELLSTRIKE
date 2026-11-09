# SPELLSTRIKE Story Panel Production Plan

**Status:** Active planning and revision schedule  
**Default daily cap:** Five numbered storyboard panels; explicitly overridden
for the current Day 2 batch to cover Panels 10-15.

## Finalized Production Format

- The prologue uses **comic-page presentation**, not one Unity screen for every
  individual illustration.
- Each illustration is generated separately without embedded text.
- Character, prop, environment, and style continuity are audited before an
  illustration is allowed into a page.
- Approved illustrations are assembled manually in Canva using controlled
  gutters, speech bubbles, narration, typography, and impact text.
- Each completed comic page is exported as a 16:9 landscape image for Unity.
- Unity advances by finished comic page. Progressive reveals or motion effects
  may be added later, but they are not required for the first working layer.
- The approved grouping is 42 source panels into 21 finished Unity pages. The
  exact mapping and Page 1 gate are locked in
  `docs/STORY_UNITY_PAGE_LAYOUT_PLAN.md`.

## Reference Authority

1. Approved Panels 1-4 and 11 establish approved story-art quality.
2. Panel 11 is the visual-style target for revised later panels.
3. Character reference sheets control Young Van, Older Van, Vander Poel, and
   Nevali identity.
4. The top-down library map controls architecture and object placement.
5. The accepted oblique library image is a camera/depth reference only; the
   top-down map wins whenever the two disagree.
6. Van Aert's canonical staff controls all staff appearances.
7. Vander Poel's staff uses the approved 360-degree reference
   `Assets/Story/References/Vander_Poel_Staff_360_REFERENCE.png`. Every panel
   containing that staff must load the turnaround as a
   mandatory object reference; it may never be replaced by Van Aert's staff.
   Its locked scene scale is approximately 1.0× Vander's standing height:
   ferrule at floor level, circular-cage top aligned with his crown, and cage,
   crystal, shaft, and ferrule proportions unchanged across camera angles.

## Whole-Frame Style Consistency Rule

Panel 1 V2's rendering language applies to **every visible element**, not only
Young Van. All characters, faces, hair, beards, robes, furniture, stonework,
books, props, lighting, and background architecture must share the same clean
outline weight, simplified readable shapes, and controlled cel-shaded value
structure. Character sheets control identity and costume but may not introduce
a more painterly or realistic rendering style into the panel.

Reject a panel when one character or the environment appears rendered by a
different artist, even if the character identity itself is accurate.

## Strict Environment Reuse Rule

Before presenting any generated library panel, compare every visible
environment element against the canonical top-down map and the immediately
preceding approved panel in the same location. Do not introduce a window,
plant, flower, banner, shelf arrangement, lamp, table prop, doorway, arch, or
other background element merely because it appears in a style reference.

Style references control rendering only. They never authorize copying their
environment objects. When a new camera cannot prove that an object belongs in
the canonical room, omit it. For consecutive shots at the same table, reuse
the prior approved shelf wall, banner/sconce arrangement, tabletop props, and
lighting unless the story explicitly changes them.

## Previous-Panel Background Audit Gate

Before generating a panel, inspect the immediately preceding approved panel
and classify its background as one of:

- **Reuse Exact** - same continuous moment/camera family; the prior background
  is the new panel's primary environment authority.
- **Reuse With Justified Camera Change** - same location but a story-required
  camera move; only landmarks physically visible from the new camera may vary.
- **New Zone Required** - the story explicitly moves to a different canonical
  room/zone.

Record the classification in the panel manifest before generation. Do not
default to an older reusable background merely because it is visually clean.
For reaction shots continuing the previous conversation, use **Reuse Exact**
unless the storyboard explicitly shows a camera cut to another side.

## Cinematography Lock Gate

Before every new panel generation, discuss and explicitly lock the shot design
with the user. At minimum record:

- shot type, such as establishing, centered two-shot, dirty OTS, reverse OTS,
  clean single, reaction close-up, or insert;
- camera position, side of the 180-degree axis, height, and shot size;
- foreground framing and maximum foreground-character coverage;
- primary subject and focal plane / depth-of-field falloff;
- table, chair, prop, and environment geometry visible in-frame;
- screen direction, eyeline match, and relationship to the previous shot;
- approved references controlling composition, identity, style, props, and
  architecture.

Do not generate until the shot type and spatial blocking are approved. Movie or
photographic references control cinematography only and never authorize copying
their actors, costumes, props, lighting, or environments.

### Mandatory Cinematography Research — Panel 25 Onward

For Panel 25 and every later panel, research real film/cinematography examples
before locking a new shot. Present two or three relevant examples when practical
and explain the camera position, shot size, 180-degree axis, eyeline, foreground
layering, table/environment geometry, and depth-of-field treatment. The user
selects or approves the shot grammar before image generation begins.

Panels 25 and 26 were already source-approved before this rule was formalized;
classify their existing book-dominant framing as high-angle insert shots without
reopening them unless the user explicitly requests a revision. Begin the full
research-first gate with Panel 27. Localized corrections that do not change the
camera or shot grammar do not require fresh research.

## Daily Batches

### Day 1 - Panels 5-9

- [x] Panel 5 shot plan, prompt, generation, audit, and source approval
- [x] Panel 6 shot plan, prompt, generation, whole-frame style correction, audit, and source approval
- [x] Panel 7 story beat, prompt, generation, audit, and source approval
- [x] Panel 8 frontal shelf-wall revision, object cleanup, audit, and source approval
- [x] Panel 9 camera/story/environment/anatomy/focus revisions and V15 source approval

### Day 2 - Panels 10-15 (Explicit Six-Panel Exception)

- [x] Panel 10 dream-conversation Candidate V2 source approved
- [x] Panel 11 source artwork and immediate post-Panel-10 sequence placement approved
- [x] Panel 12 exact Panel 10 gateway-background reuse Candidate V4 source approved
- [x] Panel 13 tighter contemplative continuation source approved
- [x] Panel 14 same-table Vander response shot source approved
- [x] Panel 15 decision-to-move gateway transition source approved

### Day 3 - Panel 16 Extension

- [x] Panel 16 gateway-threshold walking shot source approved

### Day 4 - Panels 17-21

- [x] Panel 17 ancient-volume retrieval Candidate V3 source approved
- [x] Panel 18 same-book placement at the established study table source approved
- [x] Panel 19 Vander explanation shot source approved
- [x] Panel 20 matched-pair Great War pointing Candidate V2 source approved
- [x] Panel 21 matched-pair black-robed-entity pointing Candidate V1 source approved

### Next Story Batch - Panel 22 Onward

- [ ] Lock the remaining panel count before generation
- [x] Panel 22 cinematic dirty-OTS Candidate V4 source approved as v2
- [x] Panel 23 original Candidate V6 restored as source approved after comparison with later variants
- [x] Panel 24 original Candidate V1 source approved after comparison with later variants
- [x] Panel 27 researched dirty-OTS reaction close-up Candidate V1 source approved
- [x] Panel 28 researched confrontation Candidate V2 source approved after exact Panel 26 message and Van-facing book orientation correction
- [x] Panel 29 reverse dirty-OTS explanation Candidate V6 source approved; exact Panel 8 shelf-wall arrangement replaces the impossible repeated gateway background
- [x] Panel 30 researched book-dominant reaction insert Candidate V1 source approved with the exact Nevali message preserved
- [x] Panel 31 researched clean decision close-up Candidate V1 source approved
- [x] Panel 32 researched asymmetric medium-wide warning Candidate V2 source approved with corrected Van-facing book orientation
- [x] Panel 33 researched wider profile acceptance two-shot source approved after reciprocal-eyeline correction

**Production pause:** All story-panel work stopped on 2026-08-29 at the user's
request due to an active power/flood risk. Do not resume generation, editing,
planning, or additional panel work until the user explicitly returns and asks
to continue.
- [x] Vander's guarded answer and living-spellbook explanation begins in Panel 29
- [ ] Nevali voice/message interruption
- [x] Van's confrontation and spellbook-prison explanation completed through Panels 28-30
- [x] Forbidden-technique agreement completed through Panels 32-33
- [x] Panel 34: centered one-point-perspective departure Candidate V1 source approved; Vander goes through the gateway empty-handed to retrieve his staff
- [x] Panel 35: exact-camera matched return Candidate V2 source approved with Vander's distinct staff locked to approximately his standing height
- [x] Panel 36: revised forbidden casting activation Candidate V3 source approved; Van holds his canonical staff and the storybook portal opens while he remains fully physical
- [x] Panel 37: spaghettified book-entry Candidate V3 source approved with shocked portal-directed gaze and Van's staff traveling with him

**Current source-art position:** Panels 1–42 are source approved through Van's
entry into the book, awakening in sunlight on the Tutorial map, brushing dirt
from his robe, retrieving his staff, and facing the route toward the bridge.

- [x] Panel 38: TutorialForest wake-up Candidate V5 source approved with full-length transported Van staff and coherent sunlight response
- [x] Panel 39: supine first-person sunlight POV Candidate V2 source approved; hands, clouds, and trees only, with no staff
- [x] Panel 40: standing robe-cleanup Candidate V1 source approved with staff resting intact nearby
- [x] Panel 41: staff-retrieval Candidate V2 source approved
- [x] Panel 42: previously approved rear-route/bridge artwork restored unchanged as the final illustrated handoff

**Completed source-art milestone:** Panels 1–42 are source approved through the
Tutorial-map handoff. The next art task is a focused Young Van visual-consistency
revision, followed by the complete continuity, dialogue/caption, page-turn, and
Canva page-assembly audit—not another numbered panel unless that audit proves a
missing beat.

Out-of-order composition-locked candidates already generated:

- [x] Panel 25 history-dissolution Candidate V1 source approved
- [x] Panel 26 exact handwritten Nevali-message Candidate V2 source approved

### After the Final Prologue Panel - Page Assembly Lock

- [ ] Final panel-order and continuity pass
- [x] Group approved illustrations into 19 comic pages
- [ ] Assemble and validate Unity Page 1 before integrating Pages 2–21
- [ ] Lock dialogue, narration, speech-bubble ownership, and page-turn points

The default cap prevents rushed batch generation. The user explicitly approved
Panels 10-15 as a six-panel Day 2 exception. A panel that fails continuity
remains in its current daily batch until corrected or explicitly deferred.

After the user explicitly approves a panel, promote it to source-approved,
update the trackers, and immediately start the next numbered panel in the
current daily batch without waiting for a separate proceed instruction.

## Solo Contemplation Focus Rule

For every solo contemplation or reaction shot, the character is the sole sharp
subject. Apply restrained optical depth-of-field blur to the background while
keeping the established architecture recognizable and geometrically unchanged.
Do not replace that camera treatment with Gaussian smear, fog, painterly loss of
detail, or a newly invented background. Record the immediate previous-panel
background authority before generation.

## Panel 5 Planning Card

### Story Function

Transition directly from the Great War, destroyed home, and loss in Panel 4
into Vander Poel's library sequence. Young Van Aert seeks refuge with Vander
after the war, after losing his wife and failing to find Nevali.

### Locked Content

| Field | Direction |
| --- | --- |
| Story beat | Young Van enters Vander Poel's library seeking refuge immediately after the Great War and the loss. |
| Characters | Young Van only. Vander does not appear yet. |
| Library zone | Lower-right exterior entry opening into the front study chamber. |
| Intended comic frame | Wide establishing panel; suitable as the full-width opening panel of a comic page. |
| Camera | Interior three-quarter view aimed toward the lower-right door, while preserving enough table/room landmarks to identify the canonical library. |
| Van identity | Black slightly messy hair, blue eyes, lean young build, no beard, canonical navy-and-gold robe and circular chest fastening. |
| Van pose | Entering slowly or pausing just inside the threshold; staff carried naturally, shoulders lowered. |
| Expression | Exhausted, grieving, restrained, and searching for help; no theatrical sobbing or heroic stance. |
| Lighting | Cool exterior light behind Van; limited warm amber library light ahead, suggesting the first movement toward hope. |
| Art style | Approved Panel 1 V2 is the primary style and Young Van authority for Panel 5: clean bold outlines, bright readable color separation, simplified cinematic shapes, and polished fantasy-comic rendering. Panel 11 remains a secondary global continuity reference. |
| Text policy | No generated text or bubbles. Narration/dialogue is added later in Canva. |

### Required Visual References

- `Assets/Story/References/Library_TopDown_Layout_REFERENCE.png`
- `Assets/Story/References/Library_Left_Oblique_CAMERA_REFERENCE.png`
- `Assets/Story/Approved/Prologue_Panel_01_V2_SOURCE_APPROVED.png`
- `Assets/Story/Approved/Prologue_Panel_11_SOURCE_APPROVED.png`
- young Van Aert character sheet from the Global Visual Rules
- canonical Van Aert staff reference

### Prohibited

- Older Van, Vander Poel, Nevali, or an unrelated character
- generic corridor replacing the canonical front study chamber
- relocated door, window, table, arch, rug, or shelving
- bright triumphant arrival
- duplicate staff, altered crystal, extra weapon, embedded text, speech bubble,
  caption, watermark, or random writing

### Pending Before Generation

- Final Canva narration or dialogue for Panel 5
- Exact crop allocation inside the first library comic page, beyond the locked
  full-width establishing-panel direction
