# Corsair Phantom Foundation Prompt

```md
Implement Stage 2 Node 2 enemy foundation: Corsair Phantom.

Context:
- Stage 2 is Sunken Seas.
- Node 1 Barnacle Husk already works.
- Existing systems may include StageProgressionManager, StageNodeTrigger, StageNodeController, EnemyHealth, PlayerHealth, TileDebuffManager, PotionInventory, and PassiveItemInventory.
- Do not modify Stage 1.
- Do not modify potion/passive/item selection/minigame systems.
- Do not implement final VFX yet.
- Do not implement Tile Cracking yet.
- Do not require special ability yet.

Enemy:
Corsair Phantom
Role: Stage 2 Node 2 enemy
Theme: ghostly pirate
Main debuff later: Tile Cracking

Layer 1 goal:
Create the foundation for the Corsair Phantom enemy.

Required behavior:
- Enemy GameObject starts disabled by default if required by the Stage 2 node flow.
- Node2 trigger enables the enemy.
- Node2 trigger sends StartBattle.
- Corsair Phantom has StartBattle().
- Idle before combat starts.
- Finds the player target.
- Uses NavMeshAgent to chase the player.
- Faces the player.
- Stops at attack range.
- Has a basic attack loop.
- Deals damage to the player.
- Drives Animator parameters:
  - Speed float
  - Attack trigger
  - Hit trigger
  - Die trigger
- Can die through EnemyHealth.
- On death, existing node completion flow can mark Node 2 complete.

Do not implement:
- Phantom Broadside
- Tile Cracking
- VFX
- new tile debuff architecture

Architecture rules:
- Follow BarnacleHuskController pattern where useful.
- Keep CorsairPhantomController small and testable.
- Do not create a giant manager.
- Do not change Stage 1 scripts.
- Do not touch existing working systems unless necessary.

After finishing:
- Tell me what scripts were created or modified.
- Tell me what Inspector references need to be assigned.
- Tell me the manual Unity test flow.
```
