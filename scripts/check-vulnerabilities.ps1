param(
    [string]$SolutionPath = "src/UTMO.Text.FileGenerator.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking NuGet packages for known vulnerabilities..." -ForegroundColor Cyan

$auditOutput = dotnet list $SolutionPath package --vulnerable --include-transitive 2>&1
$commandExitCode = $LASTEXITCODE
$auditOutput

if ($commandExitCode -ne 0) {
    throw "NuGet vulnerability audit command failed with exit code $commandExitCode.`n$auditOutput"
}

if ($auditOutput -match "has the following vulnerable packages") {
    Write-Host "`nVulnerable packages detected." -ForegroundColor Red
    throw "Vulnerable packages were detected by the NuGet audit."
}

Write-Host "`nNo vulnerable packages detected." -ForegroundColor Green

