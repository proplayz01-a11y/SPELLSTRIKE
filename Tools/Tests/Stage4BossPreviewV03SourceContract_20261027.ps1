# Non-Unity static preflight for the rescued Stage4 boss preview.
# Does NOT open the Editor, import assets, save scenes, or change files.
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)

$ErrorActionPreference = 'Stop'
$checks = 0

function Assert-Contract([bool]$condition, [string]$why) {
    if (-not $condition) { throw "FAIL: $why" }
    $script:checks++
}

function ProjectFile([string]$relative) {
    return Join-Path $ProjectRoot $relative
}

$production = ProjectFile 'Assets/Scenes/Stage4Scene/Stage4Scene.unity'
$sourceBackup = ProjectFile 'RecoveryBackups/Stage4BossEncounters_PreIntegration_20261027/Stage4Scene_before_boss_encounters.unity'
$v01 = ProjectFile 'Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027.unity'
$v02 = ProjectFile 'Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v02.unity'
$v03 = ProjectFile 'Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v03.unity'
$backupV03 = ProjectFile 'Library/Stage4BossEncounters_20261027/Stage4Scene_before_boss_preview_v03.unity'
$scriptFile = ProjectFile 'Assets/Editor/Stage4BossEncounterPreviewBuilder_20261027_V03.cs'
$titanScene = ProjectFile 'Assets/Scenes/Stage3Scene/Stage3Scene.unity'
$titanPrefab = ProjectFile 'Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/Titan_ProductionIntegration/S3N4_TitanV017_PhysicalCombatCandidate.prefab'
$stage4Meta = ProjectFile 'Assets/Scenes/Stage4Scene/Stage4Scene.unity.meta'

foreach ($path in @($production, $sourceBackup, $v01, $v02, $scriptFile, $titanScene, $titanPrefab, $stage4Meta)) {
    Assert-Contract (Test-Path -LiteralPath $path -PathType Leaf) "Required file present: $path"
}

$approvedHash = '7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E'
foreach ($path in @($production, $sourceBackup, $v01, $v02)) {
    Assert-Contract (((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash) -eq $approvedHash) "Original and prior version unchanged: $path"
}
Assert-Contract ((Get-Content -LiteralPath $stage4Meta -Raw) -match 'guid:\s*b69985f54b0efbb40b3bb1ff68432600') 'Production Stage4 GUID unchanged'
Assert-Contract (-not (Test-Path -LiteralPath $v03)) 'v03 preview slot unoccupied'
Assert-Contract (-not (Test-Path -LiteralPath ($v03 + '.meta'))) 'v03 preview meta slot unoccupied'
Assert-Contract (-not (Test-Path -LiteralPath $backupV03)) 'v03 backup slot unoccupied'
Assert-Contract (-not (Test-Path -LiteralPath ($backupV03 + '.meta'))) 'v03 backup meta slot unoccupied'

$code = Get-Content -LiteralPath $scriptFile -Raw
Assert-Contract ($code.Contains('class Stage4BossEncounterPreviewBuilder_20261027_V03')) 'v03 uses distinct C# editor class'
Assert-Contract ($code.Contains('BUILD Boss Encounters CONSULTATION PREVIEW 20261027 V03')) 'v03 uses distinct visible Unity menu'
Assert-Contract ($code.Contains('Stage4_BossEncounters_Preview_20261027_v03.unity')) 'v03 output filename is distinct'
Assert-Contract ($code.Contains('Stage4Scene_before_boss_preview_v03.unity')) 'v03 backup filename is distinct'
Assert-Contract ($code.Contains('File.Exists(Preview + ".meta")')) 'never-overwrite protection includes existing preview meta'
Assert-Contract ($code.Contains('File.Exists(Backup + ".meta")')) 'never-overwrite protection includes existing backup meta'
Assert-Contract ($code.Contains('SceneHash(Production) != ExpectedProductionHash')) 'production hash guarded'
Assert-Contract ($code.Contains('SceneManager.GetActiveScene().path != Stage3')) 'requires active Stage3Scene and does not switch active scene'
Assert-Contract ($code.Contains('CloneTitanRigFromStage3(')) 'dedicated Titan parent-rig cloning'
Assert-Contract ($code.Contains('original.GetComponentsInChildren<TitanEarthbreakerController>(true)')) 'searches Titan controller beneath parent'
Assert-Contract ($code.Contains('original.transform')) 'Titan parent original transform inspected'
Assert-Contract ($code.Contains('S3N4_Titan_EarthbreakerV017_BOSS')) 'exact Titan controller-bearing child named'
Assert-Contract ($code.Contains('clone.GetComponentsInChildren<TitanEarthbreakerController>(true)')) 'checks unique cloned child controller'
Assert-Contract ($code.Contains('titan.GetComponentsInChildren<TitanEarthbreakerController>(true)')) 'connects Stage4 trigger to cloned child controller'
Assert-Contract ($code.Contains('ValidatePreviewWiring(')) 'four-actor/four-node/three-gate pre-save validation exists'
Assert-Contract ($code.Contains('physics.Count(x => x.enabled) == 19')) 'preserves 19 enabled independent physics MeshColliders'
Assert-Contract ($code.Contains('allMap.Count(x => x.enabled) == 20')) 'preserves 20 enabled map MeshColliders including external bridge'
Assert-Contract ($code.Contains('sceneLods.Length == 11')) 'preserves 11 scene-wide LODGroups'
Assert-Contract ($code.Contains('allMap.Length == 21')) 'preserves 21 map MeshColliders'
Assert-Contract ($code.Contains('extras[0].name == "Bridge_Node1_Node2_wood_light"')) 'preserves named extra bridge collider'
Assert-Contract ($code.Contains('EditorSceneManager.SaveScene(previewScene, Preview)')) 'saves only versioned preview scene'
Assert-Contract (-not $code.Contains('EditorSceneManager.SaveScene(previewScene, Production)')) 'never saves production scene'

$prefab = Get-Content -LiteralPath $titanPrefab -Raw
$source = Get-Content -LiteralPath $titanScene -Raw
Assert-Contract ($prefab.Contains('m_Name: S3N4_Titan_EarthbreakerV017_BOSS')) 'actual Titan prefab contains expected child GameObject'
Assert-Contract ($prefab.Contains('Assembly-CSharp::TitanEarthbreakerController')) 'actual Titan prefab contains Titan controller'
Assert-Contract ($prefab.Contains('Assembly-CSharp::EnemyHealth')) 'actual Titan prefab contains EnemyHealth'
Assert-Contract ($source.Contains('S3N4_TitanV017_ProductionRig')) 'actual Stage3 scene uses named enclosing Titan rig'
Assert-Contract ($source.Contains('a213f73498837b94bb00bd784b4b089c')) 'actual Stage3 scene links the Titan production prefab GUID'

$opens = ([regex]::Matches($code, '\{')).Count
$closes = ([regex]::Matches($code, '\}')).Count
Assert-Contract ($opens -eq $closes) 'v03 source braces balanced (static heuristic only)'

Write-Output ("PASS: {0} Stage4 v03 offline contract checks. Original/v01/v02 SHA256 {1}. NO Unity Editor execution." -f $checks, $approvedHash)
