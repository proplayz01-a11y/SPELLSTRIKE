# Read-only contract checks for the Stage 4 boss consultation integration.
# Does not open Unity or modify scenes, sources, PlayerPrefs or saves.
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)

$ErrorActionPreference = 'Stop'
$checks = New-Object 'System.Collections.Generic.List[string]'

function Need([bool]$condition, [string]$description) {
    if (-not $condition) { throw "FAIL: $description" }
    [void]$checks.Add($description)
}

function PathInProject([string]$relative) {
    return Join-Path $ProjectRoot $relative
}

$stage4 = PathInProject 'Assets/Scenes/Stage4Scene/Stage4Scene.unity'
$stage4Meta = PathInProject 'Assets/Scenes/Stage4Scene/Stage4Scene.unity.meta'
$backup = PathInProject 'RecoveryBackups/Stage4BossEncounters_PreIntegration_20261027/Stage4Scene_before_boss_encounters.unity'
$mapJson = PathInProject 'Assets/Stage 4/ConvergenceCitadel_v003/Validation/ConvergenceCitadel_MapManifest.json'
$reference = PathInProject 'Assets/Stage 4/ConvergenceCitadel_v003/Encounters/Stage4_EncounterObjectiveReference.json'

foreach ($p in @($stage4, $stage4Meta, $backup, $mapJson, $reference)) {
    Need (Test-Path -LiteralPath $p -PathType Leaf) "Found $(Split-Path $p -Leaf)"
}
Need ((Get-FileHash -LiteralPath $stage4 -Algorithm SHA256).Hash -eq
      (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash) 'Production Stage4 is byte-identical to pre-integration backup'
Need ((Get-Content -LiteralPath $stage4Meta -Raw) -match 'guid:\s*b69985f54b0efbb40b3bb1ff68432600') 'Production scene GUID unchanged'

$sceneText = Get-Content -LiteralPath $stage4 -Raw
Need ($sceneText -match 'm_Name: Stage4_ConvergenceCitadel_PRODUCTION') 'Production world root exists'
Need ($sceneText -match 'm_Name: Stage4ProgressionManager') 'Production StageProgressionManager exists'
Need ($sceneText -match 'nodeCount: 4') 'Production progression has exactly four configured nodes'
Need ($sceneText -match 'Bridge04_SmoothCollision_Validated_Traversable') 'Bridge04 validated proxy preserved'
Need (-not ($sceneText -match 'STAGE4_Node4_VanderPoel_PREVIEW')) 'No consultation actor was prematurely installed into production'

$map = Get-Content -LiteralPath $mapJson -Raw | ConvertFrom-Json
$referenceObj = Get-Content -LiteralPath $reference -Raw | ConvertFrom-Json
Need ($referenceObj.linear_route.Count -eq 5) 'Encounter reference specifies all five islands'
Need ($map.validation.five_playable_islands -eq $true) 'Map manifest verifies five playable islands'
Need ($map.validation.four_adjacent_bridges -eq $true) 'Map manifest verifies four bridges'

foreach ($marker in @(
    'Node1_BossSpawn','Node2_BossSpawn','Node3_BossSpawn','Boss_BossSpawn',
    'Node1_Entry','Node2_Entry','Node3_Entry','Boss_Entry',
    'Bridge_Gate_02','Bridge_Gate_03','Bridge_Gate_04'
)) {
    Need ($null -ne $map.gameplay_markers.$marker) "Marker $marker exists"
}

$sources = @(
    @{ file='Assets/Scenes/SampleScene.unity'; name='Hollow Knight'; type='HollowKnightController' },
    @{ file='Assets/Scenes/Stage2Scene/Stage2Scene.unity'; name='DrownedCaptain'; type='DrownedCaptainController' },
    @{ file='Assets/Scenes/Stage3Scene/Stage3Scene.unity'; name='S3N4_TitanV017_ProductionRig'; type='TitanEarthbreakerController' }
)
foreach ($boss in $sources) {
    $path = PathInProject $boss.file
    Need (Test-Path -LiteralPath $path -PathType Leaf) "Boss source scene exists: $($boss.file)"
    $content = Get-Content -LiteralPath $path -Raw
    Need ($content -match [regex]::Escape($boss.name)) "Source scene includes $($boss.name)"
    Need ($content -match [regex]::Escape($boss.type)) "Source scene includes $($boss.type)"
}
Need (Test-Path -LiteralPath (PathInProject 'Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Prefabs/VanderPoel_CinematicActor.prefab')) 'Real Vander Poel cinematic visual asset exists'

Write-Output ("PASS: {0} Stage4 source-contract checks. Scene SHA256 {1}" -f
    $checks.Count, (Get-FileHash -LiteralPath $stage4 -Algorithm SHA256).Hash)
