param(
  [switch]$SkipDb
)

$ErrorActionPreference = 'Stop'

function Test-CommandExists {
  param([string]$Name)
  return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

Write-Host "== CRM System runner =="

if (-not $SkipDb) {
  if (Test-CommandExists 'docker') {
    Write-Host "Starting PostgreSQL via docker compose..."
    docker compose up -d

    Write-Host "Waiting for PostgreSQL to accept connections..."
    $ready = $false
    for ($i=0; $i -lt 60; $i++) {
      try {
        docker exec crm_postgres pg_isready -U postgres | Out-Null
        $ready = $true
        break
      } catch {
        Start-Sleep -Seconds 1
      }
    }

    if (-not $ready) {
      throw "PostgreSQL container did not become ready in time."
    }

    Write-Host "Applying EF Core migrations..."
    dotnet ef database update --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context AuthDbContext
    dotnet ef database update --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context CrmDbContext
  }
  elseif (Test-CommandExists 'psql') {
    Write-Host "Docker not found. Will try local PostgreSQL via psql..."
    psql -U postgres -c "CREATE DATABASE crm_auth;" 2>$null
    psql -U postgres -c "CREATE DATABASE crm_main;" 2>$null

    Write-Host "Applying EF Core migrations..."
    dotnet ef database update --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context AuthDbContext
    dotnet ef database update --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context CrmDbContext
  }
  else {
    Write-Warning "Neither docker nor psql found. Skipping DB setup. Install Docker Desktop or PostgreSQL (psql) to enable automatic DB creation."
  }
}

Write-Host "Starting backend and frontend..."

Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run --project .\CrmSystem.Api\CrmSystem.Api.csproj"
Start-Process -NoNewWindow -FilePath "npm.cmd" -ArgumentList "run dev" -WorkingDirectory (Join-Path $PSScriptRoot 'crm-web')

Write-Host "Backend: http://localhost:5035"
Write-Host "Frontend: check console output of Vite (usually http://localhost:5173)"
Write-Host "Press Ctrl+C to stop (processes were started in background)."
