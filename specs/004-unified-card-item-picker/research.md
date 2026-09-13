# Phase 0 Research — Unified Card and Category/Item Request Picker

Every entry below is a choice the plan had to make that the codebase or the spec did not already settle. Format is
**Decision** / **Rationale** / **Alternatives considered**. There is no outstanding `NEEDS CLARIFICATION`: the spec
carries none, and §16 of the seed (approved 2026-09-13) closed the brainstorm's open list.

## Sources consulted

| Source | What it settled |
|---|---|
| Microsoft Learn — *Menu flyout and menu bar* (`https://learn.microsoft.com/en-us/windows/apps/design/controls/menus`), fetched 2026-09-13 | The sort control is a `MenuFlyout` of `RadioMenuFlyoutItem` elements sharing a `GroupName`, with `IsChecked` on the selected one — the documented WinUI 3 mechanism for mutually-exclusive options. `MenuFlyoutItem`/`RadioMenuFlyoutItem` carry `Text`, which is where the localized string binds. |
| CommunityToolkit.Mvvm docs (Context7 `/websites/learn_microsoft_en-us_dotnet_communitytoolkit_mvvm`) | `[RelayCommand]` on `private async Task XAsync()` generates `XCommand` — the trailing `Async` is stripped. Confirms the repo's recorded XAML-binding trap rather than contradicting it; every new command binding follows it. |
| Repo code read in Phase 0 | `RequestItemCatalog`, `RequestItemDefinition`, `RequestCategory`, `RequestJobPartAvailability`, `RequestItemPickerRules`, `NewRequestPickerService`, `NewRequestCanonicalPicker`, `NewRequestFlowRules`, `NewRequestFlowService`, `RequestTypeCatalogService`, `WaitlistLineCardView.xaml`, `WaitlistRequestTitles`, `WaitlistRequest`, `WaitlistRequestDraft`, `WaitlistViewViewModel`, `UrgencyCalculator`, `UrgencySettingsService`, `UrgencyDeadlineService`, `ImageLocationService`, `ImageOverrideDialogViewModel`, `RequestSubtypeImagesDialogViewModel`, `RequestItemLegacyMapper`, `ActiveJobItemResolverService`, the three waitlist request procedures, and the four catalog/queue DDL artifacts. |
| `WeekendProject/Documents/Request-Config-Template.csv` and `unified-item-picker-workflows.md` | The per-Item field definitions (FR-027's sole source), each Item's availability rule, and the answer each Item captures. |

**MCP note.** The Microsoft Learn MCP tools report *"currently disabled by the user"* in this session, which the
repository's own troubleshooting record attributes to a stale tool snapshot rather than a configuration gate. The
constitution's requirement is to ground the answer, not to pick the tool, so the same Microsoft Learn page was
fetched over the web instead. Context7 answered normally.

---

## D1. Re-key the existing Category/Item layer rather than build a new one

**Decision.** Treat `RequestItemCatalog`, `RequestCategory`, `RequestItemPickerRules`,
`RequestJobPartAvailability` and `INewRequestPickerService` as the feature's foundation and change what feeds them,
rather than introducing a parallel picker.

**Rationale.** They already exist, are already DI-registered (`ServiceRegistrationExtensions`), already have tests
(`RequestItemPickerRulesTests`, `NewRequestPickerServiceTests`, `NewRequestCanonicalPickerTests`), and the Job Type
page already draws their output. The spec's Key Entities — Category, Item — map onto `RequestCategory` and
`RequestItemDefinition` exactly. The gap is that the wizard's *metadata* still comes from the retiring tree, and
that `NewRequestCanonicalPicker` exists only to group that tree into canonical items, which stops being necessary
once the tree is gone.

**Alternatives considered.** A green-field picker service was rejected: it would duplicate a tested, registered,
already-consumed surface and leave two Item catalogs to keep in step. Keeping
`NewRequestCanonicalPicker`'s tree-grouping as a permanent indirection was rejected because its entire purpose is
to read `category` / `item_id` off rows that this feature deletes — with the tree gone it would be a pass-through.

## D2. One new table, one row per Item, holding behaviour *and* allotted minutes

**Decision.** Add `waitlist_request_item_configs` with one row per Item. It carries the flow
(`direct-to-confirmation` vs `collect-input-then-confirm`), whether an answer is required, the answer's value type,
the prompt, the minimum and maximum length, the options, the ordered detail fields as JSON, and
`allotted_minutes`.

**Rationale.** FR-014 is "every Item MUST have a configuration row" in the singular, and the spec's own Key
Entities separate *Item configuration* from *Item setting* only conceptually — both are per-Item and both are one
row. Keeping them together makes the missing-row case single and unambiguous: one absent row means the Item is
unavailable, and there is no state where an Item is half-configured because its behaviour row exists and its
minutes row does not. It also removes the biggest source of the wizard's current fragility, which is that flow
metadata lives on two tables (`waitlist_request_types` *and* `waitlist_request_subtypes`) and has to be merged at
read time.

**Alternatives considered.** *A second `waitlist_request_item_settings` table* — rejected: it creates the
half-configured state FR-014 forbids, and its only real content would be one integer column. *Re-keying
`waitlist_request_subtypes` in place* — rejected: FR-023 requires that table's removal, and leaving it alive while
renaming what it means keeps the retiring vocabulary in the schema master lists. *Leaving the flow in code* —
rejected by FR-013/FR-015 and §16.15, which require the field set to change as data with no rebuild.

## D3. The card's two lines stay in the code catalog, not in the configuration table

**Decision.** The umbrella phrase (Line 1) and the identifier template (Line 2) remain properties of the Item's
code-catalog row — `RequestItemDefinition` keeps `UmbrellaVerb` and gains `CardLine2Template`, which carries the
CSV's `Card Line 2 (identifier)` as a `{token}` template plus the one conditional alternative the die needs
(`{die_number}`, or `{die_location}` when the captured destination is `Home Location`). The configuration table
holds the *behaviour*; the catalog holds identity, order, Category, umbrella verb and Line 2.

**Rationale.** §16.3 is explicit: the metadata table holds "what lives on the type/subtype rows today" — control
flow, requires-text-input, prompt, min/max, the options list and the detail fields — and "the code catalog still
owns existence, order, Category, umbrella verb and Line 2". The retiring type/subtype rows never carried a
second-line template, so moving one onto the new table would be new behaviour, not a re-key. That also matches
FR-013, whose list of what must come from stored configuration is the flow, the answer requirement, the prompt, the
limits, the options and the fields — the card's two lines are not in it. The catalog is already the design mirror
of the CSV's twenty-three rows and already asserts them, so Line 2 is where a reviewer would look for it.

**Alternatives considered.** *A `line2_template` column set on the configuration table* — rejected: it contradicts
§16.3, would leave two candidate owners for one display value, and would make the retired table's replacement
carry a fact the retired table never held. *Deriving Line 2 in the view model from `ProducedValue`* — rejected:
`ProducedValue` is the value the request *stores*, which is a different thing from the identifier the card
*displays*, and the die's two forms prove they diverge. *A general expression evaluator* — rejected as a security
and reviewability liability for one conditional rule.

### The token set Line 2 may use

Closed by design so an unknown token is a reportable configuration error rather than a silent blank. Job-derived
from the active-job snapshot: `part_number`, `part_description`, `die_number`, `die_location`, `dunnage_part`,
`sequence_number`, `scrap_type`. Captured during the flow: `answer`, `destination`, `component`, `defect`.

## D4. The per-Item allotted minutes move out of local settings and into the store

**Decision.** The Item's allotted minutes become a column on the configuration row, seeded for every Item, written
through `sp_waitlist_request_item_allotted_minutes_update` and read through the configuration read.
`UrgencySettingsService` keeps its role as the deadline service's collaborator but stops being the source of truth.

**Rationale.** FR-018 puts the configured value and the observed average side by side on one screen, and FR-024
requires the observed average to come through a stored procedure. Two numbers that are meant to be compared cannot
have one of them keyed to a Windows user profile: today `UrgencySettingsService` writes
`Urgency.MaxAllottedMinutes.<subtype>` into `%LOCALAPPDATA%` through `ILocalSettingsService`, so the configured
value is invisible to anyone else and would not appear on any other workstation. §16.7 and §16.8 say every Item is
seeded with a default allotment and name the per-Item allotments as a seed, which is only meaningful if they live in
the store.

**Alternatives considered.** *Keep local settings and add a separate DB read for the average* — rejected: the
screen would compare a per-user value with a shared value, and a fresh profile would show a default beside a real
average with no way to tell which was which. *Store minutes per-user in the database* — rejected: no requirement
asks for per-user minutes, and the role gate (FR-020) already means one configuration for everyone.

## D5. The 15-minute fallback stays app-side and is labelled as a default

**Decision.** The fallback for an Item with no configured minutes is 15 minutes, held as a constant in the app
(`UrgencySettingsService.DefaultMinutes` becomes 15, was 30), and the surface that shows a deadline for such an
Item marks the value as a default rather than as configured.

**Rationale.** FR-017 requires the fallback and the label; the Verbatim Constraints pin the number. Keeping it
app-side means an unreachable store still produces a sane deadline instead of no deadline, which is what keeps the
request in the urgency order (SC-007). The value is *not* written back — §16.13's Q10 consequence is explicit that
the configured and observed values are different numbers with different meanings, and the same discipline applies
here.

**Alternatives considered.** *Seed every Item so the fallback is unreachable* — rejected: §16.7 seeds every Item
*and* keeps the fallback, because the fallback is what covers a row that fails to load. *Make the fallback a
configuration row of its own* — rejected: a fallback that requires a successful read is not a fallback.
*Leaving the default at 30* — rejected: the Verbatim Constraints state 15.

## D6. The observed average is one stored procedure over `accepted_utc` → `completed_utc`

**Decision.** `sp_waitlist_request_item_observed_average_get` returns, per Item, the average of
`completed_utc - accepted_utc` over requests whose status is `Completed`. It groups by the request's Item, ignores a
request with a missing endpoint, and — where a request was released and later completed — counts the final
accepted → completed pair once.

**Rationale.** FR-019 fixes both endpoints and the released-then-completed rule, and §16.13's Q11 consequence fixes
the *final* pair rather than a sum of attempts. The request queue already stores `accepted_utc` and `completed_utc`
as single columns, so "the final pair" is simply the stored pair — a release that is followed by a completion
overwrites `accepted_utc`, and a release with no completion has no `completed_utc` and is excluded by the status
filter. Doing it in one procedure keeps it reusable by the analytics workstream, which §16.13 asks for explicitly.

**Alternatives considered.** *Computing it in the app from `sp_waitlist_request_list`* — rejected by FR-024 and by
accuracy: the list procedure is scoped to one building and to unresolved requests by default, so it would average
the wrong population. *A time window* — rejected: the Assumptions say the average covers all completed requests.

## D7. The sort preference is per viewer, through the existing local-settings mechanism

**Decision.** Add `IWaitlistSortPreferenceService` backed by `ILocalSettingsService`, storing one of five keys —
most urgent (default), longest waiting, press, requested by, status — and applied on load.

**Rationale.** FR-011 requires the choice be remembered for the viewer, and §16.13's Q20 settles *per person,
always*, and names the existing local-settings mechanism rather than a new one. `LocalWaitlistMessageSeenStore`
already establishes the pattern of a small store over `ILocalSettingsService` for exactly this kind of per-viewer
memory, and `UrgencySettingsService` shows the scalar-key style that works in both the MSIX and unpackaged
branches.

**Alternatives considered.** *Mirroring `BuildingSelectionService`* — rejected: it is an in-memory singleton, and
SC-009 requires the choice to survive a restart. *A per-user column in the database* — rejected: no requirement
asks for it, and it would put a display preference into the store the constitution keeps for operational data.
*Storing it in `LocalSettings.json` by hand rather than through the service* — rejected: `ILocalSettingsService`
already handles the MSIX/unpackaged split.

## D8. Ordering lives in `UrgencyCalculator`, with most-urgent-first unchanged as the default

**Decision.** Extend `UrgencyCalculator` with the four additional order keys beside `OrderMostUrgentFirst`, and keep
that method as the default path. The overloads order over the row's already-computed `UrgencyState` plus the
metadata the card shows — requested-by, press, status — so no ordering rule needs a second data read.

**Rationale.** `UrgencyCalculator.OrderMostUrgentFirst` is already the one home for the ordering rule and is
already unit-tested; `specs/003`'s FR-015 is amended from "MUST be ordered most-urgent-first" to "defaults to
most-urgent-first", and its two ordering tests become the default-order tests, which is far cheaper than a new
ordering service. Every sort key is a value the row already carries for the card, so sorting never adds a query.

**Alternatives considered.** *A separate sort service* — rejected: two homes for one rule, and the urgency rule
would have to be re-expressed. *Sorting in XAML (a `CollectionViewSource`)* — rejected: the row set is rebuilt on
each load and the order must be identical when the list refreshes.

## D9. The 'Other' card's full-width grid span

**Decision.** The card is one layout for every Item, with **no Item-selected span** (FR-006). The design document's
"full-width 'Other' card on Row 1 with RowSpan = 2" is read as a property of the **detail-field grid**, which this
feature moves off the card and onto the request page (§16.5). On the page the field grid lays the declared fields
out two per row in declared order, and a field that is alone on its row takes the full width of that row. `other`
therefore renders its single declared field full width — derived from the *declared field count*, never from the
Item's name.

**Rationale.** FR-006 is unconditional — "no layout variant may be selected by Item" — and US2 explains why: one
shape for every request. A span chosen by Item identity would be precisely the variant FR-006 forbids, and the
checklist already recorded that this is a plan-level layout value rather than a business requirement. Deriving the
span from the field count keeps the design document's *intent* (a lone field is not cramped into half a row) while
making the rule general: any Item configured with an odd field count gets the same treatment, which is what
"the field set is mutable by design" (§16.15) requires.

**Alternatives considered.** *Special-case `other` with a `RowSpan="2"`* — rejected: it is a layout variant
selected by Item, contradicting FR-006, and it would silently break the first time `other` gains a second field.
*A `MaxWidth`/`MinWidth` pair to stretch the lone field* — rejected by constitution V's no-hardcoded-boundaries
rule and because a span is a span.

## D10. Pictures are keyed by Item, inheriting the Category's family

**Decision.** Add `request_item` and `request_category` values to `ImageLocationScope` and to the
`config_images_locations.scope` column (a free-form string, so this is data, not DDL). The card resolves
Item override → Category family → the existing default placeholder, and `"nothing configured"` never overwrites an
image the request already resolves (FR-021). `RequestSubtypeImagesDialogViewModel` is **renamed and re-pointed** at
Item rather than recreated; the `request_type` / `request_subtype` scopes and their dialogs retire with the
vocabulary.

**Rationale.** FR-009 says the picture is the Item's, falling back to the Category's family — a two-hop inheritance,
which is exactly the shape `ImageOverrideDialogViewModel` already implements through `SupportsGrouping` and
`SupportsInheritance`. Reusing it keeps the storage service, the read/write services, the storage root and the
per-row batched commit path untouched, which is the difference between a rename and a second image subsystem.
§16.14's self-review corrected the card's picture from Category to Item and explicitly kept this dialog, so this is
the design of record rather than an interpretation.

**Alternatives considered.** *Reusing the `request_type` scope for the Category family* — rejected: the four
Categories (Pickup, Deliver, Assist, Other) are not in `RequestTypeInventory`, whose eight entries are the legacy
request types, so the key would have to be a lie. *A new image table for Items* — rejected: it duplicates
`config_images_locations`, its ten procedures and its storage root. *Reading the picture from the code catalog* —
rejected by FR-021.

## D11. Availability is checked before the list is built, and the out-of-scope Items are excluded by rule

**Decision.** The composition root maps `IActiveJobItemResolverService` onto `RequestJobPartAvailability` once, when
the Item step is entered, and the Item list is assembled from
`INewRequestPickerService.GetVisibleItems(category, availability)` — the filter runs *inside* the build, so an
unsupported Item is never constructed, never bound and never offered. The four Items with no value source
(`pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service`) are excluded by the visibility rules, not by
their absence from the catalog.

**Rationale.** FR-002 is emphatic that the check runs before the person can choose, "never after, so an unsupported
Item is never offered and then refused" — the workflow document's first draft had it the other way round and the
spec was refreshed specifically to fix it. FR-028 needs the four sourceless Items *hidden but catalogued*, because
they remain part of the canonical twenty-three that the catalog's own tests assert; removing them from
`RequestItemCatalog` would break the "23 rows" invariant and the CSV mirror.

**Alternatives considered.** *Filtering the bound collection after binding* — rejected: it offers an Item and then
withdraws it, which is the exact defect FR-002 names. *Deleting the four Items from the catalog* — rejected:
`RequestItemCatalog` is the design mirror of the CSV's twenty-three rows, and their own spec will need them.

## D12. Scrap availability reads the job's scrap decision through the existing predicate

**Decision.** Offer `pickup-scrap` only when the requesting job carries a *real* scrap decision — a value that is
set, is not `No Scrap`, and is not the `Scrap Type Required` placeholder — evaluated through the codebase's
canonical `HasScrapDecision` predicate and the constants in `SetupWorkflowService`.

**Rationale.** FR-031 and the Edge Cases both turn on the placeholder being persistable: the workflow falls back to
`Scrap Type Required` whenever nothing has been saved, so a naive "is it non-empty" test would offer a Scrap request
for a job where no decision was made. `No Scrap` is a real answer with nothing to collect. The spec names
`HasScrapDecision` as the canonical predicate, so the plan reuses it instead of re-deriving the three-way test —
which also means a change to the placeholder constant cannot silently diverge between Setup and New Request.

**Alternatives considered.** *A second predicate in the picker rules* — rejected: two implementations of one
three-way rule will drift. *Reading the scrap value from the request-configuration table* — rejected: the scrap
type is job state, not Item configuration; only the *visibility rule* belongs to the picker.

## D13. Field definitions are parsed in the app, not in SQL

**Decision.** The configuration read returns `detail_fields_json` as a string and the app deserializes it with
`System.Text.Json`, exactly as `RequestTypeCatalogService.ReadStringList` already does for
`center_data_grid_fields_json`.

**Rationale.** The schema targets MySQL 5.7, where `JSON_TABLE` is unavailable, so a relational unpacking of the
field list would need a second table and would make "add a field" a three-artifact change instead of one row edit —
against FR-015. The existing code already establishes this exact pattern for this exact column shape, so it is the
repo's convention rather than a new one.

**Alternatives considered.** *A `waitlist_request_item_fields` child table* — rejected: it turns a data change into
a schema change, and the field set is deliberately mutable. *`JSON_TABLE`* — unavailable on the target engine.

## D14. Item and Category display text moves to the resource mechanism

**Decision.** `RequestItemDefinition` keeps the Item's identity, order, Category, umbrella verb and value type, and
gains a resource key for its display name; the four Category names likewise resolve from resource keys. The literal
display strings leave the catalog.

**Rationale.** FR-022 requires every user-visible string introduced or changed to come from the existing resource
mechanism, and the Item step, the Category step and the card all show these names. They are *changed* by this
feature — `"Coil or Flatstock"` now labels a merged pickup Item, `"Wrong Coil"` now reads as a Deliver of the
correct coil — so the exceptions in FR-022 do not cover them. The spec's Assumption that display text is "asserted
by tests as they are today" still holds: the tests assert the resolved text, which is what they assert today.

**Alternatives considered.** *Leaving the literals and arguing they are pre-existing* — rejected: several of them
are re-worded by this feature, and the repo's rule is that a changed string is a localized string. *Putting display
text in the configuration table* — rejected: FR-013's list of configurable things does not include identity text,
and the catalog is the catalog.

## D15. Retirement order — consume the legacy mapping at design time, then remove

**Decision.** Author the new seed using the populated `category` / `item_id` columns on
`waitlist_request_types` and `waitlist_request_subtypes` as the design-time mapping from each existing request to
its Item, then remove both tables, `sp_waitlist_request_types_get`, `sp_waitlist_request_subtypes_get`,
`seed_waitlist_request_catalog`, `RequestSubtypeInventory`, `RequestTypeCatalogService`,
`RequestSubtypeDisplayLabelService` and the request's two legacy columns — with the master lists and
`update_table_descriptions.sql` updated in the same change.

**Rationale.** §15.3's finding 2 records that this mapping already exists and is fully populated, and §16.8 and
§16.14 both insist the consumption happens **at design time**, when the seed is written, so it cannot be misread as
the migration code §16.2 rules out. Nothing at runtime reads the legacy mapping. §16.9 also records two symbols
that must be *inspected, not assumed dead* — `RequestItemLegacyMapper` (which may still back the seed rewrite) and
`ImageLocationScope` (work-centre images may still use it) — so neither is deleted on the strength of its name.

**Alternatives considered.** *Deleting the legacy tables first and hand-writing the mapping* — rejected: it
throws away a populated, authoritative mapping to retype twenty-four rows. *Keeping the tables as historical
reference* — rejected: FR-023 and FR-025 require their removal and the master lists kept in sync, and
`RetiredSymbolAuditTests` exists to make a return fail the build.

## D16. Details renders from configuration, with no per-Item code path

**Decision.** The Details step reads the Item's configured flow and fields and renders accordingly: nothing for a
pure flag, nothing for a job-derived Item, one option pick where the configuration declares an enumerated answer,
one text input where it declares a text answer with limits, and a message where the Item carries its own text.
`NewRequestDetailsViewModel` stops branching on `RequestType` / `Subtype` and branches on the configuration row.

**Rationale.** FR-013 lists the flow, the requirement, the prompt, the limits, the options and the fields as
configuration; §16.4 enumerates the per-Item renderings and §16.15 states that no test may assert a frozen field
list, only that fields are read from configuration in declared order with the right value types and labels. A
`switch` on Item id would satisfy the same screens today and fail the requirement the moment a field changes.

**Alternatives considered.** *A per-Item XAML view, as the card does today* — rejected: it is the fifteen-card
problem moved into the wizard, and it makes the field set a build artifact. *A `DataTemplateSelector` keyed by
Item* — rejected for the same reason.

## D17. The per-type cards and their selector are deleted, and the freeze is lifted in writing

**Decision.** Delete the seven per-type card views, their models, their view models and the selector that chooses
between them; the list renders `WaitlistLineCardView` for every row. Record the supersession in
`specs/003-waitlist-handler-fulfilment` — its "DO NOT REGRESS" card-anatomy constraint and its matching test are
rewritten to the new anatomy rather than deleted, and its FR-015 is amended from "MUST be ordered most-urgent-first"
to "defaults to most-urgent-first".

**Rationale.** FR-003, FR-006 and the spec's Assumptions all require this, and the Assumption is explicit that the
previous spec's freeze is "superseded — amended as a recorded supersession, and rewritten to the new shape rather
than deleted, so an unintended change still fails". Deleting the constraint outright would remove the only guard
against an unintended card change.

**Alternatives considered.** *Keeping the per-type views behind a flag* — rejected: a layout variant selected by
Item, and the repo forbids feature toggles that change what is shown. *Leaving `specs/003` untouched* — rejected:
two specs asserting opposite card anatomies, with the older one still claiming authority.

## D18. Seeds for the eight job configurations are built in the writer's shape and round-tripped

**Decision.** One seed creates active jobs for eight work-centre configurations covering every visibility
condition — coil, flatstock, die, components, dunnage, everything at once, a job with no subordinate parts, and a
work centre with no active job — writing `subordinate_parts_json` in the same shape `sp_setup_save_setup` stores.
A test round-trips the seeded JSON through `ActiveJobItemResolverService`'s own deserializer and the
`SetupSubordinatePart` normalised Category.

**Rationale.** §15.3's finding 1 records that `setup_active_jobs` is empty on this workstation, so the picker's
filter has nothing to read and SC-004's "proven for each of the eight job configurations, not by inspection" is
unreachable without this seed. §16.8 requires the JSON be written in the writer's shape because the read path
normalises `MMC→Coil`, `MMF→Flatstock`, `FGT→Die` from the part-number prefix, so a hand-written JSON blob with a
`category` field alone would not exercise the real path. The round-trip test is the cheapest way to make the seed
and the reader agree rather than assume it.

**Alternatives considered.** *Seeding only one job and asserting the rule* — rejected by SC-004 and §16.10, which
make the matrix a gate. *Pulling the user's local database and describing it* — rejected: it is not reproducible
and does not prove the visibility rule.

## D19. FR-027 is enforced by a test, not by intent

**Decision.** Add an audit alongside the existing `InlineSqlAuditTests` / `RetiredSymbolAuditTests` that fails the
build if any source file, project file or build target references `Request-Config-Template.csv` or
`unified-item-picker-workflows.md`.

**Rationale.** FR-027 and the spec's Assumptions forbid the application from reading, embedding, parsing or
shipping the spreadsheet and forbid any build step from depending on it — and §16.15 makes the same promise about
the workflow document. Both files sit inside the repository under `WeekendProject/Documents/`, which is exactly
where a well-meaning future change would reach for them, so the promise needs a guard and not a comment. The repo
already has the pattern: two audit tests exist to fail the build when a retired symbol or an inline statement comes
back.

**Alternatives considered.** *A comment in the seed and a note in the plan* — rejected: this is the same class of
promise as FR-023's retirement, which the repo already backs with a test. *Moving the CSV outside the repository* —
rejected: it is a document of record and belongs with the other design documents.

## D20. The job-independent set is five, and a Category that would offer nothing is not offered

**Decision.** Two things, both forced by the spec rather than chosen.

1. The Items that depend on nothing about the job are **five**: `pickup-riser-table`, `deliver-riser-table`,
   `pickup-hopper`, `deliver-hopper` and `other`. `pickup-scrap` is **not** among them — it is gated on a real scrap
   decision (§D12), and a job whose scrap value is `No Scrap` or still the `Scrap Type Required` placeholder has
   nothing to collect.
2. The **Category step is filtered by the same availability pass as the Item step**, so a Category that would offer
   no Item at all is not offered either.

**Rationale.** The spec's own Edge Case settles the first point in so many words — "after the scrap correction that
set is **five**: both Riser Table Items, both Hopper Items, and Other" — and the seed's §16.4 still says *six*,
which is the pre-correction count. The spec is the authority here and was refreshed today.

The second point is the necessary consequence, and it is the only way FR-002's "MUST never be empty" and SC-005's
"offers at least one Item in every case" can both hold. Neither Riser Table nor Hopper nor Other belongs to
**Assist**, whose three Items each require a part on the job: `assist-coil-turn` needs a coil, and
`assist-table-place` and `assist-table-remove` each need some subordinate part. On a job with no parts, or on a work
centre with no active job, an unfiltered Assist Category would open an **empty** Item step — the exact state
FR-002 forbids and SC-005 measures. Filtering the Category list means every Item step a person can actually reach
has at least one Item in it.

**Alternatives considered.** *Leaving the Category unfiltered and showing an empty step with a message* — rejected:
it contradicts FR-002 and SC-005 outright, and offering a person a Category they cannot use is the
"offered and then refused" defect FR-002 was written to stop. *Adding a job-independent Assist Item* — rejected: it
changes the twenty-three-row catalog that the spec pins verbatim and that its own tests assert, so it is a decision
for the owner and for a different specification. *Treating `Scrap` as job-independent to keep the count at six* —
rejected: it is exactly the mistake the spec was refreshed to fix, and FR-031 forbids offering Scrap without a real
decision.

## D21. The merged pickup Item requires a coil **or** flatstock, which the current rule gets wrong

**Decision.** Correct `RequestItemPickerRules.RequiredJobPart` so the merged pickup Item is visible when the
requesting job has a coil **or** flatstock, and delete its dead reference to an Item code that does not exist.

**Rationale.** Two defects in the existing rule surface, both found while reading it against the specification.

- `pickup-coil` is the CSV's *merged* Pickup row: its `Item (display)` is `Coil or Flatstock`, its source column reads
  `MMC=>Coil, MMF=>Flatstock`, its Notes column says "Visibility: show only if requesting job has MMC/MMF
  subordinate", and its Line 2 is "coil number (part_number)". The rule currently maps it to `RequestJobPartKind.Coil`
  alone, so a job that has **only** flatstock does not get the Pickup Coil or Flatstock Item — a supported Item
  withheld, which is the second half of FR-002 and is measured by SC-004 ("zero supported Items are withheld —
  proven for each of the eight job configurations").
- The same switch carries an arm for `pickup-flatstock`, which is **not one of the twenty-three Item codes** in the
  spec's Verbatim Constraints and not in `RequestItemCatalog`. The arm is unreachable today, but it advertises an
  Item that does not exist and would become reachable the moment someone "fixed" the catalog to match it.

**Alternatives considered.** *Leaving the rule and widening the seed so no job has flatstock without a coil* —
rejected: SC-004 requires the flatstock-only configuration to be proven, and the eight-configuration seed (§D18)
includes a flatstock job. *Splitting Pickup back into two Items, coil and flatstock* — rejected: the spec pins
twenty-three codes and `pickup-coil`'s merged display name, and the configuration spreadsheet is the sole source of
per-Item field definitions.
