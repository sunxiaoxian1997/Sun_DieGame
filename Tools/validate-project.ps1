param(
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ExecuteMethod = "CorpseMechanism.Editor.ProjectAutomation.ValidateProject"
)

$ErrorActionPreference = "Stop"

if (-not $env:UNITY_EDITOR_PATH) {
    throw "UNITY_EDITOR_PATH is not set. Point it to Unity.exe."
}

$logsDir = Join-Path $ProjectPath "Logs"
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

$arguments = @(
    "-batchmode",
    "-quit",
    "-accept-apiupdate",
    "-projectPath", $ProjectPath,
    "-executeMethod", $ExecuteMethod,
    "-logFile", (Join-Path $logsDir "validation.log")
)

$unityProcess = Start-Process `
    -FilePath $env:UNITY_EDITOR_PATH `
    -ArgumentList $arguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($unityProcess.ExitCode -ne 0) {
    throw "Unity project validation failed with exit code $($unityProcess.ExitCode). See Logs/validation.log."
}
