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
$resultsPath = Join-Path $resultsDir "editmode-results.xml"

$arguments = @(
    "-batchmode",
    "-accept-apiupdate",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $resultsPath,
    "-logFile", (Join-Path $logsDir "editmode.log")
)

$unityProcess = Start-Process `
    -FilePath $env:UNITY_EDITOR_PATH `
    -ArgumentList $arguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($unityProcess.ExitCode -ne 0) {
    throw "Unity Edit Mode tests failed with exit code $($unityProcess.ExitCode). See Logs/editmode.log."
}

if (-not (Test-Path -LiteralPath $resultsPath)) {
    throw "Unity Edit Mode tests did not produce TestResults/editmode-results.xml."
}

[xml]$results = Get-Content -Raw -LiteralPath $resultsPath
$testCases = @($results.SelectNodes("//test-case"))
if ($testCases.Count -eq 0) {
    throw "Unity Edit Mode test run executed 0 tests."
}

$nonPassingTests = @($testCases | Where-Object { $_.result -ne "Passed" })
if ($nonPassingTests.Count -gt 0) {
    throw "Unity Edit Mode results contain $($nonPassingTests.Count) non-passing test(s)."
}

Write-Host "Unity Edit Mode tests passed: $($testCases.Count) test(s)."
