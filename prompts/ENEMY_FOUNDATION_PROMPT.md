# Generic Enemy Foundation Prompt

```md
Implement the foundation layer for this SPELLSTRIKE enemy:

Enemy:
[Enemy Name]

Stage/Node:
[Stage and Node]

Role:
[Gameplay role]

Existing context:
- Use existing player, health, damage, stage/node, and enemy patterns where possible.
- Do not rewrite working systems.
- Do not modify unrelated stages.
- Do not implement special abilities yet.
- Do not implement VFX yet.

Layer 1 goal:
- Enemy prefab/controller foundation works.
- Enemy can be activated by node flow.
- Enemy has StartBattle().
- Enemy finds player.
- Enemy uses NavMeshAgent or existing movement pattern.
- Enemy faces player.
- Enemy stops in attack range.
- Enemy basic attacks.
- Enemy can take damage and die.
- Existing node completion flow still works.

After finishing, report:
- scripts created/modified
- Inspector references needed
- manual Unity test flow
- known risks
```
