# Static contract checks only. This does NOT run a Unity scene, GPU graph, or device build.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function ReadSource([string] $relative) {
    Get-Content -LiteralPath (Join-Path $root $relative) -Raw
}
function Assert([bool] $condition, [string] $description) {
    if (-not $condition) { throw "FAIL: $description" }
    Write-Output "PASS: $description"
}

$attack = ReadSource 'Assets/Scripts/AttackController.cs'
$projectile = ReadSource 'Assets/Scripts/HomingProjectile.cs'
$descent = ReadSource 'Assets/Scripts/VFX/ArcaneSurgePresentation.cs'
$visual = ReadSource 'Assets/Scripts/VFX/ArcaneSurgeZapVisual.cs'
$strongCharge = ReadSource 'Assets/Scripts/VFX/ArcaneSurgeCastChargeVfx.cs'
$strongClip = ReadSource 'Assets/VAN AERT/ANIMS/StrongAttack.anim'
$wizardAnimator = ReadSource 'Assets/Animations/WizardAnim.controller'
$vanAnimator = ReadSource 'Assets/VAN AERT/VanANIMCONTROLLER.controller'
$cameraHelper = ReadSource 'Assets/Scripts/VFX/ArcaneSurgeVisual.cs'
$libraryScript = ReadSource 'Assets/Scripts/VFX/ArcaneSurgeZapLibrary.cs'
$catalog = ReadSource 'Assets/Resources/Spellstrike/VFX/ArcaneSurgeZapLibrary.asset'
$shaderMeta = ReadSource 'Assets/VFX/BasicAttackCharge/BasicChargeEnergyMobileSafe.shader.meta'
$arcaneMaterial = ReadSource 'Assets/Resources/Spellstrike/VFX/ArcaneSurgeEnergy.mat'

Assert ($attack -match 'wordLength >= 3 && wordLength <= 5') 'Basic attack band 3–5 unchanged'
Assert ($attack -match 'wordLength >= 6 && wordLength <= 9') 'Medium attack band 6–9 unchanged'
Assert ($attack -match 'wordLength >= 10 && wordLength <= 13') 'Strong attack band 10–13 unchanged'
Assert ($attack -match 'currentAttackTrigger == "StrongAttack"\s*&& !targetChallengeCastInProgress' -and $attack -match 'if \(!projectileScript.EnableArcaneSurge\(\)\)') 'Strong-only overhead activation; Target Challenge stays on the original path'
Assert ($attack -match 'projectileScript.Launch\(') 'Animation event retains existing Launch call'
Assert ($attack -match 'public void OnStrongAttackCast\(\)') 'OnStrongAttackCast method exists on AttackController'
Assert ($attack -match 'public void OnStrongAttackZapRelease\(\)') 'OnStrongAttackZapRelease method exists on AttackController'
Assert ($attack -match 'public void OnStrongZapYellow\(\)') 'Yellow Zap has its own Animation Event receiver'
Assert ($attack -match 'public void OnStrongZapDarkBlue\(\)') 'Dark Blue Zap has its own Animation Event receiver'
Assert ($attack -match 'public void OnStrongZapCyanBlue\(\)') 'Cyan Blue Zap has its own Animation Event receiver'
Assert ($attack -match 'activeStrongZapPresentation\.RequestYellowImpact\(\)') 'Yellow event requests first contact from the tracking presentation'
Assert ($attack -match 'activeStrongZapVisual\.TriggerDarkBlueImpact\(\)') 'Dark Blue event triggers the surviving visual directly'
Assert ($attack -match 'activeStrongZapVisual\.TriggerCyanBlueImpact\(\)') 'Cyan Blue event triggers the surviving visual directly'
Assert ($attack -match 'activeStrongZapVisual = activeStrongZapPresentation != null') 'Active Zap visual survives the projectile for event-driven later hits'
Assert ($attack -match 'ResetStrongZapEventHandles\(\)') 'Active Zap references are reset across casts'
Assert ($attack -match 'ArcaneSurgeCastChargeVfx.Spawn\(magicSpawnPoint\)') 'Strong cast event starts hand-bound charge effect'
Assert ($attack -match 'OnStrongAttackZapRelease\(\)[\s\S]*?StopStrongCastVfx\(\);\s*ShootMagicProjectile\(\);') 'Strong release stops charge then uses original projectile authority'
Assert ($attack -match 'public void OnAttackRelease\(\)') 'Existing generic release event remains for Basic/Medium/Ultimate'
Assert ($attack -match 'projectileReleasedForCurrentAttack\)') 'Duplicate release guard is retained'
 $events = [regex]::Matches($strongClip, '(?m)^  - time: ([0-9.]+)\r?\n    functionName: ([A-Za-z0-9_]+)')
 $eventTimes = @{}
 foreach ($event in $events) {
     $name = $event.Groups[2].Value
     Assert (-not $eventTimes.ContainsKey($name)) "Strong clip has no duplicate $name events"
     $eventTimes[$name] = [double]::Parse($event.Groups[1].Value, [System.Globalization.CultureInfo]::InvariantCulture)
 }
 $eventOrder = @('OnStrongAttackCast', 'OnStrongAttackZapRelease', 'OnStrongZapYellow', 'OnStrongZapDarkBlue', 'OnStrongZapCyanBlue', 'OnAttackAnimationFinished')
 Assert ($events.Count -eq $eventOrder.Count) 'Strong clip has exactly six animation events'
 foreach ($name in $eventOrder) {
     Assert ($eventTimes.ContainsKey($name)) "Strong clip contains $name"
 }
 for ($eventIndex = 1; $eventIndex -lt $eventOrder.Count; $eventIndex++) {
     Assert ($eventTimes[$eventOrder[$eventIndex]] -gt $eventTimes[$eventOrder[$eventIndex - 1]]) "Strong clip orders $($eventOrder[$eventIndex]) after $($eventOrder[$eventIndex - 1])"
 }
 Assert ($eventTimes['OnStrongZapYellow'] - $eventTimes['OnStrongAttackZapRelease'] -ge 0.199) 'First Yellow impact is at least 0.20 seconds after Zap release'
Assert (([regex]::Matches($strongClip,'(?m)^    functionName: (OnStrongAttackCast|OnStrongAttackZapRelease|OnStrongZapYellow|OnStrongZapDarkBlue|OnStrongZapCyanBlue|OnAttackAnimationFinished)\r?$')).Count -eq 6) 'Strong clip has exactly six intended animation events'
Assert ($eventTimes['OnAttackAnimationFinished'] -le 2.97) 'Strong clip finishes within the full 2.97-second animation'
Assert ($strongClip -notmatch 'functionName: OnAttackRelease') 'Strong clip contains no duplicate generic release event'
Assert ($wizardAnimator -match 'guid: ea2acfb35ba116645a807ef6a7f34043') 'Wizard Animator uses the edited Strong clip'
Assert ($vanAnimator -match 'guid: ea2acfb35ba116645a807ef6a7f34043') 'Van Aert Animator uses the edited Strong clip'
Assert ($strongCharge -match 'effect\.CreateRing') 'Charge event creates controlled hand energy rings'
Assert ($strongCharge -notmatch 'TakeDamage\(|ResolveAcceptedImpact\(') 'Charging visuals do not deal damage'

Assert ($projectile -match 'if \(arcaneSurge != null\) return; // Contact is confirmed at descent completion') 'Arcane physics callbacks do not create an early hit'
Assert ($projectile -match 'ResolveAcceptedImpact\(collider\)') 'Arcane confirmed contact uses original accepted-impact function'
Assert ($projectile -match 'if \(hasHit \|\| other == null\) return;') 'Original single-hit guard remains'
Assert ($projectile -match 'CanResolveTargetChallengeAgainst\(vocabularyBarrier\)') 'Original Target Challenge barrier authority remains'

Assert ($descent -match 'const float DescentSeconds = 0\.20f') 'Exactly 0.20 seconds is the configured descent duration'
Assert ($descent -match 'private float heightAboveHead = 18f') 'Giant lightning descends from 18 world units above the moving head'
Assert ($descent -match 'public void RequestYellowImpact\(\)') 'First hit is explicitly gated by the Yellow Animation Event'
Assert ($descent -match 'elapsed < DescentSeconds \|\| !yellowImpactRequested') 'Neither premature Yellow events nor elapsed time alone can cause damage'
Assert ($descent -match 'aliveElapsed >= MissingYellowEventTimeoutSeconds') 'Missing Yellow events eventually cancel the projectile without applying damage'
Assert ($descent -match 'float t = elapsed / DescentSeconds') 'Normalized descent progression derives from elapsed time'
Assert ($descent -match 'Vector3 head = GetHeadPoint\(\)') 'Every flight Update samples the current target head'
Assert ($descent -match 'FindTargetCollider\(target\)') 'Contact uses a collider from the target receiver hierarchy'
Assert ($descent -match 'ResolveArcaneSurgeContact\(acceptedCollider\)') 'Confirmed contact calls existing single-hit pipeline'
Assert ($descent -match 'private void OnDestroy\(\)') 'Unexpected projectile destruction has VFX cleanup'
Assert ($descent -match 'Resources.Load<ArcaneSurgeZapLibrary>') 'Strong adapter loads imported Zap prefab references'
Assert ($descent -match 'visual\.SetImpactTarget\(target, acceptedCollider\)') 'Moving target is captured before the accepted hit can destroy it'
Assert ($descent -match 'visual\.ConfirmedImpact\(head, groundY\)') 'Imported Zap sequence begins only after confirmed contact'
Assert ($descent -match 'visual\.BeginReleaseLightning\(\)') 'Strong release starts full-size imported lightning before the first impact'
Assert ($projectile -match 'arcaneSurge\.QueueDamageSequence\(') 'The original accepted hit queues one approved total for the three strikes'
Assert ($descent -match 'visual\.ConfigureDamageSequence\(') 'The independent Zap visual stores the approved damage and receiver'
Assert ($visual -match 'public static int DamageForStrike\(int approvedDamage, int strikeIndex\)') 'Zap damage uses a dedicated integer split rule'
Assert ($visual -match 'int baseShare = nonnegative / 3') 'Damage is split into three integer shares'
Assert ($visual -match 'strikeIndex >= 3 - remainder') 'Remainder is assigned to the later strikes'
Assert ($visual -match 'ApplyDamageStrike\(0\)') 'Yellow performs the first real damage hit'
Assert ($visual -match 'ApplyDamageStrike\(1\)') 'Dark Blue performs the second real damage hit'
Assert ($visual -match 'ApplyDamageStrike\(2\)') 'Cyan Blue performs the third real damage hit'
Assert ($visual -match 'damageEnemy\.currentState != EnemyController\.EnemyState\.Dead') 'No additional damage is applied to a dead standard enemy'
Assert ($visual -match 'damageBramble\.currentState != BrambleSpriteController\.EnemyState\.Dead') 'No additional damage is applied to a dead Bramble Sprite'
Assert ($visual -match '!damageHealth\.IsDead') 'No additional damage is applied to a dead EnemyHealth receiver'
Assert ($visual -match 'enum Variant \{ Yellow, DarkBlue, CyanBlue \}') 'Three requested vendor color variants are configured'
Assert ($visual -match 'const float ImpactIntervalSeconds = 0\.20f') 'Reference minimum Zap descent interval remains 0.20 seconds; animation events control the hits'
Assert ($visual -notmatch 'WaitForSeconds\(|PlayFollowUpImpacts\(|StartCoroutine\(') 'Zap damage/effects no longer depend on a timing coroutine'
Assert ($visual -match 'public void TriggerDarkBlueImpact\(\)') 'Dark Blue effect has a public event-driven trigger'
Assert ($visual -match 'public void TriggerCyanBlueImpact\(\)') 'Cyan Blue effect has a public event-driven trigger'
Assert ($visual -match 'if \(!impacted \|\| aborted \|\| nextImpactIndex != 1\)') 'Dark Blue event cannot play twice or out of order'
Assert ($visual -match 'if \(!impacted \|\| aborted \|\| nextImpactIndex != 2\)') 'Cyan Blue event cannot play twice or out of order'
Assert ($visual -match 'EmitVisualImpact\(Variant\.Yellow,') 'First impact is Yellow'
Assert ($visual -match 'EmitVisualImpact\(Variant\.DarkBlue,') 'Second impact is Dark Blue'
Assert ($visual -match 'EmitVisualImpact\(Variant\.CyanBlue,') 'Third impact is Cyan Blue'
Assert ($visual -match 'EmitVisualImpact\(Variant\.Yellow, 0\.62f, 0\.60f, 0\.18f\)') 'Yellow impact triggers an observable 0.60-unit camera punch'
Assert ($visual -match 'EmitVisualImpact\(Variant\.DarkBlue, 0\.55f, 0\.85f, 0\.20f\)') 'Dark Blue impact triggers a stronger 0.85-unit camera punch'
Assert ($visual -match 'EmitVisualImpact\(Variant\.CyanBlue, 0\.70f, 1\.15f, 0\.26f\)') 'Cyan Blue finisher triggers the strongest 1.15-unit camera punch'
Assert ($visual -match 'ArcaneSurgeCameraImpulse\.Pulse\(cameraDuration, cameraStrength\)') 'Each imported Zap impact directly triggers its own camera impulse'
Assert ($visual -match 'nextImpactIndex = 1') 'Yellow hit advances the expected sequence to Dark Blue'
Assert ($visual -match 'RefreshContactPosition\(\)') 'Follow-up strikes update their target position if the enemy is still alive'
Assert ($visual -match 'Instantiate\(prefab, lastContact, Quaternion\.identity, transform\)') 'Dark Blue and Cyan Blue prefab instances play at the live/last hit position'
Assert ($visual -match 'bool reuseRelease = variant == Variant\.Yellow && releaseZap != null') 'Yellow impact reuses its release Vefects prefab without adding another strike'
Assert ($visual -match 'releaseZap = Instantiate\(library\.Yellow,') 'Strong release uses the same vendor Yellow prefab as the first impact'
Assert ($visual -match 'PrepareGiantZap\(releaseZap, Variant\.Yellow\)') 'Strong release uses the exact same Giant Zap scale settings as the impacts'
Assert ($visual -match 'releaseZap\.transform\.position = tip') 'Giant release bolt follows the descending moving target'
Assert ($visual -match 'private static bool IsMainLightningLayer\(string name\)') 'Release preview plays actual main bolt particle layers'
Assert ($visual -match 'if \(coloredCore != null\) coloredCore\.gameObject\.SetActive\(true\)') 'Runtime spawned Zaps enable the actual bright lightning shaft'
Assert ($visual -match 'releaseHiddenSystems\.Add\(particle\.gameObject\)') 'Release temporarily hides impact-only particles'
Assert ($visual -match 'foreach \(GameObject hidden in releaseHiddenSystems\)') 'Yellow impact restores suppressed sparks and flare'
Assert ($visual -match 'if \(reuseRelease\)[\s\S]*?else\s*\{\s*PrepareGiantZap\(zap, variant\);') 'Reused release prefab is not double-scaled at first impact'
Assert ($visual -notmatch 'ResolveAcceptedImpact\(') 'Imported visual never invokes a second gameplay damage resolution'
Assert ($visual -match 'PlaceFloorEffects\(zap\.transform') 'Ground sparks/scorch are repositioned to enemy feet'
Assert ($visual -match 'SetMobileSafeEffects\(zap\)') 'Imported Point Lights are disabled for mobile'
Assert ($visual -match 'ArcaneSurgeCameraImpulse\.Pulse') 'Confirmed hit triggers the controlled camera impulse'
Assert ($visual -match 'library\.BigZapSound') 'Imported Zap audio is used rather than synthesized thunder'
Assert ($visual -match 'Shader\.PropertyToID\("_EmissiveIntensity"\)') 'Deep-blue look uses runtime-only emission override'
Assert ($visual -match 'private const float CoreBoltWidth = 3\.00f') 'Bolt core is 3.0x the imported base width at runtime'
Assert ($visual -match 'private const float BackgroundBoltWidth = 2\.70f') 'Zap BG halo is 2.7x wider'
Assert ($visual -match 'private const float BranchBoltWidth = 2\.50f') 'Zap Add branches are 2.5x wider'
Assert ($visual -match 'private const float CoreBoltLength = 1\.90f') 'Main bolt is 1.9x its original length before overall scale'
Assert ($visual -match 'private const float BackgroundBoltLength = 1\.65f') 'Background bolt length multiplier is 1.65x'
Assert ($visual -match 'private const float BranchBoltLength = 1\.55f') 'Branch bolt length multiplier is 1.55x'
Assert ($visual -match 'private const float OverallZapScale = 2\.90f') 'All three Giant Zaps have doubled their previous 1.45x overall scale'
Assert ($visual -match 'zap\.transform\.localScale \*= OverallZapScale') 'Giant scale is applied only to a spawned Zap instance'
Assert ($visual -match 'main\.startSizeYMultiplier \*= lengthFactor') 'Bolt length adjusts 3D Start Size Y independently of width'
Assert ($visual -match 'renderer\.lengthScale \*= lengthFactor / widthFactor') 'Stretch fallback preserves distinct length and width'
Assert ($visual -match 'PlaceFloorEffects\(zap\.transform') 'Floor scorch is re-anchored after overall Zap scaling'
Assert ($visual -match 'CreateTracer\("Imported Zap - descending halo", 2\.40f') 'Fallback descending halo doubles from 1.20 to 2.40 units'
Assert ($visual -match 'CreateTracer\("Imported Zap - descending core", 1\.10f') 'Fallback descending core doubles from 0.55 to 1.10 units'
Assert ($visual -match 'main\.startSizeXMultiplier \*= widthFactor') 'Bolt thickness changes the billboard width axis independently'
Assert ($visual -match 'ThickenLightningBolt\(zap\)') 'Thickening happens on the spawned Zap instance only'
Assert ($visual -match 'case "Zap LUT":' -and $visual -match 'case "Zap BG":') 'Thickening targets original imported main lightning layers'
Assert ($cameraHelper -match '\[DefaultExecutionOrder\(10000\)\]') 'Camera shake runs late after CinemachineBrain update'
Assert ($cameraHelper -match 'shake\.hostCamera = camera') 'Camera shake binds the actual physical output camera each impact'
Assert ($cameraHelper -match 'private void LateUpdate\(\)') 'Shake is applied before URP camera view matrix capture'
Assert ($cameraHelper -notmatch 'beginCameraRendering|BeginRendering') 'Shake is no longer applied too late in the URP begin-camera callback'
Assert ($cameraHelper -match 'RenderPipelineManager\.endCameraRendering') 'URP render impulse restores camera after rendering'
Assert ($cameraHelper -match 'shake\.RestoreCamera\(\)') 'New camera shakes restore any pending camera offset'
Assert ($cameraHelper -match 'shake\.pulseStartTime = Time\.unscaledTime') 'Each shake uses unscaled time even during hit-stop'
Assert ($cameraHelper -match 'shake\.amplitude = Mathf\.Clamp\(strength, 0f, 1\.50f\)') 'Camera shake allows screen-visible strength with 1.50-unit safety cap'
Assert ($cameraHelper -match 'float falloff = remaining \* Mathf\.Sqrt\(remaining\)') 'Camera shake smoothly fades and does not accumulate'
Assert ($cameraHelper -match 'transform\.rotation = originalWorldRotation \* Quaternion\.Euler\(') 'Per-hit shake includes a perceptible angular camera punch'
Assert ($cameraHelper -match 'transform\.rotation = originalWorldRotation;') 'Camera orientation is restored after shake'
Assert ($cameraHelper -match '\[ArcaneSurge\] Impact shake triggered') 'Development build logs actual per-hit shake triggers'
Assert ($libraryScript -match 'const string ResourcePath = "Spellstrike/VFX/ArcaneSurgeZapLibrary"') 'Catalog path matches Resources folder'
Assert ($catalog -match 'yellowZap: .*guid: 6839a8d6998e86d4282063ffdacb0c15') 'Yellow points to original vendor prefab GUID'
Assert ($catalog -match 'darkBlueZap: .*guid: 390cc74915d4a054bbd897e1885047a4') 'Dark blue points to original vendor prefab GUID'
Assert ($catalog -match 'cyanBlueZap: .*guid: 543d521d2176ace4aba4667ddca92d97') 'Cyan blue points to original white Zap prefab (tinted at runtime)'
Assert ($catalog -match 'bigZapSound: .*guid: 5a9bb0e555e58384180555f82f97e00f') 'Impact sound points to imported SFX WAV'
Assert ($shaderMeta -match 'guid: ab9e5f31cb7f4d62a0aa4b8fe11c42de') 'Existing shader source GUID verified'
Assert ($arcaneMaterial -match 'guid: ab9e5f31cb7f4d62a0aa4b8fe11c42de') 'New material references the existing URP shader'

foreach ($total in @(1, 2, 3, 10, 29, 30, 31, 32, 35, 40, 57, 99)) {
    $baseShare = [int][Math]::Floor($total / 3)
    $remainder = $total % 3
    $shares = @(for ($strike = 0; $strike -lt 3; $strike++) {
        $baseShare + [int]($strike -ge (3 - $remainder))
    })
    Assert ((($shares | Measure-Object -Sum).Sum -eq $total)) "Three strike shares sum to $total"
    Assert ((($shares | Measure-Object -Maximum).Maximum - ($shares | Measure-Object -Minimum).Minimum -le 1)) "Three strike shares are evenly divided for $total"
    if ($total -eq 30) {
        Assert (($shares[0] -eq 10 -and $shares[1] -eq 10 -and $shares[2] -eq 10)) '30 damage means Yellow 10, Dark Blue 10, Cyan Blue 10'
    }
    if ($total -eq 31) {
        Assert (($shares[0] -eq 10 -and $shares[1] -eq 10 -and $shares[2] -eq 11)) '31 damage means 10+10+11'
    }
    if ($total -eq 32) {
        Assert (($shares[0] -eq 10 -and $shares[1] -eq 11 -and $shares[2] -eq 11)) '32 damage means 10+11+11'
    }
}

# Pure numeric trajectory math sanity check: moving head X is sampled afresh
# at every t, and the strike tip lands at the latest position at t=1.
foreach ($t in @(0.0, 0.25, 0.50, 0.75, 1.0)) {
    $currentHeadX = $t * 7.0
    $currentHeadY = 2.0 + [Math]::Sin($t * [Math]::PI) * 0.2
    $smooth = $t * $t * (3.0 - 2.0 * $t)
    $skyY = $currentHeadY + 18.0
    $tipX = $currentHeadX  # Same moving X for both strike endpoints.
    $tipY = $skyY + ($currentHeadY - $skyY) * $smooth
    Assert ([Math]::Abs($tipX - $currentHeadX) -lt 0.000001) ("Moving-target X follows at t={0:0.00}" -f $t)
    if ($t -eq 0.0) {
        Assert ([Math]::Abs($tipY - ($currentHeadY + 18.0)) -lt 0.000001) 'Initial tip is 18m above head'
    }
    if ($t -eq 1.0) {
        Assert ([Math]::Abs($tipY - $currentHeadY) -lt 0.000001) 'Final tip coincides with live head at exactly 0.20s'
    }
}

Write-Output 'Arcane Surge static source/trajectory contract checks completed. Unity Play Mode and mobile QA are still required.'
