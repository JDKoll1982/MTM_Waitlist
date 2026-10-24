---
description: "Converts an existing WinUI 3 app (MTM_Waitlist, MTM_Receiving_Application) to the Lab design system: spacing scale, Windows type ramp, capped content column, adaptive breakpoints, theme brushes. Use when restyling or modernising an existing WinUI 3 codebase rather than writing a new page."
---

# Adopting the design system in an existing WinUI 3 app

Written against two real codebases:

- `C:\Users\johnk\Source\repos\MTM_Waitlist` — 65 XAML files, WinUIEx `WindowEx` + `MicaBackdrop`,
  content swapped through a `ContentPresenter` stage host, custom `Styles\*.xaml` dictionaries.
- `C:\Users\johnk\Source\repos\MTM_Receiving_Application` — 128 XAML files, single `Window` shell,
  `Module_Core\Themes\MaterialIcons.xaml` + `ModuleAccentBrushes.xaml`, several hand-templated buttons.

To use this file in one of those repos, copy it to `<repo>\.github\instructions\` and add
`applyTo: "**/*.xaml"` to the frontmatter.

## Measure before you change anything

The counts below are what makes this conversion tractable — they turn "make it consistent" into a
finite list. Re-run these in the target repo first:

```powershell
$xaml = Get-ChildItem . -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
"files:  $($xaml.Count)"
"FontSize=: " + ($xaml | Select-String -Pattern 'FontSize="' -AllMatches | Measure-Object).Count
"Margin=:   " + ($xaml | Select-String -Pattern 'Margin="' -AllMatches | Measure-Object).Count
"Width=:    " + ($xaml | Select-String -Pattern 'Width="\d' -AllMatches | Measure-Object).Count
```

Baseline at the time of writing: MTM_Waitlist 77 raw `FontSize`, 119 `Margin`; MTM_Receiving_Application
241 raw `FontSize`, 235 `Margin`. Those two numbers are the entire work surface. Nothing else in the
conversion is as large.

## Do these in order — each step is independently shippable

### 1. Replace the type ramp

MTM_Waitlist currently defines its own:

```xml
<!-- Styles\FontSizes.xaml — delete after migrating -->
<x:Double x:Key="LargeFontSize">24</x:Double>
<x:Double x:Key="MediumFontSize">16</x:Double>
```

```xml
<!-- Styles\TextBlock.xaml — PageTitleStyle -->
<Setter Property="FontWeight" Value="SemiLight" />
<Setter Property="FontSize" Value="24" />
```

Both are off the Windows ramp. 24 is not a ramp size; `SemiLight` is not a ramp weight.

| Replace | With | Why |
| --- | --- | --- |
| `PageTitleStyle` (24 / SemiLight) | `TitleTextBlockStyle` (28 / 36 semibold) | Ramp weight and size; `SemiLight` reads thin at 180% scaling |
| `BodyTextStyle` (16) | `BodyTextBlockStyle` (14 / 20) | 16 is not on the ramp; 14/20 is the Windows body |

Then delete `FontSizes.xaml` and the styles nothing references, and remove the `MergedDictionaries`
entries for them — a dangling `x:Key` reference is a `WMC0001` build failure, not a silent miss.

Same treatment for MTM_Receiving_Application's 241 inline `FontSize` values, and for
`ShellNavItemFontSize = 18` in `Module_Core\Views\ShellPage.xaml`: `NavigationView` already sizes its
own items. An 18 epx override makes the pane text larger than every page heading below it.

Do **not** use Bold or Italic for emphasis anywhere you touch — the ramp deliberately omits both. Use
`BodyStrongTextBlockStyle` or `FontWeight="SemiBold"`.

### 2. Replace margin dictionaries with spacing

MTM_Waitlist's `Styles\Thickness.xaml` is 18 margin resources plus four navigation-layout rectangles:

```xml
<Thickness x:Key="MediumTopMargin">0,24,0,0</Thickness>
<Thickness x:Key="NavigationViewPageContentMargin">56,24,56,0</Thickness>
```

Margins between siblings do not collapse, so the first and last child get a half gap and the page
edges never align. Convert:

- `Margin="0,24,0,0"` between two sections → `Spacing="24"` on the parent `StackPanel`, or
  `Grid.RowSpacing="24"`.
- `Margin="0,0,0,12"` on a card in a list → `Spacing="12"` on the parent.
- `Padding` stays for the inside of a surface; `Margin` is only for offsetting a single element.

`NavigationViewPageContentMargin` (56,24,56,0) is the clearest win: 56 uses half the width of a
1280 epx window on gutters. Replace the whole pattern with a capped column:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled"
              HorizontalScrollMode="Disabled"
              MaxWidth="1000">
    <StackPanel Padding="32,28,32,48" Spacing="24">
        ...
    </StackPanel>
</ScrollViewer>
```

`MaxWidth` belongs on the `ScrollViewer`, not the inner panel, and the scroller needs horizontal
scroll **disabled** — otherwise content measures at its natural width and silently overflows the right
edge on a 1280 epx screen.

Only these gaps are allowed: **4, 8, 12, 16, 24, 32, 40, 48.** If a margin value is 6, 10, 14, 18 or
20, it rounds to the nearest of those.

### 3. Remove hardcoded colours

`Module_Core\Views\ShellPage.xaml` contains `Color="#FFF7630C"` and
`ShellNavItemFontSize = 18` alongside it; MTM_Receiving_Application has `ModuleAccentBrushes.xaml`.
Brand colours are fine as a *resource*, but a literal in a page breaks light/dark and high contrast.

- A brand colour becomes one `SolidColorBrush` in `Themes\`, referenced with `{ThemeResource ...}`, and
  it gets a light-mode and a dark-mode variant.
- Everything else uses the semantic brushes: `TextFillColorSecondaryBrush` for secondary text,
  `CardBackgroundFillColorDefaultBrush` / `CardStrokeColorDefaultBrush` for surfaces,
  `DividerStrokeColorDefaultBrush` for separators, `SystemFillColorCautionBrush` for warnings.
- Accent is for state and one primary action per surface — not for text runs or card fills.

### 4. Fix fixed widths in templates

`Width="120"`, `Width="60"`, `Width="28"` and `Width="120"` inside the header-step template in
`ShellPage.xaml` are in effective pixels, so at 180% they occupy proportionally more of the window
than they did at 100% on the development monitor. Either:

- give the row `ColumnDefinitions` and let the steps divide the available width, or
- keep the fixed size but wrap the strip in a `ScrollViewer` with horizontal scrolling **enabled**,
  so a narrow window scrolls instead of clipping.

Never let a fixed-width template be the reason a window cannot shrink below 1200 epx.

### 5. Adopt the breakpoints

Set them explicitly rather than relying on defaults, so every page behaves the same:

```xml
<NavigationView OpenPaneLength="272"
                CompactModeThresholdWidth="640"
                ExpandedModeThresholdWidth="1008">
```

- **≥ 1008** — pane expanded.
- **641–1007** — pane compact (48 epx icon rail).
- **< 640** — pane overlay.

Two-pane detail layouts need an `AdaptiveTrigger` at 1008 that collapses the detail column and reveals
a back affordance — see `Demos/Layout/SplitViewDemo` and the master/detail pattern in the lab.

### 6. Introduce the card surface once

Add one style per repo and use it everywhere instead of re-declaring border/padding per page:

```xml
<Style x:Key="CardBorder" TargetType="Border">
    <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
    <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
</Style>
```

Rules: one surface level only (no card in a card — use a divider instead), radius 8 for cards and 4 for
buttons/inputs, and 16 epx inside every card.

### 7. Settings pages

If the repo hand-builds setting rows, move them to `SettingsCard` / `SettingsExpander`
(`CommunityToolkit.WinUI.Controls.SettingsControls`, already referenced by MTM_Waitlist): icon → header →
description, control trailing, 16 epx between text and control. A settings row is not a card inside a
card; the row is the surface.

## What to leave alone

A restyle is not a rewrite. Do **not** touch, in MTM_Waitlist:

- `Module_Shared\Services\TooltipBehavior` and `TooltipCoverageTests` — the tooltip pipeline is a
  deliberate design with a developer/localization split.
- The `ContentPresenter` stage host in `MainWindow.xaml` and the launch→shell hand-over contract.
- MVVM structure, navigation services, the logging diagnostics context.
- `Directory.Build.props` (versioning, `UseRidGraph`, the Pkcs pin) — those exist for reasons recorded
  in the file.

## Definition of done

- `FontSize` occurrences are only inside theme dictionaries, never in a page.
- `Margin` between siblings is gone; `Spacing` / `RowSpacing` / `ColumnSpacing` used instead.
- Zero literal `#` colours in page XAML.
- Every page renders correctly at **1067 epx** (16-inch laptop at 180%) and **1280 epx** (150%).
- Shrinking the window to 900 epx produces no horizontal scrollbar and no clipped text.
- Light and dark both check out with `{ThemeResource}` only.

Verify by resizing to those widths and by toggling the theme — not by reading the diff.
