# SPELLSTRIKE Comic Panel Mode v01

This is the alternate visual presentation requested by the user: **all 42
story beats played with historical rejected storyboard images**, preserving
the approved 3D cinematic separately. Source PNGs are never modified.

## Files

- ComicModePanelSources.json: source path, source SHA-256, optimized image SHA,
  confirmed rejected/fallback/exception status, title and script provenance
  for every numbered panel.
- ComicModeMapping.md: human-readable audit of all 42 source selections.
- ComicMode_42_Panel_ContactSheet_v01.png: single sheet of chosen variants.
- Assets/Resources/ComicMode/Panel_01.bytes through Panel_42.bytes: compact
  1600 x 900 max-resolution JPEG images loaded and decoded one at a time.
- Assets/Resources/ComicMode/manifest.json: runtime ordering and captions.
- ComicModeManifest.cs, ComicModeViewer.cs, ComicModeUi.cs and
  ComicModeSelection.cs: isolated Unity runtime.
- ComicModeRouteBootstrap.cs: optional scene entry to choose Cinematic vs Comic
  before prime's automatic PlayableDirector in the master scene begins.
- Assets/Editor/ComicModeInstaller.cs: optional isolated review scene builder,
  manually invoked from the Editor menu after coordination with prime.

## Master integration contract

In the shared cinematic front-end after it is safe to modify that scene, wire
the existing animated master start and its completion routing:

    Spellstrike.ComicMode.ComicModeSelection.Open(
        () => { /* invoke the current 3D master cinematic playback */ },
        () => { /* route to TutorialScene or existing finish handler */ },
        () => { /* return to story/menu entry */ });

The picker presents **Animated Cinematic** and **Comic Panels** as two choices.
The first callback invokes the prime-owned animation. The second choice creates
the 42-panel viewer automatically. Prev/Next and left/right keyboard navigation
work, Back returns to the picker, Skip requires confirmation, and Finish records
the shared SpellStrike_PrologueCompleted preference key before invoking the
completion callback.

You can also invoke only the viewer:

    Spellstrike.ComicMode.ComicModeViewer.Open(onComplete, onBack);

Neither the viewer nor selector changes production scene assets, Timeline
bindings, Build Settings or existing main-menu routing. Android uses on-screen
buttons and Screen.safeArea positioning.

**Concrete entry scene alternative:** Use the installer menu
**Create isolated story mode entry scene v01** to produce
Assets/Cinematics/ComicMode/Preview/ComicMode_StoryModeEntry_v01.unity. The
ComicModeRouteBootstrap in that scene automatically offers two choices:
Comic opens the 42 frames and on Finish routes to TutorialScene; Animated
Cinematic loads prime's
Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v04.unity, with root
PlayableDirector PROLOGUE_MASTER_TIMELINE_DIRECTOR__PLAY_ON_AWAKE.
This selection entry must be loaded before the prime master starts. **Prime
must add both entry and master scenes to the active Build Profile, route the
first-run Adventure button to the entry, and bind the animated master's end to
the existing TutorialScene handoff.** This worker leaves those shared files
untouched. The Cinematic option is disabled until the master is included.

## Isolated Editor preview

When prime confirms Editor access, run the menu
**SPELLSTRIKE > Comic Mode > Create isolated preview scene v01**. The command
creates Assets/Cinematics/ComicMode/Preview/ComicMode_Review_v01.unity only if
it does not already exist and the open scene is clean. It does not change
Build Settings. Open this scene and press Play to review 42 panels. The
3D cinematic button is disabled in the isolated preview because that route
depends on prime's master callback. A separate menu item,
**Validate historical 42-panel images**, checks the local artwork references.

## Validation, provenance and limitations

Run from the project root:

    python Assets/Cinematics/ComicMode/ComicModeBuildAssets.py --check --review-sheet

The generator checks all source assets, JPEG bytes, JSON and mapping records,
including SHA-256. It refuses to overwrite any changed generated artifact.
For deliberate content revision, create a new version.

Checked: **21 explicitly rejected panels**, **20 fallback historical Work
versions**, and **one approved exception: Panel 11**, which has no archived
Work candidate. Some fallback images were later approved; this is expressly
recorded in the mapping. Panel 39's earliest Work image is a retired
bridge-route view, so its illustration resembles Panel 42; this is flagged
instead of silently swapping in the final approved lying POV.

Text is transcribed from docs/STORY_CANVA_DIALOGUE_SCRIPT.md, including
locked and proposed lines. Proposed text is labeled **DRAFT SCRIPT** at runtime.
Silent panels stay silent. Shared narration for Panels 17–18 repeats across
the pair; Panels 20–21 each display their corresponding clause.

Offline C# validation against Unity 6.3 managed DLLs via a temporary project
in Library/ComicModeValidation: **0 errors, 0 warnings**.
Actual Unity Editor import/playback and physical Galaxy A05s testing are
pending prime's Editor coordination. No unsupported Play Mode result is claimed.
