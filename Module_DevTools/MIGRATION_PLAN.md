# Module_DevTools Migration Plan

## 1. Module Purpose
Provide developer-only tooling for rapid configuration workflows, starting with a Request Type Builder that writes metadata to the mtm_waitlist database.

## 2. Ownership Boundaries
- Owns developer tooling pages, view models, and module-scoped services.
- May depend on Module_Core and Module_Shared.

## 3. Allowed Inbound Dependencies
- Module_Core
- Module_Shared

## 4. Allowed Outbound Dependencies
- None to feature modules.

## 5. Exact Current Files/Folders to Move Here
- New module scaffold. No existing files were moved in this pass.

## 6. File-by-File Change Requirements
- Add the Request Type Builder view and view model.
- Add a dedicated module database service for request type persistence.
- Keep all developer-tooling behavior inside Module_DevTools.

## 7. DI Registration Plan
- Module_DevTools exposes AddDevToolsModuleServices.
- Module_Core composes it during startup.

## 8. Build/Reference Impact
- No new project references required.

## 9. Regression Risks and Mitigations
- Risk: developer-only page accidentally exposed to non-developer users.
- Mitigation: shell navigation visibility uses existing developer-role gating.

## 10. Validation Checklist
- Build succeeds.
- Page route resolves through PageService.
- Creating a request type writes to new devtools tables through stored procedures.
