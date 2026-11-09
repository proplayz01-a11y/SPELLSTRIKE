# Vocabulary Review Asset Pack — Isolated Unity Integration QA

Date: 2026-10-17. Project: SPELLSTRIKE, Unity 6.3 LTS.

## Scope and preservation

Integrated the approved `Spellstrike_Vocabulary_Review_Asset_Pack_v1/PNG`
22-item set ONLY in `Assets/Scenes/StageSelectBook_VocabularyReviewArt_Isolated.unity`.
The QA reconstruction and contact sheets were NOT imported. Each production
PNG uses Unity Sprite (2D and UI), Single. The original Stage Select scene,
`StageSelectBook_ReferenceFix.unity`, `ItemSelectionScene`, `SampleScene`,
vocabulary profiles, PlayerPrefs keys and stage progression were not edited.
The original ReviewRoot, one StageVocabularyReviewController and its serialized
TextMeshPro / Button / CanvasGroup references were retained.

Preintegration backup:
`Library/VocabularyReviewV1_Backup_20261017/`.

## Files

- `Assets/Scenes/StageSelectBook_VocabularyReviewArt_Isolated.unity`
- `Assets/Sprites/VocabularyReviewV1/` (22 PNG and .meta files)
- `Assets/Scripts/VocabularyPractice/StageVocabularyReviewArtSkin.cs`
- `Assets/Scripts/VocabularyPractice/StageVocabularyReferenceFit.cs`
- `Assets/Scripts/VocabularyPractice/StageVocabularyReviewController.cs` (small
  opt-in visual hooks and idempotent event listener binding)
- `Assets/Editor/StageVocabularyReferenceArtBuilder.cs`
- This report and brief entries in `docs/PROJECT_STATUS.md` and
  `docs/CHANGELOG.md`.

## Reference implementation

Reference composition is 1672×941 proportionally fitted into a 2400×1080
Game View. Uncovered exterior is black. Main navy panel, thin gold border,
header divider, underline, four parchment plaques with separate live-letter
badges/text, diamond progress row and Continue art match the supplied pack.
Existing Cinzel/Alegreya TMP fonts provide live labels and variable strings.
The derived feedback panel occupies the question/header region so the
correct/incorrect plaque states remain visible underneath. The summary remains
under the same existing controller, with live score and needs-review fields.

## Validation (actual Editor Play Mode)

- Unity scene project identity confirmed at
  `C:/Users/David/PROTOTYPE/SPELLSTRIKE/Assets`.
- Compiled corrected builder and scripts; 22 PNG Sprite/Single importers checked.
- Stage 1 profile: live MITIGATE question, four shuffled profile answer
  sentences, 1/4 progress. Button onClick invokes feedback and NEXT QUESTION,
  with incorrect answer sprite, correct answer sprite and live explanation.
  Continue onClick advances to the next actual profile word.
- Correct answer variant and feedback tested; Stage 1 final summary state was
  reached without invoking final completion/navigation.
- Stage 3 profile: real seven-word profile, SCORCHED 1/7, seven visible
  diamonds without colliding with Continue, profile-sourced answers and correct
  feedback.
- Game View selected resolution 2400×1080, no evident question/answer clipping
  at shown examples, readable black exterior, matching navy/gold art.
- Runtime testing deliberately detached the practice session after BeginReview,
  with no pending PlayerPrefs review, and did not invoke final completion,
  stage unlocking or navigation. Summary needs-review accumulation under
  uninterrupted gameplay has **not** been signed off from this isolated test.
- Unity stopped in Edit Mode, isolated scene clean/saved, inactive review canvas
  retained for mandatory runtime entry. Source/protected scene SHA256 hashes
  matched originals.

QA evidence (generated in ignored local `Library`):
`Library/VocabularyReviewArtQA/Stage3_Question_2400x1080.png` and
`Library/VocabularyReviewArtQA/Stage3_Correct_Feedback_2400x1080.png`.
Both are 2400×1080. The original pack's QA reconstruction remains external.

## Console and remaining gates

At last check Unity Console had one unrelated
`CommandInvokationFailure: Unity Remote requirements check failed` diagnostic.
No new C# compile errors were reported after correcting initial builder API
type errors. Intermittent MCP disconnections occurred during Unity domain
reload and recovered without restarting the Editor.

Not yet verified: final completion callback/progression persistence,
Enter-to-ItemSelection route, full miss-list summary after an uninterrupted
run, long answer/definition text extremes, touch targets and physical Galaxy
A05s memory/readability/FPS. Do not promote this scene or mark those tests as
passed without actual validation. No production-scene edits are authorized by
this isolated visual integration.
