param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$chargeRoot = Split-Path -Parent $PSScriptRoot
Push-Location $chargeRoot
try {
    if (Get-Process -Name '*VALORANT*' -ErrorAction SilentlyContinue) {
        throw 'VALORANT is running; development checks are paused.'
    }
    $chargeOutput = 'tmp/basic-charge-fade'
    New-Item -ItemType Directory -Force -Path $chargeOutput | Out-Null
    $chargeCompiler = "$UnityData/DotNetSdkRoslyn/csc.dll"
    $chargeDotnet = "$UnityData/NetCoreRuntime/dotnet.exe"
    $chargeCoreRefs = @('System.Private.CoreLib.dll', 'System.Runtime.dll', 'System.Console.dll') |
        ForEach-Object { "$UnityData/NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21/$_" }
    $chargeArgs = @('-nologo', '-target:exe', '-nostdlib+', "-out:$chargeOutput/FadeTimingChecks.dll")
    $chargeArgs += $chargeCoreRefs | ForEach-Object { '-r:"' + $_ + '"' }
    $chargeArgs += @('Assets/Scripts/VFX/BasicAttackChargeTiming.cs', 'Tools/Tests/BasicAttackChargeTimingChecks.cs')
    Set-Content -LiteralPath "$chargeOutput/timing.rsp" -Value $chargeArgs
    & $chargeDotnet $chargeCompiler "@$chargeOutput/timing.rsp"
    if ($LASTEXITCODE -ne 0) { throw 'Timing check compilation failed.' }
    & $chargeDotnet exec --runtimeconfig Tools/Tests/FadeTimingChecks.runtimeconfig.json "$chargeOutput/FadeTimingChecks.dll" |
        Tee-Object -FilePath "$chargeOutput/timing-result.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Timing checks failed.' }

    $chargeUnityRefs = @((Get-ChildItem -LiteralPath "$UnityData/NetStandard/ref/2.1.0" -Filter '*.dll').FullName) +
        @((Get-ChildItem -LiteralPath "$UnityData/Managed/UnityEngine" -Filter '*.dll').FullName)
    $chargeArgs = @('-nologo', '-target:library', '-nostdlib+', '-define:UNITY_EDITOR', "-out:$chargeOutput/BasicCharge.CompileCheck.dll")
    $chargeArgs += $chargeUnityRefs | ForEach-Object { '-r:"' + $_ + '"' }
    $chargeArgs += @('Assets/Scripts/VFX/BasicAttackChargeTiming.cs', 'Assets/Scripts/VFX/BasicAttackChargeBinding.cs',
        'Assets/Scripts/VFX/BasicAttackChargeVfx.cs', 'Assets/Editor/BasicAttackChargePreview.cs')
    Set-Content -LiteralPath "$chargeOutput/unity.rsp" -Value $chargeArgs
    & $chargeDotnet $chargeCompiler "@$chargeOutput/unity.rsp" | Tee-Object -FilePath "$chargeOutput/compile-result.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Targeted Unity C# compilation failed.' }
    'PASS: targeted runtime/preview C# compilation. Graph rendering and gameplay require Unity checks.'
}
finally { Pop-Location }
