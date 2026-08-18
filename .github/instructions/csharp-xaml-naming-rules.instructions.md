---
applyTo: "**/*.{cs,xaml,resw}"
---

# MTM_Waitlist WinUI3 C# + XAML Naming Rules (Locked)

## C# Naming
- Types: PascalCase.
- Interfaces: must be I-prefixed.
- Generic type params: T, TItem, TResult style.
- Acronyms in identifiers use normalized casing: Id, Utc, Db, Ui, Vm.
- Public methods/properties/events: PascalCase.
- Private fields: _camelCase.
- Private static readonly fields: s_camelCase.
- Locals/parameters: camelCase.
- Constants: PascalCase.

## Async and Commands
- Async methods with awaited async flow must end with Async.
- Task-returning wrappers without asynchronous flow may omit Async suffix.
- CommunityToolkit RelayCommand naming keeps method name as command prefix (SaveAsync -> SaveAsyncCommand).
- Event handler naming pattern: OnXxx.

## MVVM and Type Suffixes
- ViewModels must end with ViewModel.
- Views must use role suffixes: XxxPage, XxxView, XxxWindow.
- Navigation route keys use full ViewModel type names.
- ObservableProperty backing fields use _camelCase.

## XAML Naming
- x:Name values use PascalCase.
- Add x:Name only for elements referenced by code-behind/tests.
- VisualState names: PascalCase.
- Storyboards: <Target><Action>Storyboard naming.

## Resource Key Naming
- resw keys use Feature_Element.Property style.
- Module prefixes required (Shell_, Settings_, Startup_, etc.).

## Dependency/Attached Properties
- Dependency property fields: <PropertyName>Property.
- Attached property fields: <PropertyName>Property.
- Attached property accessors: GetXxx / SetXxx.

## Namespace and File Structure
- Namespace segments use PascalCase.
- Folder-to-namespace alignment is required.
- Test namespaces must mirror production namespace layout.
- One public type per .cs file; filename must match type name.
- XAML/code-behind pairs must match strictly (XxxPage.xaml + XxxPage.xaml.cs).

## Abbreviations and Banned Terms
- Allowed abbreviations: UI, DB, VM, ID, UTC.
- Disallowed abbreviations: cfg, usr, msg, auth.
- Banned generic terms in symbol names: Manager, Data, Helper unless the symbol has a distinct architectural descriptor.

## WinUI Compile Guardrails
- `ContentDialog.ShowAsync()` returns `IAsyncOperation<ContentDialogResult>`; do not call `.ConfigureAwait()` on it.
- Nullable tuples from helper methods must be guarded or destructured before `.Item1`/`.Item2` access.
- If a new converter or XAML resource is introduced, add the matching `xmlns` and `x:Key` registration in `App.xaml` during the same change; missing registration causes XAML compile failures.
- Treat XAML compile errors as build-blocking; verify with `dotnet build` or the relevant test target before considering the patch complete.

## Enforcement
- Naming violations must hard-fail PR checks.
- Exceptions require explicit written PR approval.
- Legacy naming must be bulk normalized (no grandfathering mode).
