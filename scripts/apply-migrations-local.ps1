#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Applies EF Core migrations to local PostgreSQL database.

.DESCRIPTION
    This script applies Entity Framework Core migrations to the local development
    PostgreSQL database. It assumes PostgreSQL is running via docker-compose.test.yml.

.PARAMETER ConnectionString
    Optional. Override the default connection string.

.EXAMPLE
    .\scripts\apply-migrations-local.ps1

.EXAMPLE
    .\scripts\apply-migrations-local.ps1 -ConnectionString "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"

.NOTES
    Prerequisites:
    - .NET 9.0 SDK installed
    - PostgreSQL running (via docker-compose -f docker-compose.test.yml up -d)
    - EF Core tools installed (dotnet tool install --global dotnet-ef)
#>

param(
    [string]$ConnectionString = "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"
)

$ErrorActionPreference = "Stop"

# Colors for output
$ColorSuccess = "Green"
$ColorInfo = "Cyan"
$ColorWarning = "Yellow"
$ColorError = "Red"

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $ColorSuccess
}

function Write-Warn {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $ColorWarning
}

function Write-Fail {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $ColorError
}

# Get script directory and repository root
$ScriptDir = Split-Path -Parent $PSCommandPath
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "`n========================================" -ForegroundColor $ColorInfo
Write-Host "  Country Service - Apply Migrations" -ForegroundColor $ColorInfo
Write-Host "========================================`n" -ForegroundColor $ColorInfo

# Step 1: Check prerequisites
Write-Step "Checking prerequisites..."

# Check dotnet CLI
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail ".NET SDK not found. Please install .NET 9.0 SDK."
    exit 1
}

$dotnetVersion = dotnet --version
Write-Success ".NET SDK found: $dotnetVersion"

# Check dotnet-ef tool
try {
    $efVersion = dotnet ef --version 2>&1 | Select-Object -First 1
    Write-Success "EF Core tools found: $efVersion"
} catch {
    Write-Warn "EF Core tools not found. Installing..."
    dotnet tool install --global dotnet-ef
    Write-Success "EF Core tools installed"
}

# Step 2: Check PostgreSQL connectivity
Write-Step "Checking PostgreSQL connectivity..."

$env:CountryServiceDbContext = $ConnectionString

# Test PostgreSQL connection using docker exec (assumes docker-compose is running)
try {
    $pgCheck = docker exec country-service-postgres pg_isready -U postgres 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "PostgreSQL is ready"
    } else {
        throw "PostgreSQL not ready"
    }
} catch {
    Write-Fail "Cannot connect to PostgreSQL. Is docker-compose running?"
    Write-Host ""
    Write-Host "To start PostgreSQL and Redis:" -ForegroundColor $ColorWarning
    Write-Host "  docker-compose -f docker-compose.test.yml up -d" -ForegroundColor $ColorWarning
    Write-Host ""
    exit 1
}

# Step 3: Apply migrations
Write-Step "Applying migrations..."

Push-Location $RepoRoot
try {
    # Apply migrations
    dotnet ef database update `
        --project Maliev.CountryService.Data `
        --startup-project Maliev.CountryService.Api `
        --verbose

    if ($LASTEXITCODE -ne 0) {
        throw "Migration failed with exit code $LASTEXITCODE"
    }

    Write-Success "Migrations applied successfully"
} catch {
    Write-Fail "Migration failed: $_"
    Pop-Location
    exit 1
} finally {
    Pop-Location
}

# Step 4: Verify migration
Write-Step "Verifying migration..."

try {
    $tables = docker exec country-service-postgres psql -U postgres -d country_service_app_db -t -c "\dt" 2>&1

    if ($tables -match "countries" -and $tables -match "__ef_migrations_history") {
        Write-Success "Database schema verified"
        Write-Host ""
        Write-Host "Tables found:" -ForegroundColor $ColorInfo
        docker exec country-service-postgres psql -U postgres -d country_service_app_db -c "\dt"
    } else {
        Write-Warn "Could not verify all expected tables"
    }
} catch {
    Write-Warn "Could not verify migration (this may be okay if psql is not available in container)"
}

Write-Host "`n========================================" -ForegroundColor $ColorSuccess
Write-Host "  Migration Complete!" -ForegroundColor $ColorSuccess
Write-Host "========================================`n" -ForegroundColor $ColorSuccess

Write-Host "Next steps:" -ForegroundColor $ColorInfo
Write-Host "  1. Run the API: cd Maliev.CountryService.Api && dotnet run" -ForegroundColor $ColorInfo
Write-Host "  2. Access API: http://localhost:5000/countries/v1/countries" -ForegroundColor $ColorInfo
Write-Host "  3. View Scalar UI: http://localhost:5000/countries/v1/scalar/v1`n" -ForegroundColor $ColorInfo
