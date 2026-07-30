# Module_Startup Migration Plan

## 1. Module Purpose
Own startup, bootstrap, and lifecycle initialization responsibilities that currently live in the app shell.

## 2. Ownership Boundaries
- Owns startup orchestration contracts and bootstrap services.
- May depend on Module_Core and Module_Shared.

## 3. Allowed Inbound Dependencies
- Module_Core
- Module_Shared

## 4. Allowed Outbound Dependencies
- None to feature modules.

## 5. Exact Current Files/Folders to Move Here
- Services/StartupCoordinator.cs
- Services/StartupLogService.cs
- Services/StartupLogForwarder.cs
- Services/StartupRegistrationService.cs
- Services/StartupRecoveryService.cs
- Services/StartupSessionRepository.cs
- Models/Startup*.cs

## 6. File-by-File Change Requirements
- Keep startup behavior stable while moving the relevant files into a dedicated startup module.
- Ensure startup lifecycle services still resolve from the host.

## 7. DI Registration Plan
- Module_Startup exposes AddStartupModuleServices.
- Root composition wires it through Module_Core.

## 8. Build/Reference Impact
- No new project references required.

## 9. Regression Risks and Mitigations
- Risk: launch ordering changes break startup flows.
- Mitigation: preserve registration order and service lifetimes.

## 10. Validation Checklist
- Build succeeds.
- Startup orchestration still executes.
