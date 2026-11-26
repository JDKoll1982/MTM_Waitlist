You are writing the conventions profile for the workspace folder "MTM_WIP".

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

Questions about "MTM_WIP" (232 C# files and 36 XAML files read)

1. What marks a service, store or other collaborator?
   Type names ending in: Entry (View_Inventory_AdvancedEntry, View_Inventory_Entry), Service (INavigationService, ICurrentUserService), Settings (Model_PrintSettings, Registration_Settings), Buttons (Control_Inventory_QuickButtons, ViewModel_Inventory_QuickButtons), Progress (Service_LaunchProgress, Control_Progress), Remove (View_Inventory_AdvancedRemove, View_Inventory_Remove), Row (Model_Inventory_ImportRow, Model_Inventory_PreviewRow), Store (Service_PrintSettingsStore, Service_ErrorLogStore), Cache (Service_Data_ParameterPrefixCache, IService_Data_ParameterPrefixCache), Configuration (View_Dev_Configuration, ViewModel_Dev_Configuration), Dialog (Helper_PrintDialog, Service_Dialog), Host (Service_StageHost, Window_PrintHost), Item (Dao_Inventory_Item, IDao_Inventory_Item), Launcher (Service_FileLauncher, IFileLauncher), Mapping (Dao_Settings_WindowFormMapping, Dao_Settings_UserControlMapping), Suggestion (Control_Suggestion, View_Dev_Suggestion), User (Stage_FirstRunUser, Dao_Settings_User), Analytics (Dao_Visual_Analytics, IDao_Visual_Analytics), Code (Service_Inventory_ColorCode, Dao_Inventory_ColorCode), Column (Model_PrintSettingsColumn, Model_GridColumn)
   Type names starting with: Service_ (Service_AppExit, Service_GlobalExceptionHandler), Model_ (Model_Window_Layout, Model_PrintSettings), View_ (View_QuickUserCreation, View_SignIn), ViewModel_ (ViewModel_QuickUserCreation, ViewModel_SignIn), Dao_ (Dao_DeveloperTools_Workbench, Dao_Help_ErrorLog), IDao_ (IDao_DeveloperTools_Workbench, IDao_Help_ErrorLog), Record_ (Record_PrintSelection, Record_DeveloperTools_LogQuery), Enum_ (Enum_Inventory_AdvancedMode, Enum_Inventory_TransferKind), Helper_ (Helper_Text_Ellipsis, Helper_Text_ShrinkToFit), Registration_ (Registration_Startup, Registration_Shared), Control_ (Control_Suggestion, Control_Progress), Options_ (Options_PrintHostStorage, Options_Data_Database), Stage_ (Stage_Connectivity, Stage_FirstRunUser), Converter_ (Converter_ErrorSeverityToBrush, Converter_ErrorSeverityToGlyph), Dialog_ (Dialog_ErrorDetails, Dialog_ShortcutCapture), IService_ (IService_Data_ParameterPrefixCache, IService_Data_VisualDatabase)
   Which of these endings and prefixes mean "a collaborator", and which mean "plain data"?

2. What marks a view model, and where do view models live?
   Namespaces seen: MTM_WIP (301 types), Core (100 types), Data (58 types), Models (56 types), Contracts (45 types), Module_Inventory (38 types), Shared (36 types), Startup (27 types), Views (26 types), Module_DeveloperTools (23 types), ViewModels (23 types), Services (18 types), Printing (10 types), Module_Shell (8 types), Module_Settings (6 types), Navigation (6 types), Stages (6 types), Configuration (5 types), Converters (5 types), Diagnostics (5 types)
   A namespace on its own is a weak signal: row models and navigation payloads often sit beside
   the view models that own them. Which namespaces hold view models, and what endings mark data
   that must stay a model even there?

3. How is a type registered or resolved?
   Calls seen: Add with 1 type argument (MTM_WIP.Data/Dao_SettingsAndTransactions.cs:57), AddSingleton with 1 type argument (MTM_WIP.Startup/Registration_Startup.cs:29), AddSingleton with 2 type arguments (MTM_WIP.Startup/Registration_Startup.cs:28), GetRequiredService with 1 type argument (MTM_WIP.Startup/Registration_Startup.cs:30), Field with 1 type argument (MTM_WIP.Data/Visual/Service_Data_VisualDatabase.cs:1757), AddTransient with 1 type argument (MTM_WIP.Shared/Registration_Shared.cs:47), RegisterRoute with 2 type arguments (MTM_WIP.Module_Settings/Registration_Settings.cs:60), Cast with 1 type argument (MTM_WIP.Module_Inventory/ViewModels/ViewModel_Inventory_AdvancedRemove.cs:180), Deserialize with 1 type argument (MTM_WIP.Shared/Windowing/Service_Window_Layout.cs:51), TryParse with 1 type argument (MTM_WIP.Shared/Theming/Service_Theme.cs:44), CreateInstance with 1 type argument (MTM_WIP.Startup/Stages/Stage_FirstRunUser.cs:74), Get with 1 type argument (MTM_WIP.Core/Logging/SerilogLoggingConfiguration.cs:103), GetService with 1 type argument (MTM_WIP/App.xaml.cs:79), GetValues with 1 type argument (MTM_WIP.Module_Inventory/ViewModels/ViewModel_Inventory_Advanced.cs:102), NavigateTo with 1 type argument (MTM_WIP.Module_DeveloperTools/ViewModels/ViewModel_Dev_Grid.cs:110), OfType with 1 type argument (MTM_WIP.Module_Inventory/ViewModels/ViewModel_Inventory_Remove.cs:132), PtrToStructure with 1 type argument (MTM_WIP.Shared/Printing/Helper_PrintDialog.cs:89), Register with 2 type arguments (MTM_WIP.Shared/Grid/Control_DataGrid.xaml.cs:21), AddOptions with 1 type argument (MTM_WIP.Data/Registration_Data.cs:41), CreateUnbounded with 1 type argument (MTM_WIP.Data/Logging/Service_ErrorLogStore.cs:43)
   Which of these calls register a type with the container, which pair a type with what it
   resolves, and which read a service out of the container at run time?

4. Which types start the application, and which base types add nothing to bind to?
   Base types declared here: Application, ContentDialog, Exception, IAcceleratorRegistrar, IApplicationExit, IAsyncDisposable, ICommand, ICurrentUserService, IDao_DeveloperTools_Workbench, IDao_Help_ErrorLog, IDao_Help_ErrorReports, IDao_Help_UserFeedback, IDao_Help_UserFeedbackComments, IDao_Inventory_ColorCode, IDao_Inventory_Item, IDao_Inventory_ItemType, IDao_Inventory_Location, IDao_Inventory_Operation, IDao_Inventory_Part, IDao_Settings_QuickButtons, IDao_Settings_Shortcuts, IDao_Settings_System, IDao_Settings_User, IDao_Settings_UserControlMapping, IDao_Settings_WindowFormMapping, IDao_Transactions_Entry, IDao_Transactions_History, IDao_Visual_Analytics, IDialogPresenter, IDialogService, IDispatcherService, IDisposable, IErrorLogStore, IErrorService, IFileLauncher, IGridCommands, IGridStateStore, ILaunchStage, ILogEventSink, IMasterDataCache, INavigationHost, INavigationService, INotificationService, INotifyPropertyChanged, IPageFactory, IPrintPreviewLauncher, IProgress, IQuickActionsContent, IQuickButtonReplayTarget, IService_Data_ParameterPrefixCache, IService_Data_VisualDatabase, IShellProvider, IShellStatusPublisher, IShortcutService, ISingleInstanceGuard, IStageHost, ISuggestionSource, IThemeService, IUserPreferences, IValidateOptions, IValueConverter, IWindowOwnerProvider, Model_Data_Result, Observable_Base, ObservableObject, Page, UserControl, ViewModel_DevPage, Window
   Attributes on methods: DllImport, MarshalAs, RelayCommand
   Attributes on fields: none seen in this repository
   Which base types are framework types that declare no application members, and which
   attributes make the compiler generate a member a binding may name?

5. Which files should the analysis read?
   State any folder that holds generated, vendored or sample code which should stay out.
   XAML root elements seen: Page (18), UserControl (14), Window (2), Application (1), ContentDialog (1)

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
