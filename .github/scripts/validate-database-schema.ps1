param(
    [string[]]$ConnectionStringEnvironmentVariables = @(
        'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING_HOME',
        'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING_WORK',
        'MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING'
    ),
    [string]$DatabaseName = 'mtm_waitlist',
    [string]$ValidationSqlPath = (Join-Path $PSScriptRoot '..\..\Database\Validation\0003__validate_startup_schema.sql')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot
Set-Location $repoRoot

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

$nugetRoot = $env:NUGET_PACKAGES
if ([string]::IsNullOrWhiteSpace($nugetRoot)) {
    $nugetRoot = Join-Path $HOME '.nuget\packages'
}

$loggingAbstractionsAssembly = Get-ChildItem -Path (Join-Path $nugetRoot 'microsoft.extensions.logging.abstractions') -Recurse -Filter 'Microsoft.Extensions.Logging.Abstractions.dll' |
Where-Object { $_.FullName -match '\\7\.0\.1\\' } |
Select-Object -First 1

if ($null -eq $loggingAbstractionsAssembly) {
    throw 'Unable to locate Microsoft.Extensions.Logging.Abstractions 7.0.1 in the NuGet package cache.'
}

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

$attemptErrors = New-Object System.Collections.Generic.List[string]
$resolvedConnectionStrings = @()

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
    Write-Host "Skipping database schema validation because none of the configured connection string environment variables are set."
    exit 0
}

foreach ($entry in $resolvedConnectionStrings) {
    try {
        $builder = [Activator]::CreateInstance($builderType, @($entry.ConnectionString))
        $builderType.GetProperty('ConnectionTimeout').SetValue($builder, [uint32]10)
        $builderType.GetProperty('Database').SetValue($builder, $DatabaseName)
        $effectiveConnectionString = $builderType.GetProperty('ConnectionString').GetValue($builder)

        $connection = [Activator]::CreateInstance($connectionType, @($effectiveConnectionString))
        try {
            $openMethod = $connectionType.GetMethod('Open', [Type[]]@())
            $openMethod.Invoke($connection, @())

            $command = $connectionType.GetMethod('CreateCommand', [Type[]]@()).Invoke($connection, @())
            $commandType = $command.GetType()
            $commandType.GetProperty('CommandText').SetValue($command, $sql)

            $reader = $commandType.GetMethod('ExecuteReader', [Type[]]@()).Invoke($command, @())
            $readerType = $reader.GetType()
            $issues = New-Object System.Collections.Generic.List[object]

            try {
                $readMethod = $readerType.GetMethod('Read', [Type[]]@())
                $getStringMethod = $readerType.GetMethod('GetString', [Type[]]@([int]))

                while ($readMethod.Invoke($reader, @())) {
                    $issues.Add([pscustomobject]@{
                            IssueType     = $getStringMethod.Invoke($reader, @(0))
                            ObjectName    = $getStringMethod.Invoke($reader, @(1))
                            ExpectedValue = $getStringMethod.Invoke($reader, @(2))
                            ActualValue   = $getStringMethod.Invoke($reader, @(3))
                        })
                }
            }
            finally {
                $readerType.GetMethod('Close', [Type[]]@()).Invoke($reader, @())
            }

            if ($issues.Count -gt 0) {
                $attemptErrors.Add("$($entry.EnvironmentName) connected, but schema validation failed.")
                foreach ($issue in $issues) {
                    Write-Host ("- {0} :: {1} (expected {2}, actual {3})" -f $issue.IssueType, $issue.ObjectName, $issue.ExpectedValue, $issue.ActualValue)
                }
                continue
            }

            Write-Host "Database schema validation passed using $($entry.EnvironmentName)."
            exit 0
        }
        finally {
            $connection.Dispose()
        }
    }
    catch {
        $attemptErrors.Add("$($entry.EnvironmentName) failed: $($_.Exception.Message)")
        continue
    }
}

Write-Host 'Database schema validation failed for all configured connection strings:'
foreach ($attemptError in $attemptErrors) {
    Write-Host "- $attemptError"
}
exit 1
