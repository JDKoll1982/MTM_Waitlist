# Review Checklist — Dependency and Modernization (spec 024)

What you check now that the strict data-access work has landed. Everything here is a decision or an
observation only a person can make; anything a build or a test can settle is already settled and is not asked
of you.

The tree is green as you read this — 0 warnings, 0 errors, 2,908 passed, 66 skipped, 0 failed — and nothing is
being rebuilt, so the application can be launched for the observations in Phase 4. The wording extraction from
the view definitions is deliberately held back until those observations are done, because a rebuild in progress
is the likeliest reason a launch would fail.

The work is uncommitted by your instruction, so this checklist is the gate before it is committed.

**Sources this checklist reflects**

- `spec.md` in this folder — the requirements, `FR-001` to `FR-036`
- `plan.md` in this folder — the phases, and the Constitution Check where your credential exception is recorded
- `tasks.md` in this folder — the tasks and waves named in the `(Ref: …)` on each line
- `Dependency_Assessment/2026-11-26-full-deep/DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md` — the audit this
  feature implements, cited as `(Ref: audit section N)`

**How to read a line**

Each task ends with the role best placed to judge it. Check the box only when you have actually looked; a box
ticked on assumption is worse than an unticked box, because the next person cannot tell the difference.

---

## Phase 1: Establish the baseline before reading anything

### Subphase 1.1: The build and the suite

- [ ] **Build: Run `dotnet build MTM_Waitlist.sln` and confirm 0 errors and 0 warnings.** (Ref: plan.md Verification notes) | **Persona: DevOps Engineer**
- [ ] **Testing: Run `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj` and confirm 0 failed, with 2,915 passed and 66 skipped (2,981 total).** (Ref: tasks.md Phase 5 gate T087) | **Persona: QA Engineer**
- [ ] **Testing: Confirm the one new skip is the gated rollback case, not a test that quietly stopped running.** It skips because no live store was opted in; every other skip is a pre-existing environment gate. (Ref: tasks.md T123) | **Persona: QA Engineer**
- [ ] **Testing: Confirm the 17 architecture guardrails all ran and passed, with `InlineSqlAuditTests` and `RetiredSymbolAuditTests` named explicitly.** A guardrail that was silently skipped is not a pass. (Ref: plan.md Constitution Check) | **Persona: QA Engineer**
- [ ] **Build: Confirm no `NETSDK1206` appears and that no project file carries an inline `Version=` on a package reference.** (Ref: audit section 7, `Directory.Packages.props` rows) | **Persona: DevOps Engineer**
- [ ] **Build: Confirm no `.github/workflows` directory exists, so no CI enforcement was introduced.** (Ref: audit section 15, closing paragraph) | **Persona: Tech Lead**

**GATE: A green build and a green suite before you read a single diff. A red tree makes every later
judgement unsafe, and reading code to explain a failure is the most common way an hour disappears.**

Next task: **Build: Run `dotnet build MTM_Waitlist.sln` and confirm 0 errors and 0 warnings.** | **Persona: DevOps Engineer**

---

## Phase 2: The data-access seam and the five services

This is the newest and least-exercised code in the change. It is also the part with no live database behind it
on this machine, so reading it is the only check available.

### Subphase 2.1: The new contract

- [ ] **Service Layer: Read `MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs` and confirm the strict route exists and that no member accepts caller-supplied statement text.** (Ref: tasks.md T123) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm a provider exception propagates out of the strict route rather than being caught and turned into a neutral result.** This is the whole point of the route; a swallow here silently breaks four callers. (Ref: tasks.md T123) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm the strict route does not retry inside an explicit transaction.** A retried transaction replaying a half-committed unit is worse than a failure. (Ref: tasks.md T123) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm a transaction is committed on success and rolled back on any exception.** (Ref: tasks.md T123) | **Persona: Backend Engineer**
- [ ] **Testing: Confirm one test proves the strict route surfaces a provider exception and one proves rollback when the second statement fails.** (Ref: tasks.md T079) | **Persona: QA Engineer**
- [ ] **Service Layer: Accept or reject the refusing default on the new interface member.** It throws rather than quietly answering, so none of the 40 hand-written doubles can serve a strict call by accident; the alternative is an explicit stub in each double. (Ref: tasks.md T123) | **Persona: Tech Lead**

### Subphase 2.2: The five services

Four moved and one stayed. Judge each on its own terms rather than expecting all five to have changed.

- [ ] **Service Layer: Confirm `UserManagementRepository` moved, both sites, and that the duplicate-key answer and the signalled refusal still reach the same caller with the same meaning.** (Ref: tasks.md T073) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `UserManagementRepository` now reads by column name rather than position, and that the names it asks for are the ones `sp_user_management_list` and `sp_user_management_get` actually return.** This is the one change in the whole wave that could fail silently against a live store, so it is worth reading the two procedures. (Ref: tasks.md T073) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `PermissionAdministrationService` moved and its refusal still reaches `ApplyAsync`.** (Ref: tasks.md T074) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `LuggerArrangementService` sent its write through the seam, that `affected_rows` is still read the same way, and that the missing-procedure and missing-table answers are still distinguished.** (Ref: tasks.md T075) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `WorkCenterCatalogService`'s clear-and-upsert still runs as one atomic unit, with the same connect and command timeouts it applied to its own connection.** (Ref: tasks.md T076) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `ConnectionHealthService` stayed direct, and that the note beside it gives a reason you accept — it applies its own five-second connect timeout, one of its two targets is SQL Server, and the seam cannot express either without changing the probe.** (Ref: tasks.md T077) | **Persona: Backend Engineer**
- [ ] **Service Layer: Confirm `LuggerArrangementService` no longer declares the unused settings parameter, and accept the removal of its private connection helper together with the four tests that covered it.** The test total did not change, so nothing was deleted that another test still needed. (Ref: tasks.md T075) | **Persona: Tech Lead**
- [ ] **Service Layer: Search the application services for direct `MySqlConnection` or `MySqlCommand` construction and confirm every remaining site is named and justified.** (Ref: tasks.md T078) | **Persona: Tech Lead**
- [ ] **Testing: Confirm no test double implemented the new seam member as a silent no-op, which would make the suite pass while the route is never exercised.** The interface default is what makes this checkable — a double that does not override it fails loudly. (Ref: tasks.md T078) | **Persona: QA Engineer**
- [ ] **Testing: Decide whether to run the gated rollback case against a scratch store, since a rollback that has never executed is a claim rather than a proof.** Point the test connection variable at a scratch `mtm_waitlist`, run only the strict-route tests, and confirm all four cases pass. (Ref: tasks.md T123) | **Persona: QA Engineer**

**GATE: The seam read and the five services accounted for before you run the application. If the seam is wrong,
every screen you then test is testing the wrong thing.**

Next task: **Service Layer: Read `MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs` and confirm the strict route exists and that no member accepts caller-supplied statement text.** | **Persona: Backend Engineer**

---

## Phase 3: The credential exception you approved

You chose to keep the four values and record the exception rather than remove them. These lines check that the
record is both honest and bounded.

- [ ] **Security Review: Confirm `appsettings.json` holds no credential value, still names the environment variables, and keeps the non-secret shape.** (Ref: tasks.md T006) | **Persona: Security Engineer**
- [ ] **Security Review: Start the application once with the variable unset and confirm it refuses, naming the variable; then with it set and confirm it starts.** (Ref: spec.md FR-019) | **Persona: Security Engineer**
- [ ] **Security Review: Read governance point 3 under Constitution Check in `plan.md` and confirm the scope, reason, compensating controls and end condition are what you intended to approve.** (Ref: plan.md Constitution Check) | **Persona: Tech Lead**
- [ ] **Security Review: Confirm `workstation-secrets.md` no longer claims the values are already visible in `appsettings.json`, and that it now names itself as the only tracked carrier.** (Ref: tasks.md T122) | **Persona: Security Engineer**
- [ ] **Security Review: Confirm the four disclosed values are queued for rotation on the plant host before this ships — the exception covers the record, not the exposure.** (Ref: tasks.md T025) | **Persona: Security Engineer**
- [ ] **Configuration: Confirm a Release build contains no store fallback at all, by building `-c Release` and checking that `MTM_Waitlist.Core.dll` names no localhost substitution.** The published client must be unable to reach any server but the one it was configured with. (Ref: plan.md Constitution Check, governance point 3) | **Persona: DevOps Engineer**
- [ ] **Configuration: Confirm `MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING` is unset on this machine and decide whether this host should carry it.** (Ref: audit section 10, requirement 2) | **Persona: DevOps Engineer**

**GATE: The exception understood and the rotation owned before anything is committed. A recorded exception
with nobody rotating the values is an accepted disclosure, not a controlled one.**

Next task: **Security Review: Confirm `appsettings.json` holds no credential value, still names the environment variables, and keeps the non-secret shape.** | **Persona: Security Engineer**

---

## Phase 4: The running application

Five checks you said you would run. Do them against a build that is not being rebuilt at the same time; if a
launch fails with a file-in-use error, a build is running and the app is not at fault.

### Subphase 4.1: Screens and display scales

- [ ] **Workflow: Open sign-in, the waitlist list, one request detail, Setup, a generic request and Settings at each display scale you use, and record which scales you covered.** Look for clipping, overlap and blur. (Ref: tasks.md T047) | **Persona: QA Engineer**

### Subphase 4.2: The dialog and the animations

- [ ] **Dialog Behavior: Open the shared message dialog from the mock service settings page and from one waitlist confirmation, and confirm the title, the scrolling body and the buttons read as before.** (Ref: tasks.md T091) | **Persona: QA Engineer**
- [ ] **Workflow: Watch the rotating text on the Setup review screen through several cycles — roughly ten pixels over about 140 milliseconds per half, eased, settling without flicker or a stuck half.** (Ref: tasks.md T086) | **Persona: QA Engineer**
- [ ] **Workflow: From the waitlist list, open a request and come back, confirming the connected animation runs both ways and carries the right row.** (Ref: tasks.md T064) | **Persona: QA Engineer**

### Subphase 4.3: The data path

- [ ] **Testing: Raise one generic request end to end against a store you can reach — local `127.0.0.1` if `mtm_waitlist` is deployed there, otherwise the plant host — and record which you used.** Confirm the request reaches the queue and that any refusal arrives in the queue's own words. (Ref: tasks.md T094) | **Persona: QA Engineer**

**GATE: All five observations recorded, each as pass or as what you saw. "It seemed fine" is not a result;
the next person needs the scale, the screen and the outcome.**

Next task: **Workflow: Open sign-in, the waitlist list, one request detail, Setup, a generic request and Settings at each display scale you use, and record which scales you covered.** | **Persona: QA Engineer**

---

## Phase 5: What is shipping unverified

None of these is a defect to fix now. Each is a known, recorded hole that you are accepting knowingly.

- [ ] **Frontend Engineer: Confirm the literal-wording extraction across the view definitions is either complete or still tracked as `T124`, and that no half-finished page exists.** The Setup views and the deployment tool window were the largest remaining sets. (Ref: tasks.md Phase 9 T124) | **Persona: Frontend Engineer**
- [ ] **Frontend Engineer: Confirm DevWinUI is still at 10.8.0 and that the reason is recorded where a future reader will find it.** (Ref: tasks.md T125) | **Persona: Frontend Engineer**
- [ ] **Tech Lead: Confirm the three sign-in comparison documents are untouched and remain recorded as blocked, rather than regenerated from stale screens.** (Ref: tasks.md T118) | **Persona: Tech Lead**
- [ ] **Tech Lead: Confirm the decimal-comma weight check is recorded as blocked rather than assumed to pass, since you cannot change this machine's regional format.** (Ref: tasks.md T105) | **Persona: Tech Lead**
- [ ] **Tech Lead: Spot-check five ticked tasks from different phases against the tree and confirm each one is really present — a tick with nothing behind it is the failure this repository's guardrails exist to prevent.** (Ref: tasks.md, any ticked phase) | **Persona: Tech Lead**

**GATE: Every unverified item named and accepted before the commit decision. Silence about a hole reads as a
claim that it is closed.**

Next task: **Frontend Engineer: Confirm the literal-wording extraction across the view definitions is either complete or still tracked as `T124`, and that no half-finished page exists.** | **Persona: Frontend Engineer**

---

## Phase 6: The commit decision

You chose to leave the work uncommitted. This phase is what to weigh if that changes.

- [ ] **Release: Confirm you still want the work left uncommitted, given that 200-plus files carry the change and nothing protects them from a stray command.** (Ref: plan.md Baselines) | **Persona: Tech Lead**
- [ ] **Release: If you do commit, confirm both new top-level files are included — `Directory.Packages.props` and the `specs/024-dependency-modernization` folder are untracked and would be missed by a plain `git commit -a`.** (Ref: audit section 7) | **Persona: DevOps Engineer**
- [ ] **Release: Confirm the NuGet package cache is intact on this machine, since a cache clear during an earlier cold restore was interrupted by running processes and repaired in place.** A plain `dotnet restore` succeeding is enough. (Ref: plan.md Verification notes) | **Persona: DevOps Engineer**
- [ ] **Release: Confirm no temporary or scratch file was left behind by the work.** (Ref: plan.md Output) | **Persona: DevOps Engineer**

**GATE: The commit question answered explicitly, with the untracked-file trap in mind, before the session
ends.**

Next task: **Release: Confirm you still want the work left uncommitted, given that 200-plus files carry the change and nothing protects them from a stray command.** | **Persona: Tech Lead**

---

## Already verified — no action from you

These were checked against the tree while this checklist was written, and are listed so you do not repeat them.

- [x] **Configuration: `appsettings.json` carries no credential value and still names both environment variables.** (Ref: tasks.md T006) | **Persona: Security Engineer** — verified 2026-11-27: the working diff shows every value replaced with an empty string and both variable names unchanged.
- [x] **Service Layer: Start-up refuses a machine with no records-store connection instead of reporting a store that answered.** (Ref: spec.md FR-019) | **Persona: Backend Engineer** — verified 2026-11-27: `ConfigurationRequirement` is read by both the settings step and the reachability step, and accepts the shared variable as well as the configured one.
- [x] **Service Layer: The two JSON-backed stores that still used reflection now use generated metadata.** (Ref: tasks.md T126) | **Persona: Backend Engineer** — verified 2026-11-27: `CoreJsonContext` and `DeploymentSettingsJsonContext` are both declared and both used at write and read.
- [x] **Security Review: The credential exception is recorded in the plan, scoped and dated.** (Ref: plan.md Constitution Check) | **Persona: Tech Lead** — verified 2026-11-27: governance point 3 states scope, reason, compensating controls and end condition.
- [x] **Build: The tree builds with 0 warnings and 0 errors, and the suite passes 2,915 with 66 skipped and 0 failed.** (Ref: plan.md Baselines) | **Persona: DevOps Engineer** — verified 2026-11-27: both commands run against this working tree after the strict seam, the JSON contexts, the failure classification and the compile-out of the development store fallback. A Release build also succeeds, and its `MTM_Waitlist.Core.dll` contains no reference to the fallback at all.
