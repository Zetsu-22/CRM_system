param(
  [switch]$SkipDb,
  [switch]$SkipStart,
  [string]$PgPassword = 'postgres'
)

$ErrorActionPreference = 'Stop'

function Test-CommandExists {
  param([string]$Name)
  return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Find-Psql {
  if (Test-CommandExists 'psql') {
    return 'psql'
  }

  $candidates = @(
    'C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe',
    'C:\\Program Files\\PostgreSQL\\16\\bin\\psql.exe',
    'C:\\Program Files\\PostgreSQL\\15\\bin\\psql.exe',
    'C:\\Program Files\\PostgreSQL\\14\\bin\\psql.exe'
  )

  foreach ($c in $candidates) {
    if (Test-Path $c) {
      return $c
    }
  }

  return $null
}

function Ensure-Database {
  param(
    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,
    [Parameter(Mandatory = $true)]
    [string]$PsqlPath
  )

  $query = "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName'"
  $exists = & $PsqlPath -h localhost -p 5432 -U postgres -d postgres -tAc $query

  if ([string]::IsNullOrWhiteSpace($exists)) {
    Write-Host "Creating database $DatabaseName..."
    & $PsqlPath -h localhost -p 5432 -U postgres -d postgres -c "CREATE DATABASE $DatabaseName;" | Out-Null
  }
  else {
    Write-Host "$DatabaseName already exists."
  }
}

Write-Host "== CRM System runner =="

Write-Host "Building solution..."
dotnet build .\CrmSystem.slnx

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
    $env:PGPASSWORD = $PgPassword
    dotnet ef database update --no-build --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context AuthDbContext
    if ($LASTEXITCODE -ne 0) { throw "AuthDb migrations failed." }

    dotnet ef database update --no-build --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context CrmDbContext
    if ($LASTEXITCODE -ne 0) { throw "CrmDb migrations failed." }
  }
  else {
    $psql = Find-Psql
    if ($psql) {
      Write-Host "Docker not found. Will try local PostgreSQL via psql..."

      $env:PGPASSWORD = $PgPassword
      Ensure-Database -DatabaseName 'crm_auth' -PsqlPath $psql
      Ensure-Database -DatabaseName 'crm_main' -PsqlPath $psql

      Write-Host "Applying EF Core migrations..."
      dotnet ef database update --no-build --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context AuthDbContext
      if ($LASTEXITCODE -ne 0) { throw "AuthDb migrations failed." }

      dotnet ef database update --no-build --project .\CrmSystem.Persistence\CrmSystem.Persistence.csproj --startup-project .\CrmSystem.Api\CrmSystem.Api.csproj --context CrmDbContext
      if ($LASTEXITCODE -ne 0) { throw "CrmDb migrations failed." }
    }
    else {
      Write-Warning "Neither docker nor psql found. Skipping DB setup. Install Docker Desktop or PostgreSQL (psql) to enable automatic DB creation."
    }
  }
}

if ($SkipStart) {
  Write-Host "DB setup and migrations done. Skipping start (SkipStart flag set)."
  exit 0
}

Write-Host "Starting backend and frontend..."

Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run --project .\CrmSystem.Api\CrmSystem.Api.csproj"
Start-Process -NoNewWindow -FilePath "npm.cmd" -ArgumentList "run dev" -WorkingDirectory (Join-Path $PSScriptRoot 'crm-web')

Write-Host "Backend: http://localhost:5035"
Write-Host "Frontend: check console output of Vite (usually http://localhost:5173)"
Write-Host "Press Ctrl+C to stop (processes were started in background)."
