# Titan Earthbreaker / Node 3 surface preflight — Unity Editor-only, 2026-10-20
No animation, rig, IK, boss state, damage, NavMesh or Play Mode exercised. Imported dedicated Node3 static floor MeshCollider sampled in Unity.
Center imported Unity marker (-1280,18,-620). Use full V017 stance/sole/animation/hit QA separately.
- Sole baseline separation: 20 world units, plus/minus 10 across tangent; floor delta must be <=0.30 at each of the 88 locations.
- Sampled flat static floor: 88/88 stance stations PASS; no-floor=0; excessive tilt/sole elevation delta=0; max sole height delta=0.0000; min normalY=1.0000
- Does NOT establish V017 4/4 real right-sole contacts, 94%-reach, 0.35m native stance preflight, frame28 support, animation timing, 98m wave hit logic, actual rig movement or PC/Android FPS.
