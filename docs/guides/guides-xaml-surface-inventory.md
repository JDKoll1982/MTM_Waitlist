# Every XAML surface in the application

**Feature**: `014-adopt-lab-design-system` (FR-023, FR-024, FR-025)  
**Checked by**: `MTM_Waitlist.Tests/Module_Core/Views/SurfaceInventoryAuditTests.cs`

## Why this exists

Every window, page, dialog and reusable control is listed here, with what kind of surface it is and whether it
opens as a modal. The list is not documentation for its own sake: the same rules apply to all of these files, and a
surface that is not on the list is a surface nobody checked. The audit reads this table and the folder it describes
and fails the build when the two disagree, in either direction, so a file added tomorrow cannot quietly go unlisted
and a file deleted cannot leave a stale row behind.

One rule shapes the list: **one type per file**. Every row below is one file declaring one type. Where a modal used
to be declared in the same file as something else, it now has its own file — which is why there are nine dialogs
rather than the eight content dialogs and one windowed editor the codebase held before this change.

## The five kinds

| Kind | What it is | How it opens |
|---|---|---|
| `App` | The application object and its merged resources. | At launch, once. |
| `Window` | A top-level window: the shell, the service console, the control inspector. | Owned by the desktop. |
| `Page` | A screen the shell navigates to. | Inside the one application window. |
| `Dialog` | A modal surface. Its body is a `UserControl` presented through `WindowedDialogHost`, so it opens in its own window owned by the main one. | Owned by the main window, centred on it. |
| `Control` | A reusable fragment of a screen, including the standard message body. Never a surface of its own (FR-033). | Inside whatever holds it. |
| `ResourceDictionary` | Styles, brushes and sizes only. No visuals of its own, no `x:Class`, and still held to the rules (FR-026). | Merged by `App.xaml`. |

## Totals

| Kind | Count |
|---|---|
| `App` | 3 |
| `Window` | 4 |
| `Page` | 54 |
| `Dialog` | 9 |
| `Control` | 13 |
| `ResourceDictionary` | 11 |
| **All** | **94** |

## The inventory

Paths are relative to the repository root. *Modal* says whether the surface opens over the window that raised it.

| Kind | Area | Surface | Path | Modal |
|---|---|---|---|---|
| App | (root) | App.xaml | `App.xaml` | no |
| App | MTM_Waitlist.Mock.Service | App.xaml | `MTM_Waitlist.Mock.Service/App.xaml` | no |
| App | TempGenericRequest.Preview | App.xaml | `TempGenericRequest.Preview/App.xaml` | no |
| Control | MTM_Waitlist.Shared | MessageDialogContent.xaml | `MTM_Waitlist.Shared/Dialogs/MessageDialogContent.xaml` | no |
| Control | Module_Mock | ReadStatusIndicator.xaml | `Module_Mock/Views/ReadStatusIndicator.xaml` | no |
| Control | Module_Settings | DeveloperLogPanelView.xaml | `Module_Settings/Views/DeveloperLogPanelView.xaml` | no |
| Control | Module_Settings | ImageOverrideEditorControl.xaml | `Module_Settings/Views/ImageOverrideEditorControl.xaml` | no |
| Control | Module_Settings | PermissionHoldersView.xaml | `Module_Settings/Views/PermissionHoldersView.xaml` | no |
| Control | Module_Shared | ClickToEnlargeImageView.xaml | `Module_Shared/Controls/ClickToEnlargeImageView.xaml` | no |
| Control | Module_Shared | RotatingTextView.xaml | `Module_Shared/Controls/RotatingTextView.xaml` | no |
| Control | Module_Startup | SetUpPanel.xaml | `Module_Startup/Views/SetUpPanel.xaml` | no |
| Control | Module_Waitlist | WaitlistLineCardView.xaml | `Module_Waitlist/Controls/WaitlistLineCardView.xaml` | no |
| Control | Module_Waitlist | WaitlistStatusFlyout.xaml | `Module_Waitlist/Controls/WaitlistStatusFlyout.xaml` | no |
| Control | MTM_Waitlist.GenericRequest | GenericRequestStepFrame.xaml | `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml` | no |
| Control | MTM_Waitlist.GenericRequest.Controls | GenericRequestCard.xaml | `MTM_Waitlist.GenericRequest.Controls/Controls/GenericRequestCard.xaml` | no |
| Control | TempGenericRequest.Preview | TempPreviewCardPage.xaml | `TempGenericRequest.Preview/Views/TempPreviewCardPage.xaml` | no |
| Dialog | Module_Settings | ComputerEditDialog.xaml | `Module_Settings/Views/ComputerEditDialog.xaml` | yes |
| Dialog | Module_Settings | LuggerEditorDialog.xaml | `Module_Settings/Views/LuggerEditorDialog.xaml` | yes |
| Dialog | Module_Settings | LuggerNameDialog.xaml | `Module_Settings/Views/LuggerNameDialog.xaml` | yes |
| Dialog | Module_Settings | PinRevealDialog.xaml | `Module_Settings/Views/PinRevealDialog.xaml` | yes |
| Dialog | Module_Settings | RequestItemImagesDialog.xaml | `Module_Settings/Views/RequestItemImagesDialog.xaml` | yes |
| Dialog | Module_Settings | WorkCenterImagesDialog.xaml | `Module_Settings/Views/WorkCenterImagesDialog.xaml` | yes |
| Dialog | Module_Setup | SetupDunnageImageSearchDialog.xaml | `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml` | yes |
| Dialog | Module_Setup | SetupWorkOrderSearchDialog.xaml | `Module_Setup/Views/SetupWorkOrderSearchDialog.xaml` | yes |
| Dialog | Module_Waitlist | PartStockDialog.xaml | `Module_Waitlist/Views/PartStockDialog.xaml` | yes |
| Page | MTM_Waitlist.Mock.Service | ServiceSettingsPage.xaml | `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml` | no |
| Page | MTM_Waitlist.Mock.Service | ServiceStatusPage.xaml | `MTM_Waitlist.Mock.Service/Views/ServiceStatusPage.xaml` | no |
| Page | Module_Core | ShellPage.xaml | `Module_Core/Views/ShellPage.xaml` | no |
| Page | Module_Settings | AdministrationSettingsPage.xaml | `Module_Settings/Views/AdministrationSettingsPage.xaml` | no |
| Page | Module_Settings | CreateUserPage.xaml | `Module_Settings/Views/CreateUserPage.xaml` | no |
| Page | Module_Settings | DevicesSettingsPage.xaml | `Module_Settings/Views/DevicesSettingsPage.xaml` | no |
| Page | Module_Settings | DiagnosticsSettingsPage.xaml | `Module_Settings/Views/DiagnosticsSettingsPage.xaml` | no |
| Page | Module_Settings | EditUserPage.xaml | `Module_Settings/Views/EditUserPage.xaml` | no |
| Page | Module_Settings | GeneralSettingsPage.xaml | `Module_Settings/Views/GeneralSettingsPage.xaml` | no |
| Page | Module_Settings | LuggerArrangementPage.xaml | `Module_Settings/Views/LuggerArrangementPage.xaml` | no |
| Page | Module_Settings | OperationsSettingsPage.xaml | `Module_Settings/Views/OperationsSettingsPage.xaml` | no |
| Page | Module_Settings | PartPictureManagerPage.xaml | `Module_Settings/Views/PartPictureManagerPage.xaml` | no |
| Page | Module_Settings | PermissionsPage.xaml | `Module_Settings/Views/PermissionsPage.xaml` | no |
| Page | Module_Settings | PicturesSettingsPage.xaml | `Module_Settings/Views/PicturesSettingsPage.xaml` | no |
| Page | Module_Settings | SettingsPage.xaml | `Module_Settings/Views/SettingsPage.xaml` | no |
| Page | Module_Settings | UserManagementPage.xaml | `Module_Settings/Views/UserManagementPage.xaml` | no |
| Page | Module_Setup | SetupCompletionPage.xaml | `Module_Setup/Views/SetupCompletionPage.xaml` | no |
| Page | Module_Setup | SetupDunnageTypePage.xaml | `Module_Setup/Views/SetupDunnageTypePage.xaml` | no |
| Page | Module_Setup | SetupPartSelectionPage.xaml | `Module_Setup/Views/SetupPartSelectionPage.xaml` | no |
| Page | Module_Setup | SetupReviewPage.xaml | `Module_Setup/Views/SetupReviewPage.xaml` | no |
| Page | Module_Setup | SetupSequenceSelectionPage.xaml | `Module_Setup/Views/SetupSequenceSelectionPage.xaml` | no |
| Page | Module_Setup | SetupWorkCenterHistoryPage.xaml | `Module_Setup/Views/SetupWorkCenterHistoryPage.xaml` | no |
| Page | Module_Setup | SetupWorkCenterPage.xaml | `Module_Setup/Views/SetupWorkCenterPage.xaml` | no |
| Page | Module_Setup | SetupWorkOrderPage.xaml | `Module_Setup/Views/SetupWorkOrderPage.xaml` | no |
| Page | Module_Startup | BlockedStatePage.xaml | `Module_Startup/Views/BlockedStatePage.xaml` | no |
| Page | Module_Startup | MachineSetupPage.xaml | `Module_Startup/Views/MachineSetupPage.xaml` | no |
| Page | Module_Startup | SignInPage.xaml | `Module_Startup/Views/SignInPage.xaml` | no |
| Page | Module_Startup | SplashPage.xaml | `Module_Startup/Views/SplashPage.xaml` | no |
| Page | Module_Waitlist | NewRequestCoilTurnPage.xaml | `Module_Waitlist/Views/NewRequestCoilTurnPage.xaml` | no |
| Page | Module_Waitlist | NewRequestComponentPage.xaml | `Module_Waitlist/Views/NewRequestComponentPage.xaml` | no |
| Page | Module_Waitlist | NewRequestDetailsPage.xaml | `Module_Waitlist/Views/NewRequestDetailsPage.xaml` | no |
| Page | Module_Waitlist | NewRequestDiePage.xaml | `Module_Waitlist/Views/NewRequestDiePage.xaml` | no |
| Page | Module_Waitlist | NewRequestDunnagePage.xaml | `Module_Waitlist/Views/NewRequestDunnagePage.xaml` | no |
| Page | Module_Waitlist | NewRequestHopperPage.xaml | `Module_Waitlist/Views/NewRequestHopperPage.xaml` | no |
| Page | Module_Waitlist | NewRequestItemPage.xaml | `Module_Waitlist/Views/NewRequestItemPage.xaml` | no |
| Page | Module_Waitlist | NewRequestJobTypePage.xaml | `Module_Waitlist/Views/NewRequestJobTypePage.xaml` | no |
| Page | Module_Waitlist | NewRequestPartSearchPage.xaml | `Module_Waitlist/Views/NewRequestPartSearchPage.xaml` | no |
| Page | Module_Waitlist | NewRequestResultPage.xaml | `Module_Waitlist/Views/NewRequestResultPage.xaml` | no |
| Page | Module_Waitlist | NewRequestSkidCountPage.xaml | `Module_Waitlist/Views/NewRequestSkidCountPage.xaml` | no |
| Page | Module_Waitlist | NewRequestSummaryPage.xaml | `Module_Waitlist/Views/NewRequestSummaryPage.xaml` | no |
| Page | Module_Waitlist | NewRequestWorkCenterPage.xaml | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml` | no |
| Page | Module_Waitlist | WaitlistViewDetailPage.xaml | `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` | no |
| Page | Module_Waitlist | WaitlistViewPage.xaml | `Module_Waitlist/Views/WaitlistViewPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest.Controls | GenericRequestDetailPage.xaml | `MTM_Waitlist.GenericRequest.Controls/Views/GenericRequestDetailPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestCategoryPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestCategoryPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestDetailsPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestDetailsPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestDiePage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestDiePage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestDunnagePage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestDunnagePage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestItemPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestItemPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestPartPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestPartPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestResultPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestResultPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestScrapTypePage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestScrapTypePage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestSummaryPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestSummaryPage.xaml` | no |
| Page | MTM_Waitlist.GenericRequest | GenericRequestWorkCenterPage.xaml | `MTM_Waitlist.GenericRequest/Views/GenericRequestWorkCenterPage.xaml` | no |
| ResourceDictionary | Module_Waitlist | WaitlistLineCardDetailTemplates.xaml | `Module_Waitlist/Controls/WaitlistLineCardDetailTemplates.xaml` | no |
| ResourceDictionary | Styles | ControlInspector.xaml | `Styles/ControlInspector.xaml` | no |
| ResourceDictionary | Styles | Dialogs.xaml | `Styles/Dialogs.xaml` | no |
| ResourceDictionary | Styles | FontSizes.xaml | `Styles/FontSizes.xaml` | no |
| ResourceDictionary | Styles | ModuleSetup.xaml | `Styles/ModuleSetup.xaml` | no |
| ResourceDictionary | Styles | PartPictureCardTemplate.xaml | `Styles/PartPictureCardTemplate.xaml` | no |
| ResourceDictionary | Styles | RequestDialogs.xaml | `Styles/RequestDialogs.xaml` | no |
| ResourceDictionary | Styles | TextBlock.xaml | `Styles/TextBlock.xaml` | no |
| ResourceDictionary | Styles | Thickness.xaml | `Styles/Thickness.xaml` | no |
| ResourceDictionary | Styles | TooltipDeveloper.xaml | `Styles/TooltipDeveloper.xaml` | no |
| ResourceDictionary | Styles | TooltipStandard.xaml | `Styles/TooltipStandard.xaml` | no |
| Window | MTM_Waitlist.Mock.Service | ServiceShellWindow.xaml | `MTM_Waitlist.Mock.Service/Views/ServiceShellWindow.xaml` | no |
| Window | (root) | MainWindow.xaml | `MainWindow.xaml` | no |
| Window | Module_Shared | ControlInspectorDetailWindow.xaml | `Module_Shared/Views/ControlInspectorDetailWindow.xaml` | no |
| Window | TempGenericRequest.Preview | MainWindow.xaml | `TempGenericRequest.Preview/MainWindow.xaml` | no |

## What the audit enforces

- Every `.xaml` file in the repository appears exactly once, and every row resolves to a file that exists.
- Each file declares exactly one type: a `Dialog` row is a file whose own root element is the dialog body, not a
  second type sharing a file with a page.
- A `Dialog` row is modal and every modal row is a `Dialog`, so "which dialogs exist" is answered here rather than by
  searching for `ContentDialog` in the source.
- A `ResourceDictionary` row declares no `x:Class`, which is what keeps a styles file from growing a visual tree.
