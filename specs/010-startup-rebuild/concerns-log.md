# Concerns cleared

Concerns moved off the Companion's list, each recorded in its own words with the
reason it was cleared. The list in `.spec-context.json` holds open business only;
this file is the trail for the ones that came off it.

## 2026-09-27 — Superseded: the sign-in screen was captured and its figures are recorded.

> T003 could not capture the sign-in form. Both launch configurations auto-signed-in and reached the main screens, because LocalSettings.json carries a live Startup.Session.Token and Startup.Session.ExpiresUtc. Captured: splash 760x460, automation name 'WinUI Desktop'; main window 3456x1408, restore 760x500. Clearing that token would show the sign-in form, but that mutates machine state rather than repository state.

## 2026-09-27 — The deleted-code guard now exists; the retired static logger name is added to it by T189.

> T001, the retired-symbol guard, was not started. It turns the suite red by design until the whole Phase 2 removal lands, so adding it without completing that removal would leave a red suite behind a guard that proves nothing. Phase 2 spans roughly 50 to 70 source files, a new project, 26 database artifacts and the app and DI rewiring.

## 2026-09-27 — Superseded by itself: the screen was captured and the file was restored byte-for-byte.

> Supersedes the earlier T003 concern. The sign-in form was subsequently captured (820x760, automation name Sign in, buttons Sign In and Back) by temporarily clearing the remembered session, with LocalSettings.json backed up and then restored byte-identical (SHA-256 verified). T003 is complete and all three pre-removal windows now have a baseline figure.

## 2026-09-27 — The failure was the check tool aborting on a harmless warning, not the database. The tool was corrected and now passes.

> The first recorded run of the local schema validator shows exit 1. That was a defect in the harness script, not a schema failure: the script set ErrorActionPreference to Stop, so the mysql client note on stderr about a password on the command line aborted it. The corrected script passes the credential through MYSQL_PWD and exits 0 with zero issue rows; its run is recorded as the entry labelled corrected harness. Read the exit 1 entry as a harness artefact.

## 2026-09-27 — The tool was over-counting the column header as a problem row. Verdicts unchanged: three clean, and the part-pictures findings predate this feature.

> Corrects two recorded validator checks. The temporary harness scripts counted the mysql batch column header as an issue row, so the all-four-validators entry over-states its issue-row count by one. The verdicts stand: startup, settings and user_management are clean, and part_pictures has three real stale routine bodies that predate this feature.

## 2026-09-27 — The tool no longer cuts a statement at a semicolon inside a comment, and both rollback files now drop their procedure rather than recreating it.

> The repository schema validator cannot exit zero whenever a connection string is configured: it splits each seed file on every semicolon and seed_dev_masked_baseline line 216 is a comment containing one, so the statement is cut in half and MySQL rejects it. That comment is byte-identical at HEAD and the defect predates this feature; it was invisible until now because earlier runs skipped the live phase. Two smaller items: the rollback scripts for sp_auth_credentials_check and sp_auth_password_reset_required_get recreate rather than drop their procedure, so the local store keeps two retired procedures no artifact creates. And my own repair pass was briefly wrong: applying seed_dev_masked_baseline after seed_permission_role_baselines truncates config_settings_values at its line 119 and wiped all thirty-six baselines; re-applying the baselines in the correct order restored them and the table holds thirty-nine rows.

## 2026-09-27 — The seed helper is retired and its removal is written down, and every remaining mention of the deleted logging service is a note saying it was deleted.

> Two findings from the validator repair that no task owns. First, Database/Seeds/seed_picture_layout_move creates a helper procedure, calls it and never drops it, in both its folder file and the seeds aggregate, so the local store carries sp_seed_picture_layout_move with no create.sql artifact; its sibling seed does drop its helper and says so in its header, so this is an inconsistency rather than a convention, and the fix is two lines once someone owns it. Second, deleting the orphaned logging contract left stale prose in .github/instructions/winui3-ui-automation.instructions.md and two capability specs that still describe the deleted service as the live log path. Also worth recording: the local store legitimately carries two seed-created objects beyond the artifact set, so its 119 objects against 117 artifacts is explained and not drift.

## 2026-09-27 — The contract now states every step and every time limit, and the grouping is reconciled against the brief by T190.

> The launch-step contract pins the shape of a step, the categories, the ceiling and the two best-effort steps, but it names no steps and no numbers, so the eleven-step sequence and every stated maximum in T074 were chosen by the implementer rather than taken from the contract and should be reviewed against the brief. LaunchRemedySet was also created because LaunchStepOutcome requires it, and no task owns it.

## 2026-09-27 — Environment limit: the shared store cannot be reached from this machine. The four tasks were proved against a local copy of the same schema; confirming the shared host needs a machine that can reach it.

> The shared store at 172.16.1.104:3306 does not accept a connection from this machine (TCP probe times out after 5 s), so four later tasks cannot be completed or verified here: T024, T033, T034 and T063. A separate MySQL listens on 127.0.0.1:3306.

## 2026-09-27 — Environment fact, and already the project's own guidance: use python, never python3.

> python3 is absent here and the py launcher's default interpreter (C:/Python314) is broken with 'Failed to import encodings module'. The working interpreter is 'python' (3.10.11).

## 2026-09-27 — Recorded with the environment limit above: the local copy stands in for the shared host.

> The unreachable shared store no longer blocks T024, T033, T034 and T063 from being verified: the local MAMP MySQL carries a provisioned mtm_waitlist schema matching this features pre-removal baseline object for object. Verifying against it proves the schema artifacts are correct, but not that the shared host matches, so it substitutes for the checks and not for the environment.

## 2026-09-27 — Fixed. The store's three procedure bodies were behind the artifacts, and the part-pictures check was demanding strings that belong to the triggers and to the caller rather than to the procedure bodies. The bodies were brought current and the three assertions corrected; the check now passes.

> Corrects the earlier claim that the local MAMP store is a faithful baseline for this feature without qualification. Running all four repository validators against it: startup_schema clean, settings_schema clean, user_management_schema clean, part_pictures_schema reports three stale_routine_body issues (sp_config_images_locations_paths_move, sp_config_images_locations_update, sp_config_images_locations_insert). The local store was therefore provisioned before feature 008 part pictures landed its procedure changes. It is current enough for this features startup schema work, but it must be re-provisioned from Database before any verification that spans the images locations procedures, and the exit code of one recorded check reflects that staleness rather than a script fault.

## 2026-09-27 — The timestamps are coarse because one pass did the whole removal. The order and the per-task notes are accurate.

> The 22 recorded task finishes cluster in time because one agent run completed the whole removal in a single pass, not because the record was batched as a shortcut. The per-task summaries and their order are accurate; the timestamps are best-effort and the doctor will flag the cluster.

## 2026-09-27 — Accepted: the reachability check has to run at launch to settle the cached-data verdict. It opens no window and writes nothing to the work databases.

> Residual in the launch path for the T033 proof: the App constructor still starts IVisualReachabilityProbeHost. It is Mock-module machinery registered by AddVisualReadFallback rather than by startup, it opens no window and blocks nothing, but it does probe the external source, so either stop it in the launch phase or record that the T033 no-store-read check accepts it.

## 2026-09-27 — Accepted with 6: the check reads only the local copy of external data, so the no-store-read claims hold for the work databases.

> Disclosure qualifying T030 and T033. The App constructor starts IVisualReachabilityProbeHost, whose background cycle reads the mtm_mock mirror through sp_visual_read_shape_freshness_get and issues no statement against any internal store. It was observed directly: while the app ran, its only established connection was to the local MySQL host with database mtm_mock, and that session vanished when the app closed. The no-store-read clauses therefore hold for the internal operational stores and are qualified for the mock mirror, which is the Visual verdict priming the plan keeps at launch. T030 was ticked for the placeholder being the only launch window; this is its disclosure.

## 2026-09-27 — Done: the two deleted file names were removed from both tooltip strings in ShellPage.xaml, the mis-pathed entry went with them, and the guard's two exemptions were withdrawn. Build clean, guard passing.

> T019 is deliberately not done: Module_Core/Views/ShellPage.xaml has owner edits in progress, and two TooltipBehavior.AssociatedFiles strings still name the deleted SignInSignInKeys-era file SignInSessionKeys.cs and the mis-pathed StartupState.cs. They are inert strings and caused no build error.

## 2026-09-27 — Owned by T189 to T194, which carry the reconciliation: the launch step's new field, the retry names the step split retired, and the documents still describing the deleted logging service as present.

> The code and the written spec now disagree in four places, all in files the owner has open, so I left them. LaunchStep gained a Target member, which data-model.md and the LaunchStep record shown in tasks.md do not show. STARTUP-REBUILD-BRIEF.md, STARTUP-REBUILD-CHECKLIST.md and tasks.md still describe the deleted logging service as a present type. And the step ids changed for every split entry, so the ids configuration, machine-readiness, machine-setup, session, sign-in, machine-gate and password-change no longer resolve as retry keys; only store-reachability, picture-cache, visual-priming and shell survived. One judgement to review: read-hardware-identity remains a single entry although it reads both the computer name and the hardware address, because they come from one local step with no external wait.

## 2026-09-27 — Owned by T189 to T194, which carry the outstanding reconciliation the owner approved.

> The 2026-09-27 analysis left this reconciliation outstanding, in the order the owner approved. Done already: the contract now states all seventeen maximums so FR-003 is verifiable against an artifact, and the data model step entity now carries the Target member. Outstanding, all in files the owner has open so they sit uncommitted on purpose: the stale type names across plan.md, research.md, tasks.md, STARTUP-REBUILD-BRIEF.md, STARTUP-REBUILD-CHECKLIST.md and two contracts, since none of them may be committed without sweeping the owner in-flight edits into my commit; the destructive approval recorded in the plans own Complexity Tracking rather than only in this ledger; the splash requirement added to spec.md; and the two task text corrections, T069s claim that the audit would flag the two doc comments when the audit in fact carries no pattern for that type, and the retry ids in T077, T121 and T125 which still name step ids the split retired. Also outstanding: coverage entries for FR-032 to FR-038 and for the seventeen success criteria, the three addendum phases reconciled against their main phases, and tasks created for the four unledgered changes. Two tasks stay blocked and should not be forced: T060, whose only consumer belongs to task T188 the plans last, and T019, which edits one of the owner uncommitted files.

## 2026-09-27 — All six database checks now pass against the fully installed database, so the checks are no longer running ahead of the migration.

> Sequencing hazard disclosed by the store-foundation unit. The amended startup_schema validator now asserts the four new columns and six indexes, so it reports ten issues against any store that has not yet been migrated, and the two new validators were appended to install_local_database.vbs deploy list, so a fresh local install performed before T057 and T058 register the new tables in the aggregates will report tables 34 and 35 as missing. That is the wiring running ahead of the aggregate registration those tasks own, and the order matters.

## 2026-09-27 — Both report clean against the fully installed database: the settings check and the startup check each exit with no issue rows.

> Two validators now assert store shapes no store yet has: settings_schema reports 36 issues and startup_schema reports 10 against an un-migrated store. Both are the new assertions working ahead of a migration, and both fall to zero once a store is provisioned through Database/install_local_database.vbs, which should happen after T062 has removed the retired entries.

## 2026-09-27 — The four missing admin role baselines are gone. The settings check reports no issue rows against the installed database, so T063's stated blocker no longer holds.

> T063 is left unticked although the schema validator runs: settings_schema reports four issues, all missing_role_baseline for role:admin across the four new permission keys, and the role-baseline seed deliberately covers nine roles that do not include admin because a separate seed retires Admin in favour of IT Department, so the expected role set and the seeded role set disagree. That belongs to the unticked T053. The store is otherwise sound: startup, user management, remembered sign-ins and active sessions all validate clean, and part_pictures_schema keeps its three pre-existing stale-routine-body findings. Separately, .github/scripts/validate-database-schema.ps1 cannot exit zero whenever a connection string is configured: it splits each seed file on every semicolon and seed_dev_masked_baseline line 216 is a comment containing one, so the statement is cut in half and MySQL rejects it; that comment is byte-identical at HEAD, the defect predates this feature, and it was invisible until now because earlier runs skipped the live phase. Two more items: the rollback scripts for sp_auth_credentials_check and sp_auth_password_reset_required_get recreate rather than drop their procedure, so the local store keeps two retired procedures no artifact creates. And my own repair pass was briefly wrong: applying seed_dev_masked_baseline after seed_permission_role_baselines truncates config_settings_values at its line 119 and wiped all thirty-six baselines; re-applying the baselines in the correct order restored them and the table holds thirty-nine rows.

## 2026-09-27 — Cleared with the T063 note above: the settings check now reports no issue rows, so the missing admin baselines are resolved.

> T063 is unticked although the validator runs: settings_schema reports four issues, all missing_role_baseline for role:admin across the four new permission keys. The role-baseline seed covers nine roles that exclude admin, because a separate seed retires Admin in favour of IT Department, so the expected role set and the seeded role set disagree. That belongs to the unticked T053. Everything else validates clean apart from the three pre-existing part_pictures stale-routine-body findings.

## 2026-09-27 — Accepted: the launch pipeline that calls those four pieces is a later wave and carries its own tasks.

> The identity writer seams have no production caller yet. ResolveAsync, RefreshAsync, Apply and Clear are the launch pipeline calls, and the pipeline is a later wave, so they are currently uncalled in production code.

## 2026-09-27 — T053 stays unticked by design. The missing preference rows belong to the tasks that own those settings, and inventing a session-length name would collide with the task that owns it.

> T053 is only half done and its box is therefore left unticked. Its four-keys-per-role half is implemented and verified role-explicitly across all nine catalogue roles. Its second half, the plant-scope rows and the seed rows the seven preferences need, is not: the five person-scoped preferences are per-person rows the person own choice writes, the ignored-locations live value is still a local file whose move is T142 write path, and the session-length setting key exists nowhere in the repository so a row for it would invent the name that T147 owns. Inventing two key names was judged worse than the gap.

## 2026-09-27 — Proven against the real store: the app was run and wrote 9,181 rows to the log table between 07:26:07 and 07:26:31, so the seam is no longer verified only through stand-ins.

> The logging seam is verified only through fakes: the store procedures exist as artifacts and are not deployed, so sp_ops_startup_logs_insert has never executed against a real store. payload_json.database carries the procedure, the database and the operation but leaves the server alias, timings, retry attempt and statement fingerprint null, and payload_json.ui carries only the caller target because the contract forbids touching the dispatcher during shutdown. T175-T182 chain normalisation, fingerprint normalisation and parameter redaction are deliberately deferred, and appsettings.json still carries the inert StartupLoggingOptions section until T133 deletes the type.

## 2026-09-27 — Accepted: the three surviving procedures retire with the computer registry service, and the seeds index mismatch predates this feature.

> T020 is deliberately left unticked: it names nine procedures to delete, six were deleted and three were kept because the rebuilt startup services still call them (ComputerRegistryService for the by-mac lookup, by-name-and-mac lookup and update-by-mac, and MachineFactsService for the by-name-and-mac lookup). Deleting them would leave live code calling procedures no install creates, and the surviving protected lookup matches on name alone, which would weaken the machine gate. They should retire with ComputerRegistryService. The rollback drops for the six deleted procedures were kept, matching 41 other rollback-only procedure folders, 35 of them pre-existing. Two further items: the aggregate validator reports 120 artifacts creating 120 distinct objects after the six removals, down from 126, and its live phase is skipped because no connection string is configured, so the aggregate-and-tree agreement was proved by exact comparison instead; and the seeds aggregate has 11 headers for 12 seed folders, with two headers carrying the same name, which is pre-existing and untouched.

## 2026-09-27 — The obstacle cleared: nothing in the seeds, the validation files or the procedure artifacts names the buildings table any more, and it is absent from the installed database. The task is settled separately against the current state.

> T061 cannot be executed as written. The enumeration found core_buildings_catalog is written by Database/Seeds/seed_dev_masked_baseline (create, rollback and the AllSeeds aggregate) and that Database/Validation/settings_schema/validate.sql asserts all three buildings objects exist; neither file is named by any task, so T062's file list is incomplete too. The seed consumer is sharp because the schema validator applies every seed on its success path, so the live validator cannot pass while a surviving seed writes a dropped table. T060 is executable, but its one consumer UserManagementLiveIntegrationTests belongs to the unticked final task T188, so deleting the table now leaves that test naming a table no artifact creates. Separately, eight deployed objects show no consumer by text and five of those are objects this feature just created whose consumers are the unbuilt log panel and machine configuration, so they must not be mistaken for dead weight; the first enumeration pass also wrongly read two objects as consumerless because procedure bodies were missing from the corpora, and both are in fact consumed by other procedures.

## 2026-09-27 — Task T195 now owns it: the stored procedure that returns every role a person holds, and the read in the identity service that uses it.

> Disclosed gap in IPersonIdentity: HeldRoleCodes is only partly store-backed. No existing stored procedure returns all of a person role assignments and Database is out of scope for this unit, so PersonIdentityService.ResolveAsync applies only the single role the existing logon read returns, while Apply accepts the full set for the sign-in step T116 to supply. A later database task must add the reader that returns every held role.

## 2026-09-27 — Task T196 now owns it: correct the logging contract's section 5 and the research note's D8 to state where the facade lives and why, and reconcile the standalone call-site count.

> Migrating the static logging surface forced a decision the spec never made: the facade that 418 call sites now resolve cannot live in the logging module, because that module already references MTM_Waitlist.Core and Core holds 39 of the call sites, so that placement would be a circular reference. It was put in MTM_Waitlist.Core, in the namespace the retired type occupied, which is why the migration needed no per-file using edit; contract section 5 and research D8 premised the opposite and should be amended. Separately the contract counts seven standalone ILogger call sites but only six exist plus one doc-comment mention, so its 226 total holds only on that reading. And IStartupLogService with its StartupLogService implementation now have no production consumer at all: the old App.xaml.cs line passed GetService<IStartupLogService>(), which was registered nowhere, so the retired static log had been forwarding to null in production for as long as that line existed.

## 2026-09-27 — Two service test classes added — 25 tests over the machine facts service and the person identity service, all passing. The address detection is pinned to the form the store matches on, and the registry read to the pair of values and the procedure it uses.

> Machine address detection in MachineFactsService is new rather than following an existing reader, because the repository had none: it takes the first operational physical adapter carrying a six-byte address. The two new services have no dedicated unit tests; the fakes are exercised through their consumers.

## 2026-09-27 — Traced and closed. The removal commit took out exactly four test declarations naming the deleted type and added none, so all four missing results are attributed. The 'declarations fell by three' figure was my own mis-measured count, which the note itself flagged as unreliable; measured the same way at both revisions the count falls by four.

> Open accounting item against T034 discipline. The identity unit changed the suite total from 1471 to 1467 and the passed count from 1425 to 1421, while the count of tracked test method declarations fell only from 1385 to 1382, so three net declarations removed. The four removed results are consistent with four declarations removed and one added, or with one removed declaration being parameterised, but I could not prove which, and my first attempt at the finer accounting used a mis-escaped pattern so it is not evidence. The removed tests are the StartupState model tests in CoreModelsTests and StartupModelsTests; the four T005 guard tests were separately confirmed still present. One test result is therefore unattributed.
