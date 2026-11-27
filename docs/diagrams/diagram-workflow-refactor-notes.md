# Workflow Refactor Notes — Work Order vs Generic Request

Companion to the two print-ready flowchart PDFs in this folder. Everything here is written to be
lifted straight into `/speckit.specify`: the comparison matrix supplies the *current state*, the hotspot
list supplies the *why*, and the Spec Kit inputs supply the *what*.

Nothing in this document is implemented, with one exception: the four findings in §6 (S1 to S4) were fixed
on 25 November 2026 and are marked so there. Everything else records what the code did when it was read, on
the date below. The four flows this note compares, and the eight PDFs built from them, were removed
afterwards with the flows themselves; the startup-launch pair is the only diagram set that remains.

Source revision: `main` working tree at time of writing. Diagrams verified at US Letter landscape
(792 × 612 pt), 0.5 in margins, ≥ 9.5 pt effective text, 12–16 nodes per sheet.

---

## 1. Diagram index

| # | Flow | Technical (engineer) | Plain-language (stakeholder) | Sheets |
| --- | --- | --- | --- | --- |
| 1 | Startup — launch, sign-in and hand-over | [diagram-startup-launch.pdf](./pdf/startup-launch/diagram-startup-launch.pdf) | [diagram-startup-launch-friendly.pdf](./pdf/startup-launch/diagram-startup-launch-friendly.pdf) | 3 / 2 |

The four request flows this note exists to compare — Work Order New Request, Work Order Work Center Setup,
Generic Request New Request and Generic Request Work Center Setup, each with an engineer and a friendly PDF
— were removed with their flows, so their rows are gone from this index. The comparison in §2 to §4 is the
record of what they did at the time of writing.

Each PDF in [`pdf/`](./pdf/) is generated from a `<diagram>-<sheet>.dot` Graphviz source in
[`dot/`](./dot/). The `.dot` files are the canonical, editable description of each sheet; the PDFs are
build output. Both are generated from the JSON specs in [tools/](./tools/) — edit a spec there, then
rebuild:

```powershell
python docs\diagrams\tools\render_sheets.py docs\diagrams\tools\manifest.json
python docs\diagrams\tools\verify_pdfs.py
```

See [tools/README.md](./tools/README.md) for the spec format, the node kinds and the print contract. Every sheet carries a title
header, `Sheet X of Y`, and the shared shape/colour legend. Red dashed nodes tagged `[REFACTOR]` mark a
finding recorded in §3 — the plain-language variants carry the same flags in ordinary words.

Reading order: **UI → ViewModel → Service → Store / ERP**, left to right. Off-page connectors
(`[To Sheet N: …]`) join a sheet boundary at the same command so a workflow can be followed end to end.

---

## 2. Cross-flow comparison matrix

### 2.1 Entry, navigation and views

| Aspect | Work Order — New Request | Generic Request — New Request |
| --- | --- | --- |
| Entry view | `Module_Waitlist/Views/WaitlistViewPage.xaml` (+ `NewRequest` add flyout) | `Module_GenericRequest/Views/GenericRequest…Page.xaml` |
| Step container | `Module_Waitlist/Views/NewRequest*Page.xaml` family | `Module_GenericRequest/Views/GenericRequest*Page.xaml` family |
| Navigation driver | `NavigationService` / `PageService` by view-model full name | same |
| Step order authority | `NewRequestFlowRules` (static, item-configuration driven) | `GenericRequestFlowRules` (static, draft driven) |
| Views in the step chain | WorkCenter → JobType → Item → (Hopper / CoilTurn / SkidCount / PartSearch / Dunnage / Die / Component) → Details → Summary → Result | WorkCenter → Category → Item → Part (conditional) → per-item answer steps → Review → Result |
| View models in the chain | `NewRequestWorkCenterViewModel`, `NewRequestJobTypeViewModel`, `NewRequestItemViewModel`, `NewRequestSummaryViewModel`, `NewRequestResultViewModel`, plus one per answer step (~13 total) | generic step view models bound to `GenericRequestDraft` |
| Duplicate step boilerplate | each view model re-implements `OnNavigatedTo`, back/next commands and error-strip state | same shape, separate implementation |

### 2.2 Commands and validation

| Aspect | Work Order | Generic |
| --- | --- | --- |
| Primary command | `NewRequestWorkCenterViewModel.SelectWorkCenterCommand`, `NewRequestJobTypeViewModel.SelectOptionCommand`, `NewRequestItemViewModel.SelectItemCommand`, `NewRequestSummaryViewModel.SubmitCommand` | equivalent `…Command` properties on the generic step view models (CommunityToolkit strips a trailing `Async`) |
| Precondition: work centre | hot-work-centre list for this computer | presses **marked generic** |
| Precondition: job | `sp_setup_active_jobs_latest_by_work_center_get` → refuse when no job is running | job is optional; offered as a form prefill |
| Precondition: identity | `VerifyEmployeeIdentity` → `sp_core_employee_by_identifier_get` | none |
| Role / permission gate | **none** — an employee record is the only gate (FR-046) | none |
| Answer presence rule | `NewRequestFlowRules.DeclaresAnswerLabel` — the item configuration must declare the label before a step asks for it | item answers carry the configured question directly |
| Answer label constants | `HopperSizeLabel`, `CoilWeightLabel`, `SkidCountLabel`, `IncorrectCoilLabel`, `IncorrectFlatstockLabel` | dynamic, per item configuration |
| Editing an earlier answer | clears downstream answers | clears downstream answers |
| Submit-time re-validation | `NewRequestFlowRules.ValidateCurrentJobState(WorkCenter, WorkCenter)` — job state re-checked before write | draft snapshot written as staged |

### 2.3 Data, services and stored procedures

| Aspect | Work Order | Generic |
| --- | --- | --- |
| Request write | `IWaitlistRequestService.SubmitAsync(draft)` → `sp_waitlist_request_list`, `sp_waitlist_request_insert`, `sp_waitlist_request_audit_insert` | `IGenericRequestStepData.SubmitAsync` → same request tables, separate code path |
| Configuration reads | `sp_config_hot_workcenters_get_for_computer`, `sp_setup_work_centers_get_all`, `sp_waitlist_request_item_configs_get` | `sp_setup_work_centers_generic_set` (designation), generic item configuration reads |
| Coil weight assist | `sp_receiving_history_average_coil_weight` | not used |
| Part match search | `PartMatchSearchService` → `IVisualQueryExecutor` (shape 9 area) | reuses the generic item configuration |
| Deadline / urgency | `IUrgencyDeadlineService.ComputeAsync()` | same service, computed at review |
| Post-write | result page + waitlist card refresh | result page + waitlist card refresh |
| Visual read shapes used | 1 LookupWorkOrder, 2 GetSequences, 3 GetSubordinateParts, 4 inventory_locations, 7 part_warehouse_quantities, 8 work_order_search, 9 part_matches, 10 part_sequences, 11 part_sequence_state, 12 die_search | 9, 10, 11, 12 (generic part/die/sequence reads) |
| Cache fallback | `IVisualReadFallback<TRequest,TRow>` serves `mtm_mock.sp_visual_<shape>_get` only when the reachability verdict is `Cached`, or when a live read returns `Unreachable`. A reachable-but-empty live result is a real answer and is never replaced. | same contract; route depends on which service the step view model calls |

### 2.4 Work-center setup

| Aspect | Work Order setup (`MTM_Waitlist.Setup`) | Generic request setup paths |
| --- | --- | --- |
| Primary view model | `SetupWorkCenterViewModel` | `SetupWorkCenterViewModel` (marking) + Settings surfaces |
| Work-centre selection | `sp_setup_work_centers_get_all` | `sp_setup_work_centers_generic_set` toggles the generic flag |
| Job binding | `sp_setup_active_jobs_latest_by_work_center_get` | presses list filtered by the generic flag |
| Sequence work | `sp_setup_part_sequence_custom_data_upsert` | not applicable |
| Dunnage work | `sp_setup_active_job_dunnage_assignments_get`, `sp_setup_job_history_scrap_type_get` | not applicable |
| Save | `SetupPersistenceService.SaveAsync` → `sp_setup_save_setup`, `sp_setup_part_sequence_custom_data_upsert`, `sp_setup_job_history_insert_for_pair`, `sp_setup_work_centers_touch` | `sp_setup_work_centers_generic_set` then `sp_config_settings_upsert` / `sp_config_hot_workcenters_upsert` as applicable |
| Transaction boundary | **none** — four procedures run one after another | **none** — designation is a second write after the save returns |

---

## 3. Duplication and coupling hotspots

Severity: **H** = behaviour is already at risk, **M** = duplication that will diverge, **L** = tidy-up.
Each entry names the exact class and method the diagrams flag as `[REFACTOR]`.

### H1 — Submit-time job guard compares a value with itself
`MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs` → `SubmitAsync()`.

`NewRequestFlowRules.ValidateCurrentJobState(_state.WorkCenter, _state.WorkCenter)` passes the work
centre as both the expected and the actual job target, so the guard can never fail. A stale job can be
submitted with a passing validation, and the intent of the check is lost. Fix: pass the job the operator
was placed against, and let the guard compare it with the live job.

*Diagram:* Work Order New Request sheet 5 (`[REFACTOR] job guard passes the work centre twice`).

### H2 — Duplicate detection reads an in-memory snapshot
`MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` → `SubmitAsync` and the `_requests`
`ConcurrentDictionary`.

The duplicate check compares the draft against rows already loaded in `_requests`, never against the
store. A request created on another workstation, or created before this session loaded the list, is not
seen. Fix: move the duplicate rule into the insert path (`sp_waitlist_request_insert`), or add a
`sp_waitlist_request_duplicate_get` read, so one authority decides.

*Diagram:* Work Order New Request sheet 5 (`[REFACTOR] duplicate check reads the in-memory snapshot`).

### H3 — Two independent request-writing paths
`MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.SubmitAsync` and
`MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.SubmitAsync`.

Both assemble a request, both call the request tables, and neither shares a draft builder, a validation
step or the audit write. A change to request shape (a new required field, a new audit column) has to be
made twice and will eventually be made once.

*Diagrams:* Work Order New Request sheet 5 and Generic Request New Request sheet 4 (both `[REFACTOR]`).

### H4 — Two flow-rule engines with the same responsibilities
`MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs` and
`MTM_Waitlist.GenericRequest/Services/GenericRequestFlowRules.cs`.

Both answer the same three questions — what is the step order, what comes next, what came before — from
different inputs (an item configuration versus a draft). The ordering logic, the "skip a step that has
nothing to ask" rule and the back-navigation fallback are written twice.

### H5 — Visual-read escape hatch bypasses the fallback
`MTM_Waitlist.Waitlist.NewRequest/Services/PartMatchSearchService.cs` depends on
`IVisualQueryExecutor` directly instead of the shape-9 fallback route.

The part-match search therefore answers "unavailable" when Infor Visual is unreachable while the rest of
the flow continues on cached data. `VisualReadRouteAuditTests` exists precisely to stop this pattern
from spreading; this service is the remaining instance.

### H6 — A failed availability read becomes "no stock"
The part availability path collapses a read failure into `RequestJobPartAvailability.None`. An outage
and a genuine zero stock state produce the same answer, so an operator can be told a part is missing
when the truth is that the plant network was down.

### H7 — No role gate on raising a request
`Module_Waitlist/Views/WaitlistViewPage.xaml` → `NewRequestWorkCenterViewModel.SelectWorkCenterCommand`.

The only precondition on raising a request is an employee record returned by
`sp_core_employee_by_identifier_get` (FR-046). Any employee on the list can raise any request at any
press they are standing at. Operator, material handler, setup lead and admin are not distinguished
anywhere on this path, so the role model exists only in the settings screens.

*Diagram:* Work Order New Request sheet 1 (`[REFACTOR] no role gate`).

### M1 — Thirteen view models re-implement navigation
Each New Request step view model hand-rolls `OnNavigatedTo`, the next/back commands, and its own
error-strip properties. The generic flow does the same on its own step view models. A shared step base
(load → validate → advance → report) would remove the per-step copies without changing behaviour.

### M2 — Generic designation is a second, non-atomic write
`MTM_Waitlist.Setup/Services/SetupWorkCenterService.cs` (`sp_setup_work_centers_generic_set`), called from
`SetupWorkCenterViewModel.ApplyGenericDesignationAsync`.

The setup save commits first and the generic flag is written afterwards. If the second write fails, the
work centre is saved as non-generic while the operator believes otherwise — and a generic request then
cannot find the press.

### M3 — Setup save has no transaction
`MTM_Waitlist.Setup/Services/SetupPersistenceService.SaveAsync` runs `sp_setup_save_setup`,
`sp_setup_part_sequence_custom_data_upsert`, `sp_setup_job_history_insert_for_pair` and
`sp_setup_work_centers_touch` in sequence with no transaction. A failure at step 3 leaves a saved setup
partially applied, with no rollback and no operator-visible marker.

### M4 — The generic flow repeats the work-order validations
`MTM_Waitlist.GenericRequest/Services/GenericRequestFlowRules.cs` and
`MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`.

Job binding, part eligibility, die availability and the "you cannot ask here without a job" rule are
implemented in `NewRequestFlowRules` and again in the generic step view models. The two copies have
already drifted once — the generic path permits a submission that the work-order guard would reject.
This is the finding repeated on all four generic sheets, because it belongs to the flow rather than to
one screen.

### M5 — The generic flag and the settings are re-read on every visit
`MTM_Waitlist.Setup/ViewModels/SetupWorkCenterViewModel.cs`,
`Module_Settings/ViewModels/SettingsViewModel.cs`.

Both screens read their state in `OnNavigatedTo` on every visit with no cache and no change
notification, so a flag changed on another screen is only visible after leaving the page and coming
back. A press un-marked generic keeps appearing as generic for the rest of the current visit.

### M6 — Answer labels are matched by text, not by identity
`MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.DeclaresAnswerLabel`.

The rule decides whether an item asks its question by comparing the configuration's label against a
string constant, trimmed and case-insensitive. Renaming the label in the item configuration does not
fail — it silently removes the question from the flow, and nothing reports that the step was skipped.

### M7 — The work-center list is chosen in three places
`sp_config_hot_workcenters_get_for_computer`, `sp_setup_work_centers_get_all`, and the generic-press
filter.

Which presses an operator may pick depends on which of the three answered, and the choice is split
between the item configuration and the launcher. Two operators on the same computer can be offered
different lists depending on the path that opened the picker.

### M8 — The scrap type is loaded from two sources
`MTM_Waitlist.Setup/Services/SetupPersistenceService.LoadSavedScrapTypeAsync`
(`sp_setup_job_history_scrap_type_get`) and `sp_setup_active_job_dunnage_assignments_get`.

The same answer arrives twice and is merged in the view model. The two can disagree — the job-history
row is written at save, the assignment row at dunnage setup — and the merge hides which one was used.

### M9 — The add button assembles the request itself
`Module_Waitlist/Views/WaitlistViewPage.xaml` → the `+` handler.

The draft is built in the view model rather than by a service, so the request shape is defined in UI
code as well as in `WaitlistRequestService`. A field added to the request has to be added in both, and
the UI copy is the one that runs first.

### M10 — Un-marking a press generic warns nobody
`MTM_Waitlist.Setup/Services/SetupWorkCenterService.cs` → `sp_setup_work_centers_generic_set`.

Clearing the flag is a silent edit. Any generic request in flight for that press loses its configuration
source, and no screen reports the change or checks for work in progress.

### L1 — Unreachable registered pages
`SetupPartSelectionPage` and `SetupSequenceSelectionPage` are registered in
`Services/DependencyInjection/ServiceRegistrationExtensions.cs` but no code navigates to them. They are
either dead or a half-finished path; decide, do not leave both.

### L2 — Longest files hold the most divergent logic
`Module_Settings/ViewModels/SettingsViewModel.cs` (2623 lines) and the setup view models carry the
settings and generic-flag surfaces. Splitting them is a prerequisite for testing the designation path in
isolation.

### L3 — The fallback verdict is not carried with the data
`MTM_Waitlist.Mock/Services/VisualReadFallback.cs`.

The live-versus-mirror decision is one verdict held by the reachability probe; every read consults it,
but the read result does not say which source answered. The cheapest way to make the fallback visible
on screen — and legible on the diagram — is to put the verdict and the mirror's age on the read result.

### L4 — Quick Add is gated by a literal permission string
`MTM_Waitlist.Setup/ViewModels/SetupDunnageViewModel.cs` → `setup.dunnage_quick_add`.

The permission name is written inline where it is checked. There is no shared constant and no policy
object, so a typo silently disables the feature rather than failing anywhere visible.

---

## 4. Draft Spec Kit inputs

Ready to paste into `/speckit.specify` as a feature description, or to seed `spec.md` directly. Written
as user value, not implementation.

### 4.1 Feature name

Unify and harden the Work Order and Generic Request flows.

### 4.2 Problem statement

The Work Order and Generic Request features grew separately and now carry two implementations of the
same responsibilities: step ordering, answer validation, request writing and work-centre configuration.
The duplication has already produced real defects — a submit guard that cannot fail, a duplicate check
that sees only this session's data, a part-read failure that reads as "out of stock", and a generic
designation that can be lost silently. Operators see different behaviour for the same intent depending
on which button they pressed.

### 4.3 Functional requirements (draft)

**Submit integrity**

- FR-01 — A submission must not be stored when the job on the chosen work centre has changed since the
  operator bound to it. The check must compare the bound job with the live job.
- FR-02 — Duplicate detection must be authoritative across workstations. Two operators submitting the
  same request must not both succeed, regardless of which lists each has loaded.
- FR-03 — A submission must be all-or-nothing: request row, audit row and any related configuration must
  commit together or not at all.

**Unavailable data is not empty data**

- FR-04 — Every external read must resolve through the shared fallback route. A screen must never reach
  the ERP query executor directly.
- FR-05 — A read failure must be reported as unavailable with a manual retry. It must never be presented
  as a zero, an empty list, or a "none" availability state.
- FR-06 — A reachable-but-empty live answer must remain a real answer and must never be replaced by
  cached data.

**One flow engine**

- FR-07 — Step order, next step, previous step and "skip a step with nothing to ask" must be answered by
  one engine used by both the Work Order and Generic flows.
- FR-08 — Both flows must produce the same request through the same writer, including the audit row.
- FR-09 — Changing an earlier answer must clear downstream answers in both flows, with the same rule.

**Work-centre configuration**

- FR-10 — Marking a work centre generic must be part of the same save that stores the work-centre setup;
  a failure must leave neither partially applied.
- FR-11 — A setup save must be atomic across the four stored-procedure writes, or must report exactly
  which parts were applied.

**Roles and visibility**

- FR-12 — Who may raise a request, and at which work centres, must be stated explicitly rather than
  inferred from the presence of an employee record.
- FR-13 — Every registered page must be reachable, or removed.

### 4.4 Edge cases

- The press loses its job between item selection and submit.
- Two operators submit the same request from different workstations within the same minute.
- Infor Visual is unreachable with a warm cache, and separately with no cache.
- A generic designation write fails after the setup save succeeds.
- A setup save fails after the setup row is written but before the sequence row.
- An item configuration declares no answer label for a step the flow would otherwise ask.
- An operator edits the first answer of a ten-step flow.
- The mirror is refreshed while a read is in flight.

### 4.5 Non-goals

- No demo, sample or mock mode. Internal stores stay live; external reads keep the existing automatic
  cached fallback.
- No change to the stored-procedure boundary. The work moves logic *into* procedures where it belongs
  (duplicate detection, atomic save); it does not add inline SQL to the application.
- No redesign of the visual language of either flow. The screens keep their current look and sequence.
- No new ERP read shapes. The twelve existing shapes stay the contract.
- No role model beyond what the request needs to state its gate. A full permission system is a separate
  feature.

### 4.6 Success criteria

- A duplicate submitted from a second workstation is rejected by the store.
- A part read failure shows "unavailable + retry", never "none".
- The submit guard fails when the bound job has changed.
- `VisualReadRouteAuditTests` passes with no direct-executor exception for the part-match path.
- One flow engine serves both flows; a change to step order is made in one place.
- A failed setup save leaves the store unchanged.
- No registered page is unreachable.

---

## 5. Verification record

Every PDF was checked programmatically before this file was written, when ten of them existed:

- page size exactly 792 × 612 pt (`11 in × 8.5 in` landscape) on every page;
- no text or drawing inside the 0.5 in margin band;
- smallest text on any page ≥ 9.5 pt (node labels and edge labels share one size per sheet);
- each sheet rendered to PNG at 300 DPI (3300 × 2550 px) and inspected for clipped nodes, overlapping
  labels and missing legends;
- shape count per sheet between 10 and 17. The two 10-shape sheets and the two 11-shape sheets are
  genuinely smaller flows (part/sequence and dunnage setup, save, and result), not compressed dense
  graphs; the 17-shape sheets include their plain-language note and `[REFACTOR]` callouts, which occupy
  their own lane cells rather than a row;
- every sheet carried at least one `[REFACTOR]` flag where §3 records a finding, in both the technical
  and the plain-language variant (8 of the 10 friendly sheets, 1 flag each). Both startup-launch flags
  have since been removed, because their findings were fixed (§6).

Of those ten documents, the eight belonging to the four removed request flows went with their flows. The
two startup-launch PDFs remain and still meet every check above.

No `.cs` file, test, or inline SQL was added to the solution for this work; `InlineSqlAuditTests` and
`RetiredSymbolAuditTests` are unaffected.

---

## 6. Startup launch — findings recorded on Diagram 5

Diagram 5 was added after this document's hotspots were written, so its findings are recorded here.
Two `[REFACTOR]` flags carried them: `x_authority` on sheet 1 (the words the setup stage uses) and
`x_offer` on sheet 2 (the people offered on the sign-in stage, which carried three related defects in one
flag because the sheet is at its shape ceiling). The plain-language sheets carried the same two flags.

All four findings were fixed on 25 November 2026, so both flags and their plain-language twins were
removed: sheet 1 keeps 16 shapes and sheet 2 keeps 15, and the plain-language sheets keep 13 and 14. No
`[REFACTOR]` flag remains on Diagram 5. Sheet 1's `[To Sheet 2] SignIn` connector also moved into the
service lane beside `[To Sheet 3] Blocked`, because the `configured? → SignIn` label had been landing on
the machine-readiness box; the flow it describes is unchanged.

### S1 — The startup screens name an Administrator the catalogue retired

`Strings/en-us/Resources.resw` on the machine-setup stage: `Startup_MachineSetup.Subtitle`
("A Developer or an Admin has to tell it where it stands"), `.SignInNameLabel`
("Developer / Admin user name"), `.SignInNameHint` ("The user name of a Developer or Admin account"),
`.NotAuthorized` ("Sign in with a Developer or Admin account to set this computer up") and
`.RefusedSignInRequired` ("Type a Developer or Admin user name and PIN …").

Two other strings on the same stage say something different: `.RefusedNotPermitted` ("Use an account
with Developer or IT Department authority") and `.AbortSignInDeclined` ("Nobody with IT Department or
Developer authority signed in"). `Startup_SignIn.RefusedAttemptsExhausted` adds a third vocabulary —
"Ask an administrator to issue a new temporary PIN."

The gate is `MachineSetupGate`, which authorises on `permission.settings.machine_configuration`; the
baseline for that key is `role:it_department` and `role:developer`. The permission contract retires the
old name outright: "The retired code that must not remain in the catalogue is `admin`. The display name
that replaces it is `IT Department`." Issuing a temporary PIN is `permission.admin.reset_password`,
held by Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT Department and Developer.

A person following the label looks for an account type that no longer exists, and the same screen tells
them a different one in its refusal. The sign-in stage's "an administrator" is the same drift.

**Fixed 25 November 2026.** The five machine-setup values in `Strings/en-us/Resources.resw` now name
Developer and IT Department, and the design behind the change is recorded in
[`name-the-authority-proposal.md`](../ui-designs/startup-launch/machine-setup/name-the-authority/name-the-authority-proposal.md):

| Key | Value now |
| --- | --- |
| `Startup_MachineSetup.Subtitle` | This computer has not been set up yet. A Developer or IT Department user has to tell it where it stands. |
| `.SignInNameLabel` | Developer or IT Department user name |
| `.SignInNameHint` | The user name of a Developer or IT Department account |
| `.NotAuthorized` | Sign in with a Developer or IT Department account to set this computer up. |
| `.RefusedSignInRequired` | Type a Developer or IT Department user name and PIN before unlocking setup. |

`.RefusedNotPermitted` and `.AbortSignInDeclined` already said it and were not touched. The gate, the
permission key, the save and the abort routes are unchanged; the solution builds and the 420 startup
tests pass. `Startup_SignIn.RefusedAttemptsExhausted` ("Ask an administrator to issue a new temporary
PIN") is on the sign-in stage rather than this one and is still open.

### S2 — The set-up panel writes its own English beside localised labels

`MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs` holds `HeadingText`, `NamePromptText` and
`PermissionNoteText` as literal strings, while the seven field labels beside them resolve
`Startup_SetUp.*` keys in `SetUpPanel.xaml.cs`. The panel is therefore half-translated by construction.

`PermissionNoteText` reads "A higher permission level must be requested from IT." "IT" is not a role
in the catalogue (`IT Department` is), and the sentence leaves out the fact that matters to the person:
`sp_core_account_setup_create` assigns the lowest rung of `auth_roles_catalog`
(`ORDER BY role_rank ASC`), and `AccountSetupResult.RoleCode` is returned to the panel and never shown.

**Fixed 25 November 2026.** The three sentences are resource strings now — `Startup_SetUp.Heading`,
`.NamePrompt` and `.PermissionNote` — so the panel is translated like the labels beside it. The note reads
"Your account starts at the lowest level. Ask IT Department.", which names the level the account receives
and the role that can raise it; the other two keep their English wording. The design is recorded in
[`name-the-authority-proposal.md`](../ui-designs/startup-launch/sign-in/name-the-authority/name-the-authority-proposal.md).
`AccountSetupResult.RoleCode` is still not shown anywhere, and the panel's blank-field messages in
`EnterAsync` are still literals; both are separate from the three sentences this finding named.

### S3 — The offer keeps a person who can no longer sign in

`Database/StoredProcedures/sp_auth_user_previous_sign_ins_list/create.sql` joins
`core_users_profiles` without an `is_active` filter. The two other account reads —
`sp_auth_user_credential_get` and `sp_auth_user_row_get` — both carry `u.is_active = 1`. A deactivated
account therefore stays in "Signed in here before" on every computer it once used, and the credential
stage refuses it immediately afterwards.

**Fixed 25 November 2026.** The procedure now carries `AND u.is_active = 1` on the join, in both its own
`create.sql` and `AllSPs.sql`, and its header comment separates the session flag it deliberately ignores
from the account flag it now honours. No test runs the procedure against a store, so the filter is proven
by reading the SQL — the gap is recorded in
[`../living-specs/living-specs-capability-startup-launch.coverage.md`](../living-specs/living-specs-capability-startup-launch.coverage.md).

### S4 — Nothing says whether this is one person's computer or a shared terminal

`core_computers_registry` holds `computer_name`, `hostname_normalized`, `mac_address_normalized`,
`display_name`, `location` and `is_registered`. There is no assigned user and no terminal kind, so the
application cannot tell a desk that one person uses from a terminal that a shift shares.

The read that fills the offer returns at most six rows ordered by `issued_utc DESC` and returns no
time at all. `user_active_sessions` is one row per person per machine and is never hard-deleted, so the
number of rows for one computer is the number of people who have signed in there. A capped read of that
table therefore says "fewer than six" exactly, and six rows means six or more people. That is the only
signal the stage has today, and it is not used.

The effect is the same column on two very different machines. On a dedicated computer it holds one name
beside a form that already asks for a name. On a shared terminal it holds the six most recent of dozens,
with nothing saying when any of them signed in, so the six may all be the previous shift — and the
person who is not among them still has to type. Nothing on the stage distinguishes the two cases.

**Fixed 25 November 2026, in part.** The column now states how many people have signed in on this
computer, in the band the read supports: one person, several, or six or more with "most recent first".
That answers whether this is one person's desk or a shared terminal, from the rows the read already
returns. The remaining question — *when* the top of the list signed in — is still open, because the read
returns no time; it is recorded as a deliberate follow-on in
[`terminal-aware-offer-proposal.md`](../ui-designs/startup-launch/sign-in/terminal-aware-offer/terminal-aware-offer-proposal.md)
rather than drawn as if it existed.

