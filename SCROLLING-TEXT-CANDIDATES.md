# One-line scrolling text — candidate list for review

**What this is:** the shared control `MTM_Waitlist.Module_Shared.Controls.AutoScrollTextView` draws text on **one
line** and scrolls it when it does not fit (or truncates it when Windows animations are off). It is already used for
the card title inside `Styles/PartPictureCardTemplate.xaml`. This file lists every **other** place in the app where
text can currently reach a second row, so you can decide what else should use it.

**How to read it:** every row names the file, the line of the element's opening tag, what the text is bound to, and
the automation name (the value a UI-automation lookup would find). Line numbers were captured from the working tree
on **2026-09-24**; a later edit to a file shifts its numbers, so re-check the line before acting on a row.

| Group | Meaning |
| --- | --- |
| **1 — strong candidates** | Data-bound text on a card or a fixed-width surface: a long value clips, or pushes the surface out of shape. These are what the control should replace. |
| **2 — possible** | Data-bound text with room to wrap, or a sentence the reader is meant to read as prose. Wrapping is arguably correct. |
| **3 — not candidates** | Fixed labels, help and status sentences, multi-row inputs, and elements that deliberately wrap. |

### Two facts that shape this list

1. **`TextWrapping` defaults to `NoWrap` in WinUI 3.** A `TextBlock` with no `TextWrapping` attribute does **not**
   wrap — it clips. So "has no `NoWrap`" is *not* a sign of a wrapping element; only an explicit
   `TextWrapping="Wrap"`/`WrapWholeWords`, or a wrapping **style**, can reach a second row.
2. **There is no implicit (keyless) `TextBlock` style.** Wrapping only arrives through a named style. The styles that
   wrap: `SetupPageTitleStyle` and `SetupPageSubtitleStyle` (`Styles/ModuleSetup.xaml`), `RequestDialogTitleStyle` and
   the `RequestDialog*` set (`Styles/RequestDialogs.xaml`), `BodyTextStyle` (`Styles/TextBlock.xaml`), the
   `Inspector*`/`Tooltip*` styles, and the page-local `SettingsDescriptionStyle` (`Module_Settings/Views/SettingsPage.xaml`).

---

## Group 1 — strong candidates

| # | File | Line | Type | Text source | Automation name | Reach | Why it qualifies |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `Module_Setup/Views/SetupWorkCenterPage.xaml` | 97 | TextBlock | `{x:Bind Name}` | card `Grid` names itself `{x:Bind Name}` (:39) | wraps | Work-centre card, fixed 300px photo pane beside it |
| 2 | `Module_Setup/Views/SetupWorkCenterPage.xaml` | 102 | TextBlock | `{x:Bind Building}` | same card | wraps | Card value |
| 3 | `Module_Setup/Views/SetupWorkCenterPage.xaml` | 106 | TextBlock | `{x:Bind CurrentJobSummary}` | same card | wraps | Card value |
| 4 | `Module_Setup/Views/SetupWorkCenterPage.xaml` | 110 | TextBlock | `{x:Bind CurrentPartSummary}` | same card | wraps | Card value |
| 5 | `Module_Setup/Views/SetupWorkCenterPage.xaml` | 114 | TextBlock | `{x:Bind LastUpdatedDisplay}` | same card | wraps | Card value |
| 6 | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | 92 | TextBlock | `{x:Bind WorkCenterName}` | card `Grid` names itself (:41) | wraps | The same card shape as #1, in the New Request wizard |
| 7 | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | 97 | TextBlock | `{x:Bind Building}` | same card | wraps | Card value |
| 8 | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | 101 | TextBlock | `{x:Bind CurrentJobSummary}` | same card | wraps | Card value |
| 9 | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | 105 | TextBlock | `{x:Bind CurrentPartSummary}` | same card | wraps | Card value |
| 10 | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | 109 | TextBlock | `{x:Bind LastUpdatedDisplay}` | same card | wraps | Card value |
| 11 | `Module_Waitlist/Views/NewRequestComponentPage.xaml` | 83 | TextBlock | `{x:Bind Title}` | card `Grid` `AutomationId={x:Bind AutomationId}` (:55) | wraps | 150×170 choice card — the same shape as the converted dunnage cards |
| 12 | `Module_Waitlist/Views/NewRequestDunnagePage.xaml` | 85 | TextBlock | `{x:Bind Title}` | card `Grid` names itself (:62) | wraps | 150×170 choice card |
| 13 | `Module_Waitlist/Views/NewRequestDunnagePage.xaml` | 91 | TextBlock | `{x:Bind Summary}` | same card | wraps | Card caption |
| 14 | `Module_Waitlist/Views/NewRequestDiePage.xaml` | 90 | TextBlock | `{x:Bind Title}` | card `Grid` names itself (:51) | wraps | 170–240 card; a die name is long by nature |
| 15 | `Module_Waitlist/Views/NewRequestDiePage.xaml` | 96 | TextBlock | `{x:Bind Summary}` | same card | wraps | Card caption |
| 16 | `Module_Waitlist/Views/NewRequestItemPage.xaml` | 76 | TextBlock | `{Binding DisplayName}` | card `Border` names itself (:63) | wraps | 150×150 choice card |
| 17 | `Module_Waitlist/Views/NewRequestItemPage.xaml` | 82 | TextBlock | `{Binding Summary}` | same card | wraps | Card caption |
| 18 | `Module_Waitlist/Views/NewRequestJobTypePage.xaml` | 86 | TextBlock | `{Binding Name}` | card `Border` names itself (:66) | wraps | 150×150 choice card |
| 19 | `Module_Waitlist/Views/NewRequestJobTypePage.xaml` | 92 | TextBlock | `{Binding Summary}` | same card | wraps | Card caption |
| 20 | `Module_Setup/Views/SetupWorkOrderPage.xaml` | 60 | TextBlock | `{x:Bind Description}` | tile `Button` names itself `{x:Bind PartNumber}` (:58) | wraps | Tile capped at `MaxWidth=260` |
| 21 | `Module_Setup/Views/SetupWorkOrderPage.xaml` | 82 | TextBlock | `{x:Bind Description}` | tile `Button` names itself (:80) | wraps | Tile capped at `MaxWidth=220` |
| 22 | `Module_Setup/Views/SetupReviewPage.xaml` | 96 | TextBlock | `{x:Bind Description}` | row `Grid` names itself `{x:Bind PartNumber}` | wraps | Table row with fixed columns |
| 23 | `Module_Setup/Views/SetupReviewPage.xaml` | 132 | TextBlock | `{x:Bind DisplayName}` | row `Grid` names itself (:126) | wraps | Dunnage row |
| 24 | `Module_Setup/Views/SetupPartSelectionPage.xaml` | 58 | TextBlock | `{x:Bind Description}` | row `Grid` names itself (:36) | **clips today** | List row: no wrapping and no trimming — a long description is simply cut |
| 25 | `Module_Setup/Views/SetupSequenceSelectionPage.xaml` | 29 | TextBlock | `{x:Bind Description}` | `StackPanel` names itself (:26) | **clips today** | Same as #24, another list |
| 26 | `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml` | 158 | TextBlock | `{x:Bind DunnageTypeName}` | none (tooltip holds the full text) | wraps | Grid tile caption in the image-search dialog |
| 27 | `Module_Settings/Views/PartPictureManagerPage.xaml` | 194 | TextBlock | `{x:Bind Title}` | list `PartPictureManagerPage_ChangeRecord` (:186) | wraps | Change-history row |
| 28 | `Module_Settings/Views/PartPictureManagerPage.xaml` | 199 | TextBlock | `{x:Bind Detail}` | same list | wraps | Change-history row |
| 29 | `Module_Settings/Views/PartPictureManagerPage.xaml` | 277 | TextBlock | `{x:Bind RelativePath}` | list `PartPictureManagerPage_MissingList` (:257) | wraps | Path in a `*` column beside two fixed columns |
| 30 | `Module_Settings/Views/PermissionHoldersView.xaml` | 63 | TextBlock | `{x:Bind}` (role string) | `PermissionHoldersView_Roles` (:58) | wraps | Repeating rows |
| 31 | `Module_Settings/Views/PermissionHoldersView.xaml` | 98 | TextBlock | `{x:Bind DisplayName}` | list `PermissionHoldersView_People` (:75) | wraps | Row |
| 32 | `Module_Settings/Views/PermissionHoldersView.xaml` | 104 | TextBlock | `{x:Bind RoleText}` | same list | wraps | Row |
| 33 | `Module_Settings/Views/PermissionHoldersView.xaml` | 110 | TextBlock | `{x:Bind MarkText}` | same list | wraps | Fixed column |
| 34 | `Module_Settings/Views/PermissionHoldersView.xaml` | 116 | TextBlock | `{x:Bind SwitchedOffText}` | same list | wraps | Fixed column |
| 35 | `Module_Settings/Views/PermissionsPage.xaml` | 192 | TextBlock | `{x:Bind AreaHeadingText}` | card `PermissionsPage_Matrix` (:186) | wraps | Fixed `PermissionPersonColumnWidth` |
| 36 | `Module_Settings/Views/PermissionsPage.xaml` | 207 | TextBlock | `{x:Bind LabelText}` | columns `PermissionsPage_Columns` (:196) | wraps | Fixed `PermissionCellWidth` |
| 37 | `Module_Settings/Views/PermissionsPage.xaml` | 212 | TextBlock | `{x:Bind GatesText}` | same | wraps | Fixed cell width |
| 38 | `Module_Settings/Views/PermissionsPage.xaml` | 241 | TextBlock | `{x:Bind Person.SignInName}` | matrix card | wraps | Fixed person column |
| 39 | `Module_Settings/Views/PermissionsPage.xaml` | 245 | TextBlock | `{x:Bind Person.RoleText}` | matrix card | wraps | Fixed person column |
| 40 | `Module_Settings/Views/PermissionsPage.xaml` | 250 | TextBlock | `{x:Bind Person.PendingText}` | matrix card | wraps | Fixed person column |
| 41 | `Module_Settings/Views/PermissionsPage.xaml` | 256 | TextBlock | `{x:Bind Person.LockReasonText}` | matrix card | wraps | Fixed person column |
| 42 | `Module_Settings/Views/PermissionsPage.xaml` | 145 | TextBlock | `{x:Bind PendingText, Mode=OneWay}` | none | wraps | Permission row |
| 43 | `Module_Settings/Views/ImageOverrideEditorControl.xaml` | 154 | TextBlock | `{x:Bind DisplayName}` | ancestor `Grid`; buttons name themselves | wraps | **Fixed 180px column** with a 48px preview beside it |
| 44 | `Module_Settings/Views/ImageOverrideEditorControl.xaml` | 225 | TextBlock | `{x:Bind WarningMessage, Mode=OneWay}` | none | wraps | Row warning |
| 45 | `Module_Settings/Views/UserManagementPage.xaml` | 267 | TextBlock | `{x:Bind UsernameNormalized}` | table `UserManagementPage_Roster` (:180) | **clips today** | `NoWrap` + ellipsis + tooltip: it truncates rather than wraps |
| 46 | `Module_Settings/Views/UserManagementPage.xaml` | 276 | TextBlock | `{x:Bind EmployeeIdentifier}` | same table | **clips today** | As #45 |
| 47 | `Module_Settings/Views/UserManagementPage.xaml` | 292 | TextBlock | `{x:Bind RoleText}` | same table | **clips today** | As #45 |
| 48 | `Module_Core/Views/ShellPage.xaml` | 458 | TextBlock | `{Binding CurrentUserDisplayName}` | shell header | **clips today** | `MaxWidth=220` + trim |
| 49 | `Module_Core/Views/ShellPage.xaml` | 644 | TextBlock | `{Binding CurrentUserDisplayName}` | shell flyout | **clips today** | `MaxWidth=220` + trim |
| 50 | `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` | 517 | TextBlock | `{x:Bind First.Value}` | none | wraps | Always wraps: `ValueTextWrapping` answers a wrapping value in both branches (`MTM_Waitlist.Waitlist.View/Models/WaitlistDetailTemplateSection.cs:41`) |
| 51 | `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` | 533 | TextBlock | `{x:Bind Second.Value}` | none | wraps | As #50 |

### If you only act on a few

| Priority | Rows | Why |
| --- | --- | --- |
| Highest | 24, 25, 45, 46, 47, 48, 49 | These **already cut text off today** with no way to read it. The scrolling control replaces a lossy truncation, which is a straight improvement. |
| High | 11, 12, 14, 16, 18, 20, 21 | Choice cards and tiles, exactly the surface the control was written for — the tiles hold fixed 150/170/220/260 widths and their captions are names that run long. |
| Medium | 1–10 | Work-centre cards: five values stacked per card, any of which can wrap and push the card taller. |
| Medium | 43 | Fixed 180px column in the image-override editor. |
| Review first | 50, 51 | These are detail-page *values*; a scrolling value there may be worse than a wrapped one. |

---

## Group 2 — possible (wrapping may be correct)

Data-bound text with room to wrap, or prose the reader is meant to read in sentences.

| Area | Files and lines | Notes |
| --- | --- | --- |
| Detail-page header and sections | `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` 99, 172, 208, 238, 364, 395, 422, 430, 452, 489 | Wide `*` column; trimming is already applied to the header |
| Request review blocks | `NewRequestSummaryPage.xaml` 50, 62, 75, 99, 136, 167, 178, 189, 202 (+77, 101, 191) | Field values in a `*` column, plus the per-die detail lines |
| Validation / result prose | `NewRequestDetailsPage.xaml` 60; `NewRequestResultPage.xaml` 39, 46; `SetupCompletionPage.xaml` 36, 46 | Sentences |
| Status sentences | `SetupWorkOrderPage.xaml` 90; `SetupPartSelectionPage.xaml` 21; `SetupSequenceSelectionPage.xaml` 21; `SetupDunnageTypePage.xaml` 105; `SetupWorkCenterPage.xaml` 145; `SetupReviewPage.xaml` 21; `NewRequestComponentPage.xaml` 29; `NewRequestDiePage.xaml` 29; `NewRequestDunnagePage.xaml` 29; `NewRequestItemPage.xaml` 28; `NewRequestJobTypePage.xaml` 28 | One sentence per screen |
| Settings sentences | `SettingsPage.xaml` 293, 298, 304, 374, 940, 956, 961 (+ style users 498, 622, 711, 827, 970, 1018); `PartPictureManagerPage.xaml` 246; `PermissionsPage.xaml` 67, 71, 106, 111, 351, 358; `UserManagementPage.xaml` 58, 105, 126, 130, 135; `CreateUserPage.xaml` 22, 63, 76; `EditUserPage.xaml` 70, 151, 156, 161, 166, 172, 178; `ComputerEditDialog.xaml` 57; `PermissionHoldersView.xaml` 34, 48; `PinRevealDialog.xaml` 19, 42, 44, 46, 48, 50, 53, 65 | Full-width rows built for wrapping |
| Startup / dev surfaces | `LoginPage.xaml` 71, 99, 105; `ControlInspectorDetailPage.xaml` 41, 45, 121, 122; `ReadStatusIndicator.xaml` 25, 27; `SetupDunnageImageSearchDialog.xaml` 183 | Sentences, or a developer-facing inspector |

---

## Group 3 — not candidates

- **Fixed labels** that wrap only because they share a title or subtitle style — `SetupPageTitleStyle` users
  (`SetupDunnageTypePage` 19, `SetupPartSelectionPage` 20, `SetupSequenceSelectionPage` 20, `SetupWorkCenterPage` 142,
  `SetupWorkOrderPage` 20/48/70, the New Request step headers, `PreviewPage` 24/30, `ResultPage` 36, `SummaryPage`
  146, `WorkCenterPage` 135) and `SetupPageSubtitleStyle` users. Literal text cannot grow.
- **Field labels** — every `RequestDialogFieldLabelStyle` user.
- **Bound dialog, flyout and InfoBar text** (`Message`/`Title`/`Description`/`Header` across `PermissionsPage`,
  `CreateUserPage`, `EditUserPage`, `PartPictureManagerPage`, `ReadStatusIndicator`, `WaitlistViewPage`,
  `NewRequestSummaryPage`, `NewRequestWorkCenterPage`, and the `wct:SettingsCard` literals in `SettingsPage`).
- **Elements that already solve this differently** — `WaitlistLineCardView.xaml` 87, 94 and
  `SetupDunnageImageSearchDialog.xaml` 145 use `MaxLines="1"` + `NoWrap` + trimming.
- **Multi-row inputs — report only, do not convert:** `ComputerEditDialog.xaml:47` (`TextBox`, `AcceptsReturn`,
  `TextWrapping="Wrap"`, a description) and `WaitlistViewDetailPage.xaml:355` (`TextBox`, `AcceptsReturn`, note
  draft). `NewRequestDetailsPage.xaml:36` sets `TextWrapping="Wrap"` with `AcceptsReturn="False"` and
  `MaxWidth="520"` — a single-line entry that wraps visually and could use the control, or be narrowed.
- **Tooltip chrome** — `Styles/TooltipStandard.xaml`, `Styles/TooltipDeveloper.xaml`.

---

## What this list does NOT claim

1. **No value was measured.** "Could reach two rows" is inferred from the surface (a fixed column, a card tile, a
   dialog), not proven against the data behind it. Nothing here says a given part number or work-centre name is long
   today.
2. **Nothing was rendered.** No row was observed wrapping on screen; the classification is from markup and styles.
3. **`MTM_Waitlist.Mock.Service/**` was excluded** (a separate service application). It holds 48 further explicit
   wrap sites if you ever want the same review there.
4. **Several clipped elements already carry a tooltip with the full value** (`UserManagementPage` 267/276/292,
   `WaitlistLineCardView` 87/94). Whether to convert those anyway is a judgement about the surface, not a fact about
   the markup.
5. **`WaitlistViewDetailPage.xaml` 517/533** is decided per value by `ValueTextWrapping`, which returns a wrapping
   value in *both* of its branches — so the intent implied by the surrounding comment is not what the code does. That
   is a separate finding, left for you to judge.
