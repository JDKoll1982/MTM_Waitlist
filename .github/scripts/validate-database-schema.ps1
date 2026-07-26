param(
    [string]$ConnectionStringEnvironmentVariable = 'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING',
    [string]$ValidationSqlPath = (Join-Path $PSScriptRoot '..\..\Database\Validation\0003__validate_startup_schema.sql')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

if (-not (Test-Path $ValidationSqlPath)) {
    throw "Validation SQL file not found: $ValidationSqlPath"
}

$connectionString = [Environment]::GetEnvironmentVariable($ConnectionStringEnvironmentVariable)
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host "Skipping database schema validation because $ConnectionStringEnvironmentVariable is not set."
    exit 0
}

$nugetRoot = $env:NUGET_PACKAGES
if ([string]::IsNullOrWhiteSpace($nugetRoot)) {
    $nugetRoot = Join-Path $HOME '.nuget\packages'
}

$mysqlConnectorAssembly = Get-ChildItem -Path (Join-Path $nugetRoot 'mysqlconnector') -Recurse -Filter 'MySqlConnector.dll' |
Where-Object { $_.FullName -match 'netstandard2\.0|net6\.0|net8\.0' } |
Select-Object -First 1

if ($null -eq $mysqlConnectorAssembly) {
    throw 'Unable to locate MySqlConnector.dll in the NuGet package cache.'
}

[System.Reflection.Assembly]::LoadFrom($mysqlConnectorAssembly.FullName) | Out-Null

$connectionBuilder = New-Object MySqlConnector.MySqlConnectionStringBuilder($connectionString)
$connectionBuilder['Connection Timeout'] = 10
$effectiveConnectionString = $connectionBuilder.ConnectionString
$sql = Get-Content -Path $ValidationSqlPath -Raw

$connection = New-Object MySqlConnector.MySqlConnection($effectiveConnectionString)
try {
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandText = $sql

    $reader = $command.ExecuteReader()
    $issues = New-Object System.Collections.Generic.List[object]

    try {
        while ($reader.Read()) {
            $issues.Add([pscustomobject]@{
                    IssueType     = $reader.GetString(0)
                    ObjectName    = $reader.GetString(1)
                    ExpectedValue = $reader.GetString(2)
                    ActualValue   = $reader.GetString(3)
                })
        }
    }
    finally {
        $reader.Close()
    }

    if ($issues.Count -gt 0) {
        Write-Host 'Database schema validation failed:'
        foreach ($issue in $issues) {
            Write-Host ("- {0} :: {1} (expected {2}, actual {3})" -f $issue.IssueType, $issue.ObjectName, $issue.ExpectedValue, $issue.ActualValue)
        }
        exit 1
    }

    Write-Host 'Database schema validation passed.'
}
finally {
    $connection.Dispose()
}
