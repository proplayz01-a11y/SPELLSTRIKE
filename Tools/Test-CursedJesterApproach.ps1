# Source contract checks for Cursed Jester navigation and combat-state recovery.
# Runtime chase, wall, ledge, and NavMesh reconnect tests are separate Unity QA.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Source([string] $path) {
    Get-Content -LiteralPath (Join-Path $root $path) -Raw
}
function Assert([bool] $ok, [string] $message) {
    if (-not $ok) { throw "FAIL: $message" }
    Write-Output "PASS: $message"
}

$j = Source 'Assets/Characters/CursedJester/CursedJesterController.cs'
$trigger = Source 'Assets/Characters/CursedJester/CursedJesterTriggerEncounter.cs'
$controller = Source 'Assets/Characters/CursedJester/CursedJester_AC.controller'
$tauntClip = Source 'Assets/Characters/CursedJester/Taunt.anim'
$confettiClip = Source 'Assets/Characters/CursedJester/ConfettiBlast.anim'
$basicClip = Source 'Assets/Characters/CursedJester/BasicAttack.anim'
$hitClip = Source 'Assets/Characters/CursedJester/BeingHit.anim'

Assert ($trigger -match 'requireNullifyBeforeCombat = true') 'NULLIFY precombat vocabulary gate remains enabled'
Assert ($trigger -match 'cursedJester.StartBattle\(\)') 'Encounter starts Cursed Jester combat after vocabulary progression'
Assert ($j -match 'if \(!battleStarted \|\| player == null\)') 'Jester never chases before battle starts or without a player'
Assert ($j -match 'navPathRefresh' -or $j -match 'navigationRetrySeconds = 0.4f') 'NavMesh path refresh is throttled rather than requested every Update'
Assert ($j -match 'chasePath\.status == NavMeshPathStatus.PathComplete') 'Normal NavMesh pursuit only uses a complete route'
Assert ($j -match 'BeginGroundSteering\(\)') 'Incomplete or disconnected NavMesh path starts grounded pursuit'
Assert ($j -match 'agent.enabled = false') 'Fallback movement can leave disconnected NavMesh islands'
Assert ($j -match 'Physics.SphereCastNonAlloc') 'Ground pursuit checks solid obstacles ahead'
Assert ($j -match 'hit.normal.y >= 0.55f') 'Ground pursuit distinguishes floor from blocking walls'
Assert ($j -match 'Physics.RaycastNonAlloc') 'Ground pursuit probes walkable floor at each step'
Assert ($j -match 'groundSteeringMaxDrop') 'Ground pursuit refuses unsafe drops and missing floor'
Assert ($j -match 'NavMeshQueryFilter') 'NavMesh restoration respects the original agent type and area mask'
Assert ($j -match 'chasePath.status != NavMeshPathStatus.PathComplete') 'NavMesh restoration requires a complete route'
Assert ($j -match 'agent.Warp\(start.position\)') 'Agent returns to nearby valid NavMesh position without teleporting across arena'
Assert ($j -match 'groundSteeringActive = false') 'Successful NavMesh restoration switches off fallback motor'
Assert ($j -match 'rangedApproachFraction = 0.45f') 'Jester moves closer instead of spamming abilities at maximum ranged distance'
Assert ($j -match 'if \(distance > engageRange\)') 'Chase has priority until the Jester reaches practical fighting range'
Assert ($j -match 'attackAnimationWatchdogSeconds = 4f') 'Attack animation event watchdog prevents permanent attack-pose lock'
Assert ($j -match 'hitAnimationWatchdogSeconds = 3f') 'BeingHit animation event watchdog prevents permanent reaction lock'
Assert ($j -match 'Time.time - attackStartedAt >= attackAnimationWatchdogSeconds') 'Attack flag recovers when completion event is skipped'
Assert ($j -match 'Time.time - hitReactionStartedAt >= hitAnimationWatchdogSeconds') 'BeingHit flag recovers when completion event is skipped'
Assert ($j -match 'private NavMeshPath chasePath;') 'Unity native NavMeshPath is never allocated in a MonoBehaviour field initializer'
Assert ($j -match 'chasePath = new NavMeshPath\(\)') 'Native NavMeshPath is created during valid runtime lifecycle'
Assert ($j -match 'StepGroundSteering\(float deltaSeconds\)') 'Ground motion accepts explicit timestep for safe paused-game QA'
Assert ($j -match 'Mathf.MoveTowards\(\s*groundSteeringSpeed, walkSpeed, acceleration \* deltaSeconds\)') 'Ground chase respects acceleration and world-scale walk speed'

foreach ($item in @(
    @('OnTauntFinished', $tauntClip),
    @('OnConfettiBlastFinished', $confettiClip),
    @('OnBasicAttackFinished', $basicClip),
    @('OnBeingHitFinished', $hitClip)
)) {
    Assert ($item[1].Contains("functionName: " + $item[0])) ("Existing animation event {0} remains authored" -f $item[0])
}
Assert ($controller.Contains('m_Name: Taunt') -and
        $controller.Contains('m_Name: ConfettiBlast') -and
        $controller.Contains('m_Name: BasicAttack')) 'Existing Animator ability states remain intact'
Write-Output 'Cursed Jester approach and combat recovery contract checks complete.'
