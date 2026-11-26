You are writing the conventions profile for the workspace folder "MTM_Receiving_Application".

The file csharp-xaml-graph.profile.json goes at the root of that folder and is committed with the code. It
records the C#/XAML conventions this repository follows, so the dependency graph places types and
draws registration links by what this repository actually does rather than by a built-in guess.

Every key the profile accepts is described below: what it means, the shape of its value, whether
it is required, and what the extension does when it is absent. Do not write a key this list does
not name, and do not rename or re-nest one of these.

The profile format is closed: these are all the keys it accepts.
An absent key keeps the built-in convention described under it.
A key that is not listed here is not part of the format, and is reported rather than used.

At the top of the file:
- version (the number 1, required)
  States which version of the profile format this file uses.
  If absent: A profile without it cannot be used, and the built-in conventions apply.
- repository (text, optional)
  Names the repository this profile describes. It is read by people and never used to match a folder.
  If absent: The profile describes whichever folder holds it.
- layers (a section holding the rules that place a type in a layer, optional)
  Places types in the four layers the graph colours by.
  If absent: The built-in layer rules and the built-in default layer apply.
- links (a section holding the calls that draw a dependency, optional)
  Recognises how this repository registers, resolves and binds types.
  If absent: The built-in link detection applies.
- findings (a section holding the rules that change what the cleanup report counts, optional)
  Adjusts the cleanup findings to what this repository actually does.
  If absent: The built-in findings rules apply.
- files (a section deciding which files are read, optional)
  Decides which files the analysis reads.
  If absent: The built-in file selection applies.

In layers, which places types in a layer:
- layers.rules (a list of layer rules, optional)
  Each rule places a type in a layer when every condition it states holds. The first rule that matches decides, so the order is the precedence.
  If absent: The built-in layer rules apply.
- layers.default (one of view, viewmodel, service, model, optional)
  The layer for a type no rule matches.
  One of: view, viewmodel, service, model.
  If absent: A type no rule matches is placed in the model layer.

Inside each entry of layers.rules:
- layers.rules[].layer (one of view, viewmodel, service, model, optional)
  The layer this rule assigns when it matches.
  One of: view, viewmodel, service, model.
  If absent: A rule with no layer states nothing, so it is skipped.
- layers.rules[].kinds (a list of text, each one of class, interface, enum, record, struct, optional)
  Restricts the rule to these declaration kinds.
  One of: class, interface, enum, record, struct.
  If absent: The rule applies to a declaration of any kind.
- layers.rules[].nameSuffixes (a list of text, optional)
  Matches when the type name ends with one of these.
  If absent: The rule does not test the end of the name.
- layers.rules[].namePrefixes (a list of text, optional)
  Matches when the type name starts with one of these.
  If absent: The rule does not test the start of the name.
- layers.rules[].namespaceSegments (a list of text, optional)
  Matches when the namespace has a segment exactly equal to one of these. A substring test would make Models match ViewModels, so only whole segments count.
  If absent: The rule does not test the namespace.
- layers.rules[].unlessNameSuffixes (a list of text, optional)
  Stops the rule applying when the type name ends with one of these, which is how a row model beside the view models stays a model.
  If absent: Nothing stops the rule once its other conditions hold.

In links, which recognises how a type is registered, resolved or bound:
- links.genericPairCalls (a list of text, optional)
  Method names called as x.Call<TA, TB>(): draws a resolves edge from the second type to the first and registers both.
  If absent: The built-in name Configure applies.
- links.typeofPairCalls (a list of text, optional)
  Method names called as x.Call(typeof(TA), typeof(TB)): draws a resolves edge and registers the first type.
  If absent: The built-in name RegisterRoute applies.
- links.dependencyPropertyCall (text, optional)
  A method name called as x.Call(nameof(P), typeof(TValue), typeof(TOwner)): draws a getservice edge when the value type is a view model.
  If absent: The built-in name Register applies.
- links.addCallPattern (a regular expression, optional)
  A registration method name matches this pattern. One type argument registers that type; two pair an implementation with the interface it serves.
  If absent: The built-in pattern ^Add[A-Z] applies.
- links.serviceLocator (a list of text, optional)
  Method names that read a service from the container rather than being a plain call.
  If absent: The built-in names GetService and GetRequiredService apply.
- links.dataContextCasts (a list of text, optional)
  Cast keywords that, applied to the data context with an interface type, draw a binding edge.
  If absent: The built-in keywords is and as apply.

In findings, which changes what the cleanup report counts:
- findings.entryPoints (a list of text, optional)
  Type names that are never reported unused.
  If absent: The built-in entry-point names apply.
- findings.transparentBases (a list of text, optional)
  Base type names that add no bindable members, so a binding to a member they do not declare is genuinely missing.
  If absent: The built-in transparent base names apply.
- findings.generatedMembers (a list of generated-member rules, optional)
  Members the compiler generates from an attribute, so a binding to one is not a dead binding.
  If absent: The built-in generated-member rules apply.

Inside each entry of findings.generatedMembers:
- findings.generatedMembers[].attribute (text, optional)
  The attribute name without brackets, for example RelayCommand.
  If absent: A rule with no attribute states nothing, so it is skipped.
- findings.generatedMembers[].on (one of method, field, optional)
  Which declaration carries the attribute.
  One of: method, field.
  If absent: A rule without it states nothing, so it is skipped.
- findings.generatedMembers[].form (one of commandSuffix, pascalCaseWithoutPrefix, optional)
  How the generated member is named. commandSuffix yields NameCommand, and StemCommand for a name ending in Async. pascalCaseWithoutPrefix strips leading underscores and upper-cases the first letter.
  One of: commandSuffix, pascalCaseWithoutPrefix.
  If absent: A rule without it states nothing, so it is skipped.

In files, which decides which files are read:
- files.include (a list of text, optional)
  Glob patterns, relative to the workspace folder, describing the files to read.
  If absent: The built-in include patterns apply.
- files.exclude (a list of text, optional)
  Glob patterns excluded from the read.
  If absent: The built-in exclude patterns apply.

Answer the questions below before proposing anything. Each quotes what the analysis already saw
in this repository, so answer from that evidence. If a question cannot be answered from the code
you can see, say so and leave that part out: an absent key keeps the built-in convention.

Questions about "MTM_Receiving_Application" (1015 C# files and 145 XAML files read)

1. What marks a service, store or other collaborator?
   Type names ending in: Settings (Service_DeploymentSettings, Model_ConnectionSettings), Dialog (View_Volvo_EmailPreviewDialog, View_Volvo_GeneratedLabelDataDialog), Handler (FormatEmailDataQueryHandler, GenerateLabelQueryHandler), Result (Model_DeploymentResult, ImportPartsResult), Row (Model_Tool_UnmarkedLotRow, Model_Tool_ReceivingAnalyticsAppActivityRow), Entry (View_Volvo_ShipmentEntry, ViewModel_Volvo_ShipmentEntry), Window (MainWindow, View_Volvo_ShipmentHistoryDetailWindow), Option (Model_DatabaseOption, Model_Tool_LotMarkingSortOption), Command (ActivateVolvoPartCommand, AddPartToShipmentCommand), Defaults (VolvoSettingsDefaults, View_Settings_Receiving_Defaults), Line (Model_VolvoShipmentLine, IDao_VolvoShipmentLine), Workflow (Service_SharedLookupWorkflow, IService_SharedLookupWorkflow), Hub (View_Settings_Volvo_NavigationHub, ViewModel_Settings_Volvo_NavigationHub), Query (FormatEmailDataQuery, GenerateLabelQuery), Shortcuts (View_Settings_Receiving_KeyboardShortcuts, ViewModel_Settings_Receiving_KeyboardShortcuts), Type (Enum_SharedLookupType, Enum_SettingsDataType), Main (View_ShipRecTools_Main, ViewModel_ShipRecTools_Main), Validator (ActivateVolvoPartCommandValidator, AddPartToShipmentCommandValidator), Data (Service_VolvoMasterData, InitialShipmentData), History (View_Volvo_History, ViewModel_Volvo_History)
   Type names starting with: Model_ (Model_ChangeGroup, Model_ConnectionSettings), View_ (View_Volvo_EmailPreviewDialog, View_Volvo_GeneratedLabelDataDialog), ViewModel_ (ViewModel_DatabaseDeployment, ViewModel_Volvo_EmailPreviewDialog), Service_ (Service_DatabaseBackup, Service_DeploymentEngine), IService_ (IService_VolvoAuthorization, IService_Reprint_Volvo), Dao_ (Dao_VolvoGeneratedLabelData, Dao_VolvoLabelHistory), Helper_ (Helper_VolvoGeneratedLabelDataBuilder, Helper_VolvoShipmentCalculations), Enum_ (Enum_ChangeKind, Enum_SqlObjectKind), Converter_ (Converter_LotColorCodeToBrush, Converter_ReconciliationOutcomeBrush), Dialog_ (Dialog_DunnageBookPreview, Dialog_LotMarkingBackfillConfirm), IDao_ (IDao_VolvoPart, IDao_VolvoShipment), Control_ (Control_Shared_MathQuantityTextBox, Control_Shared_TypedLookupTextBox), IViewModel_ (IViewModel_HeaderStepBarProvider, IViewModel_HeaderTitleProvider), Strategy_ (Strategy_SharedLocationLookup, Strategy_SharedPartNumberLookup)
   Which of these endings and prefixes mean "a collaborator", and which mean "plain data"?

2. What marks a view model, and where do view models live?
   Namespaces seen: MTM_Receiving_Application (1092 types), Models (273 types), Module_Core (181 types), Module_Settings (181 types), Services (171 types), Module_ShipRec_Tools (155 types), Views (140 types), ViewModels (139 types), Module_Receiving (136 types), Module_Volvo (128 types), Core (104 types), Contracts (101 types), Module_Dunnage (95 types), Helpers (60 types), Module_Reconciliation (60 types), Module_Scanner (58 types), Settings (52 types), Data (50 types), Module_Reporting (36 types), Module_Shared (36 types)
   A namespace on its own is a weak signal: row models and navigation payloads often sit beside
   the view models that own them. Which namespaces hold view models, and what endings mark data
   that must stay a model even there?

3. How is a type registered or resolved?
   Calls seen: Failure with 1 type argument (Module_Volvo/Services/Service_Volvo.cs:625), AddTransient with 1 type argument (Infrastructure/DependencyInjection/ModuleServicesExtensions.cs:118), GetService with 1 type argument (App.xaml.cs:259), Success with 1 type argument (Module_Volvo/ViewModels/ViewModel_Volvo_ShipmentEntry.cs:2346), Empty with 1 type argument (MainWindow.Search.cs:703), AddSingleton with 2 type arguments (Infrastructure/DependencyInjection/CoreServiceExtensions.cs:49), GetRequiredService with 1 type argument (App.xaml.cs:164), ExecuteListAsync with 1 type argument (Module_ShipRec_Tools/Data/Dao_Tool_LotMarking.cs:27), Register with 2 type arguments (Module_Shared/Views/Controls/Control_Shared_MathQuantityTextBox.xaml.cs:64), AddSingleton with 1 type argument (Infrastructure/DependencyInjection/CoreServiceExtensions.cs:52), Deserialize with 1 type argument (MTM_Database_Deployment/Services/Service_DeploymentSettings.cs:63), TryParse with 1 type argument (MainWindow.xaml.cs:833), ExecuteSingleAsync with 1 type argument (Module_ShipRec_Tools/Data/Dao_Tool_ReceivingAnalyticsApp.cs:91), Cast with 1 type argument (MainWindow.xaml.cs:132), OfType with 1 type argument (MainWindow.xaml.cs:941), AddTransient with 2 type arguments (Infrastructure/DependencyInjection/ModuleServicesExtensions.cs:227), GetCurrentPageItems with 1 type argument (Module_Receiving/ViewModels/ViewModel_Receiving_EditMode.cs:1188), GetViewModels with 1 type argument (Module_Receiving/ViewModels/ViewModel_Receiving_PartSelection.cs:155), GetNames with 1 type argument (Module_Settings.Core/ViewModels/ViewModel_Settings_LabelViewExecutable.cs:58), GetValues with 1 type argument (Module_Reconciliation/Models/Model_ReconciliationOutcomeFilterChoice.cs:30)
   Which of these calls register a type with the container, which pair a type with what it
   resolves, and which read a service out of the container at run time?

4. Which types start the application, and which base types add nothing to bind to?
   Base types declared here: AbstractValidator, Application, ContentDialog, DataTemplateSelector, EventArgs, IDao_VolvoGeneratedLabelData, IDao_VolvoLabelHistory, IDisposable, INotifyPropertyChanged, IPipelineBehavior, IReceivingWorkflowFocusable, IRequest, IRequestHandler, IResettableViewModel, IService_AdaptiveLayout, IService_ApplicationShutdown, IService_AppSettings, IService_AuthCredentialProtection, IService_Authentication, IService_DatabaseTargetManager, IService_Dispatcher, IService_DispatcherTimer, IService_DunnageImageStorage, IService_DunnageSettings, IService_DunnageShortcuts, IService_DunnageUserLabelSettings, IService_DunnageWorkflow, IService_EmailRecipientSettingsOperations, IService_ErrorHandler, IService_Focus, IService_HeaderBackNavigation, IService_Help, IService_InforVisual, IService_InforVisualMockDataCatalog, IService_LabelViewLauncher, IService_LoggingUtility, IService_MySQL_Dunnage, IService_MySQL_PackagePreferences, IService_MySQL_QualityHold, IService_MySQL_Receiving, IService_MySQL_ReceivingLine, IService_MySQL_ReceivingVendorVariable, IService_Navigation, IService_Notification, IService_OnStartup_AppLifecycle, IService_Pagination, IService_QualityHoldWarning, IService_ReceivingLabelData, IService_ReceivingLabelOverlay, IService_ReceivingLabelOverlaySettings, IService_ReceivingLocationReconciliation, IService_ReceivingSettings, IService_ReceivingShortcuts, IService_ReceivingUserLabelSettings, IService_ReceivingValidation, IService_ReceivingWorkflow, IService_ReconciliationAllocator, IService_ReconciliationBucketBuilder, IService_ReconciliationDayReader, IService_ReconciliationGroupResolver, IService_ReconciliationMovementSelector, IService_ReconciliationNonPurchaseOrderResolver, IService_ReconciliationPreview, IService_ReconciliationQuantityProof, IService_ReconciliationSettings, IService_ReconciliationWriteService, IService_Reporting, IService_ReportingClipboard, IService_ReportingOutput, IService_ReportingPdf, IService_ReportingRecipientSettings, IService_ReportingSettings, IService_Reprint_Dunnage, IService_Reprint_Receiving, IService_Reprint_Volvo, IService_ScannerAccessPolicy, IService_ScannerExecution, IService_ScannerHotkey, IService_ScannerInputEngine, IService_ScannerNavigation, IService_ScannerValidation, IService_ScannerWorkflow, IService_SessionManager, IService_Settings_ReceivingWeldedParts, IService_SettingsCoreFacade, IService_SettingsErrorHandler, IService_SettingsPagination, IService_SettingsUserDirectory, IService_SettingsUserLabelButtons, IService_SharedLocationRange, IService_SharedLookupWorkflow, IService_ShipRecTools_Navigation, IService_ShipRecToolsSettings, IService_SoftwareVersionMonitor, IService_ThemeManager, IService_Tool_DeliverySchedule, IService_Tool_DunnageBook, IService_Tool_LotMarking, IService_Tool_MaterialAvailabilityBoard, IService_Tool_OutsideServiceHistory, IService_Tool_POLineSpecSearch, IService_Tool_ReceivingAnalytics, IService_Tool_ReceivingAnalyticsApp, IService_Tool_WeldedCoils, IService_UserLoginCoordinator, IService_UserPreferences, IService_UserPrivileges, IService_UserSessionManager, IService_ViewModelRegistry, IService_VisualCredentialValidator, IService_Volvo, IService_VolvoAuthorization, IService_VolvoMasterData, IService_VolvoRecipientSettings, IService_VolvoSettings, IService_VolvoUserLabelSettings, IService_Window, ISettingsCache, ISettingsEncryptionService, ISettingsManifestProvider, ISettingsMetadataRegistry, ISettingsNavigationActions, ISettingsNavigationHost, ISettingsNavigationNavState, ISettingsRowSnapshot, ISharedLookupStrategy, IValueConverter, IViewModel_HeaderStepBarProvider, IViewModel_HeaderTitleProvider, IViewModel_ShellActionProvider, IWin32Window, ModalWindow, Model_Dao_Result, ObservableCollection, ObservableObject, Page, UserControl, ViewModel_Receiving_StepBase, ViewModel_Reprint_ModuleBase, ViewModel_Settings_EmailRecipientsEditorBase, ViewModel_SettingsNavigationHubBase, ViewModel_Shared_Base, ViewModel_Tool_Base, Window
   Attributes on methods: DllImport, MarshalAs, Obsolete, RelayCommand
   Attributes on fields: FieldOffset, JsonIgnore, MarshalAs, NotifyCanExecuteChangedFor, NotifyPropertyChangedFor, ObservableProperty
   Which base types are framework types that declare no application members, and which
   attributes make the compiler generate a member a binding may name?

5. Which files should the analysis read?
   State any folder that holds generated, vendored or sample code which should stay out.
   XAML root elements seen: Page (75), ContentDialog (31), UserControl (27), Window (8), Application (2), ResourceDictionary (2)

Reply with one JSON object only, in this shape. Leave out every key you are not sure about:

```json
{
  "version": 1,
  "repository": "<the folder name>",
  "layers": {
    "rules": [
      {
        "layer": "service",
        "nameSuffixes": [
          "Handler"
        ],
        "namespaceSegments": [
          "Services"
        ]
      },
      {
        "layer": "viewmodel",
        "nameSuffixes": [
          "Screen"
        ]
      }
    ],
    "default": "model"
  },
  "links": {
    "genericPairCalls": [
      "Configure"
    ],
    "typeofPairCalls": [
      "RegisterRoute"
    ],
    "dependencyPropertyCall": "Register",
    "addCallPattern": "^Add[A-Z]",
    "serviceLocator": [
      "GetService"
    ],
    "dataContextCasts": [
      "is",
      "as"
    ]
  },
  "findings": {
    "entryPoints": [
      "App",
      "Program"
    ],
    "transparentBases": [
      "Page",
      "UserControl"
    ],
    "generatedMembers": [
      {
        "attribute": "RelayCommand",
        "on": "method",
        "form": "commandSuffix"
      }
    ]
  },
  "files": {
    "include": [
      "**/*.cs",
      "**/*.xaml"
    ],
    "exclude": [
      "**/obj/**"
    ]
  }
}
```
