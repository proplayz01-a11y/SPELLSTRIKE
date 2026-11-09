# Run with PowerShell -ExecutionPolicy Bypass -File.
# Never closes applications or overwrites completed mocap data.
$ErrorActionPreference = 'Stop'
$base = Join-Path $env:USERPROFILE 'Documents\Codex\MocapLab'
$repo = Join-Path $base 'mixamo-llm-mocap'
$plate = Join-Path $repo 'plates\nevali_blockstack_flow_20261023'
$source = Join-Path $plate 'nevali_blockstack_source.mp4'
$out = Join-Path $plate 'landmarks.json'
$model = Join-Path $plate 'smplx.npz'
$estimator = Join-Path $repo 'pipeline\estimate_pose_gvhmr.py'
$launcher = Join-Path $base 'Use-Mocap.ps1'
if (Get-Process -Name 'VALORANT-Win64-Shipping' -ErrorAction SilentlyContinue) {
    throw 'HOLD: VALORANT is running. No mocap processes started.'
}
if (-not (Test-Path -LiteralPath $source)) { throw "Missing source MP4: $source" }
if (-not (Test-Path -LiteralPath $launcher)) { throw "Missing mocap launcher: $launcher" }
if ((Test-Path -LiteralPath $model)) {
    Write-Output "Existing SMPL-X output found; no overwrite: $model"
    exit 0
}
$os = Get-CimInstance Win32_OperatingSystem
$freeRamMB = [math]::Round($os.FreePhysicalMemory / 1024)
$line = (& nvidia-smi --query-gpu=memory.free --format=csv,noheader,nounits 2>&1 | Select-Object -First 1)
$freeVramMB = [int]($line.ToString().Trim())
Write-Output "Preflight: RAM free $freeRamMB MiB, GPU VRAM free $freeVramMB MiB."
if ($freeRamMB -lt 6500 -or $freeVramMB -lt 4800) {
    throw 'HOLD: insufficient free RAM/VRAM (needs >=6500 MiB RAM and >=4800 MiB VRAM before launching). Save and close unused Blender/Unity apps manually before retrying. Nothing launched.'
}
. $launcher
Set-Location -LiteralPath $MocapRepo
$env:MOCAP_BATCH_SIZE = '1'
$env:PYTORCH_CUDA_ALLOC_CONF = 'max_split_size_mb:128'
Write-Output "Running local GVHMR SMPL-X estimation from $source"
& $MocapPython $estimator --video $source --out $out --smplx-only --feet-python $MocapMediaPipePython
if ($LASTEXITCODE -ne 0) { throw "Estimator exited with code $LASTEXITCODE. No output should be treated as valid." }
if (-not (Test-Path -LiteralPath $model)) { throw "Estimator reported success but SMPL-X output is missing: $model" }
Write-Output "POSE_ESTIMATION_COMPLETE: $model"
