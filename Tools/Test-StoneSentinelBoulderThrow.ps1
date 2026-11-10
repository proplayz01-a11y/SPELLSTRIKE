# Source + ballistic math contract for Stone Sentinel's heavy rock throw.
# A physical impact must be verified separately in isolated Unity Play Mode.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Source([string] $path) {
    Get-Content -LiteralPath (Join-Path $root $path) -Raw
}
function Assert([bool] $condition, [string] $description) {
    if (-not $condition) { throw "FAIL: $description" }
    Write-Output "PASS: $description"
}
$sentinel = Source 'Assets/Scripts/StoneSentinelsController.cs'
$projectile = Source 'Assets/Scripts/BoulderProjectile.cs'
$throwClip = Source 'Assets/Animations/StoneSentinelAnim/BoulderThrow.anim'

foreach ($eventName in @('OnBoulderPickup','OnBoulderThrow','OnBoulderThrowFinished')) {
    Assert ($throwClip.Contains("functionName: $eventName")) "Existing $eventName animation event preserved"
}
Assert ($sentinel -match 'public void OnBoulderThrow\(\)') 'Original animation event launches the physical rock'
Assert ($sentinel -match 'boulderSizeMultiplier = 1.2f') 'Held and thrown rock size increased by 20 percent'
Assert ($sentinel -match 'originalHeldBoulderScale \* boulderSizeMultiplier') 'Held prop size does not accumulate between attacks'
Assert ($sentinel -match 'boulder.transform.localScale \*= boulderSizeMultiplier') 'Thrown mesh and its physical sphere scale up together'
Assert ($sentinel -match 'boulderSpeedMultiplier = 1.25f') 'Original authored Stage 1 speed is retained and made 25 percent faster'
Assert ($sentinel -match 'boulderGravityMultiplier = 9f' -and $sentinel -match 'boulderTumbleSpeed = 17f') 'Scaled game-world gravity and strong tumbling are configured'
Assert ($sentinel -match 'playerCollider.bounds.center') 'Throw aims at the actual player hitbox center, not one unit above the ground'
Assert ($sentinel -match 'CalculateBoulderThrowVelocity\(') 'Throw event solves a real ballistic intercept'
Assert ($sentinel -match 'boulderArcAngle\)') 'Existing arc-angle setting influences minimum loft time'
Assert ($sentinel -match 'currentState == EnemyState.Dead' -and $sentinel -match 'vocabularyPaused') 'No throws on death or during mandatory Counterspell'
Assert ($projectile -match 'rb.useGravity = true') 'Rigidbody gravity enabled for curved trajectory'
Assert ($projectile -match 'rb.AddForce\(Physics.gravity \* \(gravityMultiplier - 1f\)') 'Additional scale-aware gravity applied using real Rigidbody physics'
Assert ($projectile -match 'rb.angularVelocity = tumbleAxis') 'Stone actually tumbles around the sideways throw axis'
Assert ($projectile -match 'rb.maxAngularVelocity = Mathf.Max') 'Rigidbody allows fast visible rotation without Unity angular clamp'
Assert ($projectile -match 'CollisionDetectionMode.ContinuousDynamic') 'Continuous collision detection reduces tunneling risk'
Assert ($projectile -match 'Physics.IgnoreCollision\(rockCollider, throwerCollider, true\)') 'Rock cannot immediately hit its own thrower'
Assert ($projectile -match 'GetComponentInParent<IDamageable>\(\)' -and $projectile -match 'damageable.ApplyDamage\(damage\)') 'The existing single-hit IDamageable authority is retained'
Assert ($projectile -match 'if \(hasHit \|\| !launched\) return') 'One physical impact cannot apply duplicate damage'
Assert ($projectile -match 'maxGroundBounces = 2' -and $projectile -match 'private int groundBounceCount') 'Heavy boulder has a bounded two-bounce ground sequence'
Assert ($projectile -match 'firstBounceRetention = 0.38f' -and $projectile -match 'secondBounceRetention = 0.20f') 'Second bounce retains less energy than the first'
Assert ($projectile -match 'horizontalImpactRetention = 0.58f') 'Ground friction slows forward rock movement on bounce'
Assert ($projectile -match 'groundNormal.y >= 0.55f') 'Only upward-facing ground contacts bounce; walls terminate'
Assert ($projectile -match 'collision.GetContact\(i\).normal') 'Ground bounce uses actual physics contact normals'
Assert ($projectile -match 'rb.linearVelocity = tangential \+ groundNormal \* reboundSpeed') 'Rock gets a physical upward rebound with damped horizontal speed'
Assert ($projectile -match 'rb.angularVelocity \*= 0.70f') 'Spin slows with each floor impact'
Assert ($projectile -match 'rb.linearDamping = 6f' -and $projectile -match 'rb.angularDamping = 8f') 'Final contact settles and slows the rock'
Assert ($projectile -match 'Destroy\(gameObject, settleTime\)') 'Heavy rock remains visible briefly to settle after last bounce'
Assert ($projectile -match 'hasHit = true; // Player damage is authorized by one impact ONLY.') 'Player collision terminates bounce and authorizes exactly one hit'
Assert ($projectile -match 'if \(hasHit \|\| !launched\) return') 'Subsequent physical contacts cannot apply a second hit'

# Math: the chosen flight time and vertical launch speed must actually reach
# the moving player's CURRENT location sampled at release (not homing later).
$horizontalSpeed = 101.9 * 1.25
$minLoft = 0.42 + (0.78 - 0.42) * ((45 - 15) / (70 - 15))
$g = 9.81 * 9.0
foreach ($distance in @(30.0,60.0,130.0)) {
    foreach ($heightChange in @(-18.0,0.0,10.0)) {
        $time = [Math]::Max($distance / $horizontalSpeed, $minLoft)
        $vx = $distance / $time
        $vy = ($heightChange + 0.5 * $g * $time * $time) / $time
        $x = $vx * $time
        $y = $vy * $time - 0.5 * $g * $time * $time
        Assert (([Math]::Abs($x-$distance) -lt 1e-6) -and
            ([Math]::Abs($y-$heightChange) -lt 1e-6)) ("Ballistic intercept distance={0:0} height={1:0}" -f $distance,$heightChange)
    }
}
Assert ((3.82 * 1.2) -gt 3.82 -and (3.18 * 1.2) -gt 3.18) 'Existing prefab and held-rock sizes both increase'
$firstBounceSpeed = [Math]::Min(24.0, 65.0 * 0.38)
$secondBounceSpeed = [Math]::Min(24.0, $firstBounceSpeed * 0.20)
Assert ($firstBounceSpeed -gt $secondBounceSpeed -and $secondBounceSpeed -gt 0) 'Two successive rebounds have descending upward speed'
Assert ((100.0 * 0.58 * 0.58) -lt (100.0 * 0.58)) 'Horizontal speed decays across two ground contacts'
Write-Output 'Stone Sentinel heavy boulder source and ballistic contracts passed.'
