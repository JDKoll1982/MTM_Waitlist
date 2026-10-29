# Contract: pinned identifiers

These strings are the contract. A consumer, a test or a later chunk codes against them, so they are copied
exactly and never recased, renamed or pluralized (FR-021, FR-022).

## Projects, assemblies and root namespaces

| Project / assembly | Root namespace |
| --- | --- |
| `MTM_Waitlist.GenericRequest` | `MTM_Waitlist.GenericRequest` |
| `MTM_Waitlist.GenericRequest.Controls` | `MTM_Waitlist.GenericRequest.Controls` |
| `TempGenericRequest.Preview` | `MTM_Waitlist.GenericRequest.Temp` |

## `Temp` naming shapes (FR-021)

| Kind | Shape | Example |
| --- | --- | --- |
| type, interface, enumeration | PascalCase `Temp` prefix | `TempGenericRequestStepData` |
| namespace | trailing `.Temp` segment | `MTM_Waitlist.GenericRequest.Temp` |
| project, folder, file | `Temp` as the first segment | `TempGenericRequest.Preview`, `Temp/`, `TempPreviewShell.xaml` |
| `x:Name` | PascalCase `Temp` prefix | `TempScenarioPicker` |
| resource key | `Temp` as the feature segment | `TempGenericRequest_Scenario.Title` |
| database object | `temp_` prefix | `temp_generic_request_part` |

A C# identifier never uses a lower-case `temp` prefix, because the repository's naming guard requires
PascalCase for types and would fail the build.

## Resource-key shapes (FR-009)

| Project | Key shape | Example |
| --- | --- | --- |
| flow library | `GenericRequest_<Screen>_<Part>` | `GenericRequest_Part_Title.Text` |
| card library | `GenericRequest_Card_<Part>` | `GenericRequest_Card_Header_Part.Text` |
| preview application | `TempGenericRequest_<Area>_<Part>` | `TempGenericRequest_Scenario_Empty.Content` |

Static XAML labels use `x:Uid` matching the key, so `x:Uid="GenericRequest_Part_Title"` resolves
`GenericRequest_Part_Title.Text`. Values composed in code are read through `"key".GetLocalized()`.

## Seams

| Interface | Project |
| --- | --- |
| `IGenericRequestStepData` | `MTM_Waitlist.GenericRequest` |
| `IGenericRequestNavigator` | `MTM_Waitlist.GenericRequest` |
