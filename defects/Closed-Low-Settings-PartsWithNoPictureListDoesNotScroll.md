# Low — Settings — the parts-with-no-picture list does not scroll

| Field | Value |
| --- | --- |
| **Criticality** | Low — no data is wrong and nothing is lost; the list simply runs off the bottom of the window with no way to reach the rest of it |
| **Feature area** | Settings → Operations → Part Pictures — the "Parts with no picture" list and the change record beside it |
| **Type** | Layout defect: a list measured inside a vertical `StackPanel`, which hands it unlimited height |
| **Found** | 2026-09-24, by the owner using the built app at `0b78b2c`, with 192 parts listed |
| **Status** | **FIXED** in the working tree on top of `0b78b2c`. Not visually re-verified in the running app — the wheel itself is UI-only behaviour, see §5. |
| **Fixed by** | Both columns are now `Grid`s with the list in a `*` row, and each list asks for its own vertical scroll bar |
| **Files** | `Module_Settings/Views/PartPictureManagerPage.xaml` |

---

## 1. Reported symptom

> "this settings list should have a scroll wheel"

The screen showed **192 parts have no picture**, and the list of them ran from the top of the lower half of the window
straight past the bottom edge. There was no scroll bar and the wheel did nothing: the rows below the window's bottom
edge were unreachable, so most of the 192 could not be read at all.

## 2. Root cause

The page's root `Grid` already gives the last row `Height="*"`, so the region for the two lists **is** bounded:

```xml
<RowDefinition Height="Auto" />
<RowDefinition Height="Auto" />
<RowDefinition Height="Auto" />
<RowDefinition Height="Auto" />
<RowDefinition Height="*" />
```

Both columns inside that row were vertical `StackPanel`s. A vertical `StackPanel` is the trap: it measures every child
with **unlimited height** and then grows to whatever the child asks for, so the `ListView` inside reported its full
content height (all 192 rows) and the `StackPanel` grew to match. The list was never the thing that overflowed, so its
own scroll viewer never engaged.

Nothing else scrolled either. The shell puts page content straight into `NavigationFrame` — a `Frame` in a `Grid` row
(`Module_Core/Views/ShellPage.xaml:688`) — so there is no outer scroll viewer to catch an over-long page. `SettingsPage`
has one of its own (`<ScrollViewer VerticalScrollBarVisibility="Auto">`, line 46), which is why lists that grow
unbounded on **that** page are fine; this screen does not.

## 3. Impact

- On 2026-09-24 the screen listed 192 parts that still have no picture; only the first ~19 were reachable by any means.
- The **Export list** button still exported the whole list, so the data was reachable by exporting it — which is what
  made the screen look like it worked while the list itself was unusable.
- The change record beside it had the same fault and would have hit it as soon as enough changes accumulated.

## 4. Fix

Each column is now a `Grid` that keeps its headings in `Auto` rows and puts the list in a `*` row, so the list is
bounded by the space left in the page and scrolls its own content:

```xml
<Grid Grid.Column="1" RowSpacing="8">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />   <!-- heading + Export list -->
        <RowDefinition Height="Auto" />   <!-- count -->
        <RowDefinition Height="Auto" />   <!-- coverage note -->
        <RowDefinition Height="Auto" />   <!-- empty-state text -->
        <RowDefinition Height="*" />      <!-- the list -->
    </Grid.RowDefinitions>
```

Both lists also declare `ScrollViewer.VerticalScrollBarVisibility="Auto"` explicitly, the way the wizard's card list
does, so the scroll bar is asked for rather than inherited from the default.

This matches the surfaces that were already right: `SetupPartSelectionPage` and `SetupSequenceSelectionPage` put their
`ListView` directly in a `*` row. `SettingsPage`'s computers list instead uses `MaxHeight="320"`, which is correct
there because that page scrolls as a whole.

## 5. Evidence

| Item | Evidence |
| --- | --- |
| Guard added | `MTM_Waitlist.Tests/Module_Settings/PartPictureManagerPageMarkupTests.PartPictureLists_ScrollTheirOwnContent_InsteadOfGrowingPastTheWindow` — fails if either list has a vertical `StackPanel` in its ancestor chain, or does not ask for a vertical scroll bar |
| **Fails before the fix** | The test was run against the reverted markup (`git stash push` of the page): **1 failed, 0 passed** — *"PartPictureManagerPage_ChangeRecord is measured inside a vertical StackPanel, which hands it unlimited height…"* |
| Passes after the fix | Same test: **1 passed, 0 failed** |
| Build | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` — succeeded, no XAML errors |
| Suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — **1552 total, 0 failed, 1497 passed, 55 skipped** (environment-gated) |
| **Not verified** | The wheel itself, in the running app. The suite deliberately does not cover what renders (test conventions, "UI-only behaviour"), and no UI-automation pass was run |
