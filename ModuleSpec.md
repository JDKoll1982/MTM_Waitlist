# Module Refactor Execution Checklist

- [x] Review the modular refactor brief and confirm the target architecture and constraints
- [x] Analyze the current Waitlist structure and identify likely module boundaries
- [x] Create the initial module folder scaffold for core/shared/waitlist/settings/startup/reporting modules
- [x] Create mirrored module-oriented test folders under MTM_Waitlist.Tests
- [x] Create a repository-level modularization master spec at the root
- [x] Create module migration plan documents for the scaffolded modules
- [x] Move the main waitlist/settings/startup feature assets into their module folders
- [x] Move the remaining infrastructure services into module-owned folders where they fit the new boundaries
- [x] Wire module-level DI registration services into the root composition pipeline
- [x] Update the app and tests to compile against the new module-owned namespaces
- [x] Add or adapt missing shared/core artifacts from the Receiving-style architecture (for example shared helpers, cross-cutting config, and reusable infra abstractions)
- [x] Document the Receiving-to-Waitlist migration matrix and the highest-risk areas in the master spec
- [x] Validate all module migration documentation remains implementation-ready for follow-up passes
- [x] Re-run the full build and tests after the final module asset additions

You are continuing a modular refactor of this repository using strict architecture and zero-regression discipline.

## Target Repository
- Repo: `JDKoll1982/MTM_Waitlist`
- Use this reference architecture for module style, boundaries, and folder approach:
  - `C:\Users\johnk\source\repos\MTM_Receiving_Application`

---

## Core Refactor Goal
Break down the current application into self-contained modules with strict isolation boundaries, following the same modular style as `MTM_Receiving_Application`.

### Non-negotiable rule
A module must be a self-sustained folder.  
The only legal cross-module connection points are:
1. `Module_Core`
2. `Module_Shared`
3. DI wiring through designated dependency injection services

No feature module may directly reference another feature module.

Example:
- `Module_Waitlist` must NOT directly call `Module_Setup`
- shared communication must happen via contracts/models/services in `Module_Shared` (or core abstractions if appropriate)

---

## Explicit Decisions You Must Implement
1. Use the same module naming/layout style as Receiving (`Module_<Feature>`).
2. Include settings modularization style comparable to Receiving (`Module_Settings.*`) where appropriate.
3. Remove standalone `MTM_Waitlist.Core` project concept via **Option A**:
   - Keep one application project file.
   - Move/organize core files into `Module_Core` folder.
   - Do NOT create a separate new core project.
4. Keep existing root namespaces unchanged (do not rename root namespaces globally).
5. Enforce boundaries via conventions + instruction docs (no analyzer/tooling enforcement required right now).
6. Strict module isolation remains mandatory by architecture and code review.
7. DI structure must be:
   - Root DI orchestrator: `Module_Core/Services/DependencyInjection`
   - Each module has its own DI registration service, e.g.:
     - `Module_Waitlist/Services/DependencyInjection`
   - Root DI calls each module’s DI extension/registration in one composition pipeline.
8. Stay on the **current branch** (do not create a new branch).
9. Keep this as one continuous effort (no PR splitting strategy needed).

---

## Architecture & Coding Constraints
1. Preserve MVVM flow:
   - View → ViewModel → Service → DAO → Database
2. No ViewModel direct DAO/DB access.
3. Keep `x:Bind` usage pattern (do not convert to runtime `{Binding}` unless already present and justified).
4. Keep business logic out of `.xaml.cs`.
5. Async methods end with `Async`.
6. Avoid regressions and keep behavior unchanged while restructuring.
7. Prefer incremental, reversible phases.

---

## Work You Must Do Now (This Run)

## Part A — Planning + Scaffolding (Required)
### Step 1: Analyze
- Inspect current `MTM_Waitlist` structure.
- Compare against `MTM_Receiving_Application` module architecture.
- Infer natural feature boundaries for Waitlist.
- Identify likely high-risk workflows and migration-sensitive areas automatically.

### Step 2: Propose module map
- Produce explicit target module list, including at minimum:
  - `Module_Core`
  - `Module_Shared`
  - `Module_Waitlist`
  - additional modules inferred from codebase reality (e.g., setup, reporting, settings variants, etc.)
- Explain why each module exists and what it owns.

### Step 3: Scaffold folders
- Create root-level folders for each proposed module.
- Create mirrored high-level test folders in `MTM_Waitlist.Tests` aligned to proposed module boundaries (folder scaffolding only, no test rewrites yet).

### Step 4: Create module migration plans
Inside each module folder, create `MIGRATION_PLAN.md` with all sections below:

1. Module Purpose  
2. Ownership Boundaries  
3. Allowed Inbound Dependencies  
4. Allowed Outbound Dependencies  
5. Exact Current Files/Folders to Move Here (full relative paths)  
6. File-by-File Change Requirements (namespace adjustments, constructor/DI updates, interface extraction, using updates, access modifiers, etc.)  
7. DI Registration Plan (module-level DI + root DI hook)  
8. Build/Reference Impact  
9. Regression Risks and Mitigations  
10. Validation Checklist (compile, tests, smoke checks relevant to this module)

### Step 5: Create master migration spec
At repository root, create:
- `MODULARIZATION_MASTER_SPEC.md`

This file must include:
1. Current-state architecture summary
2. Target-state modular architecture summary
3. Dependency rules matrix (what can depend on what)
4. Phase-by-phase migration sequence
5. Critical path
6. Rollback plan per phase
7. Test strategy using `MTM_Waitlist.Tests`
8. Manual UI smoke checklist template
9. High-risk workflow analysis:
   - Identify risky areas by reading current code
   - Rank risks
   - Recommend migration order to reduce regressions

---

## Part B — Add Missing Core/Shared Assets from Receiving (Required in this same run)
Inspect `MTM_Receiving_Application` and identify reusable core/shared capabilities Waitlist lacks, including but not limited to:
- Infor Visual related services/integrations
- MySQL related services/infrastructure
- Helpers/utilities
- Converters
- Shared infrastructure abstractions
- Reusable config and cross-cutting service patterns

### For each candidate:
1. Classify as:
   - Core
   - Shared
   - Feature-specific (migrate only if applicable)
2. Decide action:
   - Copy as-is
   - Adapt
   - Skip (with reason)
3. Choose target module path in Waitlist.
4. Define DI registration location and lifetime.
5. Document risks and compatibility considerations.

### Implement now:
- Add/scaffold missing interfaces/services/helpers/converters/config artifacts in the correct module folders.
- Add/update module-level DI registration files.
- Update root DI orchestrator `Module_Core/Services/DependencyInjection` to call module registrations.
- Do not introduce direct feature-module dependencies.

---

## Validation & Quality Gates
After changes:
1. Build must pass.
2. `MTM_Waitlist.Tests` must pass (or clearly report blockers).
3. Provide/update manual UI smoke checklist.

If commands cannot run in environment, provide exact commands and expected results for local execution.

---

## Strict Reporting Requirements
At the end, output:

1. **Folders created**
2. **Files created**
3. **Files updated**
4. **Receiving → Waitlist migration matrix** (required table):
   - Receiving Path
   - Waitlist Path
   - Action (Copy / Adapt / Skip)
   - Reason
5. **Top 5 highest-risk areas discovered**
6. **Recommended Phase 1 execution set**
7. **Any assumptions made**
8. **Any unresolved blockers**

---

## Output/Execution Rules
- Be concrete and path-accurate (no vague placeholders).
- Do not fabricate existing source paths when mapping; verify real paths before referencing.
- If a boundary decision is ambiguous, document assumption in the relevant plan.
- Keep plans implementation-ready for follow-up execution passes.
- Keep behavior stable and regression risk minimized.