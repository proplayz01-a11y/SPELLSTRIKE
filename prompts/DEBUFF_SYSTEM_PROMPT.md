# Debuff System Prompt

```md
Implement the next layer of the SPELLSTRIKE Stage 2 debuff system.

Context:
- Stage 1 has no debuffs.
- Stage 2 introduces pressure through debuffs.
- Do not rewrite the existing word/tile/combat system.
- Extend existing tile/debuff managers if present.
- Do not create duplicate managers unless there is no existing system.

Required TileState states:
- Normal
- Locked
- Cracked
- Broken

Selectability:
- Normal and Cracked are selectable.
- Locked and Broken are not selectable.

Implementation layer:
[Specify exact layer: TileState / Tile Locking / Tile Cracking / Bleeding / Stun / Knockback]

After finishing:
- scripts changed
- Inspector setup needed
- manual test flow
- risks
```
