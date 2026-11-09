# Tutorial persistent player scale repair

Date: 2026-10-21

## Root cause

The official TutorialScene has exactly one named PlayerSpawnPoint marker but no serialized PlayerSpawnPoint MonoBehaviour. On scene load, PlayerSpawnPoint.TryFind used to create a runtime fallback with (1,1,1). The persistent player root then reads PlayerScale and sets its own Transform.localScale in TeleportTo, explaining the 1x Play Mode behavior even when the Edit Mode prefab appears larger.

## Change

- Assets/Scripts/PlayerPersistence/PlayerSpawnPoint.cs: the named-marker fallback now supplies scale (10,10,10) for TutorialScene only, keeping Vector3.one for other scenes.
- Assets/Editor/TutorialPlayerScale10Fix.cs: guarded visible menu can add an explicit serialized PlayerSpawnPoint scale 10 and align the Tutorial prefab instance editor scale 10. This menu was **not run**, because Unity was in SampleScene Play Mode.
- No other stage scales, scene GUIDs, authoritative save data, canonical player prefab, or build indices changed.

## Verification

- PASS: single Tutorial marker exists without a serialized PlayerSpawnPoint, source code uses scale10 fallback, and PersistentPlayerRoot.TeleportTo consumes spawnPoint.PlayerScale.
- PASS: Unity successful C# compile after the source edit.
- PASS: original saved Tutorial scene after preserving pending Editor edits still byte-identical, canonical player prefab and Build Settings byte-identical, authoritative JSON save SHA-256 unchanged at 2289E13A7B7F53D187743635F07089FA31894FB6156B29CB0B8913D424AB066D.
- **NOT TESTED**: a fresh Tutorial runtime scale read-back after the change and enlarged character's ground/collider/camera/animation fit.

Backups: Library/TutorialPlayerScale10_20261021_1043 (initial disk snapshot, after-editor-save snapshot, prior scripts, prefab, Build Settings, save hash). Once Unity is out of Play, open TutorialScene and press Play, select runtime VAN AERT under DontDestroyOnLoad, verify local Scale X/Y/Z all equal to 10, and check ground contact, camera and foot IK. The optional visible menu can align the authored Editor prefab instance after saving the scene.
