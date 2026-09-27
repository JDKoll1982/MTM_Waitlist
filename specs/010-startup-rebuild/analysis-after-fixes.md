# Specification Analysis Report — After Fixes

Feature: `010-startup-rebuild` · Verification pass: 2026-09-26 · Constitution: v1.2.0

This is the re-analysis required after the fixes were applied. `analysis.md` is deliberately left untouched:
it is the pre-fix report and carries the owner's per-finding dispositions in its ID column, so overwriting it
would destroy the decision record. Read the two together: `analysis.md` for what was found and chosen,
this file for what changed and what now stands.

Source of the fixes: the owner's dispositions supplied on 2026-09-26, applied to `tasks.md`, `plan.md`,
`research.md` and one new artifact.

---

## Findings, and their state after the fix pass

| ID | Severity | Disposition chosen by the owner | Applied | Verified at |
|----|----------|--------------------------------|---------|-------------|
| I1 | HIGH | Restate T053/T054 role-explicitly; `log_panel` to `Developer` alone | Yes | tasks.md T053, T054 |
| I2 | MEDIUM | Reconcile the plan's constraint sentence with the user-story ordering | Yes | plan.md "Delivery phases" preamble |
| G1 | MEDIUM | Add a task per uncovered edge case | Yes — T171, T172, T173, T174 added in a new Polish wave | tasks.md Phase 9, Dependencies summary |
| U1 | MEDIUM | Name one owner for the reset and say what the other service does | Yes — `IMachineConfigurationService.ResetToDefaultsAsync` is the mechanism; `StartupRecoveryService` owns the policy | tasks.md T075, T123 |
| I3 | MEDIUM | Add the severity+machine case to T129 | Yes | tasks.md T129 |
| I4 | LOW | Correct T013 to Wave 15 | Yes | tasks.md T013 |
| I5 | LOW | Build and validate a migration map of the local-state test surface | Yes — new artifact, and its two counts reconciled | `local-state-test-migration.md`; plan.md Scope; tasks.md T152, T157 |
| I6 | LOW | Re-derive the count before T070 is ticked | Yes — **226 call sites in 26 files** (219 `_logger.` plus 7 `logger.`/`Logger.`), which is exactly what the logging contract states. An earlier revision of this row said 193; that counted only `_logger.` sites outside `MTM_Waitlist.Mock.Service` and was wrong. No artifact figure needed changing | tasks.md T066, T070 |
| G2 | LOW | Add the DI extension file to T017's outputs | Yes | tasks.md T017 |
| A1 | LOW | Reword the research title | Yes | research.md D11 |
| I7 | LOW | Adjust the plan's tree comment | Yes | plan.md project tree |
| I8 | MEDIUM | Record that SC-012 pins the read restriction for ignored locations | Yes | tasks.md T139, T149 |

**All twelve findings are closed.** No finding was left deferred.

---

## Findings raised during the fix pass

Four issues surfaced after the first analysis: three while the I5 map was built, and one when the
exception-logging reference was reconciled with the repository. All four are fixed.

| ID | Severity | Finding | Fix |
|----|----------|---------|-----|
| I9 | MEDIUM | `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` is listed by S11.5.5 among the files "kept and re-pointed", but T025 deletes it with the old sign-in surface. The deletion is right and the brief's classification is wrong, which made the kept list twelve files instead of eleven. | Recorded in `local-state-test-migration.md` §1C and cited from tasks.md T025 |
| I10 | LOW | T137 named `MTM_Waitlist.Tests/Module_Settings/ViewModels/SettingsViewModelTests.cs`; the file is at `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`. Executing T137 as written would have split the ignored-locations coverage across two files. | tasks.md T137 path corrected |
| I11 | LOW | `LocalSettingsOptions_Defaults` and `LocalSettingsOptions_AcceptsConfiguredValues` construct the type T152 deletes; no task removed or rewrote them. | tasks.md T152 now names both test files |
| I12 | HIGH | `MTM_Waitlist_Exception_Logging_Reference.md` contradicted the architecture in four places: five new `app_error_*` tables beside the single `ops_startup_logs` store; CamelCase columns and `BIGINT UNSIGNED`/`DATETIME(3)`/native `JSON` types against the repo's snake_case, MySQL 5.7 and `MEDIUMTEXT` conventions; parameterized MySQL commands from C# against the stored-procedure rule; and a **durable local queue** with a local fallback file, which FR-025 and SC-003 forbid outright. | Reference reconciled in place; FR-032 to FR-037, SC-014 to SC-016, D25 to D28, the logging and SQL contracts, the data model, the plan and tasks T047–T050, T064, T065, T130, T131 plus the T175–T182 addendum

---

## Metrics after the fix pass

- Total Requirements: **55** (38 FR + 17 SC), all covered — coverage **100%**
- Total Tasks: **184** (170, plus T171–T174 for the spec edge cases, T175–T182 in the logging addendum, T183–T184
  for the panel's copy)
- Critical issues open: **0**
- Findings open: **0**
- Constitution violations: **0**
- Residual wording drift: **1**, recorded below

## One residual, left deliberately

`spec.md` SC-005 says "filtered by **severity** and machine" while US5 acceptance scenario 2 says "filters by
machine and by **error kind** together". I3's chosen fix was to test both pairs in T129 rather than to edit
`spec.md`, so the behaviour is now covered either way and the drift is cosmetic. If the wording should be
aligned, that is a `spec.md` edit and belongs to the owner, not to this pass.

## Next Actions

- The artifacts are consistent and the feature is ready for `/speckit.implement`.
- Resolve the residual SC-005 wording if it bothers a reader; nothing depends on it.
- Note for implementation: the three `*.full.md`-style side files were not touched, and `analysis.md` retains
  the dispositions, so a later re-run of `/speckit.analyze` will regenerate findings rather than merge with them.
