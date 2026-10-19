# Contract: card, settings and notice

The three UI surfaces this feature exposes. A consumer or a test codes against the identifiers below.
Every string comes from `spec.md` (Requirements and Verbatim Constraints), `plan.md` (Project
Structure, Call Paths, Screens) or `research.md` (D1, D7). Nothing is renamed, re-cased, pluralised or
invented.

## 1. The card

### The cell

| Item | Value |
|---|---|
| Item | `pickup-scrap` |
| Label | `Lugger Location` |
| Declaration | `source` `job`, `value_type` `string`, `order` 5, `is_required` false |
| Availability property | `RequestJobPartAvailability.LuggerLocation`, a nullable string |
| Resolver arm | `RequestJobFieldValues.Resolve`, under the label `lugger location` |
| Resource key of the retired placeholder | `Waitlist_Card_PendingSource.InDevelopment` |

The declaration already exists in `Database/Seeds/seed_waitlist_request_item_configs/create.sql` and
is unchanged by this feature. The label stays `Lugger Location`, and the card's layout does not
change (FR-027).

### Where the value comes from

`ILuggerArrangementService.SnapshotAsync` reads the arrangement once at the start of each list load
through `sp_waitlist_lugger_arrangement_list`. `RequestJobAvailabilityProvider` asks the snapshot for
the requesting job's scrap type and sets `LuggerLocation`. `RequestJobFieldValues.Resolve` returns that
value for the label, and `ResolveDeclaredCell` draws it. The cell resolves while the list is being
built, so a status change appears on the next load and the request stores nothing about the lugger
(FR-022, FR-023, research D1).

### The values it can show

| Situation | What the cell shows |
|---|---|
| The scrap type has a lugger holding the dump point | that lugger's name, for example `Door 6 Lugger` |
| At least one lugger for the scrap type can receive material | the name of the lugger holding the dump point, never a blocked one |
| No lugger for the scrap type can receive material | the first lugger in that scrap type's order, followed by its status in parentheses, for example `Door 9 Lugger (Full)` or `Door 9 Lugger (Do Not Use)` |

The second and third rows are one rule: the cell always names the dump point when there is one, and
when there is none it names the first lugger in the order with its status beside the name, so the
handler reads "no usable dump point" rather than nothing (FR-024).

The value is the stored `lugger_name`, so a rename shows the new name on the next card load with no
further change (FR-003, User Story 3 scenario 2).

### The two cases where no cell is drawn

1. **The job has no real scrap decision.** The job's scrap type is absent or blank, is the
   `Scrap Type Required` placeholder, or is `No Scrap`. These are not scrap types and are never
   assigned a lugger (FR-010, FR-025). The absence is not recorded as a failure.
2. **The scrap type has no assignment at all.** It may never have been configured, or its last lugger
   was removed or unassigned. The card draws no lugger cell, and the gap is recorded once through
   `AppLog.Warn` with the item and the scrap type so the configuration can be corrected (FR-026). No
   substitute value is invented, and the gap is not counted as a failure.

In both cases `LuggerLocation` is null and the existing card rule omits the cell. The cell is never
drawn blank and never drawn with a dash.

### Matching the job's scrap type

The job's stored scrap type is matched to an assignment by `LOWER(TRIM(...))` on both sides. Matching
ignores letter case and surrounding spaces, and nothing else (FR-009).

The setup workflow spells one type `5052 aluminum` while the pinned assignment is `5052 Aluminum`, and
keep the spec's spelling. Matching is case-insensitive, so either spelling resolves to `Door 8 Lugger`.
A job value of ` 5052 ALUMINUM ` resolves the same way (User Story 1 scenario 4).

### The pinned defaults a card must reflect

These are the shipped assignments, in each scrap type's try order. The default card values follow from
them and from the seeded statuses.

| Job's scrap type | First lugger (dump point) | Backup | Default card value |
|---|---|---|---|
| `3003 Aluminum` | `Door 6 Lugger` | `Extra Lugger (Outside)` | `Door 6 Lugger` |
| `Galvanized Steel` | `Door 7 Lugger` | none | `Door 7 Lugger` |
| `5052 Aluminum` | `Door 8 Lugger` | none | `Door 8 Lugger` |
| `Steel` | `Door 9 Lugger` | none | `Door 9 Lugger` |
| `Skeleton` | `Skeleton Lugger` | none | `Skeleton Lugger` |
| `Gaylord` | `Scrap Trailer` | none | `Scrap Trailer` |

The owner's walk-through is the acceptance test for the `3003 Aluminum` row: with `Door 6 Lugger`
available and `Extra Lugger (Outside)` its backup, the card reads `Door 6 Lugger`. Setting
`Door 6 Lugger` to `Full` makes it read `Extra Lugger (Outside)`. Emptying `Door 6 Lugger` leaves the
card reading `Extra Lugger (Outside)`. Setting `Extra Lugger (Outside)` to `Full` makes it read
`Door 6 Lugger` again.

### What is retired

The `In development` placeholder goes completely (FR-039):

- the `pickup-scrap` entry in `WaitlistCardCellRules.s_pendingSourceLabels`,
- the `Waitlist_Card_PendingSource.InDevelopment` resource and its wording,
- `WaitlistCardCellRules.PendingSourceKey` and the branch that draws it.

No cell anywhere in the application may be marked not-yet-built by that mechanism afterwards. The
card-rule test that asserts the placeholder is drawn, and the resolver test that asserts the label
resolves to nothing, are amended in the same change.

The `Scrap Type` cell stays. `Quantity involved` and `Scrap lugger` stay excluded by the card rule, and
no other cell on any other Item changes.

## 2. Settings

### The route

| Item | Value |
|---|---|
| View model | `MTM_Waitlist.Module_Settings.ViewModels.LuggerArrangementViewModel` |
| Page | `Module_Settings/Views/LuggerArrangementPage.xaml` beside `LuggerArrangementPage.xaml.cs` |
| Navigation | `INavigationService.NavigateTo` with the view model's full name, as every other page is routed |
| Registration | a new contribution file, `Services/DependencyInjection/ServiceRegistrationExtensions.LuggerArrangement.cs`, adding `pageService.Configure<LuggerArrangementViewModel, LuggerArrangementPage>()`, as the Permissions, User Management and Part Picture screens each do |
| Opened from | the entry card on `Module_Settings/Views/SettingsPage.xaml` |
| Route constant | `SettingsViewModel.LuggerArrangementViewModelName` |

The route is the view model's full name because this application routes by view-model name. There is
no separate URL, deep link or command line surface for this feature.

### The entry's visibility rule

The entry card on `SettingsPage` is shown only when the signed-in person holds
`permission.settings.lugger_arrangement`, which is the key pinned in
`contracts/persistence-and-permission.md` section 4. The rule:

- **Held:** the card is drawn and opens the route.
- **Not held:** the card is not drawn at all, not shown-but-disabled, and the route is refused if it
  is reached another way. The screen is not loaded and the arrangement is not shown (FR-029, SC-006).

The check is the same permission check the other permissioned settings entries use. The entry is not
gated on role name, because the baseline is data and a role's answer can change without a code change.
The lowest role that holds the key is `Material Handler Lead`, and every role above that rung holds it
too, so a lead can open the arrangement and everybody below a lead cannot (FR-028).

### The summary and the editor

Both live on the one page. The summary lists every lugger with its `lugger_name`, its `status`, and the
scrap types it takes. The editor opens from an explicit action on that summary (FR-030).

The arrangement is drawn as a **grid of lugger cards** — one card per lugger, wrapping across the page.
A card carries the lugger's name, its status with any reason in the status's own colour, the scrap types
it takes, and two actions of its own: **Edit**, which opens that lugger's editor, and **Rename**, beside
it. Adding a lugger is a button at the bottom of the grid, and the cards can be dragged into a new order:
the platform starts the drag from a card's own background once the press is held, the buttons on the card
take their own clicks, and the dropped place is written through `sp_waitlist_lugger_locations_position_set`.

The editor is a **modal window opened from one card**, so it names the lugger it was opened for and
carries no lugger picker of its own. The scrap type is a **choice from the catalogue** rather than free
text. **Make dump point** is offered only when it can do something: the chosen scrap type must be assigned
to that lugger, another lugger must be assigned as its backup, and the lugger must not already hold the
role. The display order is changed by dragging the cards, so the editor carries no move-up or move-down
actions.

The scrap-type summary is not drawn beside the cards: which lugger a scrap type dumps into is read from
that lugger's own card, and a type with no lugger assigned is visible as a gap rather than as a row.

The editor offers these actions, each backed by the procedure named beside it:

| Action | Procedure |
|---|---|
| Add a lugger | `sp_waitlist_lugger_locations_insert` |
| Rename a lugger | `sp_waitlist_lugger_locations_rename` |
| Remove a lugger | `sp_waitlist_lugger_locations_delete` |
| Set a status, with a required reason for `Do Not Use` | `sp_waitlist_lugger_locations_status_set` |
| Promote a lugger for a scrap type | `sp_waitlist_lugger_locations_promote` |
| Assign a scrap type to a lugger, and order it | `sp_waitlist_lugger_scrap_assignments_set` |
| Unassign a scrap type from a lugger | `sp_waitlist_lugger_scrap_assignments_delete` |
| Read the ordered luggers for one scrap type | `sp_waitlist_lugger_scrap_assignments_list` |

The editor shows the chosen scrap type's assignment for the lugger it was opened on, and whether that
lugger can be promoted to the type's dump point (FR-031, FR-018). The per-scrap-type list of dump points
is **not** drawn on the page: which lugger holds a type's dump point is read from that lugger's own card,
which names the scrap types it takes, and from the editor's **Make dump point** action. It offers
the scrap types the setup workflow already recognises, and a type already assigned that the catalogue
does not name (FR-011). It refuses `Scrap Type Required` and `No Scrap` as assignments, under any case or
surrounding spaces. It refuses a blank reason when `Do Not Use` is chosen (FR-019, User Story 3
scenario 7). It refuses a duplicate lugger name (FR-004).

Every change shows who made it and when, read from `waitlist_lugger_arrangement_changes` (FR-032).

When the store is unreachable the page shows its own unavailable state with a manual retry. It never
shows sample luggers and never substitutes a stored copy.

## 3. The notice

### The surface

| Item | Value |
|---|---|
| Screen | `Module_Waitlist/Views/WaitlistViewPage.xaml`, a strip above the list |
| Control | one `Microsoft.UI.Xaml.Controls.InfoBar` per undismissed notice |
| Severity | `InfoBarSeverity.Informational` |
| Close button | `IsClosable` true |
| Service | `MTM_Waitlist.Waitlist.View/Services/ILuggerChangeNoticeService.cs` and `LuggerChangeNoticeService.cs` |
| Model | the `LuggerChangeNotice` record the service hands the view model |
| Read | `sp_waitlist_lugger_arrangement_changes_list` |
| Cadence | once per existing minute tick, `WaitlistViewViewModel`'s `OnMinuteTick` |

One bar is shown per notice, so two changes produce two bars. The bar is in the application and is not
an operating-system notification (FR-034). Nothing on this surface depends on the application being
packaged.

### Who is told, and when

- A notice is shown only to a person who holds `permission.requests.handle`. A person without it is not
  interrupted (FR-038).
- When the list opens, the service takes a watermark: one call to
  `sp_waitlist_lugger_arrangement_changes_list` with a null `p_after_id`, which returns the current
  highest change id or 0.
- Each later tick asks for changes whose `id` is greater than the watermark. Nothing at or below the
  watermark is ever shown, so a change made while the person was not using the application is never
  replayed to them (FR-037).
- A change recorded between the list opening and its first poll is treated as having happened before
  the person was using it and is not shown. That is the recorded consequence of the tick cadence, not
  an accident.
- Every person using the list at the time of a change is told within one tick of it (FR-033, SC-005).

### What a notice says

A notice names what changed in plain words. It names the lugger and the scrap type, and it names both
the previous and the new value wherever a value changed (FR-035). A rename therefore names both the old
name and the new name, so the handler recognises the lugger they were told about.

The `change_kind` written by the procedures is the source of that wording:
`lugger_added`, `lugger_renamed`, `lugger_removed`, `status_changed`, `dump_point_changed`,
`assignment_added`, `assignment_removed`, `assignment_reordered`.

### Dismissal

- The person closes the bar. The service handles the `InfoBar` `Closing` event and records the
  dismissal only when `InfoBarClosingEventArgs.Reason` is `InfoBarCloseReason.CloseButton`.
- A dismissed notice is removed from the strip and is not offered again for that person in that session
  (FR-036).
- A bar closed for any other reason, for example because the list reloaded, is not recorded as a
  dismissal.
- Dismissal is held for the session. A later session starts above every older change anyway, because
  its watermark is taken when the list opens, so no dismissal table is needed (research D7).
