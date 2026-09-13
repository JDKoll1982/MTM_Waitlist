# Contract — Surface presentation

**Feature**: `002-truthful-data-and-controls` | **Spec**: [../spec.md](../spec.md) | **Model**: [../data-model.md](../data-model.md)

Every clause is testable. "MUST NOT render" means the value or control is not in the visual tree and no
substitute appears in its place (spec FR-002).

---

## C1 — Waitlist card (`Module_Waitlist/Controls/WaitlistLineCardView.xaml` and the six per-type views)

- **MUST NOT** render a material attribute value. The per-type views stop populating `Fields[0..4]` with
  fabricated content, and `WaitlistViewViewModel.AddRequestFields` holds no literal for any attribute in
  `data-model.md` §2.
- **MUST NOT** render a control that cannot act: the Cancel and Accept buttons are removed from
  `WaitlistLineCardView.xaml` (lines ~137–181 today). The action area shows the request's real lifecycle
  status, or nothing.
- **MAY** render: type/subtype title, the request's real status, remaining time / overdue — all
  substantiated today.
- A card left with no attribute rows still renders (its title, status and time are real); **it is not
  hidden**, because the card is the list item, not an optional block.
- Attribute rows use `Visibility` through the globally registered `BoolToVisibilityConverter`
  (`App.xaml` line 21) — no element inside a list template is deferred with `x:Load` (research R2).

## C2 — Request detail (`Module_Waitlist/Views/WaitlistViewDetailPage.xaml`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`)

- Rows render only when the value is sourced. `FieldValue(...)` loses its `string fallback = "Not available"`
  parameter, so no default literal can re-enter through the helper.
- The invented content is gone: `"Tipping strategy"`, `"Allowed categories"`, and the `Pending*` status
  strings.
- Section behaviour follows the block-state model: no rows with a successful empty read ⇒ the section is
  hidden (`x:Load` false, `x:Name` present); a read that throws or reports an error ⇒ the section renders the
  error text plus a retry action **in place**, with the rest of the page unaffected.
- Cached-fallback values keep rendering, and the read-only indicator keeps reporting the cached state and
  age (`specs/001` behaviour is untouched — FR-006).
- The page-level store-outage state is not reused for a section failure (research R1).

## C3 — New Request confirm (`Module_Waitlist/Views/NewRequestSummaryPage.xaml`,
`MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs`)

- No active-request count, no estimated wait time, and no stand-in card where they were.
- The coil details card is unchanged: it is already substantiated from the work centre's current job.
- Average coil weight: the constant-keyed lookup (`CoilReceivingPartId`) and the `"5,000 lb"` fallback are
  removed; the field renders no value until the per-coil resolution lands (template 03).
- New Request Alerts control: `IsEnabled` is bound to the capability the view model exposes
  (`IsNewRequestAlertsAvailable`), and a localized message replaces the static "packaged app" sentence when
  the capability is false. No preference value is written while the capability is false.

## C4 — Settings (`Module_Settings/Views/SettingsPage.xaml`, `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`)

- Three distinct labels, three keys: section header (own text), version card
  (`Settings_AppVersion_Title` → "App Version"), about card (`Settings_AboutApp_Title` → "About MTM
  Waitlist"). No `x:Uid` appears twice in the file. The About description reads the same in the resource
  file and in the XAML fallback.
- No privacy hyperlink and no privacy resource values ship.
- Every property whose getter calls `MatchesSearch(...)` is a member of the declared registration list, and
  `RefreshSearchVisibility()` raises `PropertyChanged` for every member — including
  `IsNewRequestAlertsPanelVisible`, `IsUrgencyAllotmentsPanelVisible`,
  `IsImageLocationSettingsPanelVisible` (FR-019/FR-020/FR-021).
- The version value on the unpackaged build returns a sensible string rather than empty (the packaged-only
  API call stays behind its guard).

## C5 — Cross-cutting binding and resource contract

- New and corrected text is resource-backed; new dialog and notification text resolves through the
  fallback form of `GetLocalized()` because the key is returned verbatim when the PRI is stale.
- Keys removed in this change: `Setup_Review.Status.MockSaved`, `SettingsPage_PrivacyTermsLink.Content`,
  `SettingsPage_PrivacyTermsLink.NavigateUri`, and `AppNotificationSamplePayload` when it has no consumer.
  A key another surface still reads is never removed.
- `[RelayCommand]` on a method named `XxxAsync` generates `XxxCommand` (the trailing `Async` is stripped):
  the XAML binds the stripped name.
- Every element carrying `x:Load` carries `x:Name` (compiler requirement), and each one-way `x:Load`
  depends on a property whose `PropertyChanged` is actually raised.
- No hardcoded pixel sizing on layout-critical containers introduced by this change; no converter added
  unless required, and any converter added is registered in `App.xaml` in the same change.
