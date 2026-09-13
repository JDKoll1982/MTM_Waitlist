# Medium — Settings — Three different labels are bound to one resource key and all render the same text

| Field | Value |
| --- | --- |
| **Criticality** | Medium |
| **Feature area** | Settings → About / App Info (and the Settings section header) |
| **Type** | Localization key collision — the wrong text is shown to every user in every locale |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`**. **Closure:** the section header keeps `Settings_About`, the version card moved to `Settings_AppVersion_Title` and the about card to `Settings_AboutApp_Title`, each with its own resource entry — no two distinct labels resolve to one another's text. **Remainder owned by: none** (no other spec owns the Settings page's labels). **Proof:** `MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests` (every `x:Uid` in `SettingsPage.xaml` is unique and resolves to its own `<Uid>.Text` entry). |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — gives each element its own key and its own resource entry, and adds the duplicate-`x:Uid` / missing-resource check to the verification (§6 of this file). No other spec owns the Settings page's labels. Seed: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md` |
| **Files** | `Module_Settings/Views/SettingsPage.xaml` (lines 1175, 1215, 1233), `Strings/en-us/Resources.resw` (line 481) |

---

## 1. Summary

Three separate elements on the Settings page declare the **same** `x:Uid`:

```xml
<!-- SettingsPage.xaml:1173–1175 — the section header above the About area -->
<TextBlock Grid.Row="0" x:Uid="Settings_About" FontWeight="SemiBold"
           Style="{ThemeResource SubtitleTextBlockStyle}" />

<!-- SettingsPage.xaml:1213–1217 — the version card's title -->
<TextBlock x:Uid="Settings_About" Style="{ThemeResource BodyTextBlockStyle}" Text="App Version" />

<!-- SettingsPage.xaml:1231–1235 — the about card's title -->
<TextBlock x:Uid="Settings_About" Style="{ThemeResource BodyTextBlockStyle}" Text="About MTM Waitlist" />
```

and the resource file supplies one string for that key:

```xml
<!-- Strings/en-us/Resources.resw:481 -->
<data name="Settings_About.Text" xml:space="preserve">
  <value>About this application</value>
</data>
```

In WinUI, `x:Uid` makes the resource authoritative: `<Uid>.Text` overwrites each element's `Text`
property. All three therefore render **"About this application"**, so the page shows that phrase
three times and the two card titles never appear. The literal `Text` values in the XAML
("App Version", "About MTM Waitlist") are dead — they only show in a build with no `.pri`
resources, which is the case the author would have previewed.

## 2. Impact

- Users see a duplicated, meaningless label where two distinct card titles should be; the version
  card is indistinguishable from the about card by its heading.
- Because `x:Uid` is evaluated per locale, this is a *permanent* defect in every localization,
  including the default one — not a fallback glitch.
- The same collision makes the page's accessibility names wrong for a screen reader, which reads the
  same string three times.

## 3. Evidence

| Line | Element | Intended label |
| --- | --- | --- |
| `Module_Settings/Views/SettingsPage.xaml:1175` | Section header `TextBlock` | "About this application" (matches the resource) |
| `Module_Settings/Views/SettingsPage.xaml:1215` | `SettingsCard.Header` for the version row | "App Version" |
| `Module_Settings/Views/SettingsPage.xaml:1233` | `SettingsCard.Header` for the about row | "About MTM Waitlist" |
| `Strings/en-us/Resources.resw:481` | `Settings_About.Text` | "About this application" |
| `Strings/en-us/Resources.resw:484` | `Settings_AboutDescription.Text` | "MTM Waitlist helps teams track, prioritize, and complete material requests in real time." (the XAML fallback at line 1242 says something different: "Manage waitlists and local consumer records securely.") |

Also worth noting while in this area: the version value itself comes from
`SettingsViewModel.VersionDescription`, and the same file reads
`Package.Current.Id.Version` at `SettingsViewModel.cs:887` — a packaged-only API. On the unpackaged
build that call must be reached only behind a guard (the method is already wrapped in an
`if (RuntimeHelper.IsMSIX)` at line 885); confirm the non-packaged branch returns a sensible value
rather than an empty string when the labels are fixed.

## 4. Root cause

The `x:Uid` for the section header was copied onto the two cards below it during a layout edit and the
duplication was never caught, because a duplicate `x:Uid` is not a compile error and the values
happen to read plausibly.

## 5. Fix

1. Give each element its own key and add the matching resource entries:
   - section header → keep `Settings_About` (or rename to `Settings_AboutSection_Title`);
   - version card → `Settings_AppVersion_Title` = "App Version";
   - about card → `Settings_AboutApp_Title` = "About MTM Waitlist".
2. Add them to `Strings/en-us/Resources.resw` as `<Key>.Text` entries (the `.Text` suffix applies
   because the target is a `TextBlock.Text` property).
3. Reconcile the two About descriptions so the resource and the XAML fallback say the same thing.
4. Check the rest of the solution for the same mistake: every `x:Uid` inside a single file should be
   unique, and each `x:Uid` should correspond to a `<Uid>.Text` entry in the resource file. The other
   Uids in this file (`Settings_Theme_*`, `Settings_IgnoredLocations_*`, `Settings_CacheRefresh_*`,
   `SettingsPage_PrivacyTermsLink`) are each used once and are correct.
5. Watch the build trap: a real XAML error in this file surfaces only as `WMC9999 … Could not find any
   resources appropriate for the specified culture`, so validate the change by running the app and
   reading the labels, not by trusting a green build alone.

## 6. Verification

1. Grep the Settings page for duplicate `x:Uid` values:

   ```
   Select-String -Path Module_Settings\Views\SettingsPage.xaml -Pattern 'x:Uid="([^"]+)"' -AllMatches |
     ForEach-Object { $_.Matches.Value } | Group-Object | Where-Object Count -gt 1
   ```

   Expected: no output (today it returns `x:Uid="Settings_About"` three times).
2. Assert every `x:Uid` in the page resolves to a resource entry in
   `Strings/en-us/Resources.resw` (`<Uid>.Text`).
3. Manual: open Settings in the running app and confirm the section reads "About this application",
   the first card reads "App Version", and the second reads "About MTM Waitlist".
4. Build `0 Warning(s) 0 Error(s)`; full suite `Failed: 0`.

## 7. Related

- `defects/Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` — same card block.
- `.github/instructions/csharp-xaml-naming-rules.instructions.md` — naming rules for XAML/resource
  pairs.
- `FEATURES.md` §8.
