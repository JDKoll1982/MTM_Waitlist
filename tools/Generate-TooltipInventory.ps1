<#
.SYNOPSIS
  Rerunnable inventory + missing-key generator for actionable WinUI control tooltips.

.DESCRIPTION
  Scans *.xaml for actionable controls, diffs against TooltipResources*.resw,
  and can optionally append missing normal/developer tooltip entries.

  Designed to be safe to run anytime:
  - Inventory mode (default) never writes source files
  - ApplyMissing only appends keys that do not already exist
  - Existing hand-written tooltip text is preserved

.PARAMETER ApplyMissing
  Append missing keys to TooltipResources.resw and TooltipResources.developer.resw.

.PARAMETER WireXaml
  For missing controls that have x:Name and no TooltipBehavior yet, insert
  shared:TooltipBehavior attributes using generated keys/associated files.
  Prompts control-by-control (Y/N/Q) so mass XAML edits cannot run unattended.

.PARAMETER IncludeStyles
  Include Styles/*.xaml in the scan (off by default).

.PARAMETER OutputDirectory
  Where inventory artifacts are written. Defaults to tools/tooltip-inventory.

.EXAMPLE
  pwsh -File tools/Generate-TooltipInventory.ps1

.EXAMPLE
  pwsh -File tools/Generate-TooltipInventory.ps1 -ApplyMissing

.EXAMPLE
  pwsh -File tools/Generate-TooltipInventory.ps1 -ApplyMissing -WireXaml
#>
[CmdletBinding()]
param(
    [switch]$ApplyMissing,
    [switch]$WireXaml,
    [switch]$IncludeStyles,
    [string]$RepoRoot,
    [string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
else {
    $scriptDir = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $scriptDir "tooltip-inventory"
}

function Write-Info([string]$Message) {
    Write-Host "[tooltip-inventory] $Message"
}

function Get-ActionableControlNames {
    @(
        "Button",
        "ToggleButton",
        "RepeatButton",
        "HyperlinkButton",
        "AppBarButton",
        "MenuFlyoutItem",
        "DropDownButton",
        "SplitButton",
        "TextBox",
        "PasswordBox",
        "NumberBox",
        "AutoSuggestBox",
        "ComboBox",
        "CheckBox",
        "RadioButton",
        "ToggleSwitch",
        "Slider",
        "CalendarDatePicker",
        "DatePicker",
        "TimePicker",
        "ListView",
        "GridView",
        "TreeView",
        "NavigationViewItem",
        "PivotItem"
    )
}

function Get-RelativePath([string]$FullPath, [string]$Root) {
    $full = [System.IO.Path]::GetFullPath($FullPath)
    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($full.Substring($rootFull.Length) -replace '\\', '/')
    }
    return ($full -replace '\\', '/')
}

function ConvertTo-HumanLabel([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $cleaned = ($Value -replace '[_\-]+', ' ').Trim()
    $withSpaces = [regex]::Replace($cleaned, '([a-z0-9])([A-Z])', '$1 $2')
    $withSpaces = [regex]::Replace($withSpaces, '([A-Za-z])([0-9])', '$1 $2')
    $parts = $withSpaces.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
    $titled = foreach ($part in $parts) {
        if ($part.Length -eq 1) {
            $part.ToUpperInvariant()
        }
        else {
            $part.Substring(0, 1).ToUpperInvariant() + $part.Substring(1)
        }
    }
    return ($titled -join ' ')
}

function Get-ModulePrefix([string]$RelativePath) {
    if ($RelativePath -match 'Module_([^/]+)/') {
        return $Matches[1]
    }
    if ($RelativePath -match '^(App|MainWindow)\.xaml$') {
        return "App"
    }
    return "App"
}

function Get-AttributeValue([string]$Block, [string]$AttributeName) {
    $pattern = [regex]::Escape($AttributeName) + '\s*=\s*"([^"]*)"'
    $match = [regex]::Match($Block, $pattern)
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }
    return ""
}

function Get-ExistingReswKeys([string]$ReswPath) {
    # Case-insensitive map for PS 5.1 compatibility.
    $keys = New-Object 'System.Collections.Hashtable' ([System.StringComparer]::OrdinalIgnoreCase)
    if (-not (Test-Path $ReswPath)) {
        return $keys
    }

    [xml]$xml = Get-Content -Path $ReswPath -Raw
    foreach ($node in @($xml.root.data)) {
        if ($null -ne $node -and $null -ne $node.name) {
            $keys[[string]$node.name] = $true
        }
    }
    return $keys
}

function Ensure-ReswFile([string]$ReswPath) {
    if (Test-Path $ReswPath) {
        return
    }

    $dir = Split-Path -Parent $ReswPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    @"
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a7c34c29f</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a7c34c29f</value>
  </resheader>
</root>
"@ | Set-Content -Path $ReswPath -Encoding UTF8
}

function Add-ReswEntries {
    param(
        [Parameter(Mandatory = $true)][string]$ReswPath,
        # Allow empty arrays: -ApplyMissing is often re-run when keys already exist.
        [Parameter(Mandatory = $false)]
        [AllowEmptyCollection()]
        [hashtable[]]$Entries = @()
    )

    if ($null -eq $Entries -or @($Entries).Count -eq 0) {
        return 0
    }

    Ensure-ReswFile -ReswPath $ReswPath
    [xml]$xml = Get-Content -Path $ReswPath -Raw
    $existing = Get-ExistingReswKeys -ReswPath $ReswPath
    $added = 0

    foreach ($entry in @($Entries)) {
        if ($null -eq $entry) {
            continue
        }

        $name = [string]$entry.Name
        $value = [string]$entry.Value
        if ([string]::IsNullOrWhiteSpace($name) -or $existing.ContainsKey($name)) {
            continue
        }

        $data = $xml.CreateElement("data")
        $null = $data.SetAttribute("name", $name)
        $null = $data.SetAttribute("xml:space", "preserve")
        $valueNode = $xml.CreateElement("value")
        $valueNode.InnerText = $value
        $null = $data.AppendChild($valueNode)
        $null = $xml.root.AppendChild($data)
        $existing[$name] = $true
        $added++
    }

    if ($added -gt 0) {
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.Indent = $true
        $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
        $writer = [System.Xml.XmlWriter]::Create($ReswPath, $settings)
        try {
            $xml.Save($writer)
        }
        finally {
            $writer.Dispose()
        }
    }

    return $added
}

function Get-AssociatedFiles([string]$RelativeXamlPath) {
    $files = New-Object System.Collections.Generic.List[string]
    $files.Add($RelativeXamlPath)

    $codeBehind = "$RelativeXamlPath.cs"
    $codeBehindFs = Join-Path $RepoRoot ($codeBehind -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (Test-Path $codeBehindFs) {
        $files.Add($codeBehind)
    }

    if ($RelativeXamlPath -match '^(?<prefix>.*/Views/)(?<name>[^/]+)\.xaml$') {
        $vmRel = ($Matches.prefix -replace '/Views/', '/ViewModels/') + $Matches.name + "ViewModel.cs"
        # Page names often end with Page; ViewModel usually drops Page suffix.
        $vmRelAlt = ($Matches.prefix -replace '/Views/', '/ViewModels/') + ($Matches.name -replace 'Page$', '') + "ViewModel.cs"
        foreach ($candidate in @($vmRel, $vmRelAlt)) {
            $fs = Join-Path $RepoRoot ($candidate -replace '/', [IO.Path]::DirectorySeparatorChar)
            if (Test-Path $fs) {
                $files.Add(($candidate -replace '\\', '/'))
            }
        }
    }

    # Shared/core tooltip infrastructure commonly needed for developer inspector.
    foreach ($infra in @(
            "Module_Shared/Services/TooltipBehavior.cs",
            "Module_Shared/Services/TooltipService.cs",
            "Module_Shared/Services/ControlInspectorService.cs",
            "Module_Core/Helpers/ResourceExtensions.cs"
        )) {
        $fs = Join-Path $RepoRoot ($infra -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path $fs) {
            $files.Add($infra)
        }
    }

    return ($files | Select-Object -Unique)
}

function New-TooltipKey {
    param(
        [string]$ModulePrefix,
        [string]$ControlName,
        [string]$ControlType,
        [string]$FileLeaf,
        [int]$Occurrence
    )

    $basis = if (-not [string]::IsNullOrWhiteSpace($ControlName)) {
        $ControlName
    }
    elseif (-not [string]::IsNullOrWhiteSpace($ControlType)) {
        "$($FileLeaf)_$ControlType$Occurrence"
    }
    else {
        "$($FileLeaf)_Control$Occurrence"
    }

    $basis = ($basis -replace '[^A-Za-z0-9_]', '_')
    $basis = ($basis -replace '_+', '_').Trim('_')
    if ([string]::IsNullOrWhiteSpace($basis)) {
        $basis = "Control$Occurrence"
    }

    return "${ModulePrefix}_${basis}_Tooltip"
}

function Get-DefaultNormalText {
    param(
        [string]$ControlName,
        [string]$ControlType,
        [string]$HintText
    )

    if (-not [string]::IsNullOrWhiteSpace($HintText)) {
        $hint = $HintText.Trim()
        if ($hint.Length -gt 0) {
            if ($hint.EndsWith('.')) { return $hint }
            return "$hint."
        }
    }

    $label = ConvertTo-HumanLabel $ControlName
    if ([string]::IsNullOrWhiteSpace($label)) {
        $label = ConvertTo-HumanLabel $ControlType
    }
    if ([string]::IsNullOrWhiteSpace($label)) {
        $label = "this control"
    }

    switch -Regex ($ControlType) {
        'Button|MenuFlyoutItem|HyperlinkButton|AppBarButton|SplitButton|DropDownButton' {
            return "Activate $label."
        }
        'TextBox|PasswordBox|NumberBox|AutoSuggestBox' {
            return "Enter a value in $label."
        }
        'ComboBox|ListView|GridView|TreeView|NavigationViewItem|PivotItem' {
            return "Select an option in $label."
        }
        'CheckBox|RadioButton|ToggleSwitch|ToggleButton' {
            return "Toggle $label."
        }
        default {
            return "Use $label."
        }
    }
}

function Get-DefaultDeveloperText {
    param(
        [string]$ControlName,
        [string]$ControlType
    )

    $label = ConvertTo-HumanLabel $ControlName
    if ([string]::IsNullOrWhiteSpace($label)) {
        $label = ConvertTo-HumanLabel $ControlType
    }
    if ([string]::IsNullOrWhiteSpace($label)) {
        $label = "Control"
    }
    return $label
}

function Get-HintText([string]$Block) {
    foreach ($attr in @("FallbackText", "PlaceholderText", "Header", "Content", "AutomationProperties.Name", "ToolTipService.ToolTip")) {
        # attached-style attribute names in source may appear as AutomationProperties.Name
        $value = Get-AttributeValue -Block $Block -AttributeName $attr
        if (-not [string]::IsNullOrWhiteSpace($value) -and $value -notmatch '[{}]') {
            return $value
        }
    }

    # shared tooltip fallback if already present
    $behaviorFallback = Get-AttributeValue -Block $Block -AttributeName "shared:TooltipBehavior.FallbackText"
    if (-not [string]::IsNullOrWhiteSpace($behaviorFallback)) {
        return $behaviorFallback
    }

    return ""
}

function Find-ActionableControls {
    param([string[]]$XamlFiles)

    $controlNames = Get-ActionableControlNames
    $alt = ($controlNames -join '|')
    $startPattern = [regex]::new("<(?<type>$alt)(?=[\s>/])", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $results = New-Object System.Collections.Generic.List[object]
    $keyUsage = @{}

    foreach ($file in $XamlFiles) {
        $relative = Get-RelativePath -FullPath $file -Root $RepoRoot
        if (-not $IncludeStyles -and $relative.StartsWith("Styles/", [StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $text = Get-Content -Path $file -Raw
        if ([string]::IsNullOrWhiteSpace($text)) {
            continue
        }

        $module = Get-ModulePrefix -RelativePath $relative
        $fileLeaf = [IO.Path]::GetFileNameWithoutExtension($relative)
        $matches = $startPattern.Matches($text)
        $occurrenceByType = @{}

        foreach ($match in $matches) {
            $type = $match.Groups["type"].Value
            if (-not $occurrenceByType.ContainsKey($type)) {
                $occurrenceByType[$type] = 0
            }
            $occurrenceByType[$type]++
            $occurrence = [int]$occurrenceByType[$type]

            # Capture from control start to end of opening tag.
            $slice = $text.Substring($match.Index)
            $endIdx = $slice.IndexOf('>')
            if ($endIdx -lt 0) {
                continue
            }
            $openTag = $slice.Substring(0, $endIdx + 1)

            # Also include a small following window for multi-line attributes still inside open tag.
            # (openTag already includes full opening tag even when multi-line because first '>' ends it.)
            $block = $openTag

            $name = Get-AttributeValue -Block $block -AttributeName "x:Name"
            if ([string]::IsNullOrWhiteSpace($name)) {
                $name = Get-AttributeValue -Block $block -AttributeName "Name"
            }
            $uid = Get-AttributeValue -Block $block -AttributeName "x:Uid"
            $existingKey = Get-AttributeValue -Block $block -AttributeName "shared:TooltipBehavior.ResourceKey"
            $existingAssociated = Get-AttributeValue -Block $block -AttributeName "shared:TooltipBehavior.AssociatedFiles"
            $existingFallback = Get-AttributeValue -Block $block -AttributeName "shared:TooltipBehavior.FallbackText"
            $hasBehavior = -not [string]::IsNullOrWhiteSpace($existingKey) -or
                -not [string]::IsNullOrWhiteSpace($existingAssociated) -or
                -not [string]::IsNullOrWhiteSpace($existingFallback)

            $hint = Get-HintText -Block $block
            $proposedKey = if (-not [string]::IsNullOrWhiteSpace($existingKey)) {
                $existingKey
            }
            else {
                New-TooltipKey -ModulePrefix $module -ControlName $name -ControlType $type -FileLeaf $fileLeaf -Occurrence $occurrence
            }

            if ([string]::IsNullOrWhiteSpace($existingKey)) {
                $uniqueKey = $proposedKey
                $suffix = 2
                while ($keyUsage.ContainsKey($uniqueKey)) {
                    $uniqueKey = "${proposedKey}_$suffix"
                    $suffix++
                }
                $proposedKey = $uniqueKey
            }

            if (-not $keyUsage.ContainsKey($proposedKey)) {
                $keyUsage[$proposedKey] = 0
            }
            $keyUsage[$proposedKey]++

            $lineNumber = ($text.Substring(0, $match.Index) -split "`n").Count
            $associated = Get-AssociatedFiles -RelativeXamlPath $relative

            $results.Add([pscustomobject]@{
                    File                = $relative
                    Line                = $lineNumber
                    ControlType         = $type
                    Name                = $name
                    Uid                 = $uid
                    Module              = $module
                    ProposedKey         = $proposedKey
                    ExistingKey         = $existingKey
                    HasTooltipBehavior  = $hasBehavior
                    HintText            = $hint
                    NormalText          = Get-DefaultNormalText -ControlName $name -ControlType $type -HintText $hint
                    DeveloperText       = Get-DefaultDeveloperText -ControlName $name -ControlType $type
                    AssociatedFiles     = ($associated -join '|')
                    OpenTagPreview      = ($openTag -replace '\s+', ' ').Trim()
                }) | Out-Null
        }
    }

    return $results
}

function Get-XamlWirePreview {
    param(
        [Parameter(Mandatory = $true)]$Candidate
    )

    $result = [ordered]@{
        CanWire      = $false
        Reason       = ""
        FullPath     = ""
        PreviewAttrs = ""
        NeedsXmlns   = $false
    }

    if ($Candidate.HasTooltipBehavior) {
        $result.Reason = "already wired"
        return [pscustomobject]$result
    }
    if ([string]::IsNullOrWhiteSpace($Candidate.Name)) {
        $result.Reason = "unnamed control (skipped to avoid ambiguous edits)"
        return [pscustomobject]$result
    }

    $fullPath = Join-Path $RepoRoot ($Candidate.File -replace '/', [IO.Path]::DirectorySeparatorChar)
    $result.FullPath = $fullPath
    if (-not (Test-Path $fullPath)) {
        $result.Reason = "file not found"
        return [pscustomobject]$result
    }

    $content = Get-Content -Path $fullPath -Raw
    $result.NeedsXmlns = ($content -notmatch 'xmlns:shared="using:MTM_Waitlist\.Module_Shared\.Services"')

    $name = [regex]::Escape([string]$Candidate.Name)
    $pattern = "(?s)(<(?:$([regex]::Escape($Candidate.ControlType)))\b(?=[^>]*\bx:Name=`"$name`")[^>]*?)(/?)>"
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        $result.Reason = "could not uniquely locate control open tag"
        return [pscustomobject]$result
    }

    $open = $match.Groups[1].Value
    if ($open -match 'shared:TooltipBehavior\.') {
        $result.Reason = "already has TooltipBehavior attributes"
        return [pscustomobject]$result
    }

    $fallback = ([string]$Candidate.NormalText).Replace('"', '&quot;')
    $associated = ([string]$Candidate.AssociatedFiles).Replace('"', '&quot;')
    $result.PreviewAttrs = "shared:TooltipBehavior.ResourceKey=`"$($Candidate.ProposedKey)`" shared:TooltipBehavior.AssociatedFiles=`"$associated`" shared:TooltipBehavior.FallbackText=`"$fallback`""
    $result.CanWire = $true
    $result.Reason = "ready"
    return [pscustomobject]$result
}

function Set-XamlTooltipBehavior {
    param(
        [Parameter(Mandatory = $true)]$Candidate
    )

    $preview = Get-XamlWirePreview -Candidate $Candidate
    if (-not $preview.CanWire) {
        return $false
    }

    $fullPath = [string]$preview.FullPath
    $content = Get-Content -Path $fullPath -Raw
    if ($preview.NeedsXmlns) {
        $content = [regex]::Replace(
            $content,
            '(xmlns:x="http://schemas\.microsoft\.com/winfx/2006/xaml")',
            "`$1`r`n    xmlns:shared=`"using:MTM_Waitlist.Module_Shared.Services`"",
            1
        )
    }

    $name = [regex]::Escape([string]$Candidate.Name)
    $pattern = "(?s)(<(?:$([regex]::Escape($Candidate.ControlType)))\b(?=[^>]*\bx:Name=`"$name`")[^>]*?)(/?)>"
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        return $false
    }

    $open = $match.Groups[1].Value
    if ($open -match 'shared:TooltipBehavior\.') {
        return $false
    }

    $insertion = " " + [string]$preview.PreviewAttrs
    $selfClosing = $match.Groups[2].Value
    $replacement = $open.TrimEnd() + $insertion + $selfClosing + ">"
    $newContent = $content.Substring(0, $match.Index) + $replacement + $content.Substring($match.Index + $match.Length)
    Set-Content -Path $fullPath -Value $newContent -Encoding UTF8
    return $true
}

function Read-WireConfirmation {
    param(
        [Parameter(Mandatory = $true)][int]$Index,
        [Parameter(Mandatory = $true)][int]$Total,
        [Parameter(Mandatory = $true)]$Candidate,
        [Parameter(Mandatory = $true)]$Preview
    )

    Write-Host ""
    Write-Host ("=" * 72) -ForegroundColor DarkCyan
    Write-Host ("Wire candidate {0}/{1}" -f $Index, $Total) -ForegroundColor Cyan
    Write-Host ("  File      : {0}" -f $Candidate.File)
    Write-Host ("  Control   : {0}" -f $Candidate.ControlType)
    Write-Host ("  x:Name    : {0}" -f $(if ([string]::IsNullOrWhiteSpace($Candidate.Name)) { "(none)" } else { $Candidate.Name }))
    Write-Host ("  Key       : {0}" -f $Candidate.ProposedKey)
    Write-Host ("  Fallback  : {0}" -f $Candidate.NormalText)
    Write-Host ("  Assoc     : {0}" -f $Candidate.AssociatedFiles)
    if ($Preview.NeedsXmlns) {
        Write-Host "  Note      : will add xmlns:shared on this page" -ForegroundColor Yellow
    }
    if (-not $Preview.CanWire) {
        Write-Host ("  Status    : SKIP auto - {0}" -f $Preview.Reason) -ForegroundColor DarkYellow
        return "skip"
    }

    Write-Host "  Will add  :" -ForegroundColor Gray
    Write-Host ("    {0}" -f $Preview.PreviewAttrs) -ForegroundColor Gray
    Write-Host ""
    Write-Host "Wire this control? [Y]es / [N]o / [Q]uit remaining" -ForegroundColor Yellow

    while ($true) {
        $answer = Read-Host "Confirm"
        if ($null -eq $answer) {
            $answer = ""
        }
        switch -Regex ($answer.Trim().ToLowerInvariant()) {
            '^(y|yes)$' { return "yes" }
            '^(n|no|)$' { return "no" }
            '^(q|quit)$' { return "quit" }
            default {
                Write-Host "Enter Y, N, or Q." -ForegroundColor DarkYellow
            }
        }
    }
}

# -------------------- main --------------------
Write-Info "RepoRoot: $RepoRoot"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$normalResw = Join-Path $RepoRoot "Strings\en-us\TooltipResources.resw"
$developerResw = Join-Path $RepoRoot "Strings\en-us\TooltipResources.developer.resw"

$xamlFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter *.xaml |
    Where-Object {
        $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' -and
        $_.FullName -notmatch '\\tools\\tooltip-inventory\\' -and
        (
            $IncludeStyles -or
            $_.FullName -notmatch '\\Styles\\'
        )
    } |
    Select-Object -ExpandProperty FullName

Write-Info "Scanning $($xamlFiles.Count) XAML files..."
$candidates = @(Find-ActionableControls -XamlFiles $xamlFiles)
Write-Info "Found $($candidates.Count) actionable controls."

$existingNormal = Get-ExistingReswKeys -ReswPath $normalResw
$existingDeveloper = Get-ExistingReswKeys -ReswPath $developerResw

foreach ($item in $candidates) {
    $key = [string]$item.ProposedKey
    $missingNormal = -not $existingNormal.ContainsKey($key)
    $missingDeveloper = -not $existingDeveloper.ContainsKey($key)
    $item | Add-Member -NotePropertyName MissingNormalKey -NotePropertyValue $missingNormal -Force
    $item | Add-Member -NotePropertyName MissingDeveloperKey -NotePropertyValue $missingDeveloper -Force
    $item | Add-Member -NotePropertyName NeedsResx -NotePropertyValue ($missingNormal -or $missingDeveloper) -Force
    $item | Add-Member -NotePropertyName NeedsXamlWire -NotePropertyValue (-not [bool]$item.HasTooltipBehavior) -Force
}

$inventoryPath = Join-Path $OutputDirectory "tooltip-inventory.json"
$missingPath = Join-Path $OutputDirectory "tooltip-missing.json"
$csvPath = Join-Path $OutputDirectory "tooltip-inventory.csv"
$summaryPath = Join-Path $OutputDirectory "summary.txt"

$candidates | ConvertTo-Json -Depth 6 | Set-Content -Path $inventoryPath -Encoding UTF8
$missing = @($candidates | Where-Object { $_.NeedsResx -or $_.NeedsXamlWire })
$missing | ConvertTo-Json -Depth 6 | Set-Content -Path $missingPath -Encoding UTF8
$candidates | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

$summary = @()
$summary += "Actionable controls: $($candidates.Count)"
$summary += "Already wired with TooltipBehavior: $((@($candidates | Where-Object HasTooltipBehavior)).Count)"
$summary += "Missing normal resx keys: $((@($candidates | Where-Object MissingNormalKey)).Count)"
$summary += "Missing developer resx keys: $((@($candidates | Where-Object MissingDeveloperKey)).Count)"
$summary += "Need XAML wiring: $((@($candidates | Where-Object NeedsXamlWire)).Count)"
$summary += "Inventory: $inventoryPath"
$summary += "Missing: $missingPath"
$summary += "CSV: $csvPath"
$summary -join [Environment]::NewLine | Set-Content -Path $summaryPath -Encoding UTF8
$summary | ForEach-Object { Write-Info $_ }

$appliedNormal = 0
$appliedDeveloper = 0
$wiredXaml = 0

if ($ApplyMissing) {
    $normalEntries = @(
        $candidates |
            Where-Object { $_.MissingNormalKey } |
            ForEach-Object {
                @{
                    Name  = [string]$_.ProposedKey
                    Value = [string]$_.NormalText
                }
            }
    )
    $developerEntries = @(
        $candidates |
            Where-Object { $_.MissingDeveloperKey } |
            ForEach-Object {
                @{
                    Name  = [string]$_.ProposedKey
                    Value = [string]$_.DeveloperText
                }
            }
    )

    $appliedNormal = Add-ReswEntries -ReswPath $normalResw -Entries $normalEntries
    $appliedDeveloper = Add-ReswEntries -ReswPath $developerResw -Entries $developerEntries
    Write-Info "Applied normal resx entries: $appliedNormal"
    Write-Info "Applied developer resx entries: $appliedDeveloper"
}

if ($WireXaml) {
    $wireTargets = @($candidates | Where-Object { $_.NeedsXamlWire })
    $wireTotal = $wireTargets.Count
    $wireSkipped = 0
    $wireDeclined = 0
    $wireFailed = 0
    $wireIndex = 0

    Write-Info "WireXaml is interactive: confirm each control with Y before editing."
    Write-Info "Answers: Y = wire this control, N = skip, Q = stop wiring."

    foreach ($item in $wireTargets) {
        $wireIndex++
        $preview = Get-XamlWirePreview -Candidate $item
        $decision = Read-WireConfirmation -Index $wireIndex -Total $wireTotal -Candidate $item -Preview $preview

        if ($decision -eq "quit") {
            Write-Info "Stopped wiring early at $wireIndex of $wireTotal."
            break
        }
        if ($decision -eq "skip") {
            $wireSkipped++
            continue
        }
        if ($decision -eq "no") {
            $wireDeclined++
            Write-Info "Skipped $($item.ControlType) '$($item.Name)' in $($item.File)."
            continue
        }

        if (Set-XamlTooltipBehavior -Candidate $item) {
            $wiredXaml++
            Write-Info "Wired $($item.ControlType) '$($item.Name)' in $($item.File)."
        }
        else {
            $wireFailed++
            Write-Info "Failed to wire $($item.ControlType) '$($item.Name)' in $($item.File)."
        }
    }

    Write-Info "Wired XAML controls: $wiredXaml"
    Write-Info "Declined by user: $wireDeclined"
    Write-Info "Auto-skipped (unnamed/unlocatable): $wireSkipped"
    Write-Info "Failed: $wireFailed"
}

Write-Info "Done. Re-run anytime after UI changes."
if (-not $ApplyMissing) {
    Write-Info "Tip: add -ApplyMissing to append missing resx keys."
}
if (-not $WireXaml) {
    Write-Info "Tip: add -WireXaml to attach TooltipBehavior one control at a time (Y/N/Q prompts)."
}

exit 0
