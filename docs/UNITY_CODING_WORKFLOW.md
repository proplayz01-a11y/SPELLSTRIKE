# Unity Coding Workflow for SPELLSTRIKE

## Before Editing

Codex must inspect relevant existing scripts first, especially:
- player movement
- attack/word submission
- enemy controller pattern
- stage/node trigger pattern
- health/damage scripts
- tile scripts
- existing managers

## Implementation Rules

- Implement one layer at a time.
- Keep each script focused.
- Avoid monolithic controllers unless refactoring has been requested.
- Preserve serialized fields.
- Prefer inspector assignment over hardcoded scene object names.
- Use existing patterns before inventing new architecture.
- Never break existing Stage 1 flow while building Stage 2.

## Report Format After Changes

Always end with:

```md
## Files Changed
- `path/File.cs`: what changed

## Inspector Setup Needed
- Object: field/reference to assign

## Manual Test Flow
1. Open scene.
2. Press Play.
3. Trigger the node/feature.
4. Expected result.

## Risks / Notes
- Any assumptions or incomplete parts.
```

## Testing Baseline

Before claiming success:
- No Unity console errors.
- Player can move.
- Word input still works.
- Enemy spawns/activates.
- Damage works.
- Death works.
- Node completion works if relevant.
- UI still updates.
