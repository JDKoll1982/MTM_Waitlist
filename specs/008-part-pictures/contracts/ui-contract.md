# Contract: The Surfaces, Their Identifiers and Their Gates

What a person sees, and the identifiers a consumer or a test codes against. Nothing here renames an id the
specification or the existing markup already pins.

## Surfaces that draw a part's picture

| Surface | File | What changes | The rule |
| --- | --- | --- | --- |
| The Setup part list | `Module_Setup/Views/SetupPartSelectionPage.xaml` | the list becomes cards carrying the part's picture; choosing one still selects the part | FR-018, FR-021 |
| The Setup review of a job's parts | `Module_Setup/Views/SetupReviewPage.xaml` | each subordinate part's row gains its picture, still grouped by family | FR-018 |
| The wizard's die card | `Module_Waitlist/Views/NewRequestDiePage.xaml` | the die card draws the part's picture; it is no longer text only | FR-018 |
| The wizard's confirmation step | `Module_Waitlist/Views/NewRequestSummaryPage.xaml` (and the same list in `NewRequestPreviewPage.xaml` until that step was removed on 2026-09-24) | the part being asked for is drawn with its picture | FR-018 |
| The waitlist card | `Module_Waitlist/Controls/WaitlistLineCardView.xaml` | the card draws the **part's** picture rather than the picture for the kind of request | FR-019 |
| A request whose material cannot be pictured | the same card | the one shared placeholder, and never the family's picture as a stand-in | FR-020 |

The waitlist card's picture is already bound through `SampleOrder.EffectiveImagePath`. What changes is what that
property resolves to — the request's material part, and the placeholder when there is none — not the binding.

## Surfaces that become cards

| Surface | File | The choice that must still take effect |
| --- | --- | --- |
| The dunnage types that may be shown or hidden | `Module_Settings/Views/SettingsPage.xaml`, the two lists `SettingsPage_VisibleDunnageTypesList` and `SettingsPage_HiddenDunnageTypesList` | showing and hiding a type |
| The Setup part list | `Module_Setup/Views/SetupPartSelectionPage.xaml`, the list `SetupPartSelectionPage_PartList` | selecting the part |
| The remaining answer list in the wizard | `Module_Waitlist/Views/NewRequestDetailsPage.xaml`, the control `NewRequestDetailsPage_OptionCombo` | choosing the answer |
| The building choice | `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml`, `NewRequestWorkCenterPage_BuildingCombo`, and `Module_Core/Views/ShellPage.xaml` | changing the building |

**The building choice becomes a flyout** listing every building as a selectable card, and **the building currently in
effect does not appear in that list** (FR-022). Every other converted list keeps the choice it performed, with its
entries drawn as cards (FR-021, SC-009). An entry with no picture is still offered as a card, drawing the shared
placeholder (FR-023).

## The card, and how a test finds it

One shared card template, so four lists answer the picture question once. It follows the component card already in
`Module_Waitlist/Views/NewRequestComponentPage.xaml` and its model `NewRequestComponentOption`: the entry's name as
the card's label, the picture in a fixed-shape box inside the card, a selection outline on the card currently
chosen, and a picture path that always resolves to a drawable picture.

| Identifier | Shape |
| --- | --- |
| `AutomationProperties.Name` on the card | The entry's name or number. |
| `AutomationProperties.AutomationId` on the card | `<CardIdPrefix>_<key>`, where `<key>` is the entry's own identifier. |
| `AutomationProperties.AutomationId` on the picture box | `<CardIdPrefix>Image_<key>`. |
| `CardIdPrefix` | A `const string` on the card's model, one per converted list, exactly as `NewRequestComponentOption.AutomationIdPrefix` is today. |

The card's bounds are `Min*` and `Max*` rather than a fixed width and height, so the sizing is fluid and the same
template serves a list with a long name and one with a short one.

## The Settings surfaces

| Surface | Where | Gate |
| --- | --- | --- |
| The part-picture screen | New page `Module_Settings/Views/PartPictureManagerPage.xaml`, on the existing picture editor base | `permission.settings.part_pictures` |
| The parts with no picture, and their export | The same page | the same entitlement (FR-024, FR-026) |
| The picture folder and the key folder | New panel in `Module_Settings/Views/SettingsPage.xaml` | `permission.settings.storage_paths` — IT Department and Developer (FR-016, FR-017) |
| The retention period for a replaced picture | The same panel | the same storage entitlement (FR-037) |
| The picture cache | The existing panel, `IsPictureCachePanelVisible` | **moves** to `permission.settings.storage_paths` (research D10) |

The part-picture screen offers what the specification asks for and nothing else: find a part, set or replace its
picture, see the parts that have no picture, export that list, and read back who changed what and when (FR-024,
FR-026, FR-027). A refusal states why in the person's words — the limit when a number is too long, and the name of
the part already holding the name when two would share one file.

## Entitlements as this contract uses them

| Key | Holders after this change | What it gates here |
| --- | --- | --- |
| `permission.settings.part_pictures` | `developer`, `it_department`, **`plant_manager`**, **`setup_lead`** | the part-picture screen (FR-024, FR-025) |
| `permission.settings.storage_paths` | `developer`, `it_department` — unchanged | both folders, the retention period, and the picture cache (FR-017) |

## Navigation

The new page routes through the existing `PageService` and `NavigationService` by view-model full name, registered in
`Services/DependencyInjection/ServiceRegistrationExtensions.cs` beside the existing Settings pages. No second
navigation stack, and no dialog where a page is called for.

## Strings

Every new label, action, heading and refusal message is localized in `Strings/en-us/Resources.resw` under a
`Settings_PartPictures_*` prefix for the new screen, `Settings_StoragePaths_*` for the new panel, and the existing
per-surface prefixes for the converted lists and the card. `x:Uid` values are unique, and no label is hardcoded.

## What a test can prove without a screen

| Claim | How |
| --- | --- |
| A surface draws a part's picture when it has one, and the shared placeholder when it does not | Markup read as text plus a view-model assertion per surface, in the shape `WaitlistLineCardMarkupTests` already uses |
| No converted list changed the choice it performs | The existing per-list rules' suites, extended rather than rewritten |
| A Visual part and a WIP part never show each other's picture | Resolver assertions over two rows sharing one part number, then the surface's own projection |
| No surface draws a blank space where a part's picture belongs | The markup and policy scans, extended so a new surface without a placeholder path fails |
