# Module_Waitlist Migration Plan

## 1. Module Purpose
Own the waitlist feature flow, including its view models, pages, and feature-specific services.

## 2. Ownership Boundaries
- Owns waitlist UI and waitlist feature services.
- May depend on Module_Shared and Module_Core only.

## 3. Allowed Inbound Dependencies
- Module_Core
- Module_Shared

## 4. Allowed Outbound Dependencies
- None to other feature modules.

## 5. Exact Current Files/Folders to Move Here
- Views/WaitlistViewPage.xaml
- Views/WaitlistViewPage.xaml.cs
- ViewModels/WaitlistViewViewModel.cs
- ViewModels/WaitlistViewDetailViewModel.cs
- Views/WaitlistViewDetailPage.xaml
- Views/WaitlistViewDetailPage.xaml.cs

## 6. File-by-File Change Requirements
- Keep existing page/view-model structure intact while moving files into Module_Waitlist.
- Update namespace declarations and DI registrations to point at the module extension.

## 7. DI Registration Plan
- Module_Waitlist exposes AddWaitlistModuleServices.
- Root composition calls it through Module_Core.

## 8. Build/Reference Impact
- Current app project remains the only build target.

## 9. Regression Risks and Mitigations
- Risk: navigation and page registration regressions.
- Mitigation: preserve existing page/view-model naming and navigation keys.

## 10. Validation Checklist
- Build succeeds.
- Waitlist pages and view models resolve from DI.
- Navigation remains intact.
