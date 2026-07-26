# C# + XAML (WinUI 3) Naming Conventions Questionnaire

Purpose: Capture explicit naming standards before creating enforced WinUI3 C#/XAML naming rules and CI checks.

Please answer each item.

## 1) C# Type Naming
1. Confirm class/record/interface/enum naming style:
- Option A: PascalCase (standard .NET)
- Option B: custom
2. Interface prefix:
- Option A: I-prefixed (for example, `IStartupCoordinator`)
- Option B: no prefix
3. Generic type parameter naming:
- Option A: `T`, `TItem`, `TResult`
- Option B: custom pattern
4. Acronym style in type names:
- Option A: `XmlParser`, `HttpClient` (only first letter capitalized in mixed words)
- Option B: `XMLParser`, `HTTPClient`

## 2) C# Member Naming
1. Public properties/methods/events:
- Option A: PascalCase
- Option B: custom
2. Private fields:
- Option A: `_camelCase`
- Option B: `m_camelCase`
- Option C: `camelCase`
3. Private static readonly fields:
- Option A: `_camelCase`
- Option B: `s_camelCase`
4. Local variables and parameters:
- Option A: camelCase
- Option B: custom
5. Constant fields:
- Option A: PascalCase
- Option B: UPPER_SNAKE_CASE

## 3) Async and Command Naming
1. Async method suffix:
- Option A: required `Async`
- Option B: optional
2. Task-returning methods that are sync wrappers:
- Option A: still include `Async`
- Option B: no suffix for wrappers
3. CommunityToolkit command naming:
- Option A: method `SaveAsync` generates `SaveAsyncCommand`
- Option B: method `Save` generates `SaveCommand`
4. Event handler naming pattern:
- Option A: `OnXxx`
- Option B: `XxxHandler`
- Option C: custom

## 4) WinUI3 XAML Naming
1. Element names (`x:Name`) style:
- Option A: PascalCase (for example, `NavigationViewControl`)
- Option B: camelCase
2. Should every interactable element have `x:Name`?
- Option A: only when referenced from code-behind or tests
- Option B: always
3. Resource keys naming:
- Option A: `Feature_Element.Property`
- Option B: `feature.element.property`
- Option C: custom
4. VisualState names:
- Option A: PascalCase (`Compact`, `Wide`)
- Option B: custom
5. Named Storyboards/Animations:
- Option A: `<Target><Action>Storyboard`
- Option B: custom

## 5) MVVM and View/ViewModel Naming
1. ViewModel suffix:
- Option A: required `ViewModel`
- Option B: custom suffix
2. View naming:
- Option A: `XxxPage`, `XxxView`, `XxxWindow` by role
- Option B: single suffix strategy
3. ViewModel to View key mapping:
- Option A: full type name route keys
- Option B: short symbolic keys
4. Backing field naming for generated `[ObservableProperty]` fields:
- Option A: `_camelCase`
- Option B: `camelCase`

## 6) Dependency Properties and Attached Properties
1. Dependency property static field naming:
- Option A: `<PropertyName>Property`
- Option B: custom
2. Attached property static field naming:
- Option A: `<PropertyName>Property`
- Option B: custom
3. Accessor methods:
- Option A: `GetXxx` / `SetXxx`
- Option B: custom

## 7) Namespaces and Folder Structure
1. Namespace casing:
- Option A: PascalCase segments (`MTM_Waitlist.Services`)
- Option B: custom
2. Folder-to-namespace strict alignment:
- Option A: required
- Option B: flexible
3. Test project namespace mirroring production:
- Option A: required
- Option B: optional

## 8) Localization and Resource Identifier Naming
1. Resw key naming:
- Option A: `Shell_MainShell.Content` style
- Option B: dot-only `shell.mainShell.content`
- Option C: custom
2. Should key prefixes be module-based (`Shell_`, `Settings_`, `Startup_`)?
- Option A: yes
- Option B: no

## 9) Boolean and Enum Naming
1. Boolean property prefix:
- Option A: `Is/Has/Can/Should`
- Option B: custom
2. Enum member naming:
- Option A: PascalCase singular nouns
- Option B: custom
3. Flags enums naming:
- Option A: plural enum type, singular members
- Option B: custom

## 10) Abbreviations, Acronyms, and Banned Terms
1. Allowed abbreviations list (for example, `UI`, `DB`, `VM`, `ID`, `UTC`).
2. Disallowed abbreviations (for example, `cfg`, `usr`, `msg`).
3. Banned terms in symbol names (if any).
4. Acronym normalization choice for code identifiers:
- Option A: `Id`, `Utc`, `Db`, `Ui`, `Vm`
- Option B: keep full uppercase in identifiers

## 11) File Naming
1. C# file naming:
- Option A: one public type per file, filename matches type
- Option B: grouped types allowed
2. XAML/code-behind pairing:
- Option A: strict match (`XxxPage.xaml`, `XxxPage.xaml.cs`)
- Option B: flexible
3. Naming for behavior/helper/converter classes:
- Option A: required suffixes (`Behavior`, `Helper`, `Converter`)
- Option B: custom

## 12) Enforcement and Exceptions
1. Should naming convention violations fail PR checks?
- Option A: yes (hard fail)
- Option B: warning only
2. Are exceptions allowed only with explicit PR note approval?
- Option A: yes
- Option B: no
3. Should legacy code be grandfathered or gradually migrated?
- Option A: grandfathered + touch-to-fix
- Option B: bulk normalize immediately

---

## Answer Template

Copy/paste and fill:

- 1.1:
- 1.2:
- 1.3:
- 1.4:
- 2.1:
- 2.2:
- 2.3:
- 2.4:
- 2.5:
- 3.1:
- 3.2:
- 3.3:
- 3.4:
- 4.1:
- 4.2:
- 4.3:
- 4.4:
- 4.5:
- 5.1:
- 5.2:
- 5.3:
- 5.4:
- 6.1:
- 6.2:
- 6.3:
- 7.1:
- 7.2:
- 7.3:
- 8.1:
- 8.2:
- 9.1:
- 9.2:
- 9.3:
- 10.1:
- 10.2:
- 10.3:
- 10.4:
- 11.1:
- 11.2:
- 11.3:
- 12.1:
- 12.2:
- 12.3:

---

## Completed Answers (2026-07-26)

- 1.1: Option A: PascalCase
- 1.2: Option A: I-prefixed
- 1.3: Option A: T, TItem, TResult
- 1.4: Option A: XmlParser, HttpClient
- 2.1: Option A: PascalCase
- 2.2: Option A: _camelCase
- 2.3: Option B: s_camelCase
- 2.4: Option A: camelCase
- 2.5: Option A: PascalCase
- 3.1: Option A: required Async
- 3.2: Option B: no suffix for wrappers
- 3.3: Option A: SaveAsync -> SaveAsyncCommand
- 3.4: Option A: OnXxx
- 4.1: Option A: PascalCase
- 4.2: Option A: only when referenced from code-behind or tests
- 4.3: Option A: Feature_Element.Property
- 4.4: Option A: PascalCase
- 4.5: Option A: <Target><Action>Storyboard
- 5.1: Option A: required ViewModel
- 5.2: Option A: XxxPage, XxxView, XxxWindow by role
- 5.3: Option A: full type name route keys
- 5.4: Option A: _camelCase
- 6.1: Option A: <PropertyName>Property
- 6.2: Option A: <PropertyName>Property
- 6.3: Option A: GetXxx / SetXxx
- 7.1: Option A: PascalCase segments
- 7.2: Option A: required
- 7.3: Option A: required
- 8.1: Option A: Shell_MainShell.Content style
- 8.2: Option A: yes
- 9.1: Option A: Is/Has/Can/Should
- 9.2: Option A: PascalCase singular nouns
- 9.3: Option A: plural enum type, singular members
- 10.1: UI, DB, VM, ID, UTC
- 10.2: cfg, usr, msg, auth
- 10.3: Manager, Data, Helper (banned when generic)
- 10.4: Option A: Id, Utc, Db, Ui, Vm
- 11.1: Option A: one public type per file, filename matches type
- 11.2: Option A: strict match
- 11.3: Option A: required suffixes
- 12.1: Option A: yes (hard fail)
- 12.2: Option A: yes
- 12.3: Option B: bulk normalize immediately
