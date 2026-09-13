# Feature Specification: Truthful Data and Truthful Controls

**Feature Branch**: `002-truthful-data-and-controls`  
**Created**: 2026-09-12  
**Status**: Draft  
**Input**: User description: "Close the gap between what the MTM Waitlist application displays and what it can actually support. A full inventory of the running application found ten verified defects, and the rule for this feature is that the application must never display a value it cannot substantiate and must never offer a control it cannot honour: where the real value or real behaviour does not exist yet, the field or the control is removed or marked clearly unavailable rather than filled or shown as if functional, and where wording (on screen, in a resource file, or in a code comment) describes behaviour that was deleted, it is corrected or deleted. The ten defects are: (1) every waitlist card and request detail page shows hard-coded material values — coil number, part numbers, on-hand quantity, customer, packlist, traceability identifier, WIP destination, outside-service vendor, die number, quantity — for every real request, so an operator can act on a wrong coil, part, customer or vendor; (2) every card draws a red Cancel and a green Accept button that have no behaviour at all; (3) the New Request confirm screen shows a fixed '0 active request(s) for this work center' and 'Estimated wait time: approximately 15 minutes' for every user and every work center; (4) the average coil weight is always looked up for one fixed part and falls back to a fixed 5,000 lb, so it never describes the coil on the request; (5) the New Request Alerts switch cannot fire because the application is installed in a way Windows notifications do not support, yet the control is offered as if it worked; (6) three separate labels in Settings share one resource key, so all three read 'About this application'; (7) the Privacy Policy link opens an unfinished placeholder address; (8) three Settings panels ignore the settings search because they are never told the search changed; (9) the application still ships a 'Setup saved using sample data' message and code comments describing the retired sample/mock mechanism; (10) tapping a new-request notification while the application is already running shows an internal 'TODO' dialog instead of the request. Each defect must end with the application telling the truth about what it knows and what it can do; the fields and controls that cannot yet tell the truth are removed or disabled rather than populated with placeholder content, and the feature specifications that follow this one restore the real values and the real behaviour (item resolution, the handler workflow, analytics, and notification delivery). Documentation that describes the retired sample/mock system — including the backlog and changelog files that still present it as current — must be corrected or retired in the same change so no reader is sent to re-implement deleted behaviour."

## Clarifications

### Session 2026-09-12

- Q: When a card or a request-detail section is left with no substantiated attribute at all, should that empty space be hidden, or kept and explained with a localized message? → A: Hide the block when the absence is the legitimate answer (for example a work order / part / operation pair that has no coil hides the coil information card); where the cause is a failure rather than a legitimate absence, the error must be shown instead of hidden. Every UI-facing value must have its source established during this feature.
- Q: Which surfaces must the value-source audit cover? → A: Every screen in the shipping application — a full inventory of displayed values, not only the screens named by the ten defects.
- Q: What counts as a failure that must be shown, rather than an absence that hides the block? → A: Any read that throws or reports an error result is a failure and is shown; a read that succeeds and finds no matching row is a legitimate absence and the block is hidden.
- Q: How should a failed read inside a card or section be shown? → A: In place, scoped to the block that failed — the block shows the error and a retry action while the rest of the page renders normally; the existing whole-screen unavailable state keeps its current meaning for a store outage.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The waitlist tells the truth about a request (Priority: P1)

An operator opens the waitlist, and opens a request from it. Every value shown against the request is a value that belongs to *that* request. Where the application does not yet have a real source for an attribute, no value is shown at all, and no button is offered that does nothing. The same test is applied to every value that every other screen displays: each one is traced back to its source, and the ones with no source are removed.

**Why this priority**: This is a physical-safety defect. An operator who reads a fabricated coil number, part number, customer, vendor or die number can carry the wrong material to the wrong destination. The inert Cancel/Accept buttons compound it: an operator who presses Accept and sees nothing happen cannot tell whether the request was accepted, so the queue silently diverges from the floor.

**Independent Test**: Open the waitlist list and a request detail page on the running application for a request whose real material attributes are not yet resolvable. Confirm that no material value appears unless it can be traced to that request or to an authoritative read for it, and that no control appears which performs no action.

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

A requester works through New Request. The confirm step reports only figures the application can substantiate for *their* work center and request type, the coil weight shown (if any) describes the coil actually being requested, and the alerts control is honest about whether this installation can deliver a notification at all.

**Why this priority**: The confirm step is the requester's last chance to catch a mistake. A fixed queue count and a fixed "approximately 15 minutes" are read as facts about the current shop state and are used to decide whether to submit now or walk away; a coil weight resolved for an unrelated part is simply wrong. The alerts switch is worse than wrong — it silently accepts a setting the application can never honour.

**Independent Test**: Run New Request to its confirm step for two different work centers and two different request types. Confirm the step carries no queue count and no wait estimate, that no coil weight is presented which was resolved for a part other than the one on the request, and that the alerts control reports unavailability (with a reason) whenever the installation cannot deliver notifications.

**Acceptance Scenarios**:

1. **Given** a requester on the confirm step, **When** the step renders, **Then** no active-request count and no estimated wait time are shown, and no stand-in figure or empty card is displayed in their place.
2. **Given** two requests for different work centers, **When** the confirm step renders for each, **Then** no displayed figure is a constant shared by both unless a real source produced it.
3. **Given** a request for a coil, **When** the confirm step renders, **Then** no average coil weight is shown unless it was resolved for the part on that request, and no fixed fallback weight appears in any state.
4. **Given** an installation that cannot deliver notifications, **When** a requester reaches the alerts control, **Then** the control is disabled and accompanied by the reason, and no preference is silently stored for it.
5. **Given** an installation that can deliver notifications, **When** a requester reaches the alerts control, **Then** it remains usable exactly as before.

---

### User Story 3 - Settings tells the truth and searches completely (Priority: P3)

A user opens Settings. Each label shows its own text rather than another panel's text. The privacy entry leads somewhere real or is not offered. Searching the settings surfaces every panel that matches, including Image Location Settings, New Request Alerts and Max Allotted Time.

**Why this priority**: These are trust and compliance defects, not safety defects. Three labels that all read "About this application" make the page unreadable; a privacy link to an unfinished address is a compliance exposure; a search that silently omits three panels teaches users that search cannot be relied on.

**Independent Test**: Open Settings, read the section header, the version card and the about card; follow the privacy entry; then search a term that matches only each of the three previously unsearched panels and confirm each responds.

**Acceptance Scenarios**:

1. **Given** the Settings page, **When** the user reads the section header, the version card and the about card, **Then** each displays its own distinct, correct text.
2. **Given** the privacy entry, **When** the user activates it, **Then** it either opens a real, published policy destination or is not offered at all; no placeholder address ships in any shipped text.
3. **Given** the user types a search term that matches only one of Image Location Settings, New Request Alerts, or Max Allotted Time, **When** the term changes, **Then** that panel reflects the search just as every other search-aware panel does.
4. **Given** the search is cleared, **When** the term becomes empty, **Then** every panel returns to its unfiltered state, including the three above.
5. **Given** a future panel is added that consumes the settings search, **When** it is not wired to receive search changes, **Then** the automated suite fails.

---

### User Story 4 - Opening a notification while running behaves like a cold start (Priority: P4)

An operator taps a new-request notification while the application is already running. The request the notification names opens — the same outcome the cold-start path already produces. Nothing internal appears: no developer placeholder dialog. An activation that cannot be mapped to a request does nothing visible.

**Why this priority**: Low frequency while the application ships unpackaged (so a notification cannot fire at all today), but the failure is a visible internal artefact in a production-facing surface, and the correct behaviour already exists on the other activation path, so the fix is bounded. Delivering notifications at all belongs to a later specification.

**Independent Test**: While the application is running, trigger an activation with a recognised argument and with an unrecognised argument. The recognised one opens the named request exactly as a cold start would; the unrecognised one produces no visible UI.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** a new-request notification naming a request is activated, **Then** that request's detail page is shown, exactly as activation from a cold start does, and no internal placeholder or developer-facing dialog is displayed.
2. **Given** an activation whose arguments cannot be mapped to a request, **When** it is processed, **Then** nothing visible happens, the event is recorded for diagnosis, and the application window is brought to the front.
3. **Given** the same activation is repeated, **When** it is processed again, **Then** the result is the same and no dialog accumulates.

---

### User Story 5 - The repository describes only what still exists (Priority: P5)

A developer or reviewer reads the changelog, the backlog and the module task files. Nothing presents the retired sample/mock mechanism as current, no shipped message or comment refers to deleted behaviour, and the counting traps that inflate the backlog are marked as retired.

**Why this priority**: Lowest operational impact, but it is the mechanism by which deleted behaviour gets rebuilt. Every reader sent to re-implement a removed toggle costs a rewrite and a review cycle.

**Independent Test**: Read the affected documents and shipped text and confirm no statement presents the removed manual external-system toggle, sample rows or mock mode as current behaviour.

**Acceptance Scenarios**:

1. **Given** a reader opening the changelog entry for the removed manual external-system toggle, **When** they read it, **Then** it is corrected to describe the actual current behaviour or is retired.
2. **Given** the backlog index, **When** a reader reaches the retired sources, **Then** each is labelled retired with the reason it must not be reopened.
3. **Given** the module task file that is behind reality, **When** a reader opens it, **Then** its completed items are reconciled so its count matches what is actually finished.
4. **Given** any shipped user-facing text, resource entry or code comment, **When** it is read, **Then** no wording describes the retired sample/mock mechanism as live.

---

### Edge Cases

- **A real but empty answer.** A live read that returns no rows for a request is a substantiated answer ("there is none") and hides the block; it MUST NOT be confused with an unknown value (whose row is omitted) or with a failed read (whose error is shown).
- **One block fails while its neighbours succeed.** Only the blocked read reports the error, in place and with its own retry; the rest of the page renders normally and the other blocks are unaffected.
- **Cached fallback rows.** Attributes supplied by the automatic cached fallback are substantiated and MUST continue to display, with the read-only indicator and cached-age reporting untouched.
- **A legitimately shared label.** Two panels may intentionally show the same text; the defect is three *different* labels collapsing to one key, not the existence of a shared key.
- **A field removed for now, restored later.** The follow-on specifications (item resolution, handler workflow, analytics, notification delivery) restore real values and real behaviour; nothing in this feature may leave a scar that blocks them (no "reserved" placeholder rows, no dead resource keys left behind for reuse).
- **A control that cannot be hidden.** Where a control's capability is unavailable but the control stays visible (the alerts toggle), it is disabled and states the reason. A visible control that neither acts nor states that it cannot act is not acceptable.
- **Search entered before panels exist.** A search term applied while a panel is not yet loaded must still leave that panel correctly filtered once it loads (or correctly unfiltered, consistently with the other panels).
- **Coil weight units and multiple coils.** Where a request carries several coils, the weight shown must be the request's coil, and no fixed value may be substituted when it is unknown.
- **Notification arrival while the application is not running.** Activation from a cold start must not gain a new placeholder path as a side effect of removing the existing one.
- **Orphaned strings.** Removing a displayed field must not leave a shipped string that still asserts the removed content, and must not remove a string another surface still uses.
- **Retired documents already retired.** Where a document is superseded, the retired-sources note must not reopen it or merge its stale counts into live counts.

## Requirements *(mandatory)*

### Functional Requirements

**Truthful request data**

- **FR-001**: The system MUST NOT display a material attribute value on a waitlist card or a request detail page unless that value is derived from the request being viewed or from an authoritative read for that request. In addition, the source of every value displayed on every screen of the shipping application MUST be identified and recorded during this feature, and any value whose source cannot be identified MUST be removed.
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
- **FR-011**: No queue or wait figure may be displayed unless it is derived from the real data for the work center on screen, with any wait estimate derived from the configured allowance rather than a constant; restoring such a figure belongs to the follow-on specification the defect map names for it, and this feature MUST leave no placeholder in its place.
- **FR-012**: The average coil weight MUST NOT be requested with a constant part identifier: the constant-keyed lookup is removed, and no value is displayed for that field until the request's own coil or part is what the lookup uses (the per-coil resolution belongs to the follow-on specification the defect map names for it).
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

- **Value Source**: For every value a screen displays, the identified origin of that value — the request on screen, an authoritative read, or the cached external read. Every displayed value has one; a value with no identified source is removed rather than shown.
- **Block Failure**: A read that threw or reported an error result while a card or section was being filled. Surfaced in place with a retry action, never rendered as an absence.
- **Waitlist Request**: A real submitted request with a lifecycle status. Attributes of the material it refers to may or may not be resolvable yet; the request itself is always real.
- **Material Attribute (presented)**: A named attribute shown against a request. Either carries a substantiated source (the request, an authoritative read, or the cached external fallback) or is not shown.
- **Unavailable Capability**: A behaviour the application cannot currently perform (notification delivery, accept/cancel handling, analytics-derived figures). Represented in the UI by absence or by an explicit unavailable state with a reason.
- **Settings Panel**: A section of Settings that can consume the search term. Recorded as search-aware or not; the defect class is panels that consume the term without being notified of changes.
- **Notification Activation**: An incoming activation carrying arguments. Either maps to a request (handled silently or routed) or does not map (recorded, nothing visible).
- **Documentation Source**: A repository document that describes behaviour. Recorded as live or retired; retired sources are labelled and excluded from live counts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the values displayed on every screen of the shipping application are traceable to an identified source, and 0 fabricated values remain against roughly thirty literal values on the waitlist surfaces today; the audit that establishes this is recorded as part of the feature.
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
- **SC-012**: A manual pass over every screen of the shipping application finds 0 items that invite an action the application cannot perform or state a fact it cannot support.
- **SC-013**: The follow-on capabilities are not blocked: the cached external read behaviour, its indicator, its retry and the stored-procedure-first rule are unchanged (0 behavioural differences observed in a cache-fallback walkthrough).
- **SC-014**: 0 failed reads are rendered as an absent or hidden block: every block whose read threw or reported an error shows an inline error with a retry action while the rest of the page remains usable, and no failure is presented as an absence.

## Assumptions

- **Card actions are hidden, not disabled.** Grounded in the card-buttons defect, which requires hiding rather than showing disabled controls, because a disabled control still advertises a capability the application does not have. The New Request Alerts control is the deliberate exception: the alerts defect's least-cost branch keeps the control visible, disables it, and states in localized text why it is unavailable, so the user learns the capability exists but is not available in this installation.
- **Defect files are the historical record.** The ten defect documents quote the removed behaviour as evidence, so the quoted evidence is left as written and those files are excluded from the retired-wording scans (the source seed's gates already exclude them). Their status lines are the exception: each defect this feature closes records that closure, which is the convention the downstream specifications follow for their own defects.
- **Missing values are omitted, not labelled.** Grounded in the fabricated-card-data defect: a field that genuinely cannot be sourced yet has its row omitted, because "a missing row is truthful, a fake value is not". The hard-coded "Not available" defaults already on the detail page are counted among the fabricated literals and are removed with the rest, so no absence label takes their place.
- **No published privacy statement exists.** Neither the repository nor the company-facing documentation names a privacy statement address, and the application ships no in-app policy document or asset. The source defect's third branch therefore applies: the entry and its resource values are removed rather than pointing at an unfinished address. If a real published address is supplied later, the entry is restored pointing at it. The unused content string and the sample activation payload are removed if they have no consumer.
- **Internal symbol naming.** The source defect makes renaming the "sample"-named type optional, and the retired-symbol audit deliberately does not match the bare word "sample" because legitimate type names remain in the live path. This feature corrects shipped wording and comments and leaves internal type names alone.
- **Retired-sources note location.** The note is added where the backlog index already lists the retired sources, and the documentation-hygiene entry that names them is updated to point at it. The already-closed acceptance item in that document is not reopened.
- **Scope of enforcement.** Every screen in the shipping application is in scope for the value-source audit: each displayed value gets an identified source and every value without one is removed. The ten defect classes are the known instances that motivate the work, not the boundary of it.
- **Reversibility.** Every field and control removed here is restored by a later specification (item resolution, handler workflow, analytics, notification delivery) with a real source; this feature must therefore leave no barrier — no reserved placeholder rows and no orphan keys left for reuse.
- **Users.** Operators and requesters are authenticated users on the shop floor; no new role or permission is introduced by this feature.
- **Notification capability.** Whether notifications can be delivered is a property of how the application is installed and of the host, not a user preference; deciding packaging is a later specification's concern.
- **Retention of behaviour.** The external read fallback, its indicator, its cached-age reporting, its manual retry, and the stored-procedure-first rule are delivered behaviour and are treated as fixed constraints, not as scope.
- **Verification environment.** The documented build and test gates remain the evidence of record; live-database integration checks remain opt-in and environment-gated.
- **Localization.** Removed user-facing strings are deleted unless another surface still uses them; any retained string must not still assert removed content, and new unavailable-state text is localized like all other user-facing text.
