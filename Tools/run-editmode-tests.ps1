param(
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

if (-not $env:UNITY_EDITOR_PATH) {
    throw "UNITY_EDITOR_PATH is not set. Point it to Unity.exe."
}

$resultsDir = Join-Path $ProjectPath "TestResults"
$logsDir = Join-Path $ProjectPath "Logs"
New-Item -ItemType Directory -Force -Path $resultsDir, $logsDir | Out-Null

& $env:UNITY_EDITOR_PATH `
    -batchmode `
    -quit `
    -accept-apiupdate `
    -projectPath $ProjectPath `
    -runTests `
    -testPlatform EditMode `
    -testResults (Join-Path $resultsDir "editmode-results.xml") `
    -logFile (Join-Path $logsDir "editmode.log")

if ($LASTEXITCODE -ne 0) {
    throw "Unity Edit Mode tests failed with exit code $LASTEXITCODE. See Logs/editmode.log."
}
