# C# + XAML (WinUI 3) Naming Ruleset

## Scope
This document locks naming conventions for C#, XAML, and RESW resources in the MTM_Waitlist repository.

## Locked Conventions
- Type/member casing follows .NET defaults (PascalCase for public symbols, camelCase for locals/parameters).
- Interfaces are I-prefixed.
- Private fields use _camelCase; private static readonly fields use s_camelCase.
- Async methods with actual awaited flow must end in Async.
- Event handlers use OnXxx naming.
- ViewModels must end in ViewModel.
- Views must use XxxPage/XxxView/XxxWindow role suffixes.
- x:Name values are PascalCase and only used when needed for code-behind/tests.
- VisualState names are PascalCase.
- Storyboards follow <Target><Action>Storyboard.
- RESW keys follow Feature_Element.Property style and module prefixes.
- Dependency and attached property field names use <PropertyName>Property.
- Attached property accessors use GetXxx/SetXxx.
- Namespace segments are PascalCase and folder-to-namespace alignment is required.
- One public type per file and strict XAML/code-behind file pairing required.

## Acronyms and Restricted Tokens
- Allowed abbreviation set: UI, DB, VM, ID, UTC.
- Normalized identifier casing for acronyms: Ui, Db, Vm, Id, Utc.
- Disallowed abbreviations: cfg, usr, msg, auth.
- Generic terms Manager, Data, Helper are disallowed unless accompanied by a distinct architectural boundary descriptor.

## Enforcement
- Violations hard-fail PR checks.
- Exceptions require explicit PR note approval.
- Existing legacy naming is not grandfathered and should be normalized in bulk.
