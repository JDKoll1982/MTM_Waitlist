# Dunnage Catalogue Surface

## Purpose

The generic request flow needs a dunnage step that draws the plant's real, image-backed dunnage catalogue as a page of the wizard. The work order flow already draws the same catalogue as a dialog. This contract fixes the one shared catalogue read, the shared view model that filters and selects it, the shared control that draws it, and the two surfaces that host the control. It also fixes what a UI test may rely on and what the wizard step promises.

## The read seam

`MTM_Waitlist.Shared.Services.IDunnageCatalogueReader` is the one interface the generic flow uses to reach the catalogue. It exposes a single method.

```csharp
Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default)
```

`MTM_Waitlist.Setup.Services.DunnageCatalogueReader` is the implementation. The composition root binds them in `Services/DependencyInjection/ServiceRegistrationExtensions.cs`.

The read path is fixed. The reader calls `IDunnageWorkflowService.GetAllDunnagePartsAsync()`, which calls `sp_Dunnage_Parts_GetAll` on `mtm_receiving_application`.

No consumer names a store, a table or a procedure directly. The read keeps the one implementation the substitute dialog already uses.

## The shared view model

`MTM_Waitlist.Shared.ViewModels.DunnageCatalogueViewModel` owns the filter, the picture choice, the selection and the load outcome. A consumer or a test codes against these members.

| Member | Meaning |
| --- | --- |
| `FilterText` | the one-box free-text filter |
| `ShowPartsWithoutImages` | whether parts with no picture are shown |
| `DisplayedParts` | the narrowed list a surface draws |
| `SelectedPart` | the chosen part, or nothing |
| `IsBusy` | whether a read is in flight |
| `EmptyStateMessage` | the words for a read that ended with nothing to show |
| `HasNoResults` | true when `DisplayedParts` is empty |

The load call is `LoadAsync`. It runs the read and reports one of three outcomes: the read succeeded and returned parts, the read succeeded and returned none, or the read failed. The view model returns the outcome and does not choose the words a surface shows.

The filter rule is one box. It matches a part when `PartNumber`, `DunnageTypeName` or `HomeLocation` contains the text, case-insensitively.

The picture rule is one choice. When `ShowPartsWithoutImages` is false, only parts that have an image are shown.

## The persisted preference

The picture choice is remembered per person under the key `Setup.DunnageImageSearch.ShowPartsWithoutImages`. The default is `true`, which shows every part.

## The control and its hosts

The catalogue markup lives in `DunnageCatalogueView` under `Module_Shared/Controls`. Two surfaces host it.

| Host | Surface |
| --- | --- |
| `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml` | the work order substitute dialog, whose behaviour does not change |
| `Module_GenericRequest/Views/GenericRequestDunnagePage.xaml` | the generic wizard step |

The page is routed in `Services/DependencyInjection/ServiceRegistrationExtensions.cs` with this line.

```csharp
pageService.Configure<GenericRequestDunnageViewModel, GenericRequestDunnagePage>()
```

## Automation identifiers

A UI test codes against these `AutomationId` values, kept exactly as the shipped markup spells them.

| `AutomationId` |
| --- |
| `SetupDunnageImageSearchDialog_FilterInput` |
| `SetupDunnageImageSearchDialog_RefreshButton` |
| `SetupDunnageImageSearchDialog_ShowAllToggle` |
| `SetupDunnageImageSearchDialog_PartTiles` |

The values stay as they are because the dialog's behaviour and any automation naming it must not change, even though the control is now shared.

## Step presentation

The wizard step maps the catalogue's three outcomes onto `GenericRequestPresentation`. Its values are `Populated`, `Empty` and `Unavailable`.

`Empty` and `Unavailable` are never shown for the same read. The forward button is gated by `CanAnswer`, which is true only when a part is chosen.

## Not part of this contract

There is no HTTP endpoint. There is no command line. No database artifact is added. The work order substitute dialog keeps its current behaviour and is not re-specified here.
