# SPELLSTRIKE Story/Cutscene Foundation Handoff

**Status:** Planning approved; no story runtime code or scene has been created.

## Current Development Position

- Stage 1 shared post-stage review: validated with four questions and four indicators.
- Stage 2 PC vertical slice: validated end-to-end through Stage Select,
  Node 1 -> Node 2 -> ERODE -> Node 3 -> Node 4 -> Drowned Captain,
  Stage Complete, five-question review, and Stage Select return.
- Stage 2 includes validated ERODE environmental vocabulary targeting, boulder
  fracture, Tidecarved Shell pickup/persistence, and Steady Current mitigation.
- The next development priority is story/cutscene foundation. Android build and
  device validation is priority two.

## Scope Decisions

- Remove the pre-test/post-test system from the proposal and implementation
  scope. Evaluation is functionality, usability, and user experience; do not
  claim measured vocabulary improvement.
- Remove mandatory tile-based missed-word retry from the proposed review flow.
  Retain Teach, Target Challenge/combat retrieval, contextual review,
  immediate explanatory feedback, summary, and durable attempt records.
- Retain the four intended attack tiers as required future scope:
  Word Cast, Spell Strike, Arcane Surge, and Absolute Word. Damage bands exist;
  differentiated VFX/behavior remains future work.
- TD-019 future-pool refill planning is deliberately deferred. Preserve the
  validated exact-16 UI-plus-pending-world-tile lifecycle.

## Canonical Narrative Source

Read before authoring story content:

`C:\Users\David\Downloads\SPELLSTRIKE_STORY.docx`

Key narrative facts:

- Van Aert enters the living spellbook-prison to recover fragments of Nevali.
- Vander Poel / Master Wizard and Broken Language are central antagonistic
  forces.
- Fragment sequence: Stage 1 Voice, Stage 2 Memories, Stage 3 Emotions,
  Stage 4 Identity, Stage 5 Soul.

## Locked Prologue Panels

Panels 1-42 are source approved through Van's entry into the living book,
TutorialForest awakening, robe cleanup, staff retrieval, and final rear
route/bridge handoff before the live `IntroWelcome` tutorial state. Panel 5 is the immediate
post-Great-War transition in which Young Van seeks refuge at Vander Poel's
library after losing his wife and failing to find Nevali.
The earlier Panel 5-11 numbering was superseded by explicit user correction on
2026-08-26. Do not infer the new Panel 6 onward sequence; lock each submitted
panel in order before generating replacements.

Panels 1, 2, and 4 use explicit approved v2 replacements; Panel 3 remains
approved. The sequence is family memory, Great War devastation, Great War
combat/Word magic, return home, then Young Van seeking refuge at the library. The
Panel 6 is source approved as Young Van telling Vander Poel about the loss of
his wife and Nevali; Van speaks and Vander listens. Panel 7 is source approved
as Young Van's silent grief reaction at the canonical study table. Panels 8 and
9 are source approved. Panel 8 shows Vander silently considering Young Van's
confession at the straight shelf-wall study table. Panel 9 is the decades-later
time jump: Older Van is the sharp foreground subject while Vander and the
canonical library background are depth-of-field softened. Panel 10 is source
approved as Older Van telling Vander about the dream. Panel 11 is source
approved and placed immediately after Panel 10 as Nevali's magical-contact
confirmation. Panels 12-13 are approved solo Vander contemplation shots using
the same gateway background with restrained depth-of-field. Panel 14 returns to
the established same-table two-shot; Panel 15 initiates movement and restores
Van's canonical staff; Panel 16 carries both men across the same gateway
threshold without redesigning the archive. Panel 17 retrieves the ancient
volume; Panel 18 places it on the study table; Panel 19 begins Vander's history
explanation. Panels 20-21 form a matched book-dominant pair in which the same
active hand points first to the Great War and then to the black-robed entity.
Panels 22-24 cover Van's suspicion/question and Vander's guarded answer using
the centered study-table camera. Panels 25-26 form a matched book transition:
the history dissolves, then Nevali's complete handwritten message appears.
Panels 27-37 complete recognition, confrontation, living-prison explanation,
forbidden-technique agreement, Vander-staff retrieval, casting, and Van's
book-entry beats. Panels 38-39 complete the actual TutorialForest wake-up and
bridge-route handoff with Van's transported staff. Final continuity audit,
Canva page assembly, standardized Unity exports, and runtime implementation
remain.

## Visual Reference Authority

- The preserved Global Visual Rules PDF governs global style, character identity
  sheets, and staff constraints. Its stale old-Van black-hair wording is
  overridden by its visual sheet and approved continuity: older Van has white
  hair and a tapered white beard.
- The preserved top-down library image is the final spatial-layout authority:
  lower-right exterior door, lower-center stained-glass window and banners,
  front study room/table, flanking shelves, central arch, red carpet, and rear
  archive room. Future library panels must preserve these relationships. It
  does not govern the final library's camera, lighting, or visual treatment.
- Approved Panel 11 is the visual-style target for all later-panel revisions.
  Its setting and character blocking are not automatically reused.
- The finalized Vander Poel character sheet is the current character-appearance
  reference: loose white/grey hair, broad white beard, glasses, teal mantle,
  blue diamond chest gem, and frail scholarly posture.
- `docs/LIBRARY_ENVIRONMENT_BIBLE.md` is the canonical continuity brief for
  every prologue panel set in Vander Poel's library.

## Chosen Technical Direction

Use one reusable **2D illustrated StoryScene**, not a one-off sequence and not
a heavy 3D Timeline-first solution. The player advances finished 16:9 comic
pages assembled in Canva from individually generated and continuity-approved
illustrations. One illustration does not automatically equal one Unity page.

```text
Main Menu Adventure
  -> StoryScene (first-time Prologue)
  -> TutorialScene (if tutorial incomplete)
  -> StageSelectScene
```

Later stage ending sequences reuse StoryScene with different data profiles.
Panel production is capped at five numbered panels per development day; see
`docs/STORY_PANEL_PRODUCTION_PLAN.md`.

The approved Prologue grouping is **21 finished Unity pages from 42 source
panels**. `docs/STORY_UNITY_PAGE_LAYOUT_PLAN.md` is the canonical page map,
hierarchy, interaction contract, import baseline, and Page 1 validation gate.

## Current Integration Gaps

- `AdventureButtonHandler` currently loads `StageSelectScene` directly.
- `TutorialScene` is not currently included in Build Settings.
- `TutorialDirector.onTutorialCompleted` has no assigned destination.
- No durable story-sequence completion state exists.

## Proposed Runtime Contracts

### Story Data

Create `StorySequenceProfile` and page records containing:

```text
sequence ID
page ID
finished page sprite
optional narration/audio
allow skip
next scene
completion key
```

### Story UI

```text
StoryScene
  -> StorySequenceController
  -> StoryCanvas
      -> PanelImage
      -> CaptionPanel / SpeakerText / DialogueText
      -> ProgressText
      -> click/tap continue receiver
      -> SkipButton
  -> AudioSource
```

### Input and Safety

- Click/tap anywhere, Enter, and Space advance one panel.
- Fade transitions use unscaled time.
- Skip marks the sequence complete and continues to the configured next scene.
- Missing artwork must show the caption and log an error, never soft-lock.
- Avoid embedding dialogue text in generated images; render it through Unity UI.

## Planned Files

### New

- `Assets/Scripts/Story/StorySequenceProfile.cs`
- `Assets/Scripts/Story/StorySequenceController.cs`
- `Assets/Scripts/Story/StorySequenceRequest.cs`
- `Assets/Scripts/Story/AdventureFlowRouter.cs`
- `Assets/Editor/Story/StorySceneSetupMenu.cs`
- `Assets/Data/Story/Prologue_Sequence.asset`
- `Assets/Scenes/StoryScene.unity`
- `Assets/Story/Prologue/Panel_01.png` through final panel assets

### Modified

- `Assets/Scripts/AdventureButtonHandler.cs`
- `Assets/Scripts/GameDatabaseManager.cs`
- `Assets/Scripts/Tutorial/TutorialDirector.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- relevant canonical docs after validation

## First Sprint Acceptance Tests

1. Fresh save -> Adventure opens Panel 1.
2. Panels 5-11 retain their locked beat order.
3. Panel completion -> TutorialScene.
4. Tutorial completion -> StageSelectScene.
5. Restart -> completed prologue does not replay.
6. Skip -> completion persists and routes correctly.
7. Missing panel sprite -> text fallback; no soft lock.
8. Rapid inputs cannot advance more than one panel per transition.
9. Main Menu Minigames remains unaffected.

## Recommended First Action in the New Chat

"Read docs/STORY_CUTSCENE_IMPLEMENTATION_HANDOFF.md and the canonical
SPELLSTRIKE_STORY.docx. Audit Panels 1-4 and Panels 12 onward, then propose the
final prologue panel manifest without changing locked Panels 5-11."
