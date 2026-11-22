# Offline/source-only contract for combat feedback V01. Never launches Unity.
# Does not assert that TMPro imported or rendered correctly on PC/Android.
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)

$ErrorActionPreference = 'Stop'
$checks = New-Object 'System.Collections.Generic.List[string]'

function Need([bool]$condition, [string]$description) {
    if (-not $condition) { throw "FAIL: $description" }
    [void]$checks.Add($description)
}

function Source([string]$path) {
    $absolute = Join-Path $ProjectRoot $path
    Need (Test-Path -LiteralPath $absolute -PathType Leaf) "Source exists: $path"
    return Get-Content -LiteralPath $absolute -Raw
}

$popup = Source 'Assets/Scripts/EnemyScripts/FloatingDamageNumbersV01.cs'
$meta = Source 'Assets/Scripts/EnemyScripts/FloatingDamageNumbersV01.cs.meta'
Need ($meta -match 'guid: 09bc2841a63a40b79c2e860efa7f3f0b') 'Stable V01 Unity script GUID'

foreach ($name in @('EnemyHealth','EnemyController','BrambleSpriteController')) {
    $source = Source "Assets/Scripts/EnemyScripts/$name.cs"
    Need ($source -match 'float beforeDamage = currentHealth;\s*currentHealth = Mathf.Max\(0f, currentHealth - damage\);\s*FloatingDamageNumbersV01.Show\(this, beforeDamage - currentHealth\);') "$name publishes only its actual post-clamp HP delta"
    Need (($source.Split([string[]]@('FloatingDamageNumbersV01.Show('), [StringSplitOptions]::None).Count - 1) -eq 1) "$name owns exactly one notification site"
}

Need ($popup -match 'if \(!Application.isPlaying \|\| enemy == null \|\| confirmedLoss <= 0f') 'Play-only, positive damage and null guard'
Need ($popup -match 'float.IsNaN\(confirmedLoss\) \|\| float.IsInfinity\(confirmedLoss\)') 'Nonfinite popup rejection'
Need ($popup -match 'private const int PoolSize = 20') 'Bounded 20-text pool'
Need ($popup -match 'if \(slot == null\)' -and $popup -match 'slot = CreateSlot\(ordinal\)') 'Lazily allocates labels only on pool demand'
Need ($popup -match 'candidate != null && !candidate.Active' -and $popup -match 'nextSlot = \(ordinal \+ 1\) % PoolSize;') 'Reuses inactive labels before growing bounded pool'
Need ($popup -match 'Time.unscaledDeltaTime' -and $popup -match 'slot.Age / LifetimeSeconds') 'Bounded popup lifetime'
Need ($popup -match 'Mathf.SmoothStep' -and $popup -match 'slot.Label.color') 'Per-frame alpha fade'
Need ($popup -match 'Vector2.up \* \(RisePixels \* progress\)') 'Popup rises over its lifetime'
Need ($popup -match 'WorldToScreenPoint\(ResolveAnchor\(enemy\)\)' -and
      $popup -match 'screen.z <= viewCamera.nearClipPlane') 'World anchor projection with behind-camera culling'
Need ($popup -match 'RenderMode.ScreenSpaceOverlay' -and $popup -match 'typeof\(TextMeshProUGUI\)') 'Screen-space pooled TMPro presentation'
Need ($popup -match 'slot.Label.text = loss.ToString' -and $popup -notmatch 'Instantiate\(') 'Text updated only on hit without Instantiate'
Need ($popup -notmatch 'TakeDamage\(' -and $popup -notmatch 'currentHealth\s*=') 'Presentation owns no health mutation'
Need ($popup -notmatch 'DontDestroyOnLoad|SceneManager|Physics\.') 'No persistent scene state or per-hit physics effects'

# Standalone contract math examples. These do not substitute for Unity tests.
foreach ($case in @(
    @{ before=100.; damage=12.; expected=12. },
    @{ before=8.; damage=50.; expected=8. },
    @{ before=8.; damage=0.; expected=0. },
    @{ before=8.; damage=-2.; expected=-2. }
)) {
    $after = [Math]::Max(0.0, $case.before - $case.damage)
    $loss = $case.before - $after
    Need ([Math]::Abs($loss - $case.expected) -lt 0.000001) "Confirmed-loss formula $($case.before) HP, $($case.damage) requested"
}

Write-Output ("PASS: {0} V01 source-contract checks; Unity/physical-A05s visual tests remain PENDING." -f $checks.Count)
