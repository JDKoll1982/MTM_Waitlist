# C#/XAML/RESW naming violations - backlog

**Recorded:** 2026-09-20 | **Re-measured:** 2026-10-10 | **Decision:** **D-4** (owner, 2026-09-20), amended 2026-10-10 | **Findings:** **267**
**Guard:** `.github/scripts/validate-csharp-xaml-naming.ps1` | **Rules:** `.github/instructions/csharp-xaml-naming-rules.instructions.md`

Per decision **D-4** the pre-existing findings below are **grandfathered, not normalized in a bulk pass**. The
rules file keeps its hard-fail discipline for **new and changed** code. This report is the measured backlog.

**Amendment 2026-10-10 — two rules taken, and the count fell from 1155 to 267.**

1. **The shared strings file's RESW keys** were normalized in bulk by owner decision: 199 keys renamed across
   every reference, taking `Strings/en-us/Resources.resw` from 202 findings to 3.
2. **Every test method was renamed to `{Category}_{File}_{Method}_{TestName}`**, also by owner decision: 1862
   methods across 238 files. That is not a rule being normalized in the backlog sense — it is a new rule and a
   total migration — and it removed 898 findings in one move, because the `Async` suffix rule no longer applies
   to a test method (the two rules cannot both hold, so the naming rules now state the precedence).

The remaining findings are untouched and stay grandfathered. The "taken one rule at a time" discipline holds:
each of the two was estimated, put to the owner, and taken deliberately.

Earlier counts recorded elsewhere — **901** in `READINESS-CHECKLIST.md` and in T226 of
`specs/004-unified-card-item-picker/tasks.md`, and **910** at the first measurement — are superseded.

## Why the backlog was never visible

`.github/workflows/csharp-xaml-naming-compliance.yml` built the changed-file list from the pull request diff
and validated **only those files**, so a run with no changed C#/XAML/RESW files passed by design. **That
workflow was deleted from the working tree on 2026-10-10**, so the guard below is now the only check and has to
be run deliberately.

## How to reproduce

```powershell
powershell -ExecutionPolicy Bypass -File .github/scripts/validate-csharp-xaml-naming.ps1
```

Exit code `1` with every finding printed. Passing `-Files <list>` limits the run to a pull request's files.

## Summary by rule

| Rule | Findings |
| --- | ---: |
| `Async Task method '<name>' must end with Async` | 10 |
| `RESW key '<name>' must match Feature_Element.Property` | 168 |
| `Private static readonly field '<name>' must be s_camelCase` | 37 |
| `Public type '<name>' must match file name '<name>'` | 48 |
| `Private field '<name>' must be _camelCase` | 4 |
| `Test method '<name>' must be named Category_File_Method_TestName` | 0 |
| **Total** | **267** |

## Distribution by project

| Project | Findings |
| --- | ---: |
| `Strings` | 150 |
| `MTM_Waitlist.Tests` | 32 |
| `MTM_Waitlist.Core` | 19 |
| `MTM_Waitlist.Mock.Service` | 18 |
| `MTM_Waitlist.Settings` | 13 |
| `MTM_Waitlist.Startup` | 9 |
| `MTM_Waitlist.Waitlist.View` | 7 |
| `Services` | 6 |
| `MTM_Waitlist.Setup` | 4 |
| `MTM_Waitlist.Shared` | 4 |
| `Module_Shared` | 2 |
| `MTM_Waitlist.Mock` | 1 |
| `MTM_Waitlist.Waitlist.NewRequest` | 1 |
| `MainWindow.xaml.cs` | 1 |

## Findings, rule by rule

### Rule 1 - `Async Task method '<name>' must end with Async` (10 findings)

| # | File | Line |
| ---: | --- | ---: |
| 1 | `MTM_Waitlist.Tests/Module_Mock_Service/ServiceApiTests.cs` | 66 |
| 2 | `MTM_Waitlist.Tests/Module_Settings/ViewModels/PartPictureManagerViewModelTests.cs` | 225 |
| 3 | `MTM_Waitlist.Tests/Module_Settings/ConfigImagesLocationsHistoryIntegrationTests.cs` | 43 |
| 4 | `MTM_Waitlist.Tests/Module_Settings/ConfigImagesLocationsHistoryIntegrationTests.cs` | 96 |
| 5 | `MTM_Waitlist.Tests/Module_Settings/ConfigImagesLocationsIntegrationTests.cs` | 33 |
| 6 | `MTM_Waitlist.Tests/Module_Settings/ConfigImagesLocationsIntegrationTests.cs` | 67 |
| 7 | `MTM_Waitlist.Tests/Module_Settings/DefectTypesCrudIntegrationTests.cs` | 40 |
| 8 | `MTM_Waitlist.Tests/Module_Settings/ImageLocationServiceCascadeTests.cs` | 18 |
| 9 | `MTM_Waitlist.Tests/Module_Settings/ImageOverrideWriteServiceTests.cs` | 26 |
| 10 | `MTM_Waitlist.Tests/Module_Setup/ActiveJobReadBackSpIntegrationTests.cs` | 48 |

### Rule 2 - `RESW key '<name>' must match Feature_Element.Property` (168 findings)

| # | File | Line |
| ---: | --- | ---: |
| 1 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 361 |
| 2 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 364 |
| 3 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 367 |
| 4 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 370 |
| 5 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 373 |
| 6 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 376 |
| 7 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 379 |
| 8 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 382 |
| 9 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 385 |
| 10 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 541 |
| 11 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 544 |
| 12 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 547 |
| 13 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 550 |
| 14 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 553 |
| 15 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 558 |
| 16 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 561 |
| 17 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 564 |
| 18 | `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` | 567 |
| 19 | `Strings/en-us/TooltipResources.developer.resw` | 15 |
| 20 | `Strings/en-us/TooltipResources.developer.resw` | 18 |
| 21 | `Strings/en-us/TooltipResources.developer.resw` | 21 |
| 22 | `Strings/en-us/TooltipResources.developer.resw` | 24 |
| 23 | `Strings/en-us/TooltipResources.developer.resw` | 27 |
| 24 | `Strings/en-us/TooltipResources.developer.resw` | 30 |
| 25 | `Strings/en-us/TooltipResources.developer.resw` | 33 |
| 26 | `Strings/en-us/TooltipResources.developer.resw` | 36 |
| 27 | `Strings/en-us/TooltipResources.developer.resw` | 39 |
| 28 | `Strings/en-us/TooltipResources.developer.resw` | 42 |
| 29 | `Strings/en-us/TooltipResources.developer.resw` | 45 |
| 30 | `Strings/en-us/TooltipResources.developer.resw` | 48 |
| 31 | `Strings/en-us/TooltipResources.developer.resw` | 51 |
| 32 | `Strings/en-us/TooltipResources.developer.resw` | 54 |
| 33 | `Strings/en-us/TooltipResources.developer.resw` | 57 |
| 34 | `Strings/en-us/TooltipResources.developer.resw` | 60 |
| 35 | `Strings/en-us/TooltipResources.developer.resw` | 63 |
| 36 | `Strings/en-us/TooltipResources.developer.resw` | 66 |
| 37 | `Strings/en-us/TooltipResources.developer.resw` | 69 |
| 38 | `Strings/en-us/TooltipResources.developer.resw` | 72 |
| 39 | `Strings/en-us/TooltipResources.developer.resw` | 75 |
| 40 | `Strings/en-us/TooltipResources.developer.resw` | 78 |
| 41 | `Strings/en-us/TooltipResources.developer.resw` | 81 |
| 42 | `Strings/en-us/TooltipResources.developer.resw` | 84 |
| 43 | `Strings/en-us/TooltipResources.developer.resw` | 87 |
| 44 | `Strings/en-us/TooltipResources.developer.resw` | 90 |
| 45 | `Strings/en-us/TooltipResources.developer.resw` | 93 |
| 46 | `Strings/en-us/TooltipResources.developer.resw` | 96 |
| 47 | `Strings/en-us/TooltipResources.developer.resw` | 99 |
| 48 | `Strings/en-us/TooltipResources.developer.resw` | 102 |
| 49 | `Strings/en-us/TooltipResources.developer.resw` | 105 |
| 50 | `Strings/en-us/TooltipResources.developer.resw` | 108 |
| 51 | `Strings/en-us/TooltipResources.developer.resw` | 111 |
| 52 | `Strings/en-us/TooltipResources.developer.resw` | 114 |
| 53 | `Strings/en-us/TooltipResources.developer.resw` | 117 |
| 54 | `Strings/en-us/TooltipResources.developer.resw` | 120 |
| 55 | `Strings/en-us/TooltipResources.developer.resw` | 123 |
| 56 | `Strings/en-us/TooltipResources.developer.resw` | 126 |
| 57 | `Strings/en-us/TooltipResources.developer.resw` | 129 |
| 58 | `Strings/en-us/TooltipResources.developer.resw` | 132 |
| 59 | `Strings/en-us/TooltipResources.developer.resw` | 135 |
| 60 | `Strings/en-us/TooltipResources.developer.resw` | 138 |
| 61 | `Strings/en-us/TooltipResources.developer.resw` | 141 |
| 62 | `Strings/en-us/TooltipResources.developer.resw` | 144 |
| 63 | `Strings/en-us/TooltipResources.developer.resw` | 147 |
| 64 | `Strings/en-us/TooltipResources.developer.resw` | 150 |
| 65 | `Strings/en-us/TooltipResources.developer.resw` | 153 |
| 66 | `Strings/en-us/TooltipResources.developer.resw` | 156 |
| 67 | `Strings/en-us/TooltipResources.developer.resw` | 159 |
| 68 | `Strings/en-us/TooltipResources.developer.resw` | 162 |
| 69 | `Strings/en-us/TooltipResources.developer.resw` | 165 |
| 70 | `Strings/en-us/TooltipResources.developer.resw` | 168 |
| 71 | `Strings/en-us/TooltipResources.developer.resw` | 171 |
| 72 | `Strings/en-us/TooltipResources.developer.resw` | 174 |
| 73 | `Strings/en-us/TooltipResources.developer.resw` | 177 |
| 74 | `Strings/en-us/TooltipResources.developer.resw` | 180 |
| 75 | `Strings/en-us/TooltipResources.developer.resw` | 183 |
| 76 | `Strings/en-us/TooltipResources.developer.resw` | 186 |
| 77 | `Strings/en-us/TooltipResources.developer.resw` | 189 |
| 78 | `Strings/en-us/TooltipResources.developer.resw` | 192 |
| 79 | `Strings/en-us/TooltipResources.developer.resw` | 195 |
| 80 | `Strings/en-us/TooltipResources.developer.resw` | 198 |
| 81 | `Strings/en-us/TooltipResources.developer.resw` | 201 |
| 82 | `Strings/en-us/TooltipResources.developer.resw` | 204 |
| 83 | `Strings/en-us/TooltipResources.developer.resw` | 207 |
| 84 | `Strings/en-us/TooltipResources.developer.resw` | 210 |
| 85 | `Strings/en-us/TooltipResources.developer.resw` | 213 |
| 86 | `Strings/en-us/TooltipResources.developer.resw` | 216 |
| 87 | `Strings/en-us/TooltipResources.developer.resw` | 219 |
| 88 | `Strings/en-us/TooltipResources.developer.resw` | 222 |
| 89 | `Strings/en-us/TooltipResources.developer.resw` | 225 |
| 90 | `Strings/en-us/TooltipResources.developer.resw` | 228 |
| 91 | `Strings/en-us/TooltipResources.developer.resw` | 231 |
| 92 | `Strings/en-us/TooltipResources.developer.resw` | 234 |
| 93 | `Strings/en-us/TooltipResources.developer.resw` | 237 |
| 94 | `Strings/en-us/TooltipResources.resw` | 15 |
| 95 | `Strings/en-us/TooltipResources.resw` | 18 |
| 96 | `Strings/en-us/TooltipResources.resw` | 21 |
| 97 | `Strings/en-us/TooltipResources.resw` | 24 |
| 98 | `Strings/en-us/TooltipResources.resw` | 27 |
| 99 | `Strings/en-us/TooltipResources.resw` | 30 |
| 100 | `Strings/en-us/TooltipResources.resw` | 33 |
| 101 | `Strings/en-us/TooltipResources.resw` | 36 |
| 102 | `Strings/en-us/TooltipResources.resw` | 39 |
| 103 | `Strings/en-us/TooltipResources.resw` | 42 |
| 104 | `Strings/en-us/TooltipResources.resw` | 45 |
| 105 | `Strings/en-us/TooltipResources.resw` | 48 |
| 106 | `Strings/en-us/TooltipResources.resw` | 51 |
| 107 | `Strings/en-us/TooltipResources.resw` | 54 |
| 108 | `Strings/en-us/TooltipResources.resw` | 57 |
| 109 | `Strings/en-us/TooltipResources.resw` | 60 |
| 110 | `Strings/en-us/TooltipResources.resw` | 63 |
| 111 | `Strings/en-us/TooltipResources.resw` | 66 |
| 112 | `Strings/en-us/TooltipResources.resw` | 69 |
| 113 | `Strings/en-us/TooltipResources.resw` | 72 |
| 114 | `Strings/en-us/TooltipResources.resw` | 75 |
| 115 | `Strings/en-us/TooltipResources.resw` | 78 |
| 116 | `Strings/en-us/TooltipResources.resw` | 81 |
| 117 | `Strings/en-us/TooltipResources.resw` | 84 |
| 118 | `Strings/en-us/TooltipResources.resw` | 87 |
| 119 | `Strings/en-us/TooltipResources.resw` | 90 |
| 120 | `Strings/en-us/TooltipResources.resw` | 93 |
| 121 | `Strings/en-us/TooltipResources.resw` | 96 |
| 122 | `Strings/en-us/TooltipResources.resw` | 99 |
| 123 | `Strings/en-us/TooltipResources.resw` | 102 |
| 124 | `Strings/en-us/TooltipResources.resw` | 105 |
| 125 | `Strings/en-us/TooltipResources.resw` | 108 |
| 126 | `Strings/en-us/TooltipResources.resw` | 111 |
| 127 | `Strings/en-us/TooltipResources.resw` | 114 |
| 128 | `Strings/en-us/TooltipResources.resw` | 117 |
| 129 | `Strings/en-us/TooltipResources.resw` | 120 |
| 130 | `Strings/en-us/TooltipResources.resw` | 123 |
| 131 | `Strings/en-us/TooltipResources.resw` | 126 |
| 132 | `Strings/en-us/TooltipResources.resw` | 129 |
| 133 | `Strings/en-us/TooltipResources.resw` | 132 |
| 134 | `Strings/en-us/TooltipResources.resw` | 135 |
| 135 | `Strings/en-us/TooltipResources.resw` | 138 |
| 136 | `Strings/en-us/TooltipResources.resw` | 141 |
| 137 | `Strings/en-us/TooltipResources.resw` | 144 |
| 138 | `Strings/en-us/TooltipResources.resw` | 147 |
| 139 | `Strings/en-us/TooltipResources.resw` | 150 |
| 140 | `Strings/en-us/TooltipResources.resw` | 153 |
| 141 | `Strings/en-us/TooltipResources.resw` | 156 |
| 142 | `Strings/en-us/TooltipResources.resw` | 159 |
| 143 | `Strings/en-us/TooltipResources.resw` | 162 |
| 144 | `Strings/en-us/TooltipResources.resw` | 165 |
| 145 | `Strings/en-us/TooltipResources.resw` | 168 |
| 146 | `Strings/en-us/TooltipResources.resw` | 171 |
| 147 | `Strings/en-us/TooltipResources.resw` | 174 |
| 148 | `Strings/en-us/TooltipResources.resw` | 177 |
| 149 | `Strings/en-us/TooltipResources.resw` | 180 |
| 150 | `Strings/en-us/TooltipResources.resw` | 183 |
| 151 | `Strings/en-us/TooltipResources.resw` | 186 |
| 152 | `Strings/en-us/TooltipResources.resw` | 189 |
| 153 | `Strings/en-us/TooltipResources.resw` | 192 |
| 154 | `Strings/en-us/TooltipResources.resw` | 195 |
| 155 | `Strings/en-us/TooltipResources.resw` | 198 |
| 156 | `Strings/en-us/TooltipResources.resw` | 201 |
| 157 | `Strings/en-us/TooltipResources.resw` | 204 |
| 158 | `Strings/en-us/TooltipResources.resw` | 207 |
| 159 | `Strings/en-us/TooltipResources.resw` | 210 |
| 160 | `Strings/en-us/TooltipResources.resw` | 213 |
| 161 | `Strings/en-us/TooltipResources.resw` | 216 |
| 162 | `Strings/en-us/TooltipResources.resw` | 219 |
| 163 | `Strings/en-us/TooltipResources.resw` | 222 |
| 164 | `Strings/en-us/TooltipResources.resw` | 225 |
| 165 | `Strings/en-us/TooltipResources.resw` | 228 |
| 166 | `Strings/en-us/TooltipResources.resw` | 231 |
| 167 | `Strings/en-us/TooltipResources.resw` | 234 |
| 168 | `Strings/en-us/TooltipResources.resw` | 237 |

### Rule 3 - `Private static readonly field '<name>' must be s_camelCase` (37 findings)

| # | File | Line |
| ---: | --- | ---: |
| 1 | `Module_Shared/Controls/RotatingTextView.xaml.cs` | 60 |
| 2 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 17 |
| 3 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 18 |
| 4 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 19 |
| 5 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 20 |
| 6 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 21 |
| 7 | `MTM_Waitlist.Core/Services/ReportPrintService.cs` | 22 |
| 8 | `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` | 27 |
| 9 | `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` | 28 |
| 10 | `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` | 29 |
| 11 | `MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs` | 21 |
| 12 | `MTM_Waitlist.Settings/Services/PartPictureResolver.cs` | 50 |
| 13 | `MTM_Waitlist.Tests/Core/Services/ResolvedRetentionFilterTests.cs` | 10 |
| 14 | `MTM_Waitlist.Tests/Core/Services/UrgencyCalculatorTests.cs` | 12 |
| 15 | `MTM_Waitlist.Tests/Core/Services/UrgencyDeadlineServiceTests.cs` | 16 |
| 16 | `MTM_Waitlist.Tests/Module_Core/Permissions/RoleAuthorizationTests.cs` | 36 |
| 17 | `MTM_Waitlist.Tests/Module_Logging/RecordingLogStore.cs` | 25 |
| 18 | `MTM_Waitlist.Tests/Module_Mock/EmployeeByNumberShapeParityTests.cs` | 23 |
| 19 | `MTM_Waitlist.Tests/Module_Mock/ReadStatusProviderTests.cs` | 17 |
| 20 | `MTM_Waitlist.Tests/Module_Mock_Service/BackupRestoreTests.cs` | 32 |
| 21 | `MTM_Waitlist.Tests/Module_Mock_Service/BackupRetentionTests.cs` | 18 |
| 22 | `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigurationServiceTests.cs` | 31 |
| 23 | `MTM_Waitlist.Tests/Module_Settings/TestPngWriter.cs` | 12 |
| 24 | `MTM_Waitlist.Tests/Module_Settings/TestPngWriter.cs` | 85 |
| 25 | `MTM_Waitlist.Tests/Module_Startup/Services/DiagnosticContextTests.cs` | 21 |
| 26 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailActionTests.cs` | 28 |
| 27 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 37 |
| 28 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 39 |
| 29 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 40 |
| 30 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 41 |
| 31 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 42 |
| 32 | `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs` | 43 |
| 33 | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs` | 23 |
| 34 | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` | 75 |
| 35 | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` | 82 |
| 36 | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` | 561 |
| 37 | `MainWindow.xaml.cs` | 15 |

### Rule 4 - `Public type '<name>' must match file name '<name>'` (48 findings)

| # | File | Line |
| ---: | --- | ---: |
| 1 | `MTM_Waitlist.Core/Contracts/Services/IPermissionAdministrationService.cs` | 7 |
| 2 | `MTM_Waitlist.Core/Contracts/Services/IRoleCatalogService.cs` | 10 |
| 3 | `MTM_Waitlist.Core/Contracts/Services/IScopedPreferenceStore.cs` | 13 |
| 4 | `MTM_Waitlist.Core/Models/UserManagement/UserManagementResult.cs` | 7 |
| 5 | `MTM_Waitlist.Core/Models/ComputerGateCheck.cs` | 3 |
| 6 | `MTM_Waitlist.Core/Models/ConnectionHealthState.cs` | 7 |
| 7 | `MTM_Waitlist.Core/Models/InternalStoreAvailability.cs` | 8 |
| 8 | `MTM_Waitlist.Core/Services/IgnoredLocationsService.cs` | 11 |
| 9 | `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` | 9 |
| 10 | `MTM_Waitlist.Core/Services/RequestDeepLinkHandler.cs` | 16 |
| 11 | `MTM_Waitlist.Settings/Models/ImageValidation.cs` | 12 |
| 12 | `MTM_Waitlist.Settings/Models/PermissionHoldersView.cs` | 14 |
| 13 | `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` | 8 |
| 14 | `MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs` | 9 |
| 15 | `MTM_Waitlist.Settings/Services/LogStoreLevelService.cs` | 21 |
| 16 | `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs` | 12 |
| 17 | `MTM_Waitlist.Settings/Services/PartPictureCoverageService.cs` | 18 |
| 18 | `MTM_Waitlist.Settings/Services/RequestDispositionMapper.cs` | 8 |
| 19 | `MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs` | 20 |
| 20 | `MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs` | 15 |
| 21 | `MTM_Waitlist.Settings/Services/RequestItemObservedTimeService.cs` | 17 |
| 22 | `MTM_Waitlist.Setup/Contracts/Services/SetupContracts.cs` | 5 |
| 23 | `MTM_Waitlist.Setup/Converters/SetupReviewConverters.cs` | 9 |
| 24 | `MTM_Waitlist.Setup/Models/SetupModels.cs` | 10 |
| 25 | `MTM_Waitlist.Setup/ViewModels/SetupCompletionViewModel.cs` | 12 |
| 26 | `MTM_Waitlist.Shared/Services/ImageCacheSyncService.cs` | 24 |
| 27 | `MTM_Waitlist.Shared/Services/PartPictureCacheStore.cs` | 15 |
| 28 | `MTM_Waitlist.Startup/Models/IdentifierSignInResult.cs` | 7 |
| 29 | `MTM_Waitlist.Startup/Services/AccountCreationService.cs` | 17 |
| 30 | `MTM_Waitlist.Startup/Services/AppProcessRestarter.cs` | 24 |
| 31 | `MTM_Waitlist.Startup/Services/BlockedStateRemedies.cs` | 23 |
| 32 | `MTM_Waitlist.Startup/Services/IPlantEmployeeReader.cs` | 10 |
| 33 | `MTM_Waitlist.Startup/Services/LaunchStepGroups.cs` | 16 |
| 34 | `MTM_Waitlist.Startup/Services/WindowPlacementService.cs` | 9 |
| 35 | `MTM_Waitlist.Startup/ViewModels/SetUpPanelViewModel.cs` | 12 |
| 36 | `MTM_Waitlist.Startup/ViewModels/SignInViewModel.cs` | 449 |
| 37 | `MTM_Waitlist.Tests/Module_Mock/FabricatedValueGuard.cs` | 80 |
| 38 | `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs` | 22 |
| 39 | `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestResultViewModel.cs` | 12 |
| 40 | `MTM_Waitlist.Waitlist.View/Helpers/RequestStatusPalette.cs` | 11 |
| 41 | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestCancelResult.cs` | 6 |
| 42 | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestSubmitResult.cs` | 3 |
| 43 | `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureCache.cs` | 15 |
| 44 | `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictures.cs` | 18 |
| 45 | `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureScreen.cs` | 16 |
| 46 | `Services/DependencyInjection/ServiceRegistrationExtensions.Permissions.cs` | 12 |
| 47 | `Services/DependencyInjection/ServiceRegistrationExtensions.UserAccount.cs` | 12 |
| 48 | `Services/DependencyInjection/ServiceRegistrationExtensions.UserManagement.cs` | 12 |

### Rule 5 - `Private field '<name>' must be _camelCase` (4 findings)

| # | File | Line |
| ---: | --- | ---: |
| 1 | `Module_Shared/Controls/ClickToEnlargeImageView.xaml.cs` | 115 |
| 2 | `MTM_Waitlist.Mock/Services/VisualReadFallback.cs` | 66 |
| 3 | `MTM_Waitlist.Shared/Services/ControlInspectorService.cs` | 43 |
| 4 | `MTM_Waitlist.Shared/Services/TooltipService.cs` | 33 |

### Rule 6 - `Test method '<name>' must be named Category_File_Method_TestName` (0 findings)

None. This rule is clean across the tree.

## What "grandfathered" means here

- New and changed code still **hard-fails**. Nothing in this report excuses a new violation.
- The list is a backlog, not an approval. A finding stays until its rule is taken deliberately.
- **Two rules have been taken**, as recorded above: the shared strings file's RESW keys, and the test-method
  format. The same shape applies to any further rule — estimate the blast radius, put it to the owner, take the
  rule, and re-measure this report from the guard's own output.
- Rule 4 and Rule 5 are the smallest remaining, each mechanical. Rule 2's surviving findings are mostly
  `TooltipResources*.resw`, which pair with the generators in `scripts/assets/`.
- `MTM_Waitlist.Tests` carries 32 of the remaining findings.
