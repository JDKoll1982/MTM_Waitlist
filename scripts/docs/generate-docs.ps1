<#
.SYNOPSIS
    Reads the C# and XAML in this repository and produces the inputs the diagram and UI-design
    pipelines consume: a code-facts graph, a spec gap worklist, and mockup HTML built from real XAML.

.DESCRIPTION
    Modes, all read-only against the application:

    Facts    Parse every .cs file with the Roslyn parser PowerShell already carries
             ($PSHOME\Microsoft.CodeAnalysis.CSharp.dll) and write code-facts.json: types, members,
             constructor/field/creation/call edges, DI registrations and stored-procedure references.

    Gaps     Compare a diagram spec (docs\diagrams\tools\spec-diagram-*.json) with code-facts.json and
             write diagram-gaps.md + diagram-gaps.json: orphan nodes, labels the code does not hold,
             missing edges and missing nodes, each with the exact JSON location to edit.

    Designs  Parse a XAML view and write <refactor>-design.json plus mockups/*.html in the shape
             render_mockup.py expects, then render the PNGs with headless Edge. Generated designs land
             under -OutDir unless -WriteInPlace is passed.

    Verify   Run the existing verifiers: verify_pdfs.py for diagrams, verify_designs.py for designs.

    This is a syntax parse, not a compilation: an edge means "this text names that type". Every fact
    carries the file and line it came from so a claim can be checked, and the gap report proposes edits
    rather than making them - the wording of a label, a note or a lane choice is an authoring decision.

.PARAMETER Mode
    One or more of Facts, Gaps, Designs, Verify, All.

.PARAMETER Root
    Repository root. Defaults to two directories above this script.

.PARAMETER Workflow
    Restrict Gaps to one workflow from docs\diagrams\tools\manifest.json, for example startup-launch.

.PARAMETER SpecPath
    Check one spec file instead of the manifest's list, for example a spec that is not in the manifest yet.

.PARAMETER Paths
    Folders or files to scan for Facts. Defaults to the startup module, its views, the shell and App.xaml.cs.

.PARAMETER Screen
    XAML view to convert in Designs mode, by file name without extension. Omit for every Module_Startup view.

.PARAMETER OutDir
    Where generated facts, reports and designs are written. Defaults to %TEMP%\mtm-diagrams\facts.

.PARAMETER Refactor
    File stem for a generated design. Defaults to the screen name in kebab case.

.PARAMETER WriteInPlace
    Write a generated design under docs\ui-designs instead of OutDir.

.EXAMPLE
    pwsh scripts\docs\generate-docs.ps1 -Mode Facts

.EXAMPLE
    pwsh scripts\docs\generate-docs.ps1 -Mode Gaps -Workflow startup-launch

.EXAMPLE
    pwsh scripts\docs\generate-docs.ps1 -Mode Designs -Screen SignInPage

.EXAMPLE
    pwsh scripts\docs\generate-docs.ps1 -Mode Verify
#>
[CmdletBinding()]
param(
    [string[]]$Mode = @('All'),

    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$Workflow,

    [string]$SpecPath,

    [string[]]$Paths,

    [string]$Screen,

    [string]$OutDir = (Join-Path $env:TEMP 'mtm-diagrams\facts'),

    [string]$Refactor,

    [switch]$WriteInPlace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Root = (Resolve-Path $Root).Path
$Mode = @($Mode | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$knownModes = @('Facts', 'Gaps', 'Designs', 'Verify', 'All')
$unknown = @($Mode | Where-Object { $knownModes -notcontains $_ })
if ($unknown.Count -gt 0) {
    throw "Unknown mode(s): $($unknown -join ', '). Use Facts, Gaps, Designs, Verify or All."
}
$script:DiagramTools = Join-Path $script:Root 'docs\diagrams\tools'
$script:DesignTools = Join-Path $script:Root 'docs\ui-designs\tools'
$script:ExcludedFolders = @('bin', 'obj', '.git', 'node_modules', '.vs')
$script:ProcedurePattern = '\b(sp_[a-z0-9_]+)\b'

# --------------------------------------------------------------------------------------------------
# Roslyn
# --------------------------------------------------------------------------------------------------

function Initialize-Roslyn {
    if ('Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree' -as [type]) { return }

    $assembly = Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'
    if (-not (Test-Path $assembly)) {
        throw "Roslyn was not found beside PowerShell ($assembly). Run this in PowerShell 7.4 or later."
    }

    Add-Type -Path $assembly
}

# --------------------------------------------------------------------------------------------------
# Facts
# --------------------------------------------------------------------------------------------------

function Get-RelativePath {
    param([string]$Path)
    return [System.IO.Path]::GetRelativePath($script:Root, $Path)
}

function Get-CSharpFile {
    param([string[]]$SearchPath)

    $seen = New-Object System.Collections.Generic.HashSet[string]
    foreach ($item in $SearchPath) {
        $full = if ([System.IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $script:Root $item }
        if (-not (Test-Path $full)) { throw "Nothing to scan at $item" }

        $targets = if (Test-Path $full -PathType Leaf) { @(Get-Item $full) }
        else { @(Get-ChildItem -Path $full -Recurse -File -Filter '*.cs') }

        foreach ($file in $targets) {
            $relative = Get-RelativePath $file.FullName
            $segments = $relative -split '\\'
            if ($segments | Where-Object { $script:ExcludedFolders -contains $_ }) { continue }
            if ($seen.Add($relative)) { $file }
        }
    }
}

function Get-LineNumber {
    param($Node)
    return $Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1
}

function Get-AttributeName {
    param($AttributeList)

    $names = New-Object System.Collections.Generic.List[string]
    foreach ($list in $AttributeList) {
        foreach ($attribute in $list.Attributes) { $names.Add($attribute.Name.ToString()) }
    }
    return $names
}

function Get-SimpleTypeName {
    param([string]$TypeName)

    if (-not $TypeName) { return $null }

    $name = $TypeName.Trim()
    $name = $name -replace '\?$', ''

    # A constructor that takes IEnumerable<ILaunchStep> depends on ILaunchStep, not on IEnumerable.
    if ($name -match '^(?:global::)?(?:[A-Za-z_][\w.]*\.)?([A-Za-z_]\w*)<(.+)>$') {
        $outer = $Matches[1]
        $inner = $Matches[2]
        if ($outer -in 'IEnumerable', 'IReadOnlyList', 'IReadOnlyCollection', 'IList', 'ICollection', 'List',
            'IAsyncEnumerable', 'Task', 'ValueTask', 'Lazy', 'Func', 'Action', 'Nullable') {
            $first = ($inner -split ',')[0].Trim()
            return Get-SimpleTypeName $first
        }
        return $outer
    }

    if ($name -match '^(?:global::)?(?:[A-Za-z_][\w.]*\.)?([A-Za-z_]\w*)$') { return $Matches[1] }
    return $null
}

function Read-Invocations {
    param(
        [string]$TypeName,
        $MemberNode,
        [hashtable]$FieldType,
        [System.Collections.Generic.List[object]]$Edges,
        [System.Collections.Generic.List[object]]$Procedures,
        [string]$File
    )

    $memberName = if ($MemberNode.Identifier) { $MemberNode.Identifier.Text } else { '<initializer>' }

    foreach ($node in $MemberNode.DescendantNodes()) {
        $nodeKind = $node.GetType().Name

        if ($nodeKind -eq 'ObjectCreationExpressionSyntax') {
            $target = Get-SimpleTypeName $node.Type.ToString()
            if ($target -and $target -ne $TypeName) {
                $Edges.Add([pscustomobject]@{
                        From = $TypeName; To = $target; Kind = 'creates'
                        Member = $memberName; File = $File; Line = (Get-LineNumber $node)
                    })
            }
            continue
        }

        if ($nodeKind -eq 'InvocationExpressionSyntax') {
            $expression = $node.Expression
            if ($expression -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax]) { continue }

            $methodName = $expression.Name.Identifier.Text

            # services.GetRequiredService<ILaunchPipeline>() names the type it resolves, so it is an edge.
            if ($expression.Name.GetType().Name -eq 'GenericNameSyntax' -and
                $methodName -in 'GetRequiredService', 'GetService', 'CreateInstance', 'ActivatorUtilities') {
                foreach ($argument in $expression.Name.TypeArgumentList.Arguments) {
                    $target = Get-SimpleTypeName $argument.ToString()
                    if ($target -and $target -ne $TypeName) {
                        $Edges.Add([pscustomobject]@{
                                From = $TypeName; To = $target; Kind = 'resolves'
                                Member = $memberName; File = $File; Line = (Get-LineNumber $node)
                            })
                    }
                }
                continue
            }

            if ($methodName -in 'Add', 'AddRange', 'Select', 'Where', 'OrderBy', 'OrderByDescending', 'First',
                'FirstOrDefault', 'ToList', 'ToArray', 'ConfigureAwait', 'ToString', 'Equals', 'GetHashCode',
                'WriteLine', 'Format', 'Any', 'All', 'Count', 'Single', 'SingleOrDefault', 'Last') { continue }

            $receiver = $expression.Expression.ToString().Replace('this.', '')
            $target = $null

            if ($FieldType.ContainsKey($receiver)) { $target = $FieldType[$receiver] }
            elseif ($receiver -match '^[A-Z][A-Za-z0-9_]*$') { $target = $receiver }

            if ($target -and $target -ne $TypeName) {
                $Edges.Add([pscustomobject]@{
                        From = $TypeName; To = $target; Kind = 'calls'
                        Member = $memberName; File = $File; Line = (Get-LineNumber $node)
                    })
            }
            continue
        }

        if ($nodeKind -eq 'LiteralExpressionSyntax') {
            continue
        }
    }
}

function Read-ProcedureReferences {
    param(
        $TypeNode,
        [string]$TypeName,
        [string]$File,
        [System.Collections.Generic.List[object]]$Procedures
    )

    foreach ($node in $TypeNode.DescendantNodes()) {
        if ($node.GetType().Name -ne 'LiteralExpressionSyntax') { continue }
        if ($node.Kind().ToString() -ne 'StringLiteralExpression') { continue }

        $text = $node.Token.ValueText
        if (-not $text -or $text -notmatch 'sp_') { continue }

        $member = ''
        $parent = $node.Parent
        while ($null -ne $parent) {
            $parentKind = $parent.GetType().Name
            if ($parentKind -in 'MethodDeclarationSyntax', 'ConstructorDeclarationSyntax',
                'PropertyDeclarationSyntax', 'FieldDeclarationSyntax') {
                if ($parentKind -eq 'FieldDeclarationSyntax') {
                    if ($parent.Declaration.Variables.Count -gt 0) {
                        $member = $parent.Declaration.Variables[0].Identifier.Text
                    }
                }
                elseif ($parent.Identifier) {
                    $member = $parent.Identifier.Text
                }
                break
            }
            $parent = $parent.Parent
        }

        foreach ($match in [regex]::Matches($text, $script:ProcedurePattern)) {
            $Procedures.Add([pscustomobject]@{
                    Name = $match.Groups[1].Value; Type = $TypeName; Member = $member
                    File = $File; Line = (Get-LineNumber $node)
                })
        }
    }
}

function Read-TypeDeclarations {
    param(
        $TypeNode,
        [string]$Namespace,
        [string]$File,
        [string]$Project,
        [System.Collections.Generic.List[object]]$Types,
        [System.Collections.Generic.List[object]]$Edges,
        [System.Collections.Generic.List[object]]$Procedures,
        [System.Collections.Generic.List[object]]$Registrations
    )

    $typeName = $TypeNode.Identifier.Text
    $kind = switch ($TypeNode.GetType().Name) {
        'ClassDeclarationSyntax' { 'class' }
        'RecordDeclarationSyntax' { 'record' }
        'StructDeclarationSyntax' { 'struct' }
        'InterfaceDeclarationSyntax' { 'interface' }
        'EnumDeclarationSyntax' { 'enum' }
        default { 'type' }
    }

    $bases = @()
    if ($TypeNode.BaseList) { $bases = @($TypeNode.BaseList.Types | ForEach-Object { $_.Type.ToString() }) }

    $fieldType = @{}
    $members = New-Object System.Collections.Generic.List[object]
    $injected = New-Object System.Collections.Generic.List[string]

    foreach ($member in $TypeNode.Members) {
        $memberKind = $member.GetType().Name
        $memberAttributes = Get-AttributeName $member.AttributeLists
        $line = Get-LineNumber $member

        if ($memberKind -eq 'ConstructorDeclarationSyntax') {
            foreach ($parameter in $member.ParameterList.Parameters) {
                $parameterType = Get-SimpleTypeName $parameter.Type.ToString()
                if ($parameterType) {
                    $injected.Add($parameterType)
                    $Edges.Add([pscustomobject]@{
                            From = $typeName; To = $parameterType; Kind = 'injects'
                            Member = '.ctor'; File = $File; Line = (Get-LineNumber $parameter)
                        })
                }
            }
            Read-Invocations -TypeName $typeName -MemberNode $member -FieldType $fieldType `
                -Edges $Edges -Procedures $Procedures -File $File
        }
        elseif ($memberKind -eq 'MethodDeclarationSyntax') {
            $isCommand = [bool]($memberAttributes | Where-Object { $_ -match '(^|\.)RelayCommand' })
            $members.Add([pscustomobject]@{
                    Name = $member.Identifier.Text; Line = $line; Command = $isCommand
                    Parameters = @($member.ParameterList.Parameters | ForEach-Object { $_.Type.ToString() })
                })
            Read-Invocations -TypeName $typeName -MemberNode $member -FieldType $fieldType `
                -Edges $Edges -Procedures $Procedures -File $File
        }
        elseif ($memberKind -eq 'PropertyDeclarationSyntax') {
            $declared = Get-SimpleTypeName $member.Type.ToString()
            if ($declared) { $fieldType[$member.Identifier.Text] = $declared }
            $members.Add([pscustomobject]@{
                    Name = $member.Identifier.Text; Line = $line; Command = $false; Parameters = @()
                })
        }
        elseif ($memberKind -eq 'FieldDeclarationSyntax') {
            $declared = Get-SimpleTypeName $member.Declaration.Type.ToString()
            foreach ($variable in $member.Declaration.Variables) {
                if ($declared) { $fieldType[$variable.Identifier.Text] = $declared }
            }
        }
        elseif ($memberKind -in 'ClassDeclarationSyntax', 'RecordDeclarationSyntax', 'StructDeclarationSyntax',
            'InterfaceDeclarationSyntax', 'EnumDeclarationSyntax') {
            Read-TypeDeclarations -TypeNode $member -Namespace $Namespace -File $File -Project $Project `
                -Types $Types -Edges $Edges -Procedures $Procedures -Registrations $Registrations
        }
    }

    $Types.Add([pscustomobject]@{
            Name = $typeName; Namespace = $Namespace; Kind = $kind; Base = $bases
            File = $File; Project = $Project; Line = (Get-LineNumber $TypeNode)
            Members = $members.ToArray(); Injected = $injected.ToArray()
        })

    Read-ProcedureReferences -TypeNode $TypeNode -TypeName $typeName -File $File -Procedures $Procedures

    foreach ($key in $fieldType.Keys) {
        $target = $fieldType[$key]
        if ($target -eq $typeName -or $target -in 'string', 'int', 'bool', 'double', 'long', 'decimal',
            'Task', 'DateTime', 'DateTimeOffset', 'TimeSpan', 'object', 'CancellationToken') { continue }
        $Edges.Add([pscustomobject]@{
                From = $typeName; To = $target; Kind = 'holds'
                Member = $key; File = $File; Line = (Get-LineNumber $TypeNode)
            })
    }

    foreach ($node in $TypeNode.DescendantNodes()) {
        if ($node.GetType().Name -ne 'InvocationExpressionSyntax') { continue }
        $expression = $node.Expression
        if ($expression -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax]) { continue }
        if ($expression.Name.GetType().Name -ne 'GenericNameSyntax') { continue }

        $lifetime = $expression.Name.Identifier.Text
        if ($lifetime -notin 'AddSingleton', 'AddScoped', 'AddTransient') { continue }

        $arguments = @($expression.Name.TypeArgumentList.Arguments | ForEach-Object { Get-SimpleTypeName $_.ToString() })
        if ($arguments.Count -eq 0) { continue }

        $Registrations.Add([pscustomobject]@{
                Lifetime = ($lifetime -replace '^Add', '')
                Service = $arguments[0]
                Implementation = if ($arguments.Count -gt 1) { $arguments[1] } else { $arguments[0] }
                File = $File; Line = (Get-LineNumber $node)
            })
    }
}

function Get-CodeFacts {
    param([string[]]$SearchPath)

    Initialize-Roslyn

    $types = New-Object System.Collections.Generic.List[object]
    $edges = New-Object System.Collections.Generic.List[object]
    $procedures = New-Object System.Collections.Generic.List[object]
    $registrations = New-Object System.Collections.Generic.List[object]
    $files = @(Get-CSharpFile $SearchPath)

    foreach ($file in $files) {
        $relative = Get-RelativePath $file.FullName
        $project = ($relative -split '\\')[0]
      try {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($text)
        $root = $tree.GetRoot()
        $namespace = ''

        foreach ($node in $root.ChildNodes()) {
            $nodeKind = $node.GetType().Name

            if ($nodeKind -in 'FileScopedNamespaceDeclarationSyntax', 'NamespaceDeclarationSyntax') {
                $namespace = $node.Name.ToString()
                foreach ($child in $node.ChildNodes()) {
                    if ($child.GetType().Name -match '^(Class|Record|Struct|Interface|Enum)DeclarationSyntax$') {
                        Read-TypeDeclarations -TypeNode $child -Namespace $namespace -File $relative `
                            -Project $project -Types $types -Edges $edges -Procedures $procedures `
                            -Registrations $registrations
                    }
                }
            }
            elseif ($nodeKind -match '^(Class|Record|Struct|Interface|Enum)DeclarationSyntax$') {
                Read-TypeDeclarations -TypeNode $node -Namespace $namespace -File $relative `
                    -Project $project -Types $types -Edges $edges -Procedures $procedures `
                    -Registrations $registrations
            }
        }
      }
      catch {
        Write-Warning "Skipped ${relative}: $($_.Exception.Message) @ $($_.InvocationInfo.PositionMessage)"
      }
    }

    $known = @{}
    foreach ($type in $types) { if (-not $known.ContainsKey($type.Name)) { $known[$type.Name] = $type } }

    # A type that injects a contract is related to every implementation registered for it: the pipeline
    # never names ConfigurationStep, it resolves ILaunchStep, so the registration is the edge.
    $implementations = @{}
    foreach ($registration in $registrations) {
        if (-not $implementations.ContainsKey($registration.Service)) {
            $implementations[$registration.Service] = New-Object System.Collections.Generic.List[string]
        }
        if (-not $implementations[$registration.Service].Contains($registration.Implementation)) {
            $implementations[$registration.Service].Add($registration.Implementation)
        }
    }
    foreach ($type in $types) {
        foreach ($service in $type.Injected) {
            if (-not $implementations.ContainsKey($service)) { continue }
            foreach ($implementation in $implementations[$service]) {
                if ($implementation -eq $type.Name) { continue }
                $edges.Add([pscustomobject]@{
                        From = $type.Name; To = $implementation; Kind = 'resolves'
                        Member = $service; File = $type.File; Line = $type.Line
                    })
            }
        }
    }

    $resolved = New-Object System.Collections.Generic.List[object]
    $seen = New-Object System.Collections.Generic.HashSet[string]
    foreach ($edge in $edges) {
        if (-not $known.ContainsKey($edge.To)) { continue }
        $key = '{0}|{1}|{2}' -f $edge.From, $edge.To, $edge.Kind
        if ($seen.Add($key)) { $resolved.Add($edge) }
    }

    return [pscustomobject]@{
        generated = (Get-Date).ToString('s')
        root = $script:Root
        files = $files.Count
        types = $types.ToArray()
        edges = $resolved.ToArray()
        procedures = $procedures.ToArray()
        registrations = $registrations.ToArray()
    }
}

# --------------------------------------------------------------------------------------------------
# Gaps
# --------------------------------------------------------------------------------------------------

function Get-SpecLabelKey {
    param([string]$Label)

    $whole = ($Label -replace '\\n', '') -replace '[^A-Za-z0-9_.]', ''
    $first = (($Label -split '\\n')[0]) -replace '[^A-Za-z0-9_.]', ''
    $head = if ($first -match '^([A-Z][A-Za-z0-9_]*)\.') { $Matches[1] } else { $null }

    $candidates = @($whole, $first)
    if ($head) { $candidates += $head }
    return @($candidates | Where-Object { $_ } | Select-Object -Unique)
}

function Test-ProseLabel {
    param([string]$Label)

    # A line holding a space is prose, so no amount of punctuation stripping will make it a type name.
    foreach ($line in ($Label -split '\\n')) {
        if ($line -match '\s') { return $true }
    }
    return $false
}

function Resolve-SpecNode {
    param([string[]]$Candidate, [hashtable]$TypeByName, [hashtable]$ProcedureByName)

    foreach ($key in $Candidate) {
        if ($TypeByName.ContainsKey($key)) { return [pscustomobject]@{ name = $key; sourceKind = 'type' } }
        if ($ProcedureByName.ContainsKey($key)) { return [pscustomobject]@{ name = $key; sourceKind = 'procedure' } }
    }
    return $null
}

function New-Finding {
    param(
        [string]$Severity, [string]$Kind, [string]$Sheet, [string]$Where, [string]$Node,
        [string]$Detail, $Proposal
    )

    return [pscustomobject]@{
        severity = $Severity; kind = $Kind; sheet = $Sheet; where = $Where; node = $Node
        detail = $Detail; proposal = $Proposal
    }
}

function Get-SpecGaps {
    param($Facts, [string]$OnlyWorkflow, [string]$SpecPath)

    $manifestPath = Join-Path $script:DiagramTools 'manifest.json'
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

    $typeByName = @{}
    foreach ($type in $Facts.types) { if (-not $typeByName.ContainsKey($type.Name)) { $typeByName[$type.Name] = $type } }

    $procedureByName = @{}
    foreach ($procedure in $Facts.procedures) {
        if (-not $procedureByName.ContainsKey($procedure.Name)) { $procedureByName[$procedure.Name] = $procedure }
    }

    $edgePairs = New-Object System.Collections.Generic.HashSet[string]
    foreach ($edge in $Facts.edges) { $edgePairs.Add("$($edge.From)>$($edge.To)") | Out-Null }
    foreach ($procedure in $Facts.procedures) { $edgePairs.Add("$($procedure.Type)>$($procedure.Name)") | Out-Null }

    # Two implementations registered under one contract are sequenced by the thing that resolves the
    # contract, so a sheet's step-to-step arrow is verified by the registrations rather than by a call.
    $contractsByImplementation = @{}
    foreach ($registration in $Facts.registrations) {
        $implementation = $registration.Implementation
        if (-not $contractsByImplementation.ContainsKey($implementation)) {
            $contractsByImplementation[$implementation] = New-Object System.Collections.Generic.List[string]
        }
        if (-not $contractsByImplementation[$implementation].Contains($registration.Service)) {
            $contractsByImplementation[$implementation].Add($registration.Service)
        }
    }

    $findings = New-Object System.Collections.Generic.List[object]
    $documents = New-Object System.Collections.Generic.List[object]

    $targets = if ($SpecPath) {
        $resolved = if ([System.IO.Path]::IsPathRooted($SpecPath)) { $SpecPath } else { Join-Path $script:Root $SpecPath }
        @([pscustomobject]@{ workflow = 'single'; variant = 'single'; spec = $resolved })
    }
    else {
        @($manifest.documents | Where-Object { -not $OnlyWorkflow -or $_.workflow -eq $OnlyWorkflow })
    }

    foreach ($document in $targets) {
        $specPath = if ([System.IO.Path]::IsPathRooted($document.spec)) { $document.spec }
        else { Join-Path $script:DiagramTools $document.spec }        $documents.Add([pscustomobject]@{
                workflow = $document.workflow; variant = $document.variant
                spec = $document.spec; present = (Test-Path $specPath)
            })
        if (-not (Test-Path $specPath)) {
            $findings.Add((New-Finding -Severity 'H' -Kind 'missing-spec' -Sheet '' -Where $document.spec `
                        -Node '' -Detail 'The manifest names this spec but the file is not in the tree.' -Proposal $null))
            continue
        }

        $spec = Get-Content $specPath -Raw | ConvertFrom-Json
        for ($sheetIndex = 0; $sheetIndex -lt $spec.sheets.Count; $sheetIndex++) {
            $sheet = $spec.sheets[$sheetIndex]
            $sheetName = $sheet.file

            $edgeIds = New-Object System.Collections.Generic.HashSet[string]
            $pairsOnSheet = New-Object System.Collections.Generic.HashSet[string]
            foreach ($edge in $sheet.edges) {
                $edgeIds.Add($edge[0]) | Out-Null
                $edgeIds.Add($edge[1]) | Out-Null
                $pairsOnSheet.Add("$($edge[0])>$($edge[1])") | Out-Null
            }

            $nodeInfo = @{}
            $resolved = @{}
            foreach ($lane in $sheet.lanes.PSObject.Properties) {
                $nodes = @($lane.Value)
                for ($position = 0; $position -lt $nodes.Count; $position++) {
                    $node = $nodes[$position]
                    $id = $node[0]; $label = $node[1]; $kind = $node[2]
                    $where = "sheets[$sheetIndex].lanes.$($lane.Name)[$position] ($id)"
                    $keys = Get-SpecLabelKey $label
                    $nodeInfo[$id] = [pscustomobject]@{ key = ($keys | Select-Object -First 1); kind = $kind; label = $label; where = $where }

                    if (-not $edgeIds.Contains($id)) {
                        $findings.Add((New-Finding -Severity 'H' -Kind 'orphan-node' -Sheet $sheetName -Where $where `
                                    -Node $id -Detail "No edge in sheets[$sheetIndex].edges touches this node, so the box stands alone." -Proposal $null))
                    }

                    if ($kind -in 'dec', 'note', 'refactor', 'conn') { continue }

                    $match = Resolve-SpecNode -Candidate $keys -TypeByName $typeByName -ProcedureByName $procedureByName
                    if ($match) {
                        $resolved[$id] = $match
                        continue
                    }

                    if (Test-ProseLabel $label) { continue }

                    $subject = $keys | Select-Object -First 1
                    if ($subject -notmatch '^[A-Za-z][A-Za-z0-9_]{3,}$') { continue }

                    $pool = if ($subject -match '^sp_') { $procedureByName.Keys } else { $typeByName.Keys }
                    $nearest = $pool | Where-Object { $_ -like "*$subject*" -or $subject -like "*$_*" } | Select-Object -First 3
                    $hint = if ($nearest) { ' Nearest names in code: ' + ($nearest -join ', ') + '.' } else { '' }
                    $findings.Add((New-Finding -Severity 'M' -Kind 'label-not-in-code' -Sheet $sheetName -Where $where `
                                -Node "$id = $subject" -Detail "The label names something the parse did not find.$hint" -Proposal $null))
                }
            }

            foreach ($source in ($sheet.source -split ',')) {
                $name = $source.Trim()
                if (-not $name) { continue }
                if ($name -match '\.(cs|xaml)$') { continue }
                $simple = Get-SimpleTypeName $name
                if (-not $simple) { continue }
                if (-not $typeByName.ContainsKey($simple)) {
                    $findings.Add((New-Finding -Severity 'M' -Kind 'missing-source' -Sheet $sheetName `
                                -Where "sheets[$sheetIndex].source" -Node $simple `
                                -Detail "The sheet cites $simple but the parse did not find it." -Proposal $null))
                }
            }

            foreach ($edge in $sheet.edges) {
                $from = $nodeInfo[$edge[0]]; $to = $nodeInfo[$edge[1]]
                if (-not $from -or -not $to) { continue }
                if ($from.kind -in 'dec', 'note', 'refactor', 'conn' -or $to.kind -in 'dec', 'note', 'refactor', 'conn') { continue }
                if (-not $resolved.ContainsKey($edge[0]) -or -not $resolved.ContainsKey($edge[1])) { continue }

                $fromName = $resolved[$edge[0]].name
                $toName = $resolved[$edge[1]].name
                if ($edgePairs.Contains("$fromName>$toName") -or $edgePairs.Contains("$toName>$fromName")) { continue }

                $shared = $false
                if ($contractsByImplementation.ContainsKey($fromName) -and $contractsByImplementation.ContainsKey($toName)) {
                    foreach ($contract in $contractsByImplementation[$fromName]) {
                        if ($contractsByImplementation[$toName].Contains($contract)) { $shared = $true; break }
                    }
                }
                if ($shared) { continue }

                $findings.Add((New-Finding -Severity 'L' -Kind 'unverified-edge' -Sheet $sheetName `
                            -Where "sheets[$sheetIndex].edges ($($edge[0]) -> $($edge[1]))" -Node "$fromName -> $toName" `
                            -Detail 'The spec draws this, the parse found no dependency between those two names.' -Proposal $null))
            }

            foreach ($id in @($resolved.Keys)) {
                $source = $resolved[$id]
                if ($source.sourceKind -ne 'type') { continue }

                foreach ($edge in $Facts.edges) {
                    if ($edge.From -ne $source.name) { continue }
                    if ($edge.To -in 'App', 'ShellPage') { continue }
                    $targetId = $null
                    foreach ($candidate in @($resolved.Keys)) {
                        if ($candidate -eq $id) { continue }
                        if ($resolved[$candidate].name -eq $edge.To) { $targetId = $candidate; break }
                    }
                    if (-not $targetId) { continue }
                    if ($pairsOnSheet.Contains("$id>$targetId") -or $pairsOnSheet.Contains("$targetId>$id")) { continue }

                    $findings.Add((New-Finding -Severity 'M' -Kind 'missing-edge' -Sheet $sheetName `
                                -Where "sheets[$sheetIndex].edges (append)" -Node "$id -> $targetId" `
                                -Detail "Code: $($source.name) $($edge.kind) $($edge.To) ($($edge.File):$($edge.Line))." `
                                -Proposal @($id, $targetId, $edge.kind, '')))
                }

                foreach ($procedure in $Facts.procedures) {
                    if ($procedure.Type -ne $source.name) { continue }
                    $procedureId = $null
                    foreach ($candidate in $nodeInfo.Keys) {
                        if ($nodeInfo[$candidate].key -eq $procedure.Name) { $procedureId = $candidate; break }
                    }
                    if (-not $procedureId) { continue }
                    if ($pairsOnSheet.Contains("$id>$procedureId")) { continue }

                    $findings.Add((New-Finding -Severity 'M' -Kind 'missing-edge' -Sheet $sheetName `
                                -Where "sheets[$sheetIndex].edges (append)" -Node "$id -> $procedureId" `
                                -Detail "Code: $($procedure.Type) names $($procedure.Name) ($($procedure.File):$($procedure.Line))." `
                                -Proposal @($id, $procedureId, '', '')))
                }
            }
        }
    }

    return [pscustomobject]@{
        generated = (Get-Date).ToString('s')
        workflow = $OnlyWorkflow
        documents = $documents.ToArray()
        findings = $findings.ToArray()
    }
}

function Write-GapReport {
    param($Gaps, [string]$Path)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('# Diagram spec gaps')
    $lines.Add('')
    $lines.Add("Generated $($Gaps.generated) by scripts/docs/generate-docs.ps1 -Mode Gaps.")
    $lines.Add('')
    $lines.Add('Each item names the JSON location to edit. Nothing here is applied: the wording of a label,')
    $lines.Add('a note or a lane choice is an authoring decision that a syntax parse cannot make.')
    $lines.Add('')

    foreach ($group in ($Gaps.findings | Group-Object sheet | Sort-Object Name)) {
        $title = if ($group.Name) { $group.Name } else { '(spec level)' }
        $lines.Add("## $title")
        $lines.Add('')
        foreach ($finding in $group.Group) {
            $where = if ($finding.where) { '`' + $finding.where + '`' } else { '' }
            $lines.Add("- **$($finding.kind)** [$($finding.severity)] $where")
            if ($finding.node) { $lines.Add("  - subject: $($finding.node)") }
            $lines.Add("  - $($finding.detail)")
            if ($finding.proposal) {
                $lines.Add('  - proposed entry: `' + ($finding.proposal | ConvertTo-Json -Compress) + '`')
            }
        }
        $lines.Add('')
    }

    $missing = @($Gaps.documents | Where-Object { -not $_.present })
    if ($missing.Count -gt 0) {
        $lines.Add('## Documents the manifest names but the tree does not hold')
        $lines.Add('')
        foreach ($document in $missing) {
            $lines.Add("- ``$($document.spec)`` ($($document.workflow) / $($document.variant))")
        }
        $lines.Add('')
    }

    $lines | Set-Content -Path $Path -Encoding utf8
}

# --------------------------------------------------------------------------------------------------
# Designs: XAML to mockup HTML
# --------------------------------------------------------------------------------------------------

function Get-XamlValue {
    param($Element, [string]$Name)

    if (-not $Element -or -not $Element.Attributes) { return $null }
    foreach ($attribute in $Element.Attributes) {
        if ($attribute.Name -eq $Name) { return $attribute.Value }
    }
    return $null
}

function Get-FallbackText {
    param([string]$Text, [string]$Fallback)

    if (-not $Text) { return $null }
    if ($Text -match '^\{(ThemeResource|StaticResource)\s') { return $null }

    if ($Text -match '^\{x:Bind\s+([^,}]+)') {
        $leaf = (($Matches[1].Trim() -split '\.')[-1]) -replace 'Text$|Label$|Action$', ''
        if (-not $leaf) { return $Fallback }
        return ($leaf -creplace '([a-z0-9])([A-Z])', '$1 $2')
    }
    if ($Text -match '^\{Binding\s+([^,}]+)') {
        return ($Matches[1] -creplace '([a-z0-9])([A-Z])', '$1 $2')
    }
    if ($Text -match '^\{\w+:?Bind') { return $Fallback }
    return $Text
}

function Convert-XamlThickness {
    param([string]$Value)

    if (-not $Value) { return $null }
    $parts = @($Value -split ',' | ForEach-Object { $_.Trim() })
    $sized = @($parts | ForEach-Object { if ($_ -match '^\d+(\.\d+)?$') { "$_`px" } else { $_ } })
    switch ($sized.Count) {
        1 { return $sized[0] }
        2 { return "$($sized[0]) $($sized[1])" }
        4 { return "$($sized[0]) $($sized[1]) $($sized[2]) $($sized[3])" }
        default { return $Value }
    }
}

function Convert-XamlLength {
    param([string]$Value, [switch]$Flex)

    if (-not $Value) { return $null }
    if ($Value -match '^\d+(\.\d+)?$') { return "$Value" + 'px' }
    if ($Value -match '^\d+(\.\d+)?\*$') { return if ($Flex) { '1 1 0' } else { '100%' } }
    if ($Value -eq '*') { return if ($Flex) { '1 1 0' } else { '100%' } }
    if ($Value -eq 'Auto') { return 'auto' }
    return $Value
}

function New-HtmlStyle {
    param([hashtable]$Style)

    $parts = foreach ($key in $Style.Keys) {
        if ($null -ne $Style[$key] -and $Style[$key] -ne '') { "${key}: $($Style[$key])" }
    }
    if (-not $parts) { return '' }
    return ' style="' + (@($parts) -join '; ') + '"'
}

function Get-XamlChild {
    param($Element)
    return @($Element.ChildNodes | Where-Object { $_.NodeType -eq 'Element' -and $_.LocalName -notmatch 'Definitions$' })
}

function Convert-XamlGrid {
    param($Element, [hashtable]$Style)

    $columns = @()
    foreach ($definition in $Element.ChildNodes) {
        if ($definition.NodeType -eq 'Element' -and
            ($definition.LocalName -match '^(Grid\.)?ColumnDefinitions$' -or $definition.LocalName -eq 'ColumnDefinitions')) {
            $columns = @($definition.ChildNodes | Where-Object { $_.NodeType -eq 'Element' })
        }
    }

    $children = Get-XamlChild $Element

    if ($columns.Count -le 1) {
        if ($Style['display'] -ne 'none') { $Style['display'] = 'flex'; $Style['flex-direction'] = 'column' }
        $rowGap = Get-XamlValue $Element 'RowSpacing'
        $Style['row-gap'] = (Convert-XamlLength ($rowGap ?? '0'))
        return ((Get-XamlChild $Element | ForEach-Object { Convert-XamlNode -Element $_ }) -join "`n")
    }

    $columnSpacing = Get-XamlValue $Element 'ColumnSpacing'
    if ($Style['display'] -ne 'none') { $Style['display'] = 'flex'; $Style['flex-direction'] = 'row' }
    $Style['column-gap'] = (Convert-XamlLength ($columnSpacing ?? '0'))
    $Style['align-items'] = 'stretch'

    $buckets = @{}
    $order = New-Object System.Collections.Generic.List[string]
    foreach ($child in (Get-XamlChild $Element)) {
        $index = Get-XamlValue $child 'Grid.Column'
        if (-not $index) { $index = '0' }
        if (-not $buckets.ContainsKey($index)) {
            $buckets[$index] = New-Object System.Collections.Generic.List[object]
            $order.Add($index)
        }
        $buckets[$index].Add($child)
    }

    $html = New-Object System.Collections.Generic.List[string]
    for ($index = 0; $index -lt $columns.Count; $index++) {
        $key = "$index"
        if (-not $buckets.ContainsKey($key)) { continue }
        $width = Get-XamlValue $columns[$index] 'Width'
        $flex = if ($width -match '\*' -or -not $width) { '1 1 0' } else { '0 0 ' + (Convert-XamlLength $width) }
        $inner = ($buckets[$key] | ForEach-Object { Convert-XamlNode -Element $_ }) -join "`n"
        $html.Add("<div style=`"flex: $flex; display: flex; flex-direction: column; min-width: 0; row-gap: 8px;`">$inner</div>")
    }
    return ($html -join "`n")
}

function Convert-XamlNode {
    param($Element)

    $name = $Element.LocalName
    $style = @{}
    $class = ''
    $inner = ''

    $padding = Get-XamlValue $Element 'Padding'
    $margin = Get-XamlValue $Element 'Margin'
    $spacing = Get-XamlValue $Element 'Spacing'
    $width = Get-XamlValue $Element 'Width'
    $height = Get-XamlValue $Element 'Height'
    $minHeight = Get-XamlValue $Element 'MinHeight'
    $styleRef = Get-XamlValue $Element 'Style'
    $elementText = Get-XamlValue $Element 'Text'
    $content = Get-XamlValue $Element 'Content'
    $header = Get-XamlValue $Element 'Header'
    $automationId = Get-XamlValue $Element 'AutomationProperties.AutomationId'

    if ($padding) { $style['padding'] = (Convert-XamlThickness $padding) }
    if ($margin) { $style['margin'] = (Convert-XamlThickness $margin) }
    $visibility = Get-XamlValue $Element 'Visibility'
    if ($visibility -and $visibility -match 'Collapsed') { $style['display'] = 'none' }
    if ($width) { $style['width'] = (Convert-XamlLength $width) }
    if ($height) { $style['height'] = (Convert-XamlLength $height) }
    if ($minHeight) { $style['min-height'] = (Convert-XamlLength $minHeight) }
    if ($styleRef -match 'AccentButtonStyle') { $class = 'button primary' }

    $inner = ((Get-XamlChild $Element) | ForEach-Object { Convert-XamlNode -Element $_ }) -join "`n"

    switch ($name) {
        'Grid' {
            $inner = Convert-XamlGrid -Element $Element -Style $style
        }
        'StackPanel' {
            $orientation = Get-XamlValue $Element 'Orientation'
            if ($style['display'] -ne 'none') {
                $style['display'] = 'flex'
                $style['flex-direction'] = if ($orientation -eq 'Horizontal') { 'row' } else { 'column' }
            }
            $style['gap'] = (Convert-XamlLength ($spacing ?? '0'))
        }
        'ScrollViewer' {
            $style['overflow'] = 'auto'; $style['min-height'] = '0'; $style['flex'] = '1 1 auto'
        }
        'Border' {
            $corner = Get-XamlValue $Element 'CornerRadius'
            $background = Get-XamlValue $Element 'Background'
            if ($corner) { $style['border-radius'] = $corner }
            if ($background -match 'Card|Layer|Fill') { $class = 'card' }
        }
        'TextBlock' {
            $text = Get-FallbackText $elementText 'Text'
            if (-not $text -and $inner) { $text = $inner }
            if ($styleRef -match 'TitleTextBlockStyle') { return "<h1 class=`"page-title`"$((New-HtmlStyle $style))>$text</h1>" }
            if ($styleRef -match 'SubtitleTextBlockStyle') { return "<p class=`"page-subtitle`"$((New-HtmlStyle $style))>$text</p>" }
            if ($styleRef -match 'CaptionTextBlockStyle') { return "<p class=`"caption`"$((New-HtmlStyle $style))>$text</p>" }
            return "<p$((New-HtmlStyle $style))>$text</p>"
        }
        'TextBox' {
            $label = Get-FallbackText $header 'Field'
            $parts = New-Object System.Collections.Generic.List[string]
            if ($label) { $parts.Add("<div class=`"label`">$label</div>") }
            $parts.Add('<div class="input"></div>')
            return "<div class=`"field`"$((New-HtmlStyle $style))>$($parts -join '')</div>"
        }
        'PasswordBox' {
            $label = Get-FallbackText $header 'PIN'
            $parts = New-Object System.Collections.Generic.List[string]
            if ($label) { $parts.Add("<div class=`"label`">$label</div>") }
            $parts.Add('<div class="input" style="letter-spacing: 4px;">••••</div>')
            return "<div class=`"field`"$((New-HtmlStyle $style))>$($parts -join '')</div>"
        }
        'Button' {
            $label = Get-FallbackText ($content ?? $elementText) 'Continue'
            $enabled = Get-XamlValue $Element 'IsEnabled'
            if ($enabled -and $enabled -match 'false') { $class = "$class disabled".Trim() }
            if (-not $class) { $class = 'button' }
            return "<button class=`"$class`"$((New-HtmlStyle $style))>$label</button>"
        }
        'HyperlinkButton' {
            $label = Get-FallbackText ($content ?? $elementText) 'Link'
            return "<button class=`"button`"$((New-HtmlStyle $style))>$label</button>"
        }
        'ProgressBar' { return '<div class="infobar" style="min-height: 8px;"><div class="title">Working…</div></div>' }
        'ProgressRing' { return '<div class="infobar" style="min-width: 32px; min-height: 32px;">◌</div>' }
        'InfoBar' {
            $severity = Get-XamlValue $Element 'Severity'
            $kind = switch ($severity) { 'Error' { 'error' } 'Warning' { 'caution' } 'Success' { 'success' } default { '' } }
            $title = Get-FallbackText (Get-XamlValue $Element 'Title') ''
            $message = Get-FallbackText (Get-XamlValue $Element 'Message') ''
            $body = if ($title) { "<div class=`"title`">$title</div><div>$message</div>" } else { "<div>$message</div>" }
            if ($inner) { $body += $inner }
            return "<div class=`"infobar $kind`"$((New-HtmlStyle $style))>$body</div>"
        }
        'Image' { return '<div class="tile" style="min-width: 64px; min-height: 64px;">image</div>' }
        { $_ -in 'FontIcon', 'SymbolIcon' } {
            $glyph = Get-XamlValue $Element 'Glyph'
            return "<span class=`"icon`"$((New-HtmlStyle $style))>$glyph</span>"
        }
        { $_ -in 'ListView', 'GridView', 'ItemsRepeater', 'ItemsControl' } {
            if ($inner) { return $inner }
            return '<div class="row wrap"><div class="tile"><span class="name">item</span></div><div class="tile"><span class="name">item</span></div><div class="tile"><span class="name">item</span></div></div>'
        }
        'DataTemplate' { return $inner }
        'ComboBox' {
            $label = Get-FallbackText $header ''
            $field = if ($label) { "<div class=`"label`">$label</div>" } else { '' }
            return "<div class=`"field`"$((New-HtmlStyle $style))>$field<div class=`"input`">choose…</div></div>"
        }
        { $_ -in 'CheckBox', 'RadioButton' } {
            return "<p$((New-HtmlStyle $style))>☐ $(Get-FallbackText $content '')</p>"
        }
        'ToggleSwitch' {
            return "<p$((New-HtmlStyle $style))>◻ $(Get-FallbackText $header '')</p>"
        }
        { $_ -in 'Page', 'UserControl', 'Window' } { }
        default {
            $style['border'] = '1px dashed #c00'
            $style['padding'] = '4px'
            $inner = "<span style=`"font-size: 11px; color: #c00;`">$name (not mapped)</span>" + $inner
        }
    }

    $attribute = if ($automationId) { " data-automation-id=`"$automationId`"" } else { '' }
    $visibility = Get-XamlValue $Element 'Visibility'
    if ($visibility -match '^\{(x:Bind|Binding)\s+([^,}]+)') {
        $attribute += " data-visible-when=`"$($Matches[2].Trim())`""
    }
    return "<div$((New-HtmlStyle $style))$attribute>`n$inner`n</div>"
}

function New-MockupHtml {
    param([string]$XamlPath, [string]$Title, [string]$Stem, [string]$MockupFolder, [string]$Banner)

    [xml]$xaml = Get-Content $XamlPath -Raw
    $body = Convert-XamlNode -Element $xaml.DocumentElement
    $relative = Get-RelativePath $XamlPath
    $cssPath = Join-Path $script:DesignTools 'mockup\mockup.css'
    $cssHref = [System.IO.Path]::GetRelativePath($MockupFolder, $cssPath) -replace '\\', '/'

    return @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>$Banner — $Title</title>
  <link rel="stylesheet" href="$cssHref">
</head>
<body class="mockup">
  <div class="variant-banner">
    $($Banner.ToUpper()) — GENERATED FROM $($relative.ToUpper())
    <span class="provenance">built by scripts/docs/generate-docs.ps1; an unmapped control is outlined in red</span>
  </div>

  <div class="titlebar">
    <span class="caption">MTM Waitlist</span>
    <span class="glyph">— □ ✕</span>
  </div>

  <main class="page" style="padding: 24px; row-gap: 16px;">
$body
  </main>
</body>
</html>
"@
}

function New-DesignRecord {
    param([string]$ScreenName, [string]$RefactorName, [string]$Title, [string[]]$SourceXaml)

    return [ordered]@{
        workflow         = 'startup-launch'
        workflow_variant = 'engineer'
        screen           = $ScreenName
        refactor         = $RefactorName
        title            = $Title
        audience         = 'developer'
        window           = [ordered]@{
            viewports = @(
                [ordered]@{ name = 'wide'; width = 1600; height = 1000; scale = 1 }
                [ordered]@{ name = 'medium'; width = 1280; height = 800; scale = 1 }
                [ordered]@{ name = 'minimum'; width = 900; height = 500; scale = 1 }
            )
        }
        before           = [ordered]@{
            kind = 'reconstruction'; mockup = "mockups/$RefactorName-before.html"
            captured_at = $null; capture_command = $null; source_xaml = $SourceXaml
        }
        after            = [ordered]@{ mockup = "mockups/$RefactorName-after.html" }
        states           = @([ordered]@{ name = 'default'; applies_to = @('before', 'after') })
        callouts         = @()
        elements         = @()
        workflow_refs    = @()
        design_system    = [ordered]@{
            resources = @('Styles/FontSizes.xaml', 'Styles/Thickness.xaml', 'Styles/TextBlock.xaml',
                'Styles/ModuleSetup.xaml', 'Styles/Dialogs.xaml')
        }
        approval         = [ordered]@{
            approved_at = $null; approved_by = $null; sources_sha256 = $null
            rendered_sha256 = $null; design_system_sha256 = $null
        }
    }
}

function Invoke-DesignGeneration {
    param([string]$ScreenName, [string]$TargetDir)

    $views = @(
        if ($ScreenName) {
            Get-ChildItem -Path $script:Root -Recurse -File -Filter "$ScreenName.xaml" |
                Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
        }
        else {
            Get-ChildItem -Path (Join-Path $script:Root 'Module_Startup\Views') -File -Filter '*.xaml' -ErrorAction SilentlyContinue
        }
    )
    if ($views.Count -eq 0) { throw "No XAML view matched '$ScreenName'." }

    $written = New-Object System.Collections.Generic.List[object]
    foreach ($view in $views) {
        $stem = [System.IO.Path]::GetFileNameWithoutExtension($view.Name)
        $kebab = ($stem -creplace '([a-z0-9])([A-Z])', '$1-$2').ToLower()
        $refactorName = if ($Refactor) { $Refactor } else { $kebab }

        $folder = Join-Path $TargetDir $kebab
        $mockups = Join-Path $folder 'mockups'
        New-Item -ItemType Directory -Force -Path $mockups | Out-Null

        $title = ($stem -creplace '([a-z0-9])([A-Z])', '$1 $2')

        $before = New-MockupHtml -XamlPath $view.FullName -Title $title -Stem $stem -MockupFolder $mockups -Banner 'BEFORE'
        $before | Set-Content -Path (Join-Path $mockups "$refactorName-before.html") -Encoding utf8

        $after = New-MockupHtml -XamlPath $view.FullName -Title $title -Stem $stem -MockupFolder $mockups -Banner 'AFTER — NOT YET DESIGNED'
        $after | Set-Content -Path (Join-Path $mockups "$refactorName-after.html") -Encoding utf8

        $record = New-DesignRecord -ScreenName $kebab -RefactorName $refactorName -Title $title `
            -SourceXaml @((Get-RelativePath $view.FullName))
        $recordPath = Join-Path $folder "$refactorName-design.json"
        $record | ConvertTo-Json -Depth 8 | Set-Content -Path $recordPath -Encoding utf8

        $written.Add([pscustomobject]@{ screen = $stem; design = $recordPath; mockups = $mockups })
    }
    return $written
}

function Invoke-DesignRender {
    param([string]$TargetDir)

    $designs = @(Get-ChildItem -Path $TargetDir -Recurse -File -Filter '*-design.json')
    if ($designs.Count -eq 0) { Write-Host 'No design records to render.'; return }

    $python = if (Get-Command python -ErrorAction SilentlyContinue) { 'python' } else { 'py' }
    foreach ($design in $designs) {
        Write-Host "Rendering $($design.Name)"
        & $python (Join-Path $script:DesignTools 'render_mockup.py') --design $design.FullName
        if ($LASTEXITCODE -ne 0) { throw "render_mockup.py failed for $($design.FullName)" }
    }
}

# --------------------------------------------------------------------------------------------------
# Verify
# --------------------------------------------------------------------------------------------------

function Invoke-Verify {
    $python = if (Get-Command python -ErrorAction SilentlyContinue) { 'python' } else { 'py' }

    if (-not (Test-Path $script:DiagramTools) -or -not (Test-Path $script:DesignTools)) {
        throw 'The diagram or UI-design tool folder is missing; nothing to verify.'
    }

    $pdfs = @(Get-ChildItem -Path (Join-Path $script:Root 'docs\diagrams\pdf') -Recurse -File -Filter '*.pdf' -ErrorAction SilentlyContinue)
    if ($pdfs.Count -gt 0) {
        & $python (Join-Path $script:DiagramTools 'verify_pdfs.py') --no-png
        if ($LASTEXITCODE -ne 0) { throw 'verify_pdfs.py reported a print-contract failure.' }
    }

    $designs = @(Get-ChildItem -Path (Join-Path $script:Root 'docs\ui-designs') -Recurse -File -Filter '*-design.json' -ErrorAction SilentlyContinue)
    if ($designs.Count -gt 0) {
        & $python (Join-Path $script:DesignTools 'verify_designs.py')
        if ($LASTEXITCODE -ne 0) { throw 'verify_designs.py reported a failure.' }
    }
}

# --------------------------------------------------------------------------------------------------
# Entry point
# --------------------------------------------------------------------------------------------------

$modes = if ($Mode -contains 'All') { @('Facts', 'Gaps', 'Designs', 'Verify') } else { @($Mode) }
$searchPaths = if ($Paths) { $Paths }
else { @('MTM_Waitlist.Startup', 'Module_Startup', 'Module_Core\Views', 'App.xaml.cs') }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$facts = $null
if ($modes -contains 'Facts' -or $modes -contains 'Gaps') {
    Write-Host "Reading C# under: $($searchPaths -join ', ')"
    $facts = Get-CodeFacts -SearchPath $searchPaths
    $factsPath = Join-Path $OutDir 'code-facts.json'
    $facts | ConvertTo-Json -Depth 8 | Set-Content -Path $factsPath -Encoding utf8
    Write-Host ('Parsed {0} files: {1} types, {2} edges, {3} stored-procedure references, {4} DI registrations.' -f `
            $facts.files, $facts.types.Count, $facts.edges.Count, $facts.procedures.Count, $facts.registrations.Count)
    Write-Host "Facts: $factsPath"
}

if ($modes -contains 'Gaps') {
    $gaps = Get-SpecGaps -Facts $facts -OnlyWorkflow $Workflow -SpecPath $SpecPath
    $gapsPath = Join-Path $OutDir 'diagram-gaps.json'
    $gaps | ConvertTo-Json -Depth 8 | Set-Content -Path $gapsPath -Encoding utf8

    $reportPath = Join-Path $OutDir 'diagram-gaps.md'
    Write-GapReport -Gaps $gaps -Path $reportPath

    $counts = $gaps.findings | Group-Object severity | ForEach-Object { "$($_.Name)=$($_.Count)" }
    Write-Host ('Gaps: {0} findings ({1}).' -f $gaps.findings.Count, ($counts -join ', '))
    Write-Host "Report: $reportPath"
}

if ($modes -contains 'Designs') {
    $designDir = if ($WriteInPlace) { Join-Path $script:Root 'docs\ui-designs' } else { Join-Path $OutDir 'designs' }
    New-Item -ItemType Directory -Force -Path $designDir | Out-Null

    foreach ($item in (Invoke-DesignGeneration -ScreenName $Screen -TargetDir $designDir)) {
        Write-Host "Design: $($item.design)"
    }

    try { Invoke-DesignRender -TargetDir $designDir }
    catch { Write-Warning "Rendering stopped: $($_.Exception.Message)" }
}

if ($modes -contains 'Verify') {
    Invoke-Verify
}

Write-Host 'Done.'
