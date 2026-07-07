# SPELLSTRIKE Debuff System Rules

## Debuff Introduction

```text
Stage 1 = no debuffs, learning stage
Stage 2 = debuffs introduced, pressure stage
Stage 3+ = debuffs expanded and combined
```

## Stage 2 Debuff Map

| Enemy | Debuffs |
|---|---|
| Barnacle Husk | Tile Locking, Bleeding |
| Corsair Phantom | Tile Cracking |
| Tide Wraith | Stun, Knockback |
| Drowned Captain | Combines Stage 2 debuffs |

## TileState System

Before debuffs work cleanly, every tile needs a state.

```csharp
public enum TileState
{
    Normal,
    Locked,
    Cracked,
    Broken
}
```

## Tile State Rules

```text
Normal = fully usable
Locked = visible but cannot be selected
Cracked = usable but one hit away from Broken
Broken = removed/unavailable until restored or respawned
```

## Transitions

```text
Normal → Locked
Normal → Cracked
Cracked → Broken
Locked → Normal
Broken → Normal
```

## Selectability

Selectable:
- Normal
- Cracked

Not selectable:
- Locked
- Broken

## Implementation Order

1. Add TileState to tile script.
2. Add visual update hook.
3. Add selectability guard.
4. Create or extend TileDebuffManager/DebuffManager.
5. Implement Tile Locking.
6. Implement Tile Cracking.
7. Hook Bleeding into word submission.
8. Hook Stun into player movement.
9. Hook Knockback into player movement/word interruption.
10. Add UI/VFX polish last.

## Important Rule

Do not create a second debuff architecture if `TileDebuffManager` or an equivalent already exists. Extend the existing system carefully, because duplicate managers are how Unity projects become haunted houses with C# files.
