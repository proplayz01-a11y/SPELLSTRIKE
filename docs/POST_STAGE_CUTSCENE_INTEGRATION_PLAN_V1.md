# SPELLSTRIKE post-stage and ending cutscenes — candidate v1

**Date:** 2026-10-19. **Status:** narrative and implementation proposal;
**no illustrations, final pages, or StoryScene routing have been created by
this document**. Gameplay completion and story approval are prerequisites.

## Locked foundations

- User-directed current Adventure: **Stage 1, Stage 2, Stage 3, Final Stage**
  (four playable stages total, no standalone Cursed Gothic chapter).
- Canonical fragment meanings from the original story source:
  **Voice → Memories → Emotions → Identity → Soul**. The shorter adventure
  must retain all five narrative functions. A proposed mapping is:
  Stage1 Voice, Stage2 Memories, Stage3 Emotions, Final gauntlet Identity,
  Vander Poel finale Soul. This mapping remains **candidate**, not a
  silently approved story rewrite.
- Reuse the authored 2D comic-page `StoryScene` and
  `StorySequenceController` (Back/Next, page-progress, confirmed Skip,
  fade, keyboard/tap, missing-art fallback, safe-area) instead of adding
  multiple cutscene Scenes or a 3D Timeline-only system.
- Keep original `Prologue_Sequence.asset`, its 28 final Canva pages,
  `SpellStrike_PrologueCompleted` persistence, and TutorialScene route
  untouched.
- New final **1920×1080 (16:9)** page composites use approved fantasy-comic
  art direction: bold ink outlines, painterly cel-shaded detail, coherent
  cyan-and-aged-gold magic, legible visual focus. Individual source
  illustrations remain separately reviewable; captions and typography
  belong in the compositing workflow, not baked into image-generation prompts.
- Use approved canonical source images in `Assets/Story/References/`
  and `docs/STORY_PANEL_ASSET_MANIFEST.md`; the approved Panel 11 controls
  *visual style* only. Vander Poel: silver/grey loose hair, full white beard,
  glasses, teal/gold mantle, blue chest gem. His staff: twisted dark wood,
  aged gold circular cage, one pale oval crystal, scaled around his height.
  Do not substitute Van's blue-diamond staff or use rejected candidate art.

## Proposed beat sheets (not yet approved)

### Post-Stage-1: Voice

1. Defeated Hollow Knight's arena settles; fragment emerges above the
   darkened courtyard, Van grounded in the foreground
2. Cyan fragment resolves into a brief, **non-identifying** echo of Nevali's
   voice; visual sound/memory motif rather than fabricated new dialogue
3. Van grips his canonical staff as the spellbook pages change; the
   voice points toward a submerged memory, motivating Stage 2

### Post-Stage-2: Memories

1. The Drowned Captain falls; sea-borne runes rise as the Memories fragment
2. A visual glimpse of the already established family warmth, with Nevali's
   identity consistent with approved references and no invented new family
   members
3. Water imagery folds into desert manuscript pages, motivating Mythic Ruins
   without revealing the entire final antagonist confrontation early

### Post-Stage-3: Emotions

1. Titan Earthbreaker dies after the genuinely completed player fight;
   energy is released without changing the approved death animation
2. Van receives Nevali's emotional resonance and feels her fear/hope; use
   light/pose to convey it, not an unsupported spoken confession
3. A fragment of Vander Poel's previously established seal becomes
   recognizable to Van, motivating the final confrontation
4. The living book reveals the entrance to the final archive/gauntlet

### Final Stage ending: Identity and Soul

1. Upon real completion of the remnant boss gauntlet, Van recovers the
   **Identity** revelation, linking the fragments without a new fake boss
2. Vander Poel confronts Van in the final sanctum. His motive, confession,
   punishment/redemption and actual dialogue must follow the approved
   canonical story, not speculation
3. After the genuine Vander victory condition, the **Soul** fragment is
   released
4. Nevali's recovery and father–daughter resolution, preserving age and
   outfit continuity with prior approved panels
5. Quiet epilogue closing the spellbook and credits/end route, as determined
   by final narrative approval

No panel in the above list is declared source-approved or Unity-import-ready.
The approved prologue's production cap of **up to five newly numbered source
panels per day** remains a useful art-review discipline.

## Exact existing technical constraint

`Assets/Scenes/StoryScene.unity` has ONE serialized
`StorySequenceController` with `Prologue_Sequence.asset`. Its current
`StorySequenceController.Start()` always loads that serialized profile.
Therefore **do not send a post-stage Continue directly to `StoryScene`
today**: it will replay the Prologue and route to TutorialScene.

`StageCompleteUI.OnContinuePressed()` currently records completion, queues
the mandatory review and loads `StageSelectScene` directly.
`StageSelectManager.TryBeginPendingVocabularyReview()` blocks selection and
only clears the review key after the actual review completion callback.
`StageCompleteUI` was tightened on 2026-10-19 to persist this pending key
before granting the new stage unlock, even if a user quits on the clear modal.

## Proposed route when approved panel assets exist

| Event | Route | Must retain |
| --- | --- | --- |
| Stage 1 legitimate clear | StageComplete → PostStage1 Voice sequence → StageSelect mandatory review 1 → Stage2 selection | Unlock record and true Stage1 review |
| Stage 2 legitimate clear | StageComplete → PostStage2 Memories sequence → StageSelect mandatory review 2 → existing Stage3 loadout route | Stage2 Pearl/rewards and genuine review |
| Stage 3 legitimate clear | StageComplete → PostStage3 Emotions sequence → StageSelect mandatory review 3 → final-stage loadout | Titan/Node4 fragment and genuine review |
| Final Stage legitimate clear | Final completion/review 4 **with a real configured review profile** → FinalEnding Identity/Soul sequence → actual enabled Main Menu / credits scene | Final victory, review, replay protection; no deadlocking pending4 |

Implementation should introduce a small **durable sequence request** distinct
from Prologue completion state, with explicit sequence IDs and independent
profiles `PostStage1_Voice_Sequence`,
`PostStage2_Memories_Sequence`,
`PostStage3_Emotions_Sequence`,
`FinalEnding_IdentitySoul_Sequence`.
`StorySequenceController` resolves the request *before* validating and
displaying the page sequence. A legitimate Skip follows the SAME validated
destination and completion policy as Next on the last page.

Review ownership is not transferred to StoryScene. Stage 1–3 post-stage
sequences return to StageSelect with the already-persisted pending review.
The Final Stage must have an actual stage4 profile and explicitly handle
review4 before ending to prevent a permanent StageSelect lock.

**Restart and failure invariants:**

1. Stage completion, pending review and pending story request are each
   independently durable, using different keys; no new code may clear the
   Prologue key or existing saves.
2. Do not mark a story sequence complete until a nonempty, enabled scene
   destination has been validated; a broken destination cannot silently
   award progression.
3. If the app quits midway through a story, the requested sequence resumes
   deterministically or offers an explicitly safe replay; no accidental
   Prologue replay.
4. If the app quits after the ending but before final review confirmation,
   the **review** remains mandatory. No skipping review through story UI.
5. Validate Back, Next, Skip confirmation, rapid taps, 16:9 and 20:9,
   missing-art fallback, Unity Editor, physical A05s, save/restart and
   StageSelect unlock/checkmark before declaring complete.

## Dependencies

- Genuine Stage 1–3 gameplay and stage-clear regression, including the new
  review-pending safety fix
- Final stage scene, all bosses including Vander, Stage4 review profile and
  fully migrated four-stage StageSelect
- Canonical ending narrative and character reference approval
- Image generation, source-panel approval, composed pages, sprite import,
  sequence profiles and guarded Unity routing integration

**Do not implement placeholder production routing** that plays the old
Prologue or jumps around required vocabulary assessments.
