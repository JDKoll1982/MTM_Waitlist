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

$loggingAbstractionsAssembly = Get-ChildItem -Path $mysqlConnectorAssembly.Directory -Filter 'Microsoft.Extensions.Logging.Abstractions.dll' -ErrorAction SilentlyContinue |
Select-Object -First 1

if ($null -eq $loggingAbstractionsAssembly) {
    Write-Log 'Logging abstractions assembly not found beside MySqlConnector.dll; falling back to the NuGet package cache.'

    # Take the highest cached version, not the first one enumerated. The cache holds every version this
    # machine has ever restored - on this workstation 1.1.1 through 10.0.10 - and loading a version far
    # behind the one MySqlConnector was built against makes its logging type initializer throw, which
    # fails the job for a reason that has nothing to do with the schema.
    $loggingAbstractionsAssembly = Get-ChildItem -Path (Join-Path $nugetRoot 'microsoft.extensions.logging.abstractions') -Recurse -Filter 'Microsoft.Extensions.Logging.Abstractions.dll' |
    Sort-Object -Property @{ Expression = { try { [version]$_.Directory.Parent.Name } catch { [version]'0.0.0' } } } -Descending |
    Select-Object -First 1
}

if ($null -eq $loggingAbstractionsAssembly) {
    throw 'Unable to locate Microsoft.Extensions.Logging.Abstractions in the build output or the NuGet package cache.'
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
$functionCreatePaths = Get-ChildItem -Path (Join-Path $repoRoot 'Database\Functions') -Recurse -Filter 'create.sql' |
Sort-Object FullName |
Select-Object -ExpandProperty FullName
$viewCreatePaths = Get-ChildItem -Path (Join-Path $repoRoot 'Database\Views') -Recurse -Filter 'create.sql' |
Sort-Object FullName |
Select-Object -ExpandProperty FullName

# Two artifacts creating the same object is invisible to the install below: every file is applied in
# turn, the last one silently wins, and the install reports success. The check is static and runs
# before the connection-string gate on purpose, so it also holds on a runner with no database.
$objectCreatePatterns = @(
    @{ Kind = 'table'; Pattern = '(?im)^\s*CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`?(\w+)`?' },
    @{ Kind = 'procedure'; Pattern = '(?im)^\s*CREATE\s+(?:DEFINER\s*=\s*\S+\s+)?PROCEDURE\s+`?(\w+)`?' },
    @{ Kind = 'function'; Pattern = '(?im)^\s*CREATE\s+(?:DEFINER\s*=\s*\S+\s+)?FUNCTION\s+`?(\w+)`?' },
    @{ Kind = 'view'; Pattern = '(?im)^\s*CREATE\s+(?:OR\s+REPLACE\s+)?(?:ALGORITHM\s*=\s*\S+\s+)?VIEW\s+`?(\w+)`?' }
)

function Get-CreatedObject {
    param([string]$Path)

    $content = Get-Content -Path $Path -Raw
    $created = New-Object System.Collections.Generic.List[object]

    foreach ($objectCreatePattern in $objectCreatePatterns) {
        foreach ($match in [regex]::Matches($content, $objectCreatePattern.Pattern)) {
            $created.Add([pscustomobject]@{
                    Kind = $objectCreatePattern.Kind
                    Name = $match.Groups[1].Value
                    Path = $Path
                })
        }
    }

    return $created
}

function Assert-NoDuplicateObjectCreates {
    param([string[]]$Paths)

    $created = New-Object System.Collections.Generic.List[object]
    foreach ($path in $Paths) {
        foreach ($entry in (Get-CreatedObject -Path $path)) {
            $created.Add($entry)
        }
    }

    $distinct = $created | Group-Object Kind, Name
    $duplicates = $distinct | Where-Object { $_.Count -gt 1 }

    if (-not $duplicates) {
        Write-Log ("Duplicate-object check passed: {0} artifact(s) create {1} distinct object(s)." -f $created.Count, $distinct.Count)
        return
    }

    Write-Host 'Duplicate-object check failed: more than one artifact creates the same object.'
    foreach ($duplicate in $duplicates) {
        $sample = $duplicate.Group[0]
        Write-Host ("  {0} '{1}' is created by {2} artifacts:" -f $sample.Kind, $sample.Name, $duplicate.Count)
        foreach ($entry in $duplicate.Group) {
            Write-Host ("    {0}" -f $entry.Path.Substring($repoRoot.Length + 1))
        }
    }

    Write-Host 'Only one artifact may create each object; delete the superseded one, or rename it if it is not a duplicate.'
    exit 1
}

Assert-NoDuplicateObjectCreates -Paths ($tableCreatePaths + $procedureCreatePaths + $functionCreatePaths + $viewCreatePaths)

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

# Reads an install artifact as written. Stripping the `DELIMITER` directive and rewriting the terminator
# belongs to statement splitting, not to reading: a body that was normalised to `END;` before splitting gets
# cut at its own internal semicolons, so the text is passed on untouched and Split-SqlStatements honours the
# delimiter the file declares.
function Get-InstallSqlText {
    param([string]$Path)

    return (Get-Content -Path $Path -Raw)
}

# Splits a SQL script into individual statements at its statement delimiter.
#
# A naive `-split ';'` is wrong, and wrong for three separate reasons: a semicolon also occurs inside `--`
# and `#` line comments, inside `/* */` block comments, and inside single-, double- and backtick-quoted text.
# Splitting on one of those cuts a statement in half, and the server then rejects the half it was handed -
# which is exactly what the comment on line 216 of Database/Seeds/seed_dev_masked_baseline/create.sql used to
# cause. So the delimiter positions are found in a copy of the text whose comment and literal characters have
# been masked out, and the original text is then sliced at those same offsets.
#
# A `DELIMITER <token>` line is a client directive rather than SQL the server parses: it switches the
# delimiter for the statements that follow it, which is how a stored-program body keeps its own semicolons,
# and it is dropped from the output. `DELIMITER ;` switches back.
function Split-SqlStatements {
    param([string]$Text)

    $statements = New-Object System.Collections.Generic.List[string]

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return , $statements
    }

    # Ordered so that a comment or a literal which starts earlier is consumed whole instead of being re-read
    # from inside. `--` needs a space or a line end after it to begin a comment, which is MySQL's rule.
    $protectedPattern = '(?s)--[ \t][^\r\n]*|--(?=\r|\n|$)|#[^\r\n]*|/\*.*?\*/' +
    "|'(?:[^'\\]|\\.|'')*'" +
    '|"(?:[^"\\]|\\.|"")*"' +
    '|`(?:[^`]|``)*`'

    $delimiter = ';'
    $segment = New-Object System.Text.StringBuilder

    foreach ($line in [regex]::Split($Text, '(?<=\n)')) {
        $directive = [regex]::Match($line, '^\s*DELIMITER\s+(\S+?)\s*;?\s*$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if ($directive.Success) {
            foreach ($statement in (Split-SqlSegment -Text $segment.ToString() -Delimiter $delimiter -ProtectedPattern $protectedPattern)) {
                $statements.Add($statement)
            }

            [void]$segment.Clear()
            $delimiter = $directive.Groups[1].Value
            continue
        }

        [void]$segment.Append($line)
    }

    foreach ($statement in (Split-SqlSegment -Text $segment.ToString() -Delimiter $delimiter -ProtectedPattern $protectedPattern)) {
        $statements.Add($statement)
    }

    return , $statements
}

# Splits one delimiter segment. Comment and quoted-literal characters are masked in a copy of the text while
# the length and the line breaks are kept, so a masked offset is the same offset in the SQL, and the original
# text is sliced at each unmasked delimiter.
function Split-SqlSegment {
    param(
        [string]$Text,
        [string]$Delimiter,
        [string]$ProtectedPattern
    )

    $statements = New-Object System.Collections.Generic.List[string]

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return , $statements
    }

    $mask = [regex]::Replace($Text, $ProtectedPattern, {
            param($match)

            return ($match.Value -replace '[^\r\n]', ' ')
        })

    $start = 0

    while ($true) {
        $index = $mask.IndexOf($Delimiter, $start, [System.StringComparison]::Ordinal)

        if ($index -lt 0) {
            break
        }

        $statement = $Text.Substring($start, $index - $start).Trim()

        if ($statement) {
            $statements.Add($statement)
        }

        $start = $index + $Delimiter.Length
    }

    $tail = $Text.Substring($start).Trim()

    if ($tail) {
        $statements.Add($tail)
    }

    return , $statements
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

# Builds a connection string for this script's own use. Two properties matter beyond what the caller
# configured: a short timeout, so an unreachable store fails the run rather than hanging it, and
# `AllowUserVariables`, because the seed phase executes stored-program bodies that set session variables
# (`@mtm_picture_layout_move` in seed_picture_layout_move). Without it the connector reads an `@name` as a
# command parameter, and nothing here passes a parameter: the variables the SQL declares are the only `@name`
# in play, so they have to be left to the server.
function New-ValidatedConnectionString {
    param(
        [string]$ConnectionString,
        [string]$TargetDatabaseName
    )

    $builder = [Activator]::CreateInstance($builderType, @($ConnectionString))
    $builderRuntimeType = $builder.GetType()
    $builderRuntimeType.GetProperty('ConnectionTimeout').SetValue($builder, [uint32]10)
    $builderRuntimeType.GetProperty('AllowUserVariables').SetValue($builder, $true)

    if ($null -ne $TargetDatabaseName) {
        $builderRuntimeType.GetProperty('Database').SetValue($builder, $TargetDatabaseName)
    }

    return $builderRuntimeType.GetProperty('ConnectionString').GetValue($builder)
}

function Install-Or-UpdateDatabase {
    param(
        [string]$ConnectionString,
        [string]$TargetDatabaseName
    )

    $serverConnectionString = New-ValidatedConnectionString -ConnectionString $ConnectionString -TargetDatabaseName ''

    Write-Log 'Bootstrap phase: opening server connection to create the database if needed.'

    $serverConnection = [Activator]::CreateInstance($connectionType, @($serverConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($serverConnection, @())
        Write-Log 'Bootstrap phase: applying Database/Bootstrap/create_database.sql.'
        $bootstrapStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $bootstrapCreatePath)
        Invoke-SqlStatements -Connection $serverConnection -Statements $bootstrapStatements
    }
    finally {
        $serverConnection.Dispose()
    }

    $targetConnectionString = New-ValidatedConnectionString -ConnectionString $ConnectionString -TargetDatabaseName $TargetDatabaseName

    $targetConnection = [Activator]::CreateInstance($connectionType, @($targetConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($targetConnection, @())
        Write-Log 'Install phase: applying table create.sql files.'

        foreach ($tableCreatePath in $tableCreatePaths) {
            Write-Log "Applying table file: $tableCreatePath"
            $tableStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $tableCreatePath)
            Invoke-SqlStatements -Connection $targetConnection -Statements $tableStatements
        }

        Write-Log 'Install phase: applying function create.sql files.'
        foreach ($functionCreatePath in $functionCreatePaths) {
            Write-Log "Applying function file: $functionCreatePath"
            $functionStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $functionCreatePath)
            Invoke-SqlStatements -Connection $targetConnection -Statements $functionStatements
        }

        Write-Log 'Install phase: applying view create.sql files.'
        foreach ($viewCreatePath in $viewCreatePaths) {
            Write-Log "Applying view file: $viewCreatePath"
            $viewStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $viewCreatePath)
            Invoke-SqlStatements -Connection $targetConnection -Statements $viewStatements
        }

        Write-Log 'Install phase: applying stored procedure create.sql files.'
        foreach ($procedureCreatePath in $procedureCreatePaths) {
            Write-Log "Applying stored procedure file: $procedureCreatePath"
            $procedureText = Get-Content -Path $procedureCreatePath -Raw
            $procedureCreate = $procedureText -replace '(?im)^\s*DELIMITER.*$', ''
            $procedureCreate = $procedureCreate -replace '(?m)^\s*\$\$\s*$', ''
            $procedureCreate = $procedureCreate -replace 'END\s*\$\$', 'END;'

            if ($procedureCreate.Trim()) {
                Invoke-ValidatedCommand -Connection $targetConnection -CommandText $procedureCreate
            }
        }

        Write-Log 'Install phase: applying seed create.sql files.'
        foreach ($seedCreatePath in $seedCreatePaths) {
            Write-Log "Applying seed file: $seedCreatePath"
            $seedStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $seedCreatePath)
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

    $seedConnectionString = New-ValidatedConnectionString -ConnectionString $ConnectionString -TargetDatabaseName $TargetDatabaseName

    $seedConnection = [Activator]::CreateInstance($connectionType, @($seedConnectionString))
    try {
        [void]$connectionType.GetMethod('Open', [Type[]]@()).Invoke($seedConnection, @())
        Write-Log 'Seed phase: applying seed create.sql files.'

        foreach ($seedCreatePath in $seedCreatePaths) {
            Write-Log "Applying seed file: $seedCreatePath"
            $seedStatements = Split-SqlStatements -Text (Get-InstallSqlText -Path $seedCreatePath)
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

    $effectiveConnectionString = New-ValidatedConnectionString -ConnectionString $ConnectionString -TargetDatabaseName $TargetDatabaseName

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
