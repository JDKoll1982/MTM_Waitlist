# Phase 0 Research: Startup Rebuild

The unknown list this phase had to resolve. Most of it was already resolved by the project owner in
`STARTUP-REBUILD-BRIEF.md`, which locks twenty-four decisions in its S2 and works them out through S4 to S12.
Those are recorded here as decisions with their rationale rather than re-litigated. Four decisions are new in
this plan because the brief left them open or did not name them: where the remembered sign-in is stored, how
the ignored-locations restriction is expressed without breaking FR-030, which of the four privileged surfaces
need a new catalogue key, and what happens to two option sections nobody can explain.

There are no `NEEDS CLARIFICATION` markers left. Nothing in this plan is blocked on an answer.

---

## D1. Rebuild `MTM_Waitlist.Startup` in place, under the same name

**Decision**: The startup pipeline library is rebuilt in the existing project, and the XAML stays in
`Module_Startup/Views` inside the app.

**Rationale**: The project name is referenced by the solution entry, the app's `ProjectReference` and the test
project's reference. Rebuilding in place means those three are re-pointed rather than re-invented, and the
composition-root split the repo already uses (non-view code in a library, views in the app) is preserved. The
constitution's Structure rule requires exactly that split.

**Alternatives considered**: A new project name such as `MTM_Waitlist.Launch`. Rejected because it adds three
file edits and a naming question for no behavioural gain, and because the removal phase would then have to
delete the old project and create the new one, widening the window in which the tree does not build.

## D2. Old material is deleted outright, not parked

**Decision**: Every file in the removal list is deleted. Git history is the archive. There is no `_legacy/`
folder and no commented-out block.

**Rationale**: A parked copy is a live invitation for a later change to re-wire it, and FR-028 requires a build
failure if the replaced behaviour is reintroduced. Parking and guarding contradict each other.

**Alternatives considered**: A `_legacy/` folder excluded from compilation. Rejected: the retired-symbol audit
would have to pattern-match a directory that exists, and nothing stops someone re-including it.

## D3. Removal is proven by a placeholder before the rebuild starts

**Decision**: The old surface is deleted first and replaced by a minimal placeholder window. The app launches
to the placeholder, reaching neither the shell, nor the sign-in form, nor a store read. Only then is the new
module built.

**Rationale**: "The old logic is gone" becomes provable rather than asserted, and it is provable while the
removal is the only change in the tree, so a green build at that point means the removal is complete rather
than that the new code compensates for what is left.

**Alternatives considered**: Build the new pipeline alongside the old one and switch over at the end. Rejected
because the two would share window statics, DI registrations and session state, and the switchover would be the
one change nobody could test in isolation.

## D4. `StartupDebugLog` and `StartupState` are deliberately deferred out of the removal phase

**Decision**: `StartupDebugLog` is deleted in Phase 6 and `StartupState` in Phase 4.

**Rationale**: `StartupDebugLog` has 489 call sites across 83 files and `StartupState` has roughly twenty
consumers. Deleting either in Phase 2 leaves the tree uncompilable, which makes the Phase 2 gate (a green
build with the guard passing) unreachable, and an unreachable gate in a removal phase is how a removal becomes
half-done.

**Alternatives considered**: Delete everything in Phase 2 and let the tree stay red until the rebuild catches
up. Rejected: a red tree for several phases destroys the baseline comparison that Phase 9.2 depends on.

## D5. Both logging surfaces are settled in one change: a store-backed seam plus an `ILogger` provider

**Decision**: The static surface (489 references in 83 files) is migrated by script to a new seam, and the
`ILogger` surface (226 calls in 26 files) is served by a provider that writes to the same store. No `ILogger`
call site is edited.

**Rationale**: The two surfaces need opposite treatment. The static type is being replaced, so its call sites
must change; the `ILogger` abstraction is sound and only lacks a destination, so its call sites must not
change. S9.1 measured both, and the provider approach converts 226 edits into one registration.

**Alternatives considered**: Migrate both surfaces by script. Rejected: it would edit 226 call sites for no
benefit and would lose the structured properties `ILogger` already carries.

## D6. The replacement logging seam is unconditional

**Decision**: The new seam carries no `[Conditional]` attribute.

**Rationale**: `StartupDebugLog.Info` and `.Error` are both `[Conditional("DEBUG")]`, so a Release build
compiles out all 482 static calls and their arguments. That is why a released build currently records nothing
at all (SC-003) and why a production fault leaves no trace anywhere. A replacement that kept the attribute
would leave the new table empty on a shop-floor machine and the new panel showing nothing.

**Alternatives considered**: Keep the attribute for the static surface and rely on `ILogger` for Release.
Rejected: it splits the record in two and still loses everything the static surface would have written.

## D7. The call-site migration runs before the old type is deleted

**Decision**: `tools/migrate-logging-callsites.ps1` runs in dry run, its counts are reconciled against 489
sites in 83 files, then it is applied with `-Apply`, then the seven `Configure` call sites are fixed by hand,
and only then is `StartupDebugLog` deleted.

**Rationale**: The script already exists and its dry run matched an independent hand count. Running it before
the deletion means the deletion is the last step of a verified sequence rather than a leap. The seven
`Configure` calls need a human because that signature changes, which the script reports separately rather than
guessing at.

**Alternatives considered**: Delete the type first and fix the 489 compile errors. Rejected: the compiler
reports them one build at a time and the ordering guarantee would be lost.

## D8. The new type is made reachable with a global using, and the script does not touch using directives

**Decision**: One global using rather than 134 per-file using edits.

**Rationale**: `MTM_Waitlist.Module_Core.Helpers` holds ten other types and is imported by 134 files. Adding
one global using is one line; rewriting using directives in 134 files is 134 chances to break an unrelated
import.

**Alternatives considered**: Per-file using edits. Rejected on the same ground.

## D9. The stored-procedure call sites replace the local settings file directly; no new storage is created

**Decision**: The seven scoped preferences move into `config_settings_values` through the existing
`IConfigSettingsValueService` and `sp_config_settings_values_get` / `sp_config_settings_values_upsert`.

**Rationale**: The scoped store, its scope rank function and its procedures already exist, and
`PictureEnlargePreference` already uses them. Each migration is therefore one substitution plus a stored
procedure call at runtime, not a new table, a new repository or a new cache. This is the smallest thing that
works and it is already proven in production.

**Alternatives considered**: A new `user_preferences` table. Rejected: it would duplicate a store that already
solves scope precedence, and the scope rank function was recently corrected specifically to make a person's own
value beat every wider scope.

## D10. The ignored-locations restriction is expressed as a new edit key, not by narrowing the existing key

**Decision**: `permission.settings.ignored_locations` keeps its grants exactly as they are. A new key,
`permission.settings.ignored_locations_edit`, restricted to `IT Department` and `Developer`, gates every write.

**Rationale**: This is the one place in the feature where two requirements pull against each other. FR-024 says
the plant-wide list is changeable only by `IT Department` or `Developer`. FR-030 says every setting that is
restricted to particular roles today keeps the same restriction. The existing key is granted today to
`role:developer`, `role:it_department`, `role:plant_manager`, `role:production_lead`, `role:setup_lead`,
`role:production` and `role:setup`, and denied to `role:material_handler` and `role:material_handler_lead`. So
narrowing that key would remove access from five roles and break FR-030 in the plain sense. Adding a distinct
edit key satisfies FR-024 exactly, leaves FR-030 untouched, and matches the brief's own wording in S11.5.6,
which says *editing* needs its own permission key.

**Alternatives considered**: (a) Narrow the existing key to two roles. Rejected: it silently revokes read
access from five roles, which is a different feature. (b) Gate the write in the UI only. Rejected explicitly by
S11.5.6: a hidden control is not a permission. (c) Make the list person-scoped so FR-030's "setting" framing
applies per person. Rejected: FR-024 says the list belongs to the whole plant.

## D11. Three surfaces need a new catalogue key, and the fourth does not

**Decision**: Add `permission.settings.machine_configuration`, `permission.settings.log_panel` and
`permission.settings.session_length`, plus the `permission.settings.ignored_locations_edit` key from D10. The
existing `permission.settings.computers` is not reused for machine setup.

**Rationale**: The checklist names four privileged surfaces and says four keys. Three of the four genuinely
have no key today: no key in `PermissionKeys.cs` contains `machine`, `log` or `session`. The fourth is the
scoped edit key from D10. For the log panel the role set is `Developer` alone, because US5 and SC-005 both
describe a developer reading one machine's history. For session length it is `IT Department` and `Developer`,
per FR-029. For machine setup it is `IT Department` and `Developer`, per FR-007.

**Alternatives considered**: Reuse `permission.settings.computers` for machine setup, since its baseline is
already `developer` and `it_department` at 1 and every other role at 0, which is exactly the required
restriction. Rejected because the two surfaces have different blast radii: machine setup admits a computer to
the fleet before anyone signs in, while the Settings computers screen edits rows afterwards. Sharing one key
means a future loosening of the Settings screen would silently open the pre-sign-in gate. The rejection is
recorded rather than hidden because the reuse would have been the cheaper change.

## D12. The remembered sign-in needs a table the brief did not name: `auth_remembered_sign_ins`

**Decision**: A new table `auth_remembered_sign_ins`, keyed by person and machine, holding the encrypted
payload, its initialisation vector and a key fingerprint, with procedures to read, upsert and clear it.

**Rationale**: FR-014 and the spec's Key Entities require the remembered sign-in to be held against the person
in the store and to be encrypted so it can be decrypted. The brief fixes the mechanism (encrypted, not hashed;
key from the shared file; held against the person) but names no storage. It cannot live on `user_active_sessions`
because sessions are cleared on sign-out (FR-011) while a remembered sign-in has to survive sign-out, which is
its entire purpose. The `auth_` prefix matches the existing `auth_roles_catalog`, `auth_roles_assignments` and
`auth_sessions_tokens` naming.

**Alternatives considered**: (a) Columns on `core_users_profiles`. Rejected: the choice is per machine
("be recognised on one machine"), and a shared profile row cannot hold a per-machine value. (b) Columns on
`user_active_sessions`. Rejected: the sign-out clearing in FR-011 would destroy it. (c) A local DPAPI-protected
file. Rejected by FR-025: nothing but what is needed to reach the store may stay on the machine, and FR-014 says
held against the person in the store.

## D13. The key file is read at its UNC path, never at `X:`, and is never written

**Decision**: The setting is the UNC path
`\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\MTM_Application_Keys\MTM_AUTH_USER_SECRET_KEY.txt`.
The file is read only. It is never created, written, rotated, logged, copied into the repository or shown in
the panel.

**Rationale**: `X:` is a per-user mapped drive that resolves to `\\mtmanu-fs01\expo drive`. It does not exist
for a different account, a different session or a service, so a path built on it would work for the person who
tested it and fail for everyone else. The file already exists (44 bytes, last written 2026-03-23) and its name
suggests it is shared with other MTM applications' authentication, so rotating it here would have a blast radius
well outside this feature. 44 bytes is consistent with a 32-byte key in base64, which is what AES-256 expects.

**Alternatives considered**: (a) Generate a new key for this feature. Rejected: it would diverge from whatever
already uses the file. (b) Store the key in `appsettings.json`. Rejected by the constitution's Security &
Secrets constraint, which forbids committing secrets.

## D14. Each of the two identity contracts is read-only, and only the pipeline writes

**Decision**: Person identity and machine facts are separate read-only interfaces. Consumers read them. The
launch pipeline is the only writer, and it writes through services the contracts do not expose.

**Rationale**: FR-022 states it directly, and S7 gives the reason: roughly twenty consumers compile against
`StartupState`, and a shared mutable object is what allowed attribution to drift. Two narrow read-only
contracts make the write path a single named place.

**Alternatives considered**: Keep one `StartupState` object and mark it read-only in documentation. Rejected:
nothing enforces a comment, which is why the audit test exists.

## D15. `StartupState` is deleted in Phase 4, in the same change that migrates its consumers

**Decision**: The two contracts are defined and implemented first, the consumers are migrated in the order S7
lists, and only then is `StartupState` deleted.

**Rationale**: The deletion and the migration are one change or the tree does not build. Splitting them
produces a phase in which the contracts exist and nothing uses them, and a later phase in which a large
mechanical migration is mixed with new behaviour. Both are harder to review than the pair.

**Alternatives considered**: Migrate consumers first against the old object, then swap the type. Rejected:
that is two passes over the same twenty call sites.

## D16. Roles come from the store only, and the developer allow-list is retired

**Decision**: `StartupDevelopmentOptions` and its `appsettings.json` section are deleted. The development seed
grants `Developer` to `JKoll` and `JohnK` on both seeded machines.

**Rationale**: An allow-list in a deployment file is a role decision made outside the store, which is exactly
what FR-022 and S2 decision 13 forbid. Granting the two accounts in the seed keeps each seeded machine with a
developer who can sign in, so the developer walkthrough survives without the file.

**Alternatives considered**: Keep the allow-list but read it only in Debug. Rejected: a build configuration is
not an authorisation model, and the no-sign-in walkthrough the UI-automation instructions rely on is a
documentation problem, not a reason to keep an authority bypass. That documentation is updated in Phase 2.5.

## D17. Session length is a setting, defaulting to eight hours

**Decision**: The session length is a row in the scoped settings store, readable at launch and changeable only
by `IT Department` or `Developer`. The resolved value is used when a session row is written, and the row stores
the expiry it was issued with.

**Rationale**: FR-029 and SC-011 require the change to take effect at the next sign-in. Storing the resolved
expiry on the row rather than recomputing from the setting is what makes that true: an in-flight session is
unaffected, and the next sign-in reads the new value.

**Alternatives considered**: Recompute validity from the current setting on every check. Rejected: it would
retroactively shorten or extend a session that was already issued, which is not what "takes effect at the next
sign-in" means.

## D18. `fn_server_utc_now` is retained and every session decision is judged on it

**Decision**: `fn_server_utc_now` survives the dead-weight audit and is the clock for session validity,
expiry and any other time decision on the launch path.

**Rationale**: FR-010 requires the store's clock, not the computer's. `fn_server_utc_now` is the store's clock
and already exists. Retaining it is also the one exception in the otherwise-removable startup-only database set
(S3.3), so the removal phase has to be told explicitly or it would delete a function the new session rule
depends on.

**Alternatives considered**: Compute the expiry in application code from the workstation clock and compare.
Rejected: a mis-set workstation clock would then extend or deny access, which is the fault FR-010 exists to
prevent.

## D19. `ops_startup_logs` is repurposed rather than renamed

**Decision**: The log store stays `ops_startup_logs`, extended with the fields the panel filters on.

**Rationale**: The table is pinned verbatim by the spec, already carries `correlation_id`, `previous_hash`,
`entry_hash` and an index on `created_utc`, and has no writer today, so it is a ready-made store rather than a
new one. The brief allowed either the table or a renamed successor; the verbatim constraint settles it.

**Alternatives considered**: A new `ops_application_logs` table and a migration of the empty old one. Rejected:
it renames a pinned identifier and buys nothing, since the old table has no rows to preserve.

## D20. Log writes are queued and the shutdown flush is bounded

**Decision**: The seam enqueues and batches, drops the oldest entry when the queue is full, and flushes within
a bounded time on shutdown.

**Rationale**: A diagnostic that can delay its caller is worse than a diagnostic that is lost, and S13's
diagnostic guarantees include "never waits". Bounded flush is what makes SC-003 hold for a launch that ends
immediately afterwards.

**Alternatives considered**: Write synchronously on each call. Rejected: a slow store would then slow every
screen, and the store being slow is one of the faults the log is supposed to record.

## D21. Every wait on the launch path gets a stated maximum, with 30 seconds as the ceiling

**Decision**: 30 seconds is the ceiling for any single wait. The picture refresh and the Infor Visual priming
are bounded more tightly because both are best effort.

**Rationale**: FR-003 and SC-002. The sweep found four unbounded waits in the old code, including an unloaded
splash view and a stalled share with no cancellation. Both are named in S12 as must-not-recreate.

**Alternatives considered**: One global timeout for the whole pipeline. Rejected: it cannot distinguish a slow
store from a stalled share, and the diagnostic per step is the point of the feature.

## D22. A reachable-but-empty result is a real answer, and the fallback boundary does not move

**Decision**: The existing external-read fallback behaviour is carried forward unchanged in kind: the verdict
is settled before a screen opens, a reachable-but-empty live read is never replaced by cached data, and an
unreachable source serves the cached copy.

**Rationale**: FR-027 requires the verdict before the main screens open, and the repo's constitution and
`copilot-instructions` treat this boundary as settled. The rebuild changes the step's shape, its bounded wait
and its feed line, not the rule.

**Alternatives considered**: Rely on the picture-cache and priming steps to fail fast and skip the verdict.
Rejected: the shell would then open mid-decision, which the boundary rule forbids.

## D23. Two audit tests are rewritten in the same change as the code they pin

**Decision**: `PermissionGateSiteAuditTests` is rewritten against the new contract, and
`PageActivationAuditTests`' pinned exemption list is re-pinned to the new page set. `RetiredSymbolAuditTests`
is extended with the whole removed surface, plus a self-test proving each pattern bites.

**Rationale**: All three fail the build by design, so leaving any of them until later means the gate that was
supposed to catch a regression is itself the regression.

**Alternatives considered**: Exempt the two pinned tests for the duration of the rebuild. Rejected: an audit
exempted during the change it audits is not an audit.

## D24. Two option sections are verified before anything is deleted, and none is deleted blind

**Decision**: `ModuleSharedOptions` and `ModuleCoreSettingsOptions` are investigated in Phase 5.3 before any
decision about them. If nothing reads them they are deleted; if something does, it is migrated. The check is a
named task, not an assumption.

**Rationale**: Both are bound through `Configure<>` and neither key appears in `appsettings.json`, so they
currently bind to defaults. That means either they are dead or something reads a default value and would change
behaviour if the binding were removed. Deleting a binding whose reader relies on a default is a silent
behaviour change, and the plan will not guess which it is.

**Alternatives considered**: Delete both as dead weight by pattern. Rejected: the evidence does not yet support
it, and a wrong deletion here would surface as a runtime default, not a build error.

---

## Deferred to implementation

Two things are unresolved on purpose and are tasks, not assumptions:

1. **The picture-source shape in `config_images_locations`.** The picture sources are machine configuration
   (S11.5.4) and `config_images_locations` already carries a `computer` scope with `scope_item_id` and a
   `scope_paths_get` procedure, which makes it the natural home. Implementation must confirm that the machine
   scope can express the source-folder shape (shared folder, keys folder, dunnage root) and add whatever is
   missing as `create.sql` plus `rollback.sql`, rather than adding columns on `core_computers_registry`.
2. **The AES mode and payload shape for the remembered sign-in.** The key length is inferred from the file
   size, not read. The mode, the initialisation vector handling and the key fingerprint must be settled against
   documentation before code is written, per constitution Principle IV.
