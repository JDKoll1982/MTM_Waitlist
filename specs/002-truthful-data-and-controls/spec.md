# Feature Specification: Truthful Data and Truthful Controls

**Feature Branch**: `002-truthful-data-and-controls`  
**Created**: 2026-09-12  
**Status**: Draft  
**Input**: User description: "Close the gap between what the MTM Waitlist application displays and what it can actually support. A full inventory of the running application found ten verified defects, and the rule for this feature is that the application must never display a value it cannot substantiate and must never offer a control it cannot honour: where the real value or real behaviour does not exist yet, the field or the control is removed or marked clearly unavailable rather than filled or shown as if functional, and where wording (on screen, in a resource file, or in a code comment) describes behaviour that was deleted, it is corrected or deleted. The ten defects are: (1) every waitlist card and request detail page shows hard-coded material values — coil number, part numbers, on-hand quantity, customer, packlist, traceability identifier, WIP destination, outside-service vendor, die number, quantity — for every real request, so an operator can act on a wrong coil, part, customer or vendor; (2) every card draws a red Cancel and a green Accept button that have no behaviour at all; (3) the New Request confirm screen shows a fixed '0 active request(s) for this work center' and 'Estimated wait time: approximately 15 minutes' for every user and every work center; (4) the average coil weight is always looked up for one fixed part and falls back to a fixed 5,000 lb, so it never describes the coil on the request; (5) the New Request Alerts switch cannot fire because the application is installed in a way Windows notifications do not support, yet the control is offered as if it worked; (6) three separate labels in Settings share one resource key, so all three read 'About this application'; (7) the Privacy Policy link opens an unfinished placeholder address; (8) three Settings panels ignore the settings search because they are never told the search changed; (9) the application still ships a 'Setup saved using sample data' message and code comments describing the retired sample/mock mechanism; (10) tapping a new-request notification while the application is already running shows an internal 'TODO' dialog instead of the request. Each defect must end with the application telling the truth about what it knows and what it can do; the fields and controls that cannot yet tell the truth are removed or disabled rather than populated with placeholder content, and the feature specifications that follow this one restore the real values and the real behaviour (item resolution, the handler workflow, analytics, and notification delivery). Documentation that describes the retired sample/mock system — including the backlog and changelog files that still present it as current — must be corrected or retired in the same change so no reader is sent to re-implement deleted behaviour."

## Clarifications

### Session 2026-09-12

- Q: Empty block — hide it or explain it? → A: Hide a legitimate absence (a work order / part / operation pair with no coil hides the coil card); show the error when a read fails.
- Q: Must the value-source audit cover every screen in the shipping application? → A: No — the audit is removed from this feature, which it made too congested. The scope is the ten defects and the surfaces they name.
- Q: Failure or absence? → A: A read that throws or reports an error is shown; a read that succeeds and finds no matching row hides the block.
- Q: Where does a block failure appear? → A: In the failing block, with a retry; the rest of the page renders normally and the existing whole-screen unavailable state keeps its meaning.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The waitlist tells the truth about a request (Priority: P1)

An operator opens the waitlist and a request from it. Every value shown belongs to that request; where no real source exists for an attribute, no value is shown and no button is offered that does nothing.

**Why this priority**: A physical-safety defect. An operator reading a fabricated coil number, part, customer, vendor or die number can carry the wrong material to the wrong destination, and an operator who presses an inert Accept cannot tell whether the request was accepted, so the queue silently diverges from the floor.

**Independent Test**: Open the waitlist and a request detail page for a request whose material attributes are not yet resolvable. Confirm no material value appears unless it traces to that request or to an authoritative read for it, and no control appears that performs no action.

**Acceptance Scenarios**:

1. **Given** a real request on the waitlist whose material attributes cannot be sourced, **When** the operator views its card, **Then** no coil, part, quantity, customer, packlist, traceability, destination, vendor or die value is displayed for it.
2. **Given** the same request, **When** the operator opens its detail page, **Then** the page shows only attributes that have a substantiated source, and shows no status text or attribute that no source produces.
3. **Given** an operator viewing any waitlist card, **When** they look at the action area, **Then** no control is offered whose activation has no effect.
4. **Given** an operator whose view is served from the cached fallback because the external system is unreachable, **When** they view a request, **Then** the cached attribute values are still displayed and the indicator still reports that the data is cached, with age.
5. **Given** the same request viewed twice, **When** the underlying values have not changed, **Then** the displayed attributes are identical (no value varies between views).
6. **Given** a request whose coil read fails, **When** the operator opens that request's detail page, **Then** the coil card shows the error with a retry action, nothing about the failure is presented as an absence, and the rest of the page renders normally.
7. **Given** a request type that genuinely has no coil, **When** the detail page renders, **Then** the coil card is hidden rather than shown empty.

---

### User Story 2 - New Request feedback tells the truth (Priority: P2)

A requester works through New Request. The confirm step reports only figures the application can substantiate for their work center and request type, any coil weight describes the coil being requested, and the alerts control is honest about whether this installation can deliver a notification.

**Why this priority**: The confirm step is the requester's last chance to catch a mistake: a fixed queue count and a fixed "approximately 15 minutes" are read as facts about the shop and decide whether they submit now or walk away, and a weight resolved for an unrelated part is simply wrong. The alerts switch silently accepts a setting the application cannot honour.

**Independent Test**: Run New Request to the confirm step for two work centers and two request types: no queue count and no wait estimate appear, no coil weight was resolved for a part other than the request's, and the alerts control states its unavailability with the reason.

**Acceptance Scenarios**:

1. **Given** a requester on the confirm step, **When** the step renders, **Then** no active-request count and no estimated wait time are shown, and no stand-in figure or empty card is displayed in their place.
2. **Given** two requests for different work centers, **When** the confirm step renders for each, **Then** no displayed figure is a constant shared by both unless a real source produced it (a regression guard: after this feature the step is expected to show no such figure at all).
3. **Given** a request for a coil, **When** the confirm step renders, **Then** no average coil weight is shown unless it was resolved for the part on that request, and no fixed fallback weight appears in any state.
4. **Given** an installation that cannot deliver notifications, **When** a requester reaches the alerts control, **Then** the control is disabled and accompanied by the reason, and no preference is silently stored for it.
5. **Given** an installation that can deliver notifications, **When** a requester reaches the alerts control, **Then** it remains usable exactly as before.

---

### User Story 3 - Settings tells the truth and searches completely (Priority: P3)

A user opens Settings. Each label shows its own text, the privacy entry leads somewhere real or is not offered, and a search surfaces every matching panel including Image Location Settings, New Request Alerts and Max Allotted Time.

**Why this priority**: Trust and compliance, not safety. Three labels all reading "About this application" make the page unreadable, a privacy link to an unfinished address is a compliance exposure, and a search that silently omits three panels teaches users it cannot be relied on.

**Independent Test**: Open Settings and read the section header, the version card and the about card; follow the privacy entry; then search a term matching only each of the three previously unsearched panels.

**Acceptance Scenarios**:

1. **Given** the Settings page, **When** the user reads the section header, the version card and the about card, **Then** each displays its own distinct, correct text.
2. **Given** the privacy entry, **When** the user activates it, **Then** it either opens a real, published policy destination or is not offered at all; no placeholder address ships in any shipped text.
3. **Given** the user types a search term that matches only one of Image Location Settings, New Request Alerts, or Max Allotted Time, **When** the term changes, **Then** that panel reflects the search just as every other search-aware panel does.
4. **Given** the search is cleared, **When** the term becomes empty, **Then** every panel returns to its unfiltered state, including the three above.
5. **Given** a future panel is added that consumes the settings search, **When** it is not wired to receive search changes, **Then** the automated suite fails.

---

### User Story 4 - Opening a notification while running behaves like a cold start (Priority: P4)

An operator taps a new-request notification while the application is already running. The request it names opens, exactly as the cold-start path already does, and nothing internal appears. An activation that cannot be mapped to a request does nothing visible.

**Why this priority**: Low frequency while the application ships unpackaged (a notification cannot fire today), but the failure is a visible internal artefact in a production-facing surface and the correct behaviour already exists on the other activation path, so the fix is bounded. Delivering notifications at all belongs to a later specification.

**Independent Test**: While the application is running, trigger an activation with a recognised argument and with an unrecognised one: the first opens the named request as a cold start would, the second produces no visible UI.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** a new-request notification naming a request is activated, **Then** that request's detail page is shown, exactly as activation from a cold start does, and no internal placeholder or developer-facing dialog is displayed.
2. **Given** an activation whose arguments cannot be mapped to a request, **When** it is processed, **Then** nothing visible happens, the event is recorded for diagnosis, and the application window is brought to the front.
3. **Given** the same activation is repeated, **When** it is processed again, **Then** the result is the same and no dialog accumulates.

---

### User Story 5 - The repository describes only what still exists (Priority: P5)

A developer or reviewer reads the changelog, the backlog and the module task files. Nothing presents the retired sample/mock mechanism as current, no shipped message or comment refers to deleted behaviour, and the counting traps that inflate the backlog are marked retired.

**Why this priority**: Lowest operational impact, but it is the mechanism by which deleted behaviour gets rebuilt: every reader sent to re-implement a removed toggle costs a rewrite and a review cycle.

**Independent Test**: Read the affected documents and shipped text and confirm no statement presents the removed manual external-system toggle, sample rows or mock mode as current.

**Acceptance Scenarios**:

1. **Given** a reader opening the changelog entry for the removed manual external-system toggle, **When** they read it, **Then** it is corrected to describe the actual current behaviour or is retired.
2. **Given** the backlog index, **When** a reader reaches the retired sources, **Then** each is labelled retired with the reason it must not be reopened.
3. **Given** the module task file that is behind reality, **When** a reader opens it, **Then** its completed items are reconciled so its count matches what is actually finished.
4. **Given** any shipped user-facing text, resource entry or code comment, **When** it is read, **Then** no wording describes the retired sample/mock mechanism as live.

---

### Edge Cases

- **A real but empty answer.** A live read returning no rows is a substantiated answer ("there is none") and hides the block; it is not an unknown value (whose row is omitted) and not a failed read (whose error is shown).
- **One block fails while its neighbours succeed.** Only the blocked read reports the error, in place with its own retry; the rest of the page renders normally and the other blocks are unaffected.
- **Cached fallback rows.** Attributes supplied by the automatic cached fallback are substantiated and MUST continue to display, with the read-only indicator and cached-age reporting untouched.
- **A legitimately shared label.** Two panels may intentionally show the same text; the defect is three *different* labels collapsing to one key, not the existence of a shared key.
- **A field removed for now, restored later.** The follow-on specifications (item resolution, handler workflow, analytics, notification delivery) restore real values and real behaviour; nothing here may leave a scar that blocks them (no "reserved" placeholder rows, no dead resource keys left for reuse).
- **A control that cannot be hidden.** Where a control's capability is unavailable but it stays visible (the alerts toggle), it is disabled and states the reason; a visible control that neither acts nor says it cannot act is not acceptable.
- **Search entered before panels exist.** A term applied while a panel is not yet loaded must leave that panel correctly filtered once it loads, consistently with the other panels.
- **Coil weight units and multiple coils.** Where a request carries several coils, the weight shown must be the request's coil, and no fixed value may be substituted when it is unknown.
- **Notification arrival while the application is not running.** Activation from a cold start must not gain a new placeholder path as a side effect of removing the existing one.
- **Orphaned strings.** Removing a displayed field must not leave a shipped string asserting the removed content, and must not remove a string another surface still uses.
- **Retired documents already retired.** A superseded document must not be reopened by the retired-sources note, and its stale counts must not be merged into live counts.

## Requirements *(mandatory)*

### Functional Requirements

**Truthful request data**

- **FR-001**: The system MUST NOT display a material attribute value on a waitlist card or a request detail page unless that value is derived from the request being viewed or from an authoritative read for that request.
- **FR-002**: Where a material attribute has no substantiated source, the system MUST display no value for it — the attribute row is omitted rather than shown carrying a placeholder, and no default literal may stand in for the value (the hard-coded "Not available" defaults already on the detail page are themselves fabricated content and are removed with the rest). The rule for the block that held the attribute is: a read that succeeds and finds no matching row is a legitimate absence and the block MUST be hidden (a work order, part or operation pair that genuinely has no coil hides the coil information card), while a read that throws or reports an error result is a failure and MUST be surfaced in place — the block shows the error and a retry action, the rest of the page renders normally, and the failure is never disguised as an absence.
- **FR-003**: The set of attributes subject to FR-001 and FR-002 MUST cover at least: coil identification, part number(s), on-hand quantity, customer, packlist, traceability identifier, WIP destination, outside-service vendor, die number, quantity, and any status text shown for the request.
- **FR-004**: The system MUST NOT hold a constant or default literal for any material attribute, and MUST NOT substitute one when the real value is unknown.
- **FR-005**: The request detail page MUST NOT present an attribute, category or status that no data source produces (including derived-sounding strategy fields and placeholder progress text).
- **FR-006**: The feature MUST NOT alter the availability of cached external values: values served by the automatic cached fallback remain displayable, and the read-only indicator, its cached-age reporting and any manual retry remain as delivered.

**No control without behaviour**

- **FR-007**: A waitlist card MUST NOT present a control whose activation performs no action; the Cancel and Accept actions MUST be removed until the behaviour exists.
- **FR-008**: In place of the removed controls, the card MUST show only substantiated information (for example the request's real lifecycle status) or nothing.
- **FR-009**: Any control in the affected surfaces whose capability is unavailable MUST be absent, or explicitly marked unavailable with the reason; a silently inert control is not acceptable.

**New Request honesty**

- **FR-010**: The New Request confirm step MUST NOT display the fabricated active-request count or the fabricated estimated wait time; the card that carries them is removed rather than shown with a stand-in figure.
- **FR-011**: No queue or wait figure may be displayed unless it is derived from the real data for the work center on screen, with any wait estimate derived from the configured allowance rather than a constant; restoring such a figure belongs to the follow-on specification named in the defect→spec map (`WeekendProject/SpecTemplates/00-INDEX.md`), and this feature MUST leave no placeholder in its place.
- **FR-012**: The average coil weight MUST NOT be requested with a constant part identifier: the constant-keyed lookup is removed, and no value is displayed for that field until the request's own coil or part is what the lookup uses (the per-coil resolution belongs to the follow-on specification named in the defect→spec map, `WeekendProject/SpecTemplates/00-INDEX.md`).
- **FR-013**: The system MUST NOT substitute a constant weight when the average coil weight cannot be resolved.
- **FR-014**: The New Request Alerts control MUST be disabled and accompanied by a localized message stating that this installation cannot deliver notifications, and the capability MUST be exposed as state the control binds to so the toggle cannot be set On for a capability the installation lacks; where the installation can deliver notifications, the control MUST behave as before.
- **FR-015**: A preference MUST NOT be silently stored for a control the application cannot honour.

**Settings honesty and search completeness**

- **FR-016**: Each distinct Settings label MUST render its own text; no two distinct labels may resolve to one another's text.
- **FR-017**: The section header, the version card and the about card MUST each display their own correct text.
- **FR-018**: The privacy entry MUST resolve to a real, published policy destination, or MUST NOT be offered; no placeholder destination may ship in any user-facing text, and a shipped-resource audit MUST fail when a stored resource value matches a placeholder pattern (at minimum an unfinished-URL marker, `example.com`, `TODO`, `Contoso`, or `lorem`).
- **FR-019**: Every panel that consumes the settings search MUST update when the search term changes, including Image Location Settings, New Request Alerts and Max Allotted Time.
- **FR-020**: Clearing the search MUST return every affected panel to its unfiltered state.
- **FR-021**: The automated suite MUST include a coverage check that fails when a panel consuming the settings search is added without being registered to receive search changes, and the existing check that asserts only a panel's value MUST be corrected to assert that a change notification was raised (value-only assertion is what allowed this defect to ship).

**Notification activation**

- **FR-022**: Activating a new-request notification while the application is running MUST open the request the notification names, with the same outcome the cold-start activation path already produces, and MUST NOT display an internal placeholder or developer-facing dialog.
- **FR-023**: Activation arguments that cannot be mapped to a request MUST produce no visible UI, MUST be recorded for diagnosis, and MUST still bring the application window to the front; the shipped placeholder activation payload carrying the sample actions MUST be removed if it has no consumer.
- **FR-024**: The behaviour of activation from a cold start MUST NOT change, and removing the placeholder path MUST NOT introduce a new placeholder behaviour on that path.

**Wording, retirement and documentation**

- **FR-025**: No shipped user-facing text, resource entry or code comment may describe the retired sample/mock mechanism, its toggles or its sample rows as current behaviour.
- **FR-026**: Documentation that records or relies on any behaviour this feature removes or corrects MUST be updated in the same change: documents still presenting the retired manual external-system toggle as current MUST be corrected or retired, the feature inventory that records these defects MUST be updated for the ones this feature closes, any entry that describes the alerts capability MUST record that it is available only under a packaging option rather than generally, and each defect document this feature closes MUST record that closure in its own status line with its quoted evidence left intact.
- **FR-027**: The retired backlog sources MUST be labelled retired, with the reason they must not be reopened, at the place where the backlog index already names them.
- **FR-028**: The module task file that is behind reality MUST be reconciled so its completed items match what is actually finished.
- **FR-029**: The audit that forbids retired symbols from returning MUST be extended to the retired *wording* patterns, so a reintroduced message or comment fails the suite.

**Boundaries and evidence**

- **FR-030**: The feature MUST NOT introduce any demo, sample or mock mode, in any form, behind any setting.
- **FR-031**: The feature MUST NOT change the external read fallback contract: the existing read shapes, the automatic-on-unreachability behaviour, the atomic snapshot rule, and the stored-procedure-first rule remain as delivered; any database change ships its forward and rollback artifacts together.
- **FR-032**: Every defect class closed by this feature MUST have an automated check that fails against the pre-change behaviour, so the fabrication cannot silently return.
- **FR-033**: Layout, styling and information architecture of the affected surfaces MUST NOT be redesigned beyond what removing fields and controls requires; cosmetic and structural redesign belongs to the follow-on specifications.

### Key Entities

- **Value Source**: For a value the waitlist surfaces display, its identified origin — the request on screen, an authoritative read, or the cached external read. A value with no identified source is not displayed.
- **Block Failure**: A read that threw or reported an error while a card or section was being filled. Surfaced in place with a retry, never rendered as an absence.
- **Waitlist Request**: A real submitted request with a lifecycle status. Material attributes it refers to may or may not be resolvable yet; the request itself is always real.
- **Material Attribute (presented)**: A named attribute shown against a request, carrying a substantiated source (the request, an authoritative read, or the cached external fallback) — or not shown.
- **Unavailable Capability**: A behaviour the application cannot currently perform (notification delivery, accept/cancel handling, analytics-derived figures), represented by absence or by an explicit unavailable state with a reason.
- **Settings Panel**: A section of Settings that can consume the search term, recorded as search-aware or not; the defect class is panels that consume the term without being notified of changes.
- **Notification Activation**: An incoming activation carrying arguments: either maps to a request (routed) or does not (recorded, nothing visible).
- **Documentation Source**: A repository document describing behaviour, recorded as live or retired; retired sources are labelled and excluded from live counts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the material attribute values displayed on waitlist cards and request detail pages trace to the request being viewed or to an authoritative read for it; 0 fabricated values remain against roughly thirty literal values today.
- **SC-002**: 0 controls on the waitlist card perform no action, and 0 controls anywhere in scope are visible-but-inert; a control that is deliberately shown as unavailable, disabled, and accompanied by its reason is not inert.
- **SC-003**: 0 fixed figures (active-request count, estimated wait) are displayed on the confirm step for any work center or user; 0 coil weights are resolved from a constant part identifier or substituted from a constant fallback.
- **SC-004**: 3 previously colliding Settings labels display 3 distinct, correct texts; 0 shipped strings are placeholder destinations.
- **SC-005**: 100% of search-aware Settings panels respond to the search term, including the 3 that do not today; adding an unregistered search-aware panel fails the automated suite.
- **SC-006**: 0 internal placeholder dialogs are reachable from notification activation, and an activation that cannot be mapped to a request displays no dialog of any kind.
- **SC-007**: 0 occurrences of retired sample/mock wording (message, resource entry, comment, or document statement presented as current) in shipped output; the extended audit fails the suite if one returns.
- **SC-008**: Each of the 10 defect classes is covered by at least one automated check that fails against the pre-change behaviour (≥ 10 new checks).
- **SC-009**: Every declared scan gate returns empty outside the scope excluded by design — the retired-wording markers (a saved-with-sample-data message, a bare manual-toggle key, the retired mock-data feature key), the placeholder-address marker, the notification placeholder marker, and the fabricated-literal set named by the fabricated-card-data defect — with `specs/` and `defects/` recognised as the only intentional holders of the retired wording.
- **SC-010**: A reviewer unfamiliar with the codebase can open each affected document and correctly state what the application does today — 0 documents present removed behaviour as current.
- **SC-011**: Build completes with 0 warnings and 0 errors, and the automated suite reports 0 failures, on the standard build and test gates.
- **SC-012**: A manual pass over the affected surfaces (waitlist list, request detail, New Request confirm, and every Settings panel) finds 0 items that invite an action the application cannot perform or state a fact it cannot support.
- **SC-013**: The follow-on capabilities are not blocked: the cached external read behaviour, its indicator, its retry and the stored-procedure-first rule are unchanged (0 behavioural differences observed in a cache-fallback walkthrough).
- **SC-014**: 0 failed reads are rendered as an absent or hidden block: every block whose read threw or reported an error shows an inline error with a retry action while the rest of the page remains usable, and no failure is presented as an absence.

## Assumptions

- **Card actions are hidden, not disabled.** Grounded in the card-buttons defect: hiding beats disabling, because a disabled control still advertises a capability the application does not have. The New Request Alerts control is the deliberate exception — the alerts defect's least-cost branch keeps it visible, disables it, and states in localized text why it is unavailable.
- **Defect files are the historical record.** They quote the removed behaviour as evidence, so the quoted evidence stays as written and those files are excluded from the retired-wording scans (the source seed's gates already exclude them). Their status lines are the exception: each defect this feature closes records that closure, as the downstream specifications do for their own defects.
- **Missing values are omitted, not labelled.** Grounded in the fabricated-card-data defect: a field that cannot be sourced yet has its row omitted, because "a missing row is truthful, a fake value is not". The hard-coded "Not available" defaults on the detail page count among the fabricated literals and are removed with the rest, so no absence label replaces them.
- **No published privacy statement exists.** Neither the repository nor the company-facing documentation names a privacy statement address, and the application ships no in-app policy document or asset. The source defect's third branch applies: the entry and its resource values are removed rather than pointing at an unfinished address, and are restored pointing at a real one if it is supplied later. The unused content string and the sample activation payload go with them if they have no consumer.
- **Internal symbol naming.** The source defect makes renaming the "sample"-named type optional, and the retired-symbol audit deliberately does not match the bare word "sample" because legitimate type names remain in the live path. Shipped wording and comments are corrected; internal type names are left alone.
- **Retired-sources note location.** Added where the backlog index already lists the retired sources, with the documentation-hygiene entry that names them updated to point at it. The already-closed acceptance item in that document is not reopened.
- **Scope of enforcement.** The ten defects and the surfaces they name are the boundary: the waitlist list and request detail, the New Request confirm step, the Settings panels involved, and the notification activation path. A wider audit of every screen was considered and dropped as too much congestion for this feature; it belongs with the specifications that own those screens.
- **Reversibility.** Every field and control removed here is restored by a later specification (item resolution, handler workflow, analytics, notification delivery) with a real source, so this feature must leave no barrier — no reserved placeholder rows and no orphan keys for reuse.
- **Users.** Operators and requesters are authenticated users on the shop floor; no new role or permission is introduced by this feature.
- **Notification capability.** Whether notifications can be delivered is a property of how the application is installed and of the host, not a user preference; deciding packaging is a later specification's concern.
- **Retention of behaviour.** The external read fallback, its indicator, its cached-age reporting, its manual retry, and the stored-procedure-first rule are delivered behaviour and are treated as fixed constraints, not as scope.
- **Verification environment.** The documented build and test gates remain the evidence of record; live-database integration checks remain opt-in and environment-gated.
- **Localization.** Removed user-facing strings are deleted unless another surface still uses them; any retained string must not still assert removed content, and new unavailable-state text is localized like all other user-facing text.

<!-- token-budget: compacted (level=medium) on 2026-09-12; re-run 2026-09-12 found no further lossless reduction at medium — the ID-bearing requirement and criterion lines are frozen by compact.preserve_id_patterns and the rest was compacted on the first pass. NOTE: spec.full.md pre-dates the audit-removal decision and MUST NOT be used as a re-compaction baseline. -->
