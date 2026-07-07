# Corsair Phantom Skill Notes

## Identity

Corsair Phantom is the Stage 2 Node 2 enemy in Sunken Seas.

Role:
- ghostly pirate / spectral corsair
- introduces Tile Cracking later
- should feel different from Barnacle Husk

Stage 2 enemy progression:

```text
Node 1: Barnacle Husk = Tile Locking + Bleeding
Node 2: Corsair Phantom = Tile Cracking
Node 3: Tide Wraith = Stun + Knockback
Boss: Drowned Captain = combines Stage 2 debuffs
```

## Movement

The Corsair Phantom should float/glide visually, but still use NavMeshAgent for gameplay movement.

```text
Root object = NavMeshAgent movement
VisualModel = animation + bobbing
```

Do not write custom flying movement unless requested.

## Unity Prefab Hierarchy

```text
CorsairPhantom
├── NavMeshAgent
├── Rigidbody
├── CapsuleCollider
├── EnemyHealth
├── CorsairPhantomController
└── VisualModel
    ├── Animator
    ├── Corsair_Rig
    ├── Body Mesh
    └── Coat Mesh
```

## Animator Parameters

```text
Speed   float
Attack  trigger
Cast    trigger
Hit     trigger
Die     trigger
```

Root motion must be OFF.

## Layer 1 Foundation Goal

Implement only this first:

```text
Corsair Phantom exists
Node2 trigger enables it
StartBattle() works
Enemy chases player
Enemy faces player
Enemy basic attacks
Enemy can die
Node2 can complete
```

Do not implement yet:
- Phantom Broadside
- Tile Cracking
- VFX
- new tile debuff architecture

## Phantom Broadside Later

Only after Layer 1 works.

Behavior:
- every X seconds, stop moving
- cast animation
- target player's last known position
- show warning marker for about 0.7 seconds
- fire impact/projectile
- if player is inside radius:
  - deal damage
  - apply Tile Cracking to 1-2 random available tiles

Rules:
- use existing tile debuff system
- keep debuff count low
- keep implementation small
- do not modify Stage 1
