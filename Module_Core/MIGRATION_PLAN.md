# Module_Core Migration Plan

## 1. Module Purpose
Keep the shared infrastructure layer for the Waitlist application. This module owns cross-cutting services, configuration, and foundational abstractions that other modules may consume.

## 2. Ownership Boundaries
- Owns core contracts, configuration models, and shared module bootstrapping.
- Must not depend on feature modules.
- Exposes services through abstractions and DI extensions.

## 3. Allowed Inbound Dependencies
- None from feature modules.
- May consume shared abstractions only.

## 4. Allowed Outbound Dependencies
- Module_Shared for reusable helpers and converters.
- Root app infrastructure for startup and configuration.

## 5. Exact Current Files/Folders to Move Here
- Existing root services: Services/ServiceRegistrationExtensions.cs
- Existing root contracts: Contracts/Services/
- Existing root models: Models/Startup*.cs, Models/LocalSettingsOptions.cs
- Existing root helpers: Helpers/RuntimeHelper.cs, Helpers/ResourceExtensions.cs

## 6. File-by-File Change Requirements
- Create module-specific contracts and implementations under Module_Core.
- Keep root namespaces unchanged while moving files into the module folder.
- Update DI registration to use the Module_Core dependency-injection extension.

## 7. DI Registration Plan
- Module_Core exposes ModuleDependencyInjectionExtensions.
- Root App registration calls AddModuleServices.

## 8. Build/Reference Impact
- No new project references required in the current scaffold.
- Existing app project remains the single build target.

## 9. Regression Risks and Mitigations
- Risk: over-broad service registration.
- Mitigation: keep registrations limited to the core abstractions introduced in this scaffold.

## 10. Validation Checklist
- Build succeeds.
- Module_core DI registration resolves without errors.
- Existing startup path still works.
