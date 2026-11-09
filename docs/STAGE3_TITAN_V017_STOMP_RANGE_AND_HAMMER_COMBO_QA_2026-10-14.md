# Titan V017 production stomp range and Hammerfist combo QA (2026-10-14)

## Authorized mechanic

- Genuine Hammerfist frame134 player collider hit: original Van native 18m/s passive-adjusted vertical launch, input lock through actual CharacterController landing, then 0.45x movement speed for 8s. Other attacks have independent damage/cooldowns.
- Titan then walks over actual Stage3 NavMesh toward a legal, grounded V017 stance; an 18s finite pursuit reservation after Hammerfist does not extend player slow. Its <=18m V017 root-entry gate is **not** a player damage hitbox.
- Astra V017's exact original 8.767–8.8s clip, 4 right-foot stomps, first native left-foot plant around frame28, 94% anatomical limit, post-RigBuilder deformed-mesh LBS contact proof and left support remain mandatory. No changes to the source `.anim`, FBX, Blender or saved Stage3 scene.
- `TitanV017FourRightStompIKPreview.CertifiedRightSoleGroundContact` only emits once for each **genuinely certified** right-foot ground contact. The independent boss controller checks the **current real Van CharacterController** by physics overlap + closest collider point, actual sole-ground point, vertical/jump clearance and solid cover; no automatic impact based on an Animator event, fixed aim marker or time. The stationary right-foot target stays committed, not post-windup homing.

## Physical stomp range (world metres from actual deformed right-sole ground contact)

| Ring | Collider-nearest planar distance | Damage per certified sole + player HIT |
|---|---:|---:|
| Red/core | 0–7m | 5 HP |
| Orange/middle | >7–10m | 3 HP |
| Yellow/outer | >10–14m | 1 HP |
| Outside, airborne/dodged, cover blocked, no real sole contact | None | 0 HP |

Max 20 HP per original four-stomp clip (4 x 5 HP). The actual Stage3 Van has a giant scaled collider; verified physically legal near-rim contacts measured around 2.79–6.22m from the nearest physical collider point, so 7m is a grounded/real-scale core. Distances are **not** Titan-root distance and do **not** reuse the Hammerfist fist collider. Selected-Titan Scene gizmos are three colored ground-plane discs at the real/committed right-sole landing, explicitly labeled predicted versus real. Inspector production gate `enableV017VerifiedStompDamage=1`, `showV017StompRangeGizmos=1`; original `colossalStompV017MaxPlayerRange=18` independently limits starting the move.

## Terrain/root and 94% original left support

Real post-Hammerfist terrain frequently lay ~1.2m lower than the Titan's original x40.01-scaled `NavMeshAgent.baseOffset=.03` pose. Prior V017 `BeginPreview` correctly rejected first-frame foot floating/unequal floor with its immutable <=0.35m sole test. Production-only `enableV017GroundAlignedWalk=1` gradually changes `NavMeshAgent.baseOffset` while the boss is actually HeavyWalking into a NavMesh-verified stance, max 0.8 WORLD m/s, and refuses commit until the live grounded root has settled. It NEVER modifies root/target after V017 commitment or warps the player.

The specific second genuine blocker was left foot at frame28: original near-straight leg required 28.764m versus 94%-limited 28.575m. With existing production `preserveNativeLeftWhenIKUnreachable=true`, `CommitLeftPlant` now keeps IK entirely OFF if the original frame28 proxy AND actual LBS rendered-mesh sole have already independently proved real ground; subsequent mesh-ground and drift requirements still apply every frame, with NO leg stretch. After a first Hammerfist->V017 trial measured native-left vertical drift 0.381m against strict 0.35m, the follow-up **walk-only** stance includes -0.15m continuous sand planting bias; this is measured in world metres and only when `hammerfistStompPursuitPending`, not in ordinary V017, and cannot exceed the original strict first-frame ground tolerance. No tolerance was increased, and any unsafe left contact still fails closed.

## Real Unity Stage3 results

1. `TitanAttackHierarchyQA.Verify`: PASS **94/94** deterministic hierarchy, damage-band boundaries, hit budgets and invalid-input cases after core radius recalibration.
2. Original Stage3 scene NavMesh bootstrapped with one real surface and `failed=0`, original V017 first attack completed **4/4 genuinely verified right deformed-mesh contacts**, planted left sole. Four separate real Van collider hits gave **5+5+5+5 =20 HP**, health **100→80** (nearest collider contact approx6.13,6.19,6.13,6.21m).
3. Original progressive Slam next connected its actual moving 5-wave physics and Hammerfist became eligible after real Slam hit. Wave2–5 recorded **14 HP** (Van **80→66**), followed by original Hammerfist contact **12 HP** (Van **66→54**), native launcher / airborne lock, actual landing after **0.75s**, then movement slow x0.45 for 8s.
4. **Critical full-combo PASS:** Actual `Hammerfist HIT → Van launched and landed → Titan HeavyWalk 46.78m toward <=18m close entry → real V017 stance/IK committed → native f28 left source support fallback with corrective IK OFF → four post-RigBuilder LBS sole contacts → four independent core player hits`. Contact nearest distances **4.25, 2.90, 2.79, 2.86m**, damage **5+5+5+5 =20 HP**, health **54→49→44→39→34**. Native verifier reported `[V017 Stomp QA] PASS V017`, and controller reported original four post-rig contacts verified. This result supersedes earlier fail-closed trials (frame28 overreach and .381m left drift) before walk bias/native support correction.
5. Continued normal AI independently reattempted the combo. Hammerfist landed, then a later V017 again registered four actual core player hits (5HP each) before Van's actual health reached 0; game death remains authoritative and no endless player crowd control was required.

## Limits and protection

- Strict original left proxy/deformed mesh ground and right impact contact standards retain 0.35m first/native support bounds where used, 94% anatomical reach (no IK extension), physical player collision and NavMesh legality. In genuinely impossible uneven terrain, collision/ground checks can still deny an attack. No guarantee of damage to Van after moving/jumping out of a ring.
- This verifies authentic physical contact and gameplay HP, not final visual cinematic acceptance on every camera angle or every arena slope. Player damage can occur on a real sole contact even if a later frame of the 4-stomp clip aborts; log text now distinguishes individual certified hits from complete 4/4 clip credit.
- Original `Stage3Scene.unity` SHA256 `35FFB162CF563703F507AE196F331D54BD101AE1014DE994AF45547E29299AF3` unchanged; approved Astra V017 `.anim` SHA256 `8F19397D906A0E8437130AA43188A6B14E026255DAF013A695F4E45593B5713B` unchanged. Production candidate prefab and isolated runtime/editor source scripts are the modified scope.
