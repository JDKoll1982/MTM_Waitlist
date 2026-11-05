# Data Model: Generic Work Center Dunnage Catalogue Page

This document names the entities the plan introduces and the fields each one carries. It states shapes, keys, states and transitions. It does not repeat why the plan chose them.

The work turns on one row shape that both the generic dunnage page and the Setup substitute dialog draw. That row crosses an assembly boundary through a one method reader. A shared view model owns the filter, the picture choice, the selection and the load outcome. A draft field pair records the choice, and a small answer record writes it onto the raised request.

## 1. `DunnageCataloguePart`

`DunnageCataloguePart` is the row both surfaces draw. It is new and lives in `MTM_Waitlist.Shared/Models/`.

It is a projection of `MTM_Waitlist.Setup.Models.SetupDunnagePart`, made at the reader seam. The Setup type stays where it is. The Setup type's `Metadata` and `IsSelectedForPair` fields are not carried into this row.

`ImagePath` is a path string, never bytes. It is a database relative path such as `Parts/Steel Rack.png`. It is resolved against the shared dunnage image root by `DunnageImagePathResolver.GetDisplayPath`. A path whose file is missing resolves to no picture, which the surface turns into the shared placeholder, not into an unavailable store state.

| Field | Type | Meaning |
| --- | --- | --- |
| `Id` | `string` | The part's stable identifier from the catalogue read. |
| `TypeId` | `string` | The identifier of the part's dunnage type. |
| `PartNumber` | `string` | The part number the filter and the sort use. |
| `DisplayName` | `string` | The name the card shows for the part. |
| `DunnageTypeName` | `string` | The dunnage type's display name, populated by the image search load. |
| `HomeLocation` | `string` | The home location the filter matches against. |
| `ImagePath` | `string` | The database relative path to the part's picture, or blank when it has none. |

Computed members mirror the Setup row exactly:

| Member | Type | Meaning |
| --- | --- | --- |
| `HasImage` | `bool` | True when `ImagePath` is not blank. |
| `ShowNoImagePlaceholder` | `bool` | True when the part has no picture, so a placeholder glyph is shown. |
| `DisplayImagePath` | `string` | The picture path when one exists, otherwise `ImagePicturePolicy.NoImagePath`. |
| `ImageFallbackText` | `string` | `"No image available"` when there is no picture, otherwise empty. |

## 2. `IDunnageCatalogueReader`

`IDunnageCatalogueReader` is the seam by which the generic request flow reaches the catalogue without gaining an assembly reference. It is new and lives in `MTM_Waitlist.Shared/Services/`.

It declares one method that returns the parts.

| Member | Signature |
| --- | --- |
| `ReadAllAsync` | `Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken)` |

The implementation is `DunnageCatalogueReader` in `MTM_Waitlist.Setup`. It delegates to the existing `IDunnageWorkflowService.GetAllDunnagePartsAsync()`. That service calls `sp_Dunnage_Parts_GetAll` on `mtm_receiving_application`.

The composition root binds the interface to that implementation. No inline SQL is written and no second read of the receiving store is introduced.

## 3. `DunnageCatalogueViewModel`

`DunnageCatalogueViewModel` owns the catalogue's state and behaviour. It is new and lives in `MTM_Waitlist.Shared/ViewModels/`.

The Setup image search dialog view model delegates to it, so the dialog and the page share one filter, one sort, one picture choice and one read.

| State | Type | Meaning |
| --- | --- | --- |
| `FilterText` | `string` | The free text the requester typed. |
| `ShowPartsWithoutImages` | `bool` | Whether parts with no picture are shown. |
| `DisplayedParts` | `IReadOnlyList<DunnageCataloguePart>` | The parts the filter and the picture choice currently leave. |
| `SelectedPart` | `DunnageCataloguePart?` | The chosen part, or null when none is chosen. |
| `IsBusy` | `bool` | True while the read is in flight. |
| `EmptyStateMessage` | `string` | The words the view model reports, which the surface renders. |
| `HasNoResults` | `bool` | True when `DisplayedParts` is empty. |

Behaviour rules:

- The filter is one free text field. It matches `PartNumber`, `DunnageTypeName` or `HomeLocation`, case insensitive, by containment.
- The list is sorted by `PartNumber`, case insensitive, with `StringComparer.OrdinalIgnoreCase`.
- `ShowPartsWithoutImages` is a person scoped preference under the key `Setup.DunnageImageSearch.ShowPartsWithoutImages`. It defaults to true.
- When the preference is false, parts with no picture are hidden before the text filter applies.

Load outcome:

The view model reports one of three outcomes after a finished read.

| Outcome | Meaning |
| --- | --- |
| `Ready` | The read succeeded and returned parts. |
| `Empty` | The read succeeded and returned no parts. |
| `Unavailable` | The read failed. It carries `attemptedAtUtc`, the time of the attempt. |

The view model reports the outcome. It does not choose the words a surface shows. The dialog keeps its flat failure sentence. The page reports unavailable with the attempt time and a retry. Both surfaces get the same outcome value.

## 4. `DunnageSelection` on the draft

The chosen dunnage is recorded on `GenericRequestDraft` by two existing fields. The draft is not changed by this chunk.

| Field | Type | Meaning |
| --- | --- | --- |
| `DunnageType` | `string` | The chosen part's type name, or empty when nothing is chosen. |
| `DunnagePart` | `string` | The chosen part's part number, or empty when nothing is chosen. |

Whether the chosen part is still offered is not a draft field. It is established at raise time against a fresh catalogue read and is persisted on the raised request as `dunnage.offered`, described in the next section.

Selection rules:

- Exactly one part is carried by a request. A second part is a second request, and the flow says so rather than allowing one to be added in place (FR-005).
- Leaving the page without choosing records nothing and does not advance past a step that requires a part (FR-006).
- Nothing is pre selected. The old view model pre selected the first type and the first part. The new one does not. The forward gate is `CanAnswer => SelectedPart is not null`.

## 5. `GenericRequestDunnageAnswer`

`GenericRequestDunnageAnswer` is the value a raised request records in its existing `answers_json` column. It is new and lives in `MTM_Waitlist.Core/Models/`.

It mirrors the existing `GenericRequestPartAnswer` in the same folder. It is a record with const key strings, a `ToAnswers()` method that returns `Dictionary<string, string>` compared case insensitively, and a static `TryRead`.

The keys are fixed.

| Key | Meaning |
| --- | --- |
| `dunnage.part` | The chosen part's part number. |
| `dunnage.type` | The chosen part's type name. |
| `dunnage.picture` | The picture reference recorded for the part. |
| `dunnage.offered` | Whether the part is still in the catalogue. |

`dunnage.offered` records the fact of whether the part is still offered. A part that has left the catalogue since it was chosen is written as `false`. The wording a surface shows for that fact, such as a sentence reading "no longer in the catalogue", is an open implement time clarification. The mechanism is fixed here and the words are not.

`dunnage.picture` is what lets the confirmation, the card and the detail draw the part's picture when no job snapshot is in hand (FR-008). The submission also writes the chosen part into the request's `InputValue` column, so the card's second line can name the part from `input_value`.

## State transitions

### Step lifecycle

The catalogue step moves through one loading state and three settled outcomes.

```text
Loading -> Populated | Empty | Unavailable
```

| State | Meaning |
| --- | --- |
| `Loading` | The catalogue read is in flight. Nothing is offered and nothing is claimed about what the plant holds. |
| `Populated` | The read returned parts. The filter and the picture choice narrow them. |
| `Empty` | The read succeeded and the plant holds no dunnage parts. This is an answer, not a failure. |
| `Unavailable` | The read failed. The attempt time and a retry are shown, never a shortened list. |

Rules:

- `Empty` is never `Unavailable`. A read that succeeds with no parts reports nothing to offer, and never reports the catalogue as unavailable (FR-012).
- A still loading catalogue is never drawn as empty. Progress is shown while the read is in flight (FR-010). The shared step frame owns the loading state.

### Selection

```text
none -> chosen
chosen -> chosen   (when the page is revisited)
```

- With no choice the state is `none`. The forward button is unavailable.
- Choosing a part moves the state to `chosen`.
- Revisiting the page shows the chosen part as chosen (FR-004).

### At raise

```text
chosen part present in a fresh read  -> recorded normally
chosen part absent from a fresh read -> kept, dunnage.offered = false
```

- At raise time the chosen part is checked against a fresh catalogue read.
- When it is present, the request records it normally.
- When it is absent, the request keeps the part the requester chose and writes `dunnage.offered = false` (FR-009).

## Validation rules

| Rule | Statement |
| --- | --- |
| FR-002 | The page draws its parts, pictures and search from the same source the substitute picker uses. No second dunnage list is introduced. |
| FR-003 | The page offers one filter that matches part number, type or location, and the same choice of whether parts without a picture are shown. |
| FR-004 | A chosen part becomes the request's dunnage and is shown as chosen when the page is revisited. |
| FR-005 | A generic dunnage request carries exactly one dunnage part, and the flow says a second part is a second request. |
| FR-006 | Leaving the page without choosing records no dunnage part and does not advance past a step that requires one. |
| FR-007 | A catalogue that cannot be read is reported as unavailable with the time of the attempt and a retry. It is never replaced with a shortened, cached or fabricated list. |
| FR-009 | A part that has disappeared from the catalogue since it was chosen is kept on the request and marked as no longer offered, never silently dropped or replaced. |
| FR-010 | Loading is distinguishable from a finished read. Progress is shown while the catalogue loads, and a still loading catalogue is never presented as though it held no parts. |
| FR-012 | A catalogue that reads successfully and holds no parts is reported as having nothing to offer, and never reported as unavailable. |
