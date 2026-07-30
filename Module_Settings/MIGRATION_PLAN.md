# Module_Settings Migration Plan

## 1. Module Purpose
Isolate settings-related screens, options, and configuration services away from the feature shell.

## 2. Ownership Boundaries
- Owns settings UI and settings-specific options.
- May depend on Module_Core and Module_Shared.

## 3. Allowed Inbound Dependencies
- Module_Core
- Module_Shared

## 4. Allowed Outbound Dependencies
- None to feature modules.

## 5. Exact Current Files/Folders to Move Here
- Views/SettingsPage.xaml
- Views/SettingsPage.xaml.cs
- ViewModels/SettingsViewModel.cs
- Models/LocalSettingsOptions.cs

## 6. File-by-File Change Requirements
- Move settings classes into Module_Settings and update constructor dependencies and namespace imports.
- Keep the page/view-model naming intact.

## 7. DI Registration Plan
- Module_Settings exposes AddSettingsModuleServices.
- Module_Core composes it during startup.

## 8. Build/Reference Impact
- No new project references required.

## 9. Regression Risks and Mitigations
- Risk: settings persistence paths change unexpectedly.
- Mitigation: preserve existing options model names and storage semantics.

## 10. Validation Checklist
- Build succeeds.
- Settings page resolves from DI.
- Settings persistence still works.
