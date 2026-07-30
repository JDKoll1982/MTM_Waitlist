param(
    [string[]]$ConnectionStringEnvironmentVariables = @('MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING_HOME', 'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING_WORK', 'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING'),
    [string]$DatabaseName = 'mtm_waitlist',
    [string]$ValidationSqlPath = (Join-Path $PSScriptRoot '..\..\Database\Validation\startup_schema\validate.sql')
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message)

    Write-Host ("[db-validate] {0}" -f $Message)
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

Write-Log "Repo root: $repoRoot"
Write-Log "Validation file: $ValidationSqlPath"

if (-not (Test-Path $ValidationSqlPath)) {
    throw "Validation SQL file not found: $ValidationSqlPath"
}

$buildOutputRoot = Join-Path $repoRoot 'bin'
$mysqlConnectorAssembly = Get-ChildItem -Path $buildOutputRoot -Recurse -Filter 'MySqlConnector.dll' |
Where-Object { $_.FullName -match 'win-x64' } |
Select-Object -First 1

if ($null -eq $mysqlConnectorAssembly) {
    throw 'Unable to locate MySqlConnector.dll in the build output.'
}

Write-Log "Using MySqlConnector assembly: $($mysqlConnectorAssembly.FullName)"

$nugetRoot = $env:NUGET_PACKAGES
if ([string]::IsNullOrWhiteSpace($nugetRoot)) {
    $nugetRoot = Join-Path $HOME '.nuget\packages'
}

$loggingAbstractionsAssembly = Get-ChildItem -Path (Join-Path $nugetRoot 'microsoft.extensions.logging.abstractions') -Recurse -Filter 'Microsoft.Extensions.Logging.Abstractions.dll' |
Select-Object -First 1

if ($null -eq $loggingAbstractionsAssembly) {
    throw 'Unable to locate Microsoft.Extensions.Logging.Abstractions in the NuGet package cache.'
}

Write-Log "Using logging abstractions assembly: $($loggingAbstractionsAssembly.FullName)"

if (-not ('IsolatedAssemblyLoadContextV2' -as [type])) {
    Add-Type -TypeDefinition @'
using System.Reflection;
using System.Runtime.Loader;

public sealed class IsolatedAssemblyLoadContextV2 : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string loggingAssemblyPath;

    public IsolatedAssemblyLoadContextV2(string assemblyPath, string loggingAssemblyPath) : base(false)
    {
        resolver = new AssemblyDependencyResolver(assemblyPath);
        this.loggingAssemblyPath = loggingAssemblyPath;
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == "Microsoft.Extensions.Logging.Abstractions")
        {
            return LoadFromAssemblyPath(loggingAssemblyPath);
        }

        string resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
        return resolvedPath == null ? null : LoadFromAssemblyPath(resolvedPath);
    }
}
'@
}

$loadContext = [IsolatedAssemblyLoadContextV2]::new($mysqlConnectorAssembly.FullName, $loggingAbstractionsAssembly.FullName)
$mysqlConnectorRuntimeAssembly = $loadContext.LoadFromAssemblyPath($mysqlConnectorAssembly.FullName)

$builderType = $mysqlConnectorRuntimeAssembly.GetType('MySqlConnector.MySqlConnectionStringBuilder', $true)
$connectionType = $mysqlConnectorRuntimeAssembly.GetType('MySqlConnector.MySqlConnection', $true)
$sql = Get-Content -Path $ValidationSqlPath -Raw
$validationCountSql = ($sql -replace '(?im)^\s*USE\s+mtm_waitlist;\s*', '').Trim().TrimEnd(';')
$validationCountSql = "SELECT COUNT(*) FROM ( $validationCountSql ) AS validation_issues;"

Write-Log 'Prepared validation count query.'

$bootstrapCreatePath = Join-Path $repoRoot 'Database\Bootstrap\create_database.sql'
$tableCreatePaths = Get-ChildItem -Path (Join-Path $repoRoot 'Database\Tables') -Recurse -Filter 'create.sql' |
Sort-Object FullName |
Select-Object -ExpandProperty FullName
$procedureCreatePaths = Get-ChildItem -Path (Join-Path $repoRoot 'Database\StoredProcedures') -Recurse -Filter 'create.sql' |
Sort-Object FullName |
Select-Object -ExpandProperty FullName
$seedCreatePaths = Get-ChildItem -Path (Join-Path $repoRoot 'Database\Seeds') -Recurse -Filter 'create.sql' |
Sort-Object FullName |
Select-Object -ExpandProperty FullName

$attemptErrors = New-Object System.Collections.Generic.List[string]
$resolvedConnectionStrings = @()

Write-Log ("Configured connection-string env vars: {0}" -f ($ConnectionStringEnvironmentVariables -join ', '))

foreach ($connectionStringEnvironmentName in $ConnectionStringEnvironmentVariables) {
    $connectionString = [Environment]::GetEnvironmentVariable($connectionStringEnvironmentName)
    if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
        $resolvedConnectionStrings += [pscustomobject]@{
            EnvironmentName  = $connectionStringEnvironmentName
            ConnectionString = $connectionString
        }
    }
}

if ($resolvedConnectionStrings.Count -eq 0) {
    Write-Log 'Skipping validation because no configured connection-string env vars are set.'
    exit 0
}

function Invoke-ValidatedCommand {
    param(
        [object]$Connection,
        [string]$CommandText
    )

    $connectionRuntimeType = $Connection.GetType()
    $command = $connectionRuntimeType.GetMethod('CreateCommand', [Type[]]@()).Invoke($Connection, @())
    $commandType = $command.GetType()
    $commandType.GetProperty('CommandText').SetValue($command, $CommandText)
    $executeNonQueryMethod = $commandType.GetMethod('ExecuteNonQuery', [Type[]]@())
    [void]$executeNonQueryMethod.Invoke($command, @())
}

function Get-InstallSqlText {
    param([string]$Path)

    $content = Get-Content -Path $Path -Raw
    $content = $content -replace '(?im)^\s*DELIMITER.*$', ''
    $content = $content -replace '(?m)^\s*\$\$\s*$', ''
    $content = $content -replace 'END\s*\$\$', 'END;'
    return $content
}

function Invoke-SqlStatements {
    param(
        [object]$Connection,
        [string[]]$Statements
    )

    foreach ($statement in $Statements) {
        $trimmedStatement = $statement.Trim()
        if ($trimmedStatement) {
            Invoke-ValidatedCommand -Connection $Connection -CommandText $trimmedStatement
        }
    }
}

function Install-Or-UpdateDatabase {
    param(
        [string]$ConnectionString,
        [string]$TargetDatabaseName
    )

    $baseBuilder = [Activator]::CreateInstance($builderType, @($ConnectionString))
    $baseBuilderType = $baseBuilder.GetType()
    $baseBuilderType.GetProperty('ConnectionTimeout').SetValue($baseBuilder, [uint32]10)
    $baseBuilderType.GetProperty('Database').SetValue($baseBuilder, '')
    $serverConnectionString = $baseBuilderType.GetProperty('ConnectionString').GetValue($baseBuilder)

    Write-Log 'Bootstrap phase: opening server connection to create the database if needed.'

    $serverConnection = [Activator]::CreateInstance($connectionType, @($serverConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($serverConnection, @())
        Write-Log 'Bootstrap phase: applying Database/Bootstrap/create_database.sql.'
        $bootstrapStatements = (Get-InstallSqlText -Path $bootstrapCreatePath) -split ';'
        Invoke-SqlStatements -Connection $serverConnection -Statements $bootstrapStatements
    }
    finally {
        $serverConnection.Dispose()
    }

    $installBuilder = [Activator]::CreateInstance($builderType, @($ConnectionString))
    $installBuilderType = $installBuilder.GetType()
    $installBuilderType.GetProperty('ConnectionTimeout').SetValue($installBuilder, [uint32]10)
    $installBuilderType.GetProperty('Database').SetValue($installBuilder, $TargetDatabaseName)
    $targetConnectionString = $installBuilderType.GetProperty('ConnectionString').GetValue($installBuilder)

    $targetConnection = [Activator]::CreateInstance($connectionType, @($targetConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($targetConnection, @())
        Write-Log 'Install phase: applying table create.sql files.'

        foreach ($tableCreatePath in $tableCreatePaths) {
            Write-Log "Applying table file: $tableCreatePath"
            $tableStatements = (Get-InstallSqlText -Path $tableCreatePath) -split ';'
            Invoke-SqlStatements -Connection $targetConnection -Statements $tableStatements
        }

        Write-Log 'Install phase: applying stored procedure create.sql files.'
        foreach ($procedureCreatePath in $procedureCreatePaths) {
            Write-Log "Applying stored procedure file: $procedureCreatePath"
            $procedureText = Get-Content -Path $procedureCreatePath -Raw
            $procedureDrop = 'DROP FUNCTION IF EXISTS fn_server_utc_now;'
            $procedureCreate = $procedureText -replace '(?im)^\s*DROP FUNCTION IF EXISTS fn_server_utc_now;\s*', ''
            $procedureCreate = $procedureCreate -replace '(?im)^\s*DELIMITER.*$', ''
            $procedureCreate = $procedureCreate -replace '(?m)^\s*\$\$\s*$', ''
            $procedureCreate = $procedureCreate -replace 'END\s*\$\$', 'END;'

            Invoke-ValidatedCommand -Connection $targetConnection -CommandText $procedureDrop
            if ($procedureCreate.Trim()) {
                Invoke-ValidatedCommand -Connection $targetConnection -CommandText $procedureCreate
            }
        }

        Write-Log 'Install phase: applying seed create.sql files.'
        foreach ($seedCreatePath in $seedCreatePaths) {
            Write-Log "Applying seed file: $seedCreatePath"
            $seedStatements = (Get-InstallSqlText -Path $seedCreatePath) -split ';'
            Invoke-SqlStatements -Connection $targetConnection -Statements $seedStatements
        }
    }
    finally {
        $targetConnection.Dispose()
    }
}

function Apply-Seeds {
    param(
        [string]$ConnectionString,
        [string]$TargetDatabaseName
    )

    if (-not $seedCreatePaths -or $seedCreatePaths.Count -eq 0) {
        Write-Log 'Seed phase: no seed files found.'
        return
    }

    $seedBuilder = [Activator]::CreateInstance($builderType, @($ConnectionString))
    $seedBuilderType = $seedBuilder.GetType()
    $seedBuilderType.GetProperty('ConnectionTimeout').SetValue($seedBuilder, [uint32]10)
    $seedBuilderType.GetProperty('Database').SetValue($seedBuilder, $TargetDatabaseName)
    $seedConnectionString = $seedBuilderType.GetProperty('ConnectionString').GetValue($seedBuilder)

    $seedConnection = [Activator]::CreateInstance($connectionType, @($seedConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($seedConnection, @())
        Write-Log 'Seed phase: applying seed create.sql files.'

        foreach ($seedCreatePath in $seedCreatePaths) {
            Write-Log "Applying seed file: $seedCreatePath"
            $seedStatements = (Get-InstallSqlText -Path $seedCreatePath) -split ';'
            Invoke-SqlStatements -Connection $seedConnection -Statements $seedStatements
        }
    }
    finally {
        $seedConnection.Dispose()
    }
}

function Test-DatabaseSchema {
    param(
        [string]$ConnectionString,
        [string]$TargetDatabaseName
    )

    $builder = [Activator]::CreateInstance($builderType, @($ConnectionString))
    $builderType.GetProperty('ConnectionTimeout').SetValue($builder, [uint32]10)
    $builderType.GetProperty('Database').SetValue($builder, $TargetDatabaseName)
    $effectiveConnectionString = $builderType.GetProperty('ConnectionString').GetValue($builder)

    $connection = [Activator]::CreateInstance($connectionType, @($effectiveConnectionString))
    try {
        Write-Log "Validation phase: opening $TargetDatabaseName and running schema check."
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($connection, @())

        $command = $connectionType.GetMethod('CreateCommand', [Type[]]@()).Invoke($connection, @())
        $commandType = $command.GetType()
        $commandType.GetProperty('CommandText').SetValue($command, $validationCountSql)

        $executeScalarMethod = $commandType.GetMethod('ExecuteScalar', [Type[]]@())
        $issueCountObject = $executeScalarMethod.Invoke($command, @())
        if ($issueCountObject -is [System.Array]) {
            $issueCountObject = $issueCountObject | Select-Object -First 1
        }

        return [int]$issueCountObject
    }
    finally {
        $connection.Dispose()
    }
}

foreach ($entry in $resolvedConnectionStrings) {
    try {
        Write-Log "Trying connection source: $($entry.EnvironmentName)"
        try {
            $issueCount = [int](Test-DatabaseSchema -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName)
        }
        catch {
            if ($_.Exception.Message -match 'Unknown database') {
                Write-Log "$($entry.EnvironmentName) reported a missing database. Bootstrapping and installing schema..."
                Install-Or-UpdateDatabase -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName
                $issueCount = [int](Test-DatabaseSchema -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName)
            }
            else {
                Write-Log "$($entry.EnvironmentName) failed before install: $($_.Exception.Message)"
                throw
            }
        }

        if ($issueCount -gt 0) {
            Write-Log "$($entry.EnvironmentName) connected, but schema validation found gaps. Applying schema files..."
            Install-Or-UpdateDatabase -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName
            $issueCount = [int](Test-DatabaseSchema -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName)
        }

        if ($issueCount -eq 0) {
            Write-Log "$($entry.EnvironmentName) schema is valid. Applying seeds before success exit."
            Apply-Seeds -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName
            Write-Log "Database schema validation passed using $($entry.EnvironmentName)."
            exit 0
        }

        $attemptErrors.Add("$($entry.EnvironmentName) connected, but schema validation still failed after migration attempt.")
        Write-Log "Remaining missing-object count: $issueCount"
        Write-Log "Applying seeds after repair attempt for $($entry.EnvironmentName)."
        Apply-Seeds -ConnectionString $entry.ConnectionString -TargetDatabaseName $DatabaseName
    }
    catch {
        Write-Log "$($entry.EnvironmentName) failed with: $($_.Exception.Message)"
        $attemptErrors.Add("$($entry.EnvironmentName) failed: $($_.Exception.Message)")
        continue
    }
}

Write-Log 'Database schema validation failed for all configured connection strings:'
foreach ($attemptError in $attemptErrors) {
    Write-Host "- $attemptError"
}
exit 1
