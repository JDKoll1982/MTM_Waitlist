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
