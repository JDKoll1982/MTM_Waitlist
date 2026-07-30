# Modularization Master Spec

## Implementation Checklist
- [x] Create module folder scaffolding for core/shared/waitlist/settings/startup/reporting boundaries
- [x] Add module-level DI registration entry points for each scaffolded module
- [x] Wire the module composition pipeline into the app startup flow
- [x] Add migration planning documents for each module
- [x] Add a manual smoke checklist for follow-up UI validation
- [x] Add basic module-owned runtime services for core/shared/waitlist/settings/startup/reporting modules
- [x] Add a module-focused smoke test scaffold for DI/service registration
- [x] Move existing waitlist/view-model/page assets into Module_Waitlist
- [x] Move existing settings-related assets into Module_Settings
- [x] Move existing startup-related services/models into Module_Startup
- [x] Move the remaining root infrastructure services into core/settings/startup module ownership
- [x] Update navigation/page registration to use the module-owned layout
- [x] Add fuller module-focused tests for DI and service resolution once the Windows App SDK test environment issue is resolved

## 1. Current-state architecture summary
The current Waitlist application is a single WinUI 3 project with a shared startup pipeline defined in App.xaml.cs and ServiceRegistrationExtensions. The app uses MVVM, DI, and a host-managed shell with view-model-driven navigation. Existing functionality is still grouped under root-level folders such as Services, ViewModels, and Views.

## 2. Target-state modular architecture summary
The app will evolve toward module-owned folders using the Module_<Feature> convention. The initial scaffold introduces Module_Core, Module_Shared, Module_Waitlist, Module_Settings, Module_Startup, and Module_Reporting. Root DI will compose module registrations through Module_Core.

## 3. Dependency rules matrix
- Module_Core -> Module_Shared, root app infrastructure
- Module_Shared -> Module_Core only
- Module_Waitlist -> Module_Core, Module_Shared
- Module_Settings -> Module_Core, Module_Shared
- Module_Startup -> Module_Core, Module_Shared
- Module_Reporting -> Module_Core, Module_Shared
- Feature modules must not depend on each other directly

## 4. Phase-by-phase migration sequence
1. Scaffold module folders and module-specific DI entry points.
2. Move shared abstractions into Module_Core and Module_Shared.
3. Extract waitlist, settings, and startup services into module-owned folders.
4. Update navigation and page registration to use the new module boundaries.
5. Validate build and tests after each phase.

## 5. Critical path
The critical path is the DI composition root and the startup services that power launch and navigation. Those must remain stable while files are moved.

## 6. Rollback plan per phase
- Keep the existing root services and view-model registration intact during the scaffold phase.
- If a migration phase causes regressions, revert the specific module registration extension and leave the original services in place.

## 7. Test strategy using MTM_Waitlist.Tests
- Add smoke tests around service resolution and startup registration once the module boundaries are introduced.
- Keep existing tests stable while adding new module-level tests.

## 8. Manual UI smoke checklist template
- Launch application successfully.
- Navigate through waitlist page.
- Open settings page.
- Verify startup and splash flow still works.
- Confirm no navigation exceptions are raised.

## 9. High-risk workflow analysis
### Risks
1. Startup and startup logging services may break if registration order changes.
2. Navigation and page/view-model registration may regress when pages move.
3. Settings persistence may be affected if options models are moved.

### Recommended migration order
1. Module_Core and Module_Shared.
2. Module_Startup.
3. Module_Settings.
4. Module_Waitlist.
5. Module_Reporting.
