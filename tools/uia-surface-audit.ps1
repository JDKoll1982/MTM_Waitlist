# Audit the running MTM_Waitlist for interactive controls a person - or a screen reader, or this toolset's own
# name-based clicks - cannot identify.
#
# WHY THIS SHAPE
#   The other scripts drive the app by accessible Name. That works right up to the first control that has none, and
#   then the click reports 'no Button matched [...]' for a control that is plainly on screen (the wizard's Confirm
#   step was the first: its Submit button draws its caption as a child Text element and exposes no name of its own).
#   A review of "which controls are unnamed" therefore cannot be a review of buttons by name; it has to be a review
#   of what the controls can DO.
#
#   So a finding is decided by CAPABILITY, not by control type: any element that accepts Invoke, Toggle,
#   SelectionItem, Value, RangeValue or ExpandCollapse is something a person operates, and every one of those needs
#   a name. Text and image elements are not findings - they carry their content.
#
#   Each finding carries its ancestor chain, because a finding you cannot locate in the tree is a finding nobody
#   fixes: "Button '' in ShellPage>StackPanel>Grid" is actionable, "an unnamed button" is not.
#
# USAGE
#   pwsh -File tools/uia-surface-audit.ps1                          # audit the running app, print the findings
#   pwsh -File tools/uia-surface-audit.ps1 -All                     # list every interactive control, named or not
#   pwsh -File tools/uia-surface-audit.ps1 -Label 10-signin         # name the evidence file
#   pwsh -File tools/uia-surface-audit.ps1 -AnchorId WaitlistViewPage_CardList -StableMilliseconds 800
#   pwsh -File tools/uia-surface-audit.ps1 -FailOnFindings          # exit 1 when anything is unnamed (for a gate)
#   pwsh -File tools/uia-surface-audit.ps1 -Window 'Sign'           # restrict to windows whose title matches
#
# EXIT CODES
#   0  the audit ran; with -FailOnFindings, only when nothing was unnamed
#   1  -FailOnFindings was set and at least one interactive control has no accessible name

[CmdletBinding()]
param(
    # Names the evidence file. The report is always written; the label is what makes a walk legible afterwards.
    [string]$Label = 'surface-audit',

    [string]$EvidenceDir = "$env:TEMP\die-walk",

    # List every interactive control rather than only the unnamed ones.
    [switch]$All,

    # Exit 1 when anything interactive has no accessible name.
    [switch]$FailOnFindings,

    # Only audit windows whose title matches this pattern. The sign-in and machine-setup windows are separate
    # windows, so a walk that means to cover them says so.
    [string]$Window = '*',

    # Wait for this automation id to exist and for the page to settle before walking it. A page that loads
    # asynchronously - the waitlist draws its cards after its own read returns - is momentarily still while it is
    # empty, so an audit that walks immediately reports a surface that was probably about to appear.
    [string]$AnchorId = '',

    # How long the tree must stop changing before the walk reads it.
    [int]$StableMilliseconds = 0,

    # Ignored by default: the shell's own chrome and its scroll bars are not app surface, and neither is a
    # ScrollItem, which is what every list row offers in addition to its own pattern.
    [switch]$IncludeChrome
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\uia-common.ps1"

$Scope    = $UiaScope
$CT       = $UiaCT
$Walker   = [System.Windows.Automation.TreeWalker]::ControlViewWalker
$Any      = $UiaAny
$ValueP   = $UiaValue

# Text carries its own content; a scroll bar is not app surface; a ScrollItem is what a list row offers on top of
# the pattern that already made it a finding; a progress ring is read-only. Compared by NAME rather than by
# ControlType instance: the property returns a fresh instance every read, so an instance comparison silently never
# matches - which is how the first version of this list filtered nothing at all.
$s_ignoredTypes = @('Text', 'ScrollBar', 'Thumb', 'ScrollItem', 'Pane', 'Group', 'Custom', 'Image', 'ProgressBar')

# A name that only repeats the control type or the automation id identifies nothing: a screen reader announcing
# "Button" is the same failure as announcing nothing.
$s_genericNames = @('button', 'edit', 'text', 'combo box', 'combobox', 'check box', 'checkbox', 'list item',
    'menu item', 'tab item', 'tree item', 'slider', 'hyperlink', 'unknown', 'item')

function Get-AppWindows {
    if (-not (Get-MtmAppProcess)) { throw 'MTM_Waitlist is not running. Start it with tools/ui-drive.ps1 -Action launch.' }
    return @(Get-MtmAppWindows -TitlePattern $Window)
}

function Get-PatternNames {
    param($Element)

    $names = New-Object System.Collections.ArrayList
    foreach ($row in @(
            @('Invoke', [System.Windows.Automation.InvokePattern]::Pattern),
            @('Toggle', [System.Windows.Automation.TogglePattern]::Pattern),
            @('SelectionItem', [System.Windows.Automation.SelectionItemPattern]::Pattern),
            @('Value', $ValueP),
            @('RangeValue', [System.Windows.Automation.RangeValuePattern]::Pattern),
            @('ExpandCollapse', [System.Windows.Automation.ExpandCollapsePattern]::Pattern),
            @('ScrollItem', [System.Windows.Automation.ScrollItemPattern]::Pattern))) {

        $pat = $null
        $ok = $false
        try { $ok = $Element.TryGetCurrentPattern($row[1], [ref]$pat) } catch { $ok = $false }
        if ($ok) { [void]$names.Add($row[0]) }
    }

    return ($names -join '+')
}

# The chain of ancestors, which is what turns a finding back into a place in the XAML.
function Get-Ancestry {
    param($Element)

    $chain = New-Object System.Collections.ArrayList
    $node = $Element
    for ($i = 0; $i -lt 6; $i++) {
        try { $node = $Walker.GetParent($node) } catch { break }
        if (-not $node) { break }

        $type = $node.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')
        $id = $node.Current.AutomationId
        if ($id) { [void]$chain.Insert(0, "$type#$id") } else { [void]$chain.Insert(0, $type) }
    }

    return ($chain -join '>')
}

function Get-IsInteractive {
    param($Element, [string]$Patterns)

    if (-not $Patterns) { return $false }
    # A ScrollItem on its own is not interactive surface: it is what a list row offers beside its own pattern.
    if ($Patterns -eq 'ScrollItem') { return $false }

    if (-not $IncludeChrome) {
        $type = $Element.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')
        if ($s_ignoredTypes -contains $type) { return $false }
        if ($Element.Current.Name -in @('Minimize', 'Maximize', 'Restore', 'Close')) { return $false }
    }

    return $true
}

# The text the control itself draws. A button whose caption is a child TextBlock - which is how both of the unnamed
# buttons on the Settings page are built - has a name of '' and a caption of 'Add', and the caption is what says
# which control this is.
function Get-ContentText {
    param($Element)

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $CT::Text)

    $snippets = @($Element.FindAll($Scope::Descendants, $condition) |
        ForEach-Object { $_.Current.Name } |
        Where-Object { $_ } |
        Select-Object -First 3)

    return ($snippets -join ' ')
}

function Get-FindingKind {
    param($Element, [string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) { return 'unnamed' }
    if ($s_genericNames -contains $Name.Trim().ToLowerInvariant()) { return 'generic-name' }
    if ($Name -eq $Element.Current.AutomationId) { return 'name-is-the-id' }
    return ''
}

# --- walk ----------------------------------------------------------------------------------------------------

if ($AnchorId -or $StableMilliseconds -gt 0) {
    [void](Wait-MtmSettledAnchor -AnchorId $AnchorId -StableMilliseconds $StableMilliseconds)
}

$windowAudits = New-Object System.Collections.ArrayList
$findings = New-Object System.Collections.ArrayList
$interactive = New-Object System.Collections.ArrayList

foreach ($wnd in Get-AppWindows) {
    $rows = New-Object System.Collections.ArrayList
    $elements = @($wnd.FindAll($Scope::Descendants, $Any))

    foreach ($el in $elements) {
        $patterns = Get-PatternNames -Element $el
        if (-not (Get-IsInteractive -Element $el -Patterns $patterns)) { continue }

        $r = $el.Current.BoundingRectangle
        # A rectangle can be NaN (not measured yet) or infinite (a host that spans everything); both mean "no usable
        # rectangle", and neither may be treated as on-screen.
        $onScreen = -not ([double]::IsNaN($r.Width) -or [double]::IsInfinity($r.Width) -or
            [double]::IsNaN($r.Height) -or [double]::IsInfinity($r.Height))

        $row = [pscustomobject]@{
            Window       = $wnd.Current.Name
            ControlType  = $el.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')
            Name         = $el.Current.Name
            AutomationId = $el.Current.AutomationId
            Patterns     = $patterns
            Enabled      = $el.Current.IsEnabled
            Offscreen    = -not $onScreen
            Ancestry     = Get-Ancestry -Element $el
        }

        [void]$rows.Add($row)
        [void]$interactive.Add($row)

        $kind = Get-FindingKind -Element $el -Name $row.Name
        if ($kind) {
            $row | Add-Member -NotePropertyName Kind -NotePropertyValue $kind
            $row | Add-Member -NotePropertyName Caption -NotePropertyValue (Get-ContentText -Element $el)
            [void]$findings.Add($row)
        }
    }

    [void]$windowAudits.Add([pscustomobject]@{
        Window = $wnd.Current.Name
        Total  = $rows.Count
        Rows   = $rows
    })
}

# --- report --------------------------------------------------------------------------------------------------

$byType = $interactive | Group-Object ControlType | Sort-Object Count -Descending |
    ForEach-Object { '{0}={1}' -f $_.Name, $_.Count }

$lines = @(
    "# MTM_Waitlist surface audit - $Label"
    "when        : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "windows     : $(($windowAudits | ForEach-Object { "$($_.Window) ($($_.Total))" }) -join ' | ')"
    "interactive : $($interactive.Count)   by type: $($byType -join ' ')"
    "findings    : $($findings.Count)"
    ''
)

if ($findings.Count -gt 0) {
    $lines += "## Findings (an interactive control a person cannot identify)"
    foreach ($f in $findings) {
        $lines += ('{0,-14} {1,-12} id="{2}" caption="{3}" patterns={4} {5}  <-- {6}' -f
            $f.Kind, $f.ControlType, $f.AutomationId, $f.Caption, $f.Patterns, $f.Ancestry, $f.Window)
    }
    $lines += ''
}

if ($All) {
    $lines += "## Every interactive control"
    foreach ($w in $windowAudits) {
        $lines += "### $($w.Window)"
        foreach ($r in $w.Rows) {
            $lines += ('{0,-12} named={1,-5} enabled={2,-5} offscreen={3,-5} id="{4}" patterns={5} name="{6}"  {7}' -f
                $r.ControlType, (-not [string]::IsNullOrWhiteSpace($r.Name)), $r.Enabled, $r.Offscreen,
                $r.AutomationId, $r.Patterns, $r.Name, $r.Ancestry)
        }
        $lines += ''
    }
}

New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
$path = Join-Path $EvidenceDir ("{0}.txt" -f $Label)
$lines | Set-Content -Path $path -Encoding utf8

# One JSON file per walk, so a later walk can be diffed against this one instead of against memory.
$jsonPath = Join-Path $EvidenceDir ("{0}.json" -f $Label)
@{
    label       = $Label
    when        = (Get-Date).ToString('s')
    interactive = $interactive.Count
    findings    = $findings.Count
    rows        = $interactive
} | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding utf8

Write-Host "evidence  : $path" -ForegroundColor DarkGray
Write-Host "json      : $jsonPath" -ForegroundColor DarkGray
Write-Host "windows   : $(($windowAudits | ForEach-Object { "$($_.Window) ($($_.Total))" }) -join ' | ')" -ForegroundColor Cyan
Write-Host "interactive: $($interactive.Count)   $($byType -join '  ')" -ForegroundColor Cyan
Write-Host "findings  : $($findings.Count)" -ForegroundColor $(if ($findings.Count) { 'Yellow' } else { 'Green' })

foreach ($f in $findings) {
    Write-Host ('  {0,-14} {1,-12} id="{2}" caption="{3}"  {4}' -f $f.Kind, $f.ControlType, $f.AutomationId, $f.Caption, $f.Ancestry) `
        -ForegroundColor Yellow
}

if ($FailOnFindings -and $findings.Count -gt 0) { exit 1 }
exit 0
