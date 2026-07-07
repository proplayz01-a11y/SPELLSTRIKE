# SPELLSTRIKE Enemy Builder Workflow

Use this for normal enemies and bosses.

## Enemy Implementation Layers

### Layer 1 — Foundation
- enemy prefab exists
- correct hierarchy
- health component
- collider / Rigidbody
- Animator assigned
- NavMeshAgent if movement uses NavMesh
- enemy starts idle or disabled as needed
- `StartBattle()` entry point exists

### Layer 2 — Movement
- finds player target
- chases or positions correctly
- faces player
- stops at attack range
- Animator Speed parameter updates

### Layer 3 — Basic Attack
- range check
- cooldown
- animation trigger
- damage timing
- hitbox or delayed damage
- recovery

### Layer 4 — Hit / Damage Reaction
- can receive damage through existing health system
- optional Hit animation trigger
- no duplicate death calls

### Layer 5 — Death
- Die animation trigger
- movement stops
- colliders/hitboxes disabled as needed
- node completion notified
- loot/reward hooks if needed

### Layer 6 — Special Ability
Only add after foundation works.
- warning marker if needed
- cast animation
- damage / status effect
- cooldown
- testing flow

### Layer 7 — Polish
- VFX
- SFX
- balancing
- camera shake
- UI indicators

## Enemy Script Rules

- Keep controllers small and testable.
- Use existing enemy patterns where possible.
- Do not create a new global manager unless absolutely needed.
- Use serialized fields for range, damage, cooldowns, references.
- Do not implement advanced ability before basic chase/attack/death works.

## Standard Enemy Test Checklist

- [ ] Enemy starts inactive/idle correctly.
- [ ] Node trigger activates enemy.
- [ ] `StartBattle()` works.
- [ ] Enemy finds player.
- [ ] Enemy moves/chases correctly.
- [ ] Enemy faces player.
- [ ] Enemy attacks only in range.
- [ ] Attack damage is applied once per hit.
- [ ] Cooldown works.
- [ ] Enemy can be damaged.
- [ ] Enemy dies once.
- [ ] Node completion still works.
