You are writing the conventions profile for the workspace folder "MTM_Waitlist".

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

Questions about "MTM_Waitlist" (815 C# files and 93 XAML files read)

1. What marks a service, store or other collaborator?
   Type names ending in: Service (NavigationViewService, SetupDialogService), Model (ShellViewModel, PartStockDialogViewModel), Page (ServiceSettingsPage, ServiceStatusPage), Result (WaitlistRequestCancelResult, WaitlistRequestSubmitResult), Row (InventoryLocationRow, WaitlistDetailFieldRow), Store (IWaitlistMessageSeenStore, LocalWaitlistMessageSeenStore), Step (ConfigurationStep, ILaunchStep), Extensions (ModuleDependencyInjectionExtensions, ServiceRegistrationExtensions), Outcome (SetUpEnterOutcome, ISignInOutcome), Option (NewRequestComponentOption, NewRequestDieOption), Converter (RemainingTimeToBrushConverter, RequestStatusToBrushConverter), Provider (AppWindowProvider, ExternalConnectionInfoProvider), Request (WaitlistRequest, MessageDialogRequest), Fallback (VisualDieSearchFallback, VisualDispositionInputFallback), Resolver (DunnageImagePathResolver, IImageStorageConfigurationResolver), State (NewRequestFlowState, LaunchSessionState), Entry (WaitlistRequestAuditEntry, LaunchFeedEntry), Kind (IdentifierKind, LaunchFeedEntryKind), Reader (PlantEmployeeReader, IWipPartStockReader), Payload (Payload, ServiceStatusPayload)
   Type names starting with: Model_ (Model_ChangeGroup, Model_ConnectionSettings), Service_ (Service_DeploymentEngine, Service_DeploymentSettings), Enum_ (Enum_ChangeKind, Enum_SqlObjectKind)
   Which of these endings and prefixes mean "a collaborator", and which mean "plain data"?

2. What marks a view model, and where do view models live?
   Namespaces seen: MTM_Waitlist (1023 types), Services (442 types), Models (301 types), Module_Settings (191 types), Mock (182 types), Module_Core (178 types), Module_Waitlist (118 types), Module_Startup (114 types), Service (104 types), ViewModels (98 types), Contracts (94 types), Views (72 types), Module_Setup (71 types), Module_Shared (69 types), GenericRequest (60 types), MTM_Database_Deployment (28 types), Helpers (24 types), Module_Logging (17 types), Api (16 types), Converters (16 types)
   A namespace on its own is a weak signal: row models and navigation payloads often sit beside
   the view models that own them. Which namespaces hold view models, and what endings mark data
   that must stay a model even there?

3. How is a type registered or resolved?
   Calls seen: Empty with 1 type argument (Services/DependencyInjection/ServiceRegistrationExtensions.cs:408), AddSingleton with 2 type arguments (Services/DependencyInjection/ServiceRegistrationExtensions.cs:44), GetRequiredService with 1 type argument (Services/ShellContentProvider.cs:16), AddTransient with 1 type argument (Services/DependencyInjection/ServiceRegistrationExtensions.cs:186), GetService with 1 type argument (App.xaml.cs:45), AddSingleton with 1 type argument (Services/DependencyInjection/ServiceRegistrationExtensions.cs:47), Configure with 2 type arguments (Services/DependencyInjection/ServiceRegistrationExtensions.cs:69), Register with 2 type arguments (MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml.cs:21), Get with 1 type argument (MTM_Waitlist.GenericRequest/Views/GenericRequestCategoryPage.xaml.cs:12), RegisterAttached with 2 type arguments (MTM_Waitlist.Shared/Services/CardGridSizing.cs:34), Deserialize with 1 type argument (MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs:288), Configure with 1 type argument (Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs:18), GetValues with 1 type argument (MTM_Waitlist.Settings/Diagnostics/ViewModels/DeveloperLogPanelViewModel.cs:301), FromResult with 1 type argument (Services/SetupDialogService.cs:33), DeserializeAsync with 1 type argument (MTM_Waitlist.Mock.Service/Services/BackupArtifactStore.cs:72), OfType with 1 type argument (Services/NavigationViewService.cs:170), ReadFromJsonAsync with 1 type argument (MTM_Waitlist.Mock.Service/Api/ServiceApiEndpoints.cs:53), TryParse with 1 type argument (MTM_Waitlist.Settings/SettingsHub/ViewModels/SettingsViewModel.cs:2565), FromException with 1 type argument (MTM_Waitlist.Startup/Services/LaunchStepRunner.cs:120), Parse with 1 type argument (MTM_Waitlist.Core/Helpers/EnumToBooleanConverter.cs:21)
   Which of these calls register a type with the container, which pair a type with what it
   resolves, and which read a service out of the container at run time?

4. Which types start the application, and which base types add nothing to bind to?
   Base types declared here: Application, AuthenticationHandler, Behavior, DataTemplateSelector, EventArgs, Exception, GenericRequestStepResult, GenericRequestStepViewModel, IActiveJobCoordinatorService, IActiveJobItemResolverService, IAppLogSink, IAppModuleClock, IAppNotificationService, IAppWindowProvider, IAsyncDisposable, IAverageCoilWeightService, IBuildingCatalogService, IBuildingSelectionService, ICoilAvailabilityService, IComputerRegistryService, IConfigSettingsValueService, IConnectionHealthService, IControlInspectorDetailPresenter, IControlInspectorService, IDeepLinkWindow, IDefectTypeCatalogService, IDisposable, IDunnageCatalogueReader, IDunnageSubstitutePicker, IDunnageTypeVisibilityCatalogService, IDunnageWorkflowService, IEmployeeDirectoryService, IExternalConnectionInfoProvider, IFolderBrowserService, IGenericRequestDieData, IGenericRequestJobFillData, IGenericRequestLauncher, IGenericRequestNavigator, IGenericRequestPartData, IGenericRequestStepData, IHostedService, IIgnoredLocationsService, IImageCacheSyncService, IImageLocationService, IImageLocationServiceInitializer, IImageOverrideReadService, IImageOverrideWriteService, IImageStorageConfigurationResolver, IImageStorageService, IInforVisualLookupService, IInforVisualScriptStore, ILaunchActivityFeed, ILaunchPipeline, ILaunchStageSurface, ILaunchStep, ILogger, ILoggerProvider, ILogService, ILogStoreLevelService, ILuggerArrangementService, ILuggerBackupPrompt, ILuggerChangeNoticeService, ILuggerImageResolver, IMachineConfigurationService, IMachineFacts, IMachineSetupGate, ImageOverrideDialogViewModel, IMockMirrorRefreshWriter, IMockServiceRefreshClient, IModuleCoreService, IMySqlHelperServer, IMySqlStoreConnectivityProbe, INavigationAware, INavigationService, INavigationViewService, INewRequestAlertNotifier, INewRequestAlertService, INewRequestFlowService, INewRequestPickerService, INotifyPropertyChanged, IPageService, IPageTransitionService, IPartMatchSearchService, IPartNumberSource, IPartPictureCacheStore, IPartPictureResolver, IPartPictureWriteService, IPartStockService, IPendingMachineConfiguration, IPendingSignIn, IPermissionAdministrationService, IPermissionService, IPersonIdentity, IPictureCopyHold, IPictureEnlargePreference, IPlantEmployeeReader, IProcessRestarter, IProgress, IReadStatusProvider, IReportPrintService, IRequestItemAllottedMinutesStore, IRequestItemCatalogService, IRequestItemConfigurationService, IRequestItemObservedTimeService, IRequestJobPartAvailabilityProvider, IRoleCatalogService, IScopedPreferenceStore, IServiceCapabilityGate, IServiceOperatorRoleResolver, IServiceSearchHost, IServiceSearchTarget, ISetupDialogService, ISetupPersistenceService, ISetupWorkCenterService, ISetupWorkflowService, ISharedConfigurationService, ISharedModuleService, IShellContentProvider, ISignInOutcome, IStartupRegistrationStore, IStoreAvailabilityTracker, ISubordinatePartService, IThemeSelectorService, ITooltipService, IUrgencyDeadlineService, IUrgencySettingsService, IUserManagementRepository, IUserManagementService, IValueConverter, IVisualConnectionStringProvider, IVisualConnectivityProbe, IVisualQueryExecutor, IVisualReachabilityDetector, IVisualReachabilityProbeHost, IVisualReadFallback, IVisualShapeFreshnessReader, IVisualShapeMetadataReader, IVisualShapePayloadSource, IVisualVerdictPrimer, IWaitlistInventoryService, IWaitlistMessageSeenStore, IWaitlistRequestActionPrompt, IWaitlistRequestService, IWaitlistSortPreferenceService, IWipPartStockReader, IWorkCenterCatalogService, IWorkCenterImageService, IWorkOrderSearchService, IWorkOrderValidationService, ObservableObject, ObservableRecipient, Page, Panel, UserControl, VisualReadFallback, Window, WindowEx
   Attributes on methods: DllImport, MemberNotNull, RelayCommand
   Attributes on fields: none seen in this repository
   Which base types are framework types that declare no application members, and which
   attributes make the compiler generate a member a binding may name?

5. Which files should the analysis read?
   State any folder that holds generated, vendored or sample code which should stay out.
   XAML root elements seen: Page (54), UserControl (21), ResourceDictionary (11), Application (3), Window (2), windowex:WindowEx (2)

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
