# SPELLSTRIKE Prologue Unity Page Layout Plan

**Status:** Approved planning contract; no Unity scene or runtime implementation
is authorized by this document alone.

## Locked Presentation Direction

- The 42 approved source illustrations become **21 finished 16:9 comic pages**.
- Canva owns panel composition, gutters, speech bubbles, narration, typography,
  and impact text.
- Unity displays one flattened finished page at a time through one reusable
  `StoryScene`.
- Unity must not dynamically reconstruct comic layouts from the 42 raw panels.
- Page 1 is the mandatory prototype and validation gate before Pages 2–21 are
  assembled or integrated.

## Approved Page Grouping

| Unity page | Source panels | Locked beat |
| ---: | ---: | --- |
| 1 | 1 | Full-page peaceful family memory |
| 2 | 2–3 | Great War escalation and Word combat |
| 3 | 4 | Full-page destroyed-home emotional impact |
| 4 | 5 | Full-page library arrival |
| 5 | 6–7 | Van's confession and grief |
| 6 | 8 | Full-page Vander reaction |
| 7 | 9–10 | Decades-later setup and dream disclosure |
| 8 | 11 | Full-page Nevali reveal |
| 9 | 12–14 | Vander's contemplation and response |
| 10 | 15–16 | Leaving the table and crossing the gateway |
| 11 | 17–18 | Ancient-volume retrieval and placement |
| 12 | 19–21 | Historical explanation and illustrated chronicle |
| 13 | 22–24 | Suspicion, direct question, and guarded answer |
| 14 | 25–26 | History dissolves and Nevali's message appears |
| 15 | 27–28 | Recognition and confrontation |
| 16 | 29–30 | Living-prison explanation and realization |
| 17 | 31–33 | Decision, forbidden-technique warning, and acceptance |
| 18 | 34–35 | Vander retrieves and returns with his staff |
| 19 | 36–37 | Forbidden casting and Van entering the book |
| 20 | 38–39 | TutorialForest awakening and sunlight POV |
| 21 | 40–42 | Robe cleanup, staff retrieval, and bridge-route handoff |

Panel 26 must dominate Page 14 so Nevali's complete message remains readable.
Panel 37 must dominate Page 19 because Van's transfer into the book is the
climactic visual action.

## Reusable Canva Layout Families

1. **Full-page hero** — one illustration, reserved for major emotional beats.
2. **Two-panel split** — vertical or horizontal paired progression.
3. **Large hero plus two supporting panels** — one dominant event plus two
   setup/reaction frames.
4. **Three-step strip** — equal or progressively larger sequential beats.

Every page remains 1920 x 1080. Critical faces, hands, text, and props stay
inside a five-percent safe margin. Dialogue must remain readable at 1280 x 720
and on the smallest supported Android test device.

## Unity Hierarchy

```text
StoryScene
└── StoryCanvas                  Screen Space - Overlay
    └── SafeArea
        ├── Background           near-black/navy letterbox
        ├── PageImage            finished 16:9 page, Preserve Aspect
        ├── InputBlocker         active during transitions
        ├── TopBar
        │   ├── PageProgress     e.g. 03 / 21
        │   └── SkipButton
        ├── ContinueIndicator    subtle lower-right affordance
        └── FadeOverlay
```

The CanvasScaler reference resolution is 1920 x 1080 with Match Width or
Height set to 0.5. `Screen.safeArea` controls the SafeArea bounds.

## Interaction Contract

- Click/tap anywhere, Enter, or Space advances exactly one page.
- Skip requires confirmation, records completion, and routes safely onward.
- Page transition uses unscaled time with a target fade of 0.25–0.35 seconds.
- Input remains locked until the current transition completes.
- Missing artwork displays a text fallback and logs an error; it never blocks
  continuation.
- Completion routes to `TutorialScene` when the tutorial is incomplete and to
  the configured later destination when appropriate.

## Runtime Data Contract

`StorySequenceProfile` contains 21 page records. Each record needs:

```text
pageId
pageSprite
optionalAudio
minimumDisplayTime
optionalNextSceneOverride
```

The profile also owns sequence ID, completion key, skip permission, and default
next scene. Unity dialogue overlays are optional fallback/accessibility data;
the approved first layer displays Canva-composed page text.

## Texture Import Baseline

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- sRGB: enabled
- Mip Maps: disabled
- Max Size: 2048
- Preserve Aspect: enabled on `PageImage`
- Android: validate ASTC compression and text readability on device

Do not assume all 21 page textures can remain loaded indefinitely on Android.
Begin with direct profile references for the Page 1 prototype, measure memory,
then add current/next-page loading only if evidence requires it.

## Page 1 Prototype Gate

Before producing or integrating Pages 2–21, Page 1 must pass:

1. 1920 x 1080 export with approved Panel 1 composition intact.
2. PC and Android safe-area presentation.
3. Readable text at 1280 x 720 and on the target Android device.
4. Click, tap, Enter, and Space advancement without double advance.
5. Fade and input-lock behavior under rapid input.
6. Skip confirmation and durable completion behavior.
7. Missing-sprite fallback without soft lock.
8. Correct route from Main Menu Adventure to StoryScene to TutorialScene.

## Explicitly Deferred

- Per-panel motion, parallax, camera pans, animated speech bubbles, Timeline,
  voice acting, lip sync, and progressive panel reveals.
- Final Pages 2–21 until the Page 1 prototype passes.
- Claims that the existing setup command or StoryScene is runtime validated.
