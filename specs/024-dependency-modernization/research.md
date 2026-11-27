# Phase 0 Research: Dependency Currentness, Credential Removal and Platform Modernization

**Feature Directory**: `specs/024-dependency-modernization`
**Researched**: 2026-11-27
**Inputs**: `spec.md`, `DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md`, and a read of the current tree.

The source audit settled what to change. What it did not settle is how four of those changes land on code
that has moved since the audit was written, and two sequencing collisions that only appear once the trees
are read together. Each entry below records the decision this plan takes, why, and what it rejected. All
were resolved here; no `NEEDS CLARIFICATION` is carried into tasks.

## R-01 DEP-009 starts from a tracked configuration file, not from nothing

**Decision**: add `packageSourceMapping` to the already tracked root `NuGet.Config`, mapping the package
patterns in use to `nuget.org`. Do not create or commit a configuration for the first time.
**Rationale**: `git ls-files NuGet.Config` returns the file and `git log -- NuGet.Config` shows it was added
at `598c66cd`. It declares one `packageSources` entry for `nuget.org` and nothing else, so the reproducibility
half of DEP-009 is already done and the dependency-confusion guard is the whole remaining change.
**Alternatives considered**: treating the file as untracked and committing a fresh one. Rejected twice over:
it contradicts the tree, and re-committing a tracked file turns a four-line edit into a file-ownership
question. A wildcard fallback pattern was also rejected, because a second feed does not exist.

## R-02 SEC-001 needs a start-up guard, and the source's file list does not have one

**Decision**: SEC-001 owns two changes, not one. Blank every credential value in `appsettings.json`, and add
a check on the launch's first step that refuses a host with neither a saved nor an environment-supplied
connection value, naming the variable that is missing. The check reads the names from
`ConnectionStringEnvironmentVariable` rather than hard-coding a list.
**Rationale**: FR-019 requires the refusal and SC-001 measures it. Today the tree does the opposite in three
places that were read in this run. `MySqlHelperServer.ExecuteWithBoundedRetryAsync` treats a blank connection
as "nothing to report", logs at information level and returns a neutral result.
`ConfigurationStep` states in its own remarks that a machine reading its connection from the environment is
"not refused for it", and reports success either way. `StoreReachabilityStep` would then receive an empty
result set from a store that was never contacted and report "The store answered." Blanking the file without
the guard therefore produces an application that starts, says the store is reachable, and can do nothing.
**Alternatives considered**: making `MySqlHelperServer` throw on a blank connection. Rejected: the seam has a
documented, deliberate contract that an unconfigured store is not an unavailable one, so a screen that
tolerates a missing optional store would start failing on a machine that never had one. Refusing at the
launch step keeps that contract and puts the refusal where a person can act on it.

## R-03 The two missing resource keys exist, under a suffix the code cannot reach

**Decision**: add `Setup_Review_Scrap_None` and `Setup_Review_Section_SubordinateParts_Count` as dot-less
keys with the same wording as their `.Text` twins, exactly as the spec's Verbatim Constraints pin them. Keep
the `.Text` twins, because XAML `x:Uid` resolution addresses them.
**Rationale**: `Strings/en-us/Resources.resw:316` already declares `Setup_Review_Scrap_None.Text` and line
301 declares `Setup_Review_Section_SubordinateParts_Count.Text`. `ResourceExtensions.GetLocalized` never
appends `.Text`, and it converts a dot to a subtree separator when it looks a key up, so a lookup of the bare
name can never reach the suffixed entry and falls back to returning the key itself. That is the visible
defect. The audit described it as two absent keys; the accurate description is two keys under the convention
that belongs to markup rather than to code, which is why a "the key is present" check would wrongly report
the defect fixed.
**Alternatives considered**: changing the two call sites in `SetupReviewViewModel` to ask for the `.Text`
name. Rejected: the spec pins the keys to add as exact strings, and a code-side lookup carrying a markup
suffix would make this the only such call in the application, which is the opposite of UX-002's one
convention. Deleting the twins was also rejected: a `x:Uid` elsewhere would then break at run time, and
nothing in the build catches it.

## R-04 PERF-002 is three signature changes, two of them wider than one line

**Decision**: make the log flush awaitable while keeping its bounded, off-thread, best-effort behaviour; make
`IImageStorageService.GetConfiguredSharePath` awaitable and propagate through its two production call sites;
move the new-request-alerts read off the settings constructor onto the awaitable-initialisation idiom the
same class already uses. Update the two guardrail assertions that pin the flush's signature, in the same
change.
**Rationale**: `App.xaml.cs:580` is `private static void TryFlushLogStore()`, called from three sites at
lines 557, 622 and 713, and `FaultRecordAuditTests.cs:52` and `LocalStateFreeTests.cs:182` assert its name
and its exact declaration text. Awaiting it therefore cannot be a one-line edit. The settings constructor at
line 1378 cannot await at all, but the class already solves that shape: lines 1395 and 1396 expose
`HotWorkCentersLoad` and `BuildingsLoad` as task properties precisely so a page or a test can await them, so
the third wait follows that idiom rather than introducing a second one. The storage path is a synchronous
interface method used inside an interpolated string at `ImageOverrideDialogViewModel.cs:260` and inside
`DeleteStoredImageAsync` at line 424, so it needs an interface change and two callers, not an await bolted
onto a property.
**Alternatives considered**: keeping `TryFlushLogStore` synchronous and awaiting it nowhere. Rejected: the
call is on the shutdown path, which is the path FR-021 names. Keeping the interface method synchronous and
caching its value. Rejected: it would make the first picture operation read a value the screen never
refreshes, which is a behaviour change dressed as a non-change.

## R-05 MOD-006 cannot move the dialog into the app project

**Decision**: keep `MessageDialogHost`, `WindowedDialogHost` and `DialogSizing` where they are, and compose
the dialog body in code instead of loading `MessageDialogContent.xaml`. The shared library then declares no
`UserControl`, `Page` or `Window` type, and all 22 call sites keep their signature.
**Rationale**: `MessageDialogHost.AskAsync` has 22 production callers, and one of them is
`MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml.cs:63`. The service references `MTM_Waitlist.Shared`,
and the application references the service, so the reverse reference cannot exist. Moving the body into the
app's `Module_*` layer, which is what section 7 of the source prescribes, would leave the on-host service
unable to show the one dialog it shows. Composing the body in code satisfies FR-015 as written, needs no new
project, and leaves every caller untouched.
**Alternatives considered**: a new shared dialogs library referenced by both executables. Rejected: it adds a
project outside the approved register, which section 16 item 18 and FR-035 forbid. Leaving the control in
place with a comment. Rejected: FR-015 is explicit that the shared library holds no view type. Building a
`ContentDialog` inline at each call site. Rejected: it discards the one thing this class exists for, which is
that a screen cannot choose a button order or make a destructive answer the default.

## R-06 Central package management collides with the security pin, so DEP-008 relocates it

**Decision**: DEP-008 moves the `System.Security.Cryptography.Pkcs` pin out of
`Directory.Build.props:77` into `Directory.Packages.props` as a `PackageVersion` and removes the `Version`
attribute from the `PackageReference`. DEP-005 then retires it from its new home.
**Rationale**: the pin is declared as `<PackageReference Include="System.Security.Cryptography.Pkcs"
Version="9.0.21" />` in an `ItemGroup` that every project inherits. Microsoft Learn's NU1008 page states that
a project using central package management "must define a Version value on a PackageVersion item", and a
`PackageReference` carrying a `Version` is an error. The moment W6 lands, the inherited pin fails the build in
all 17 projects. Section 8 of the source treats the pin retirement as an independent change that follows
DEP-005, which is true of the retirement and not of the relocation.
**Alternatives considered**: retiring the pin early, in W6. Rejected: it is the only thing suppressing two
advisory reports on every restore, and DEP-005's SqlClient 8.0.0 is what makes it unnecessary. Leaving the
pin outside central management on purpose. Rejected: it would be the one version in the solution that central
management does not hold, which is exactly the drift DEP-008 exists to end.

## R-07 DEV-001's gate is a real cold restore, and the property is at line 56

**Decision**: remove `<UseRidGraph>` from `Directory.Build.props` only after W16 has landed and a restore from
a cleared package cache raises no NETSDK1206. If the warning appears, the property stays and the reason is
recorded against this wave.
**Rationale**: Microsoft Learn's NETSDK1206 page gives exactly two remedies: move to a package version that
ships portable RIDs, or set `UseRidGraph` and accept that the old graph "won't be updated in the future" and
may be removed. W16 takes the first remedy by moving to the toolkit line that pulls Win2D 1.4.0 with its
portable `win-*` assets. The property's own comment already predicted this and declined to act on it for
binding-risk reasons that W16 removes. The property sits at line 56 and its rationale block at lines 29 to 55,
so the comment that cites a superseded platform version at line 47 is also AI-001's target.
**Alternatives considered**: removing the property first and letting the build tell us. Rejected: it inverts
the gate, and a NETSDK1206 raised before Win2D unifies would be indistinguishable from a real asset problem.

## R-08 MOD-005 deletes a duplicate, and one test constructs it

**Decision**: delete `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs`, register Core's type in the
setup module's DI in its place, and retarget
`MTM_Waitlist.Tests/Module_Setup/Services/InforVisualSqlQueryServiceTests.cs` at the surviving type.
**Rationale**: `MTM_Waitlist.Setup` already references `MTM_Waitlist.Core`, so nothing new has to be wired.
Both types take `IConfiguration` and expose `ExecuteQueueAsync` with the same shape, but they are separate
copies, which is the drift the audit named. Core's copy carries the script store and the environment-first
resolution; the setup copy carries its own constants, so the two can already answer differently on the same
machine. The test's `CreateService()` at line 115 constructs the setup copy directly.
**Alternatives considered**: a shared base class with host-specific overrides. Rejected by the source and
here for the same reason: a base with one override each is the duplication with more files. Leaving the setup
copy and delegating to Core. Rejected: the delegation shim would itself become the second thing that can
drift.

## R-09 UX-003 asks for a resource, and the formatter is in a class library

**Decision**: the service returns the number formatted with the current culture and no unit, and the unit is
resolved from a resource where the text is composed, at the view or its view model. The resource key is added
alongside the two UX-001 keys.
**Rationale**: `AverageCoilWeightService` lives in a class library that already reaches
`Module_Core.Helpers`, so a resource lookup would compile, but the returned value is a display string and the
service's own documentation describes it as one. Today it returns
`$"{rounded.ToString("N0", CultureInfo.InvariantCulture)} lb"`, which is both halves of the defect: the
invariant culture and the literal unit. Splitting them keeps the number a number and puts the words with the
other words, which is the same split UX-002 asks for everywhere else. The exact key name is not pinned by the
spec, so it is chosen at W27 and recorded there rather than invented here.
**Alternatives considered**: keeping the service as the composer and injecting a localizer. Rejected: it
gives a data service a presentation dependency for one string, and the view is already the place the string
is shown. Formatting with the current culture but keeping the literal unit. Rejected: FR-024 requires the
unit from a resource.

## R-10 PERF-001 is a cancellation token plus a last-writer-wins guard

**Decision**: debounce in the page's text-changed handler with a cancellation token per typing pause, cancel
the superseded read, and make the view model keep a sequence number so a late answer cannot overwrite a newer
one.
**Rationale**: `GenericRequestPartPage.OnSearchTextChanged` awaits `ViewModel.SearchAsync()` on every
keystroke past two characters, which is the amplification the audit measured. Cancellation alone is not
enough: a read the plant system has already answered is not cancellable at that point, so the newest answer
has to be chosen by identity rather than by arrival order. FR-020 asks for all three behaviours separately,
which is why the plan gives them one wave and three assertions.
**Alternatives considered**: an `AutoSuggestBox` built-in delay. Rejected: it would change what a person
experiences in the control, and UX-002's neighbour requirement is that no interaction changes. Debouncing in
the view model with a timer. Rejected: the page already owns the input event, and a timer there would need
its own lifetime.

## R-11 The Infor Visual triple has no environment-variable property to name

**Decision**: the start-up guard introduced in W1 covers the two internal stores through
`ConnectionStringEnvironmentVariable`, and treats the Infor Visual values as a separate, non-fatal
requirement, because a host that cannot reach the plant system already has a defined degraded path.
**Rationale**: `InforVisualDatabaseOptions` has no equivalent property. The values are resolved inside
`ResolveConnectionString` from `INFOR_VISUAL_SQL_CONNECTION_STRING` first and then from four per-field
environment variables, each falling back to the configuration value that SEC-001 blanks. Once the file is
blanked, an unprovisioned host resolves an empty string, and the read degrades to an empty answer, which is
the fallback path the constitution allows for Infor Visual. Making that fatal would turn a supported
degradation into a refusal, which FR-034 forbids.
**Alternatives considered**: adding `ConnectionStringEnvironmentVariable` to the Visual options for symmetry.
Rejected: it is a new configuration property outside the approved register, and the resolution path already
has a working environment-first order. Leaving the Visual triple in the file. Rejected: FR-019 says no
credential value remains in any tracked file.

## R-12 Pinned baselines are the audit's measurement, not the tree's

**Decision**: keep every pinned identifier verbatim, and re-measure every pinned count at the gate that
depends on it. Report a delta rather than treating it as a failure.
**Rationale**: reading the tree found the test-method count exact at 2,768 and three counts drifted:
`SettingsViewModel.cs` is 3,023 lines with 161 public members rather than 2,623 and 106, the resource corpus
is 1,357 keys across five files rather than 1,079 across three, and `x:Uid` occurrences are 167 rather than
159. FR-036 wants each phase boundary to carry measured evidence, and a stale expected value would make a
correct change read as a regression.
**Alternatives considered**: updating the spec's baseline list. Rejected: the spec records what the audit
measured on its own date, and rewriting it would lose the comparison. Recomputing every count now. Rejected:
three of them only matter at the wave that edits them, and W28's measurement is the one that has to be right.

## R-13 MOD-002 must not lose what the seam does not model

**Decision**: before each of the five services moves, read the call it replaces and confirm the seam covers
its behaviour: the retry and timeout policy, the availability record, the cancellation contract, and whatever
shape the call returns. A behaviour the seam does not model is added to the seam, not dropped at the caller.
**Rationale**: the seam returns a neutral result for an unconfigured store and records availability as a side
channel that cannot fail the read. Two of the five callers currently throw
`InvalidOperationException("... is not configured ...")` when there is no connection, at
`UserManagementRepository.cs:374` and `PermissionAdministrationService.cs:467`. Routed through the seam those
throws become empty results, which is a behaviour change in the opposite direction from "no behaviour lost".
The rule is therefore: keep the throw at the caller that already had it, and adopt the seam around it.
**Alternatives considered**: routing everything blindly and letting the tests find the differences. Rejected:
several of those tests are the live, environment-gated ones that cannot run here, so the difference would
surface in the plant.

## R-14 Phase 1 does run the test suite

**Decision**: the test suite is a gate at every phase boundary, including Phase 1.
**Rationale**: FR-033 requires every phase to end with a green suite, and SC-004 and section 15 say the suite
gate applies "from phase 2 onward". The two differ only for Phase 1, which does change code: PERF-002 touches
three signatures and two guardrail assertions. Reading the phase-2 qualifier as a floor rather than a ceiling
satisfies both, and it is the only reading under which Phase 1's own evidence exists.
**Alternatives considered**: skipping the suite at Phase 1 as section 15 literally allows. Rejected: the
phase changes the shutdown path of the application, which is the worst possible thing to ship unmeasured.

## Unattended assumptions

This run had no person available to answer. Each item below is settled as an assumption and changes no
approved proposal.

- The plant host is not reachable. W10 and W11 prove a local-instance round trip and record the plant read as
  environment-gated evidence, naming what ran and what did not.
- The two published endpoints AI-003 declares are assumed reachable from a developer machine. If either is
  not, AI-003 is rejected rather than half-declared, which is what section 17 requires.
- The operational prerequisite for SEC-001, provisioning the connection variables and rotating the disclosed
  values, is outside this repository and is recorded as a prerequisite, not as work.
- No proposal in the register needs re-approval. Where the tree has moved since the audit, the plan records
  the difference and keeps the proposal's substance.
