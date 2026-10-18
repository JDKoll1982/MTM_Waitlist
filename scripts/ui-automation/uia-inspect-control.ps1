# Hover a control and fire the inspector's Ctrl+Alt+Right-Click at it, with real input.
#
# WHY THIS EXISTS
#   The inspector's shortcut is armed by a hover and by the tooltip being open, so it cannot be exercised through the
#   automation tree alone: reading a control's rectangle is not pointing at it, and no automation pattern sends the
#   modifier-and-right-click that the developer shortcut listens for. This drives the real pointer and the real
#   keyboard, which is the only way to test what the person actually does.
#
#   It exists for one question in particular: does the inspector open when the hovered control is inside a MODAL
#   DIALOG? That is the case that made it a window instead of a page, and the case a page can never satisfy.
#
# USAGE
#   # Hover a control and report whether a tooltip appeared
#   pwsh -File scripts/ui-automation/uia-inspect-control.ps1 -AutomationId 'WaitlistLineCardView_AcceptButton'
#
#   # Hover it and fire the shortcut, then read the inspector's window
#   pwsh -File scripts/ui-automation/uia-inspect-control.ps1 -AutomationId 'WaitlistLineCardView_AcceptButton' -Shortcut
#
#   # By accessible name, e.g. a control inside the request-item-images dialog
#   pwsh -File scripts/ui-automation/uia-inspect-control.ps1 -Name 'Search image rows' -Shortcut
#
# EXIT CODES
#   0  the sequence ran; with -Shortcut, the inspector's window appeared
#   1  -Shortcut was asked for and no inspector window appeared
#   2  the control to hover could not be found

[CmdletBinding()]
param(
    # The control to hover, by automation id or by accessible name. One of the two is required.
    [string]$AutomationId = '',
    [string]$Name = '',

    # Fire Ctrl+Alt+Right-Click after the hover, and look for the inspector's window.
    [switch]$Shortcut,

    # How long to wait for the tooltip to open before giving up on the hover.
    [int]$TooltipSeconds = 8,

    # How long to wait for the inspector's window after the shortcut.
    [int]$WindowSeconds = 10,

    # Leave the inspector's window open when the script ends.
    [switch]$KeepOpen,

    [string]$EvidenceDir = "$env:TEMP\die-walk"
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\uia-common.ps1"

if ([string]::IsNullOrWhiteSpace($AutomationId) -and [string]::IsNullOrWhiteSpace($Name)) {
    throw 'Pass -AutomationId or -Name: something has to be hovered.'
}

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class MtmInput
{
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);

    public static void Activate(IntPtr hwnd)
    {
        // Restoring a maximized window would resize it, and the control rectangles read before this call would then
        // be stale - the pointer would land somewhere the control is not. Only a minimised window is restored.
        if (IsIconic(hwnd)) { ShowWindow(hwnd, 9); }

        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
    }

    public const uint LeftDown = 0x0002;
    public const uint LeftUp = 0x0004;
    public const uint RightDown = 0x0008;
    public const uint RightUp = 0x0010;
    public const uint Move = 0x0001;
    public const uint KeyUp = 0x0002;

    public const byte VkControl = 0x11;
    public const byte VkMenu = 0x12;

    public static void MoveTo(int x, int y) => SetCursorPos(x, y);

    // Moving the cursor with SetCursorPos moves the pointer but sends no mouse message, and a WinUI control only
    // learns it is being hovered from a mouse-move message. Without this nudge the pointer sits over the control and
    // the application behaves as though it were somewhere else - no hover, no tooltip, and no way to arm the shortcut.
    public static void Nudge()
    {
        mouse_event(Move, 1u, 0u, 0u, UIntPtr.Zero);
        mouse_event(Move, unchecked((uint)-1), 0u, 0u, UIntPtr.Zero);
    }
    public static void RightClick() { mouse_event(RightDown, 0, 0, 0, UIntPtr.Zero); mouse_event(RightUp, 0, 0, 0, UIntPtr.Zero); }
    public static void CtrlAltDown() { keybd_event(VkControl, 0, 0, UIntPtr.Zero); keybd_event(VkMenu, 0, 0, UIntPtr.Zero); }
    public static void CtrlAltUp() { keybd_event(VkMenu, 0, KeyUp, UIntPtr.Zero); keybd_event(VkControl, 0, KeyUp, UIntPtr.Zero); }
}
'@

function Find-Target {
    foreach ($window in Get-MtmAppWindows) {
        $element = if ($AutomationId) {
            Find-MtmById -Root $window -Id $AutomationId
        }
        else {
            $condition = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, $Name)
            $window.FindFirst($UiaScope::Descendants, $condition)
        }

        if ($element) { return $element }
    }

    return $null
}

# The tooltip the control shows, if one is open. Both the platform's and the custom one are ToolTip controls, so
# this answers "is a tooltip showing" without caring which implementation drew it - and returns the element, so the
# text that is reported is the TOOLTIP's text rather than every string on the page.
function Get-OpenTooltip {
    param($Window)

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ToolTip)

    foreach ($candidate in @($window.FindAll($UiaScope::Descendants, $condition))) {
        if ($candidate.Current.IsOffscreen -eq $false) { return $candidate }
    }

    return $null
}

function Get-InspectorWindow {
    foreach ($window in Get-MtmAppWindows) {
        if ($window.Current.Name -like 'Control details*') { return $window }
    }

    return $null
}

function Get-TopLevelWindow {
    param($Element)

    $node = $Element
    $window = $null
    for ($i = 0; $i -lt 40 -and $node; $i++) {
        if ($node.Current.ControlType -eq [System.Windows.Automation.ControlType]::Window) { $window = $node }
        try { $node = $walker.GetParent($node) } catch { $node = $null }
    }

    return $window
}

# --- hover ----------------------------------------------------------------------------------------------------

$target = Find-Target
if (-not $target) {
    $what = if ($AutomationId) { "automation id '$AutomationId'" } else { "name '$Name'" }
    throw "No control with $what is on screen. Nothing to hover."
}

$rect = $target.Current.BoundingRectangle
if ([double]::IsNaN($rect.Width) -or [double]::IsInfinity($rect.Width)) {
    throw "The control reports no rectangle, so the pointer cannot be aimed at it."
}

$centreX = [int]($rect.X + ($rect.Width / 2))
$centreY = [int]($rect.Y + ($rect.Height / 2))
$controlName = if ($target.Current.Name) { $target.Current.Name } else { '(unnamed)' }

# A control the automation tree still lists can sit outside the window it belongs to - a list that has been rebuilt,
# a row that has scrolled away - and aiming the pointer at it puts the pointer on whatever IS there. That is a
# silent miss: the hover arms nothing, the tooltip that opens belongs to another control, and the shortcut looks
# broken. So the aim is checked against the window before it is taken.
$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
$node = $target
$ownerWindow = $null
while ($node) {
    $parent = $null
    try { $parent = $walker.GetParent($node) } catch { $parent = $null }
    if (-not $parent) { $ownerWindow = $node; break }
    $node = $parent
}

if ($ownerWindow) {
    $frame = $ownerWindow.Current.BoundingRectangle
    $insideX = $centreX -ge $frame.X -and $centreX -le ($frame.X + $frame.Width)
    $insideY = $centreY -ge $frame.Y -and $centreY -le ($frame.Y + $frame.Height)

    if (-not $insideX -or -not $insideY) {
        $frameText = '{0},{1} {2}x{3}' -f ([int]$frame.X), ([int]$frame.Y), ([int]$frame.Width), ([int]$frame.Height)
        $message = "The control's centre ($centreX,$centreY) lies outside its window ($frameText), so the pointer " +
            'would land on something else. Bring the control into view - scroll it into the window - and run this again.'
        throw $message
    }
}

Write-Host "target    : $($target.Current.ControlType.ProgrammaticName.Replace('ControlType.', '')) '$controlName' at $centreX,$centreY" -ForegroundColor Cyan

# The pointer can only hover what is actually in front. A window that is behind another one still reports valid
# rectangles, so every hover lands silently on whatever is on top and no tooltip ever opens - which looks exactly
# like a broken inspector. Bringing the app forward first removes that whole class of false failure.
$appWindow = Get-TopLevelWindow -Element $target
$appHandle = if ($appWindow) { [IntPtr]$appWindow.Current.NativeWindowHandle } else { [IntPtr]::Zero }

if ($appHandle -ne [IntPtr]::Zero) {
    [MtmInput]::Activate($appHandle)
    Start-Sleep -Milliseconds 400

    if ([MtmInput]::GetForegroundWindow() -eq $appHandle) {
        Write-Host 'foreground: the application is in front' -ForegroundColor Cyan
    }
    else {
        Write-Host 'foreground: another window is in front of the application, so the hover may miss' -ForegroundColor Yellow
    }
}

# Aiming, verified rather than assumed.
#
# A rectangle the automation tree reports is not a promise that the control is drawn under that point - a control can
# be clipped, scrolled out of view, or laid out past the edge of the window - and the pointer is therefore walked
# across the control's own rectangle. The point is accepted when a tooltip opens there, because a tooltip opening is
# the application itself saying the pointer is over the control.
#
# The hit test cannot be used to check the aim: WinUI reports its whole content as a single anonymous pane, so asking
# what is at a point answers "a pane" whether the point is over a button or over empty space. And the pointer has to
# be nudged after each move, or the control never receives a mouse-move message and never notices the hover.
function Test-TooltipHere {
    foreach ($window in Get-MtmAppWindows) {
        if (Get-OpenTooltip -Window $window) { return $true }
    }

    return $false
}

function Get-AimPoint {
    foreach ($fractionY in @(0.5, 0.35, 0.65, 0.2, 0.8)) {
        foreach ($fractionX in @(0.5, 0.3, 0.7, 0.15, 0.85)) {
            $x = [int]($rect.X + ($rect.Width * $fractionX))
            $y = [int]($rect.Y + ($rect.Height * $fractionY))

            [MtmInput]::MoveTo($x, $y)
            [MtmInput]::Nudge()
            Start-Sleep -Milliseconds 250

            if (Test-TooltipHere) {
                return [pscustomobject]@{ X = $x; Y = $y; How = 'a tooltip opened here' }
            }
        }
    }

    return $null
}

$aim = Get-AimPoint

if (-not $aim) {
    # No tooltip anywhere in the rectangle. The centre is still the most likely spot - a control with no tooltip of
    # its own opens none - and the wait below will report what happened. Refusing to try is worse than trying.
    $aim = [pscustomobject]@{ X = $centreX; Y = $centreY; How = 'the centre (no tooltip opened anywhere in the rectangle)' }
}

Write-Host "aim       : $($aim.X),$($aim.Y) over $($aim.How)" -ForegroundColor Cyan

$tooltip = $null
$deadline = (Get-Date).AddSeconds($TooltipSeconds)
while ((Get-Date) -lt $deadline) {
    foreach ($window in Get-MtmAppWindows) {
        $tooltip = Get-OpenTooltip -Window $window
        if ($tooltip) { break }
    }
    if ($tooltip) { break }
    Start-Sleep -Milliseconds 120
}

$tooltipShowing = $null -ne $tooltip
Write-Host "tooltip   : $(if ($tooltipShowing) { 'open' } else { 'did NOT open' })" -ForegroundColor $(if ($tooltipShowing) { 'Green' } else { 'Yellow' })

if ($tooltipShowing) {
    $texts = @($tooltip.FindAll($UiaScope::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $UiaCT::Text))) |
        ForEach-Object { $_.Current.Name } | Where-Object { $_ })
    Write-Host "tooltip text: $($texts -join ' / ')" -ForegroundColor Gray
}

if (-not $Shortcut) {
    New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
    "hover-only: '$controlName' tooltip=$(if ($tooltipShowing) { 'open' } else { 'none' })" |
        Set-Content (Join-Path $EvidenceDir 'inspect-hover.txt') -Encoding utf8
    exit 0
}

# --- the shortcut ---------------------------------------------------------------------------------------------

Write-Host 'shortcut  : holding Ctrl+Alt and right-clicking' -ForegroundColor Cyan
[MtmInput]::CtrlAltDown()
Start-Sleep -Milliseconds 80
[MtmInput]::RightClick()
Start-Sleep -Milliseconds 80
[MtmInput]::CtrlAltUp()

$inspector = $null
$deadline = (Get-Date).AddSeconds($WindowSeconds)
while ((Get-Date) -lt $deadline) {
    $inspector = Get-InspectorWindow
    if ($inspector) { break }
    Start-Sleep -Milliseconds 120
}

New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
$report = Join-Path $EvidenceDir 'inspect-shortcut.txt'

if (-not $inspector) {
    "shortcut: no inspector window appeared for '$controlName'" | Set-Content $report -Encoding utf8
    Write-Host 'inspector : DID NOT OPEN' -ForegroundColor Red
    exit 1
}

$lines = @(
    "# Control inspector - opened over '$controlName'"
    "when   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "window : '$($inspector.Current.Name)'"
    ''
    '## Texts'
)
$lines += @($inspector.FindAll($UiaScope::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $UiaCT::Text))) |
    ForEach-Object { $_.Current.Name } | Where-Object { $_ })

$lines | Set-Content $report -Encoding utf8

Write-Host "inspector : OPEN - '$($inspector.Current.Name)'" -ForegroundColor Green
Write-Host "evidence  : $report" -ForegroundColor DarkGray
$lines | Select-Object -Skip 4 | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

exit 0
