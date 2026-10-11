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
- **Test methods are the exception.** A method carrying `[TestMethod]` or `[DataTestMethod]` is named by the
  test-method rule below instead, so it does not end in `Async` and must not be made to. The guard skips the
  suffix check for these methods and enforces the test-method format on them in its place.
- CommunityToolkit RelayCommand naming STRIPS a trailing `Async` from the method name: `SaveAsync` -> generated command `SaveCommand` (NOT `SaveAsyncCommand`). Bind the generated command in XAML accordingly (e.g. `Command="{x:Bind ViewModel.SaveCommand}"`).
- Event handler naming pattern: OnXxx.

## Test Method Naming
`<Category>_<File>_<Method>_<TestName>` — four segments, three underscores, and no other underscore anywhere in
the name. The full statement of the rule, the closed category vocabulary and the worked examples live in
`.github/instructions/test-conventions.instructions.md` under "Test code style"; this file states only what the
guard enforces.

- **Category** is how the test is classified for sorting and filtering: `Database`, `UI`, `Model`, `ViewModel`,
  `Service`, `Helper`, `Audit` or `Converter`. A new category is added to this list before a test uses it.
- **File** is the test file's name without its extension — the file, not the class, because the file is what a
  reader locates. `<File>` carries no underscore, and a file whose name has one is renamed rather than mangled.
- **Method** is the operation or subject under test, without underscores.
- **TestName** is everything the test says after the method, camel-cased so its underscores can go: the parts are
  joined and each keeps its own capitalisation, so
  `_WhenTheStepPassesItsStatedMaximum_ReportsFailedRatherThanWaitingOnIt` becomes
  `WhenTheStepPassesItsStatedMaximumReportsFailedRatherThanWaitingOnIt`. The three underscores that separate the
  four segments are the only underscores a test name may hold.
- **Three segments are correct when there is no method to name.** A test that asserts something about a whole type
  rather than about one operation carries `<Category>_<File>_<TestName>` —
  `Audit_RetiredSymbolAuditTests_NoRetiredSymbolRemainsInCodeOrDatabaseArtifacts`. The `<Method>` segment is
  omitted rather than filled with a repeat of the test name, which would say nothing twice.

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

- Naming violations must hard-fail PR checks **on the files a pull request changed**. That was the check's
  real scope: `.github/workflows/csharp-xaml-naming-compliance.yml` built the changed-file list from the PR
  diff and validated only those files, so a run with no changed C#/XAML/RESW files passed by design and was
  not evidence that the tree was clean. **That workflow was deleted from the working tree on 2026-10-10**, so
  the guard below is now the only check and must be run deliberately:
  `powershell -ExecutionPolicy Bypass -File .github/scripts/validate-csharp-xaml-naming.ps1`.
- Exceptions require explicit written PR approval.
- Pre-existing violations are **grandfathered and tracked in a report** —
  `.github/reports/csharp-xaml-naming-violations.md` — rather than normalized in a bulk pass (decision
  **D-4**, 2026-09-20). New and changed code still hard-fails; the report is the backlog for a later
  normalization pass, taken one rule at a time.
- **D-4 amended 2026-10-10.** One rule has been taken: the shared strings file's RESW keys were normalized in
  bulk by owner decision, so `Strings/en-us/Resources.resw` went from 202 findings to 3 and the renamed keys
  moved with every reference that read them. That is the shape any further normalization takes — estimate the
  blast radius, put it to the owner, take **one rule**, and re-measure the report from the guard's own output.
  Do not extend a normalization to other rules on the strength of this one, and do not treat the surviving
  findings as a licence to rename while touching an unrelated file.
