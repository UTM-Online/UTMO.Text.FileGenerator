param(
    [string]$SolutionPath = "src/UTMO.Text.FileGenerator.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking NuGet packages for known vulnerabilities..." -ForegroundColor Cyan

$auditOutput = dotnet list $SolutionPath package --vulnerable --include-transitive 2>&1
$auditOutput

if ($auditOutput -match "has the following vulnerable packages") {
    Write-Host "`nVulnerable packages detected." -ForegroundColor Red
    exit 1
}

Write-Host "`nNo vulnerable packages detected." -ForegroundColor Green
exit 0

