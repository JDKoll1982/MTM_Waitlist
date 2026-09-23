# Implementation Plan: A Picture for Every Part

**Feature Branch**: `008-part-pictures`
**Spec**: `spec.md` (38 functional requirements, 7 user stories, 10 success criteria)
**Created**: 2026-09-23

## Scale note

This change spans about forty-five files across five areas: the picture share's layout and the move of every picture
already stored on it, the store and its procedures, a part-picture layer in `MTM_Waitlist.Shared` and
`MTM_Waitlist.Settings`, eight surfaces that draw a part, and the two Settings screens. A reader should watch three
things above all.

First, the **move is not a rename**. Every picture the application stores today sits one folder short of where the
specification puts it, and the recorded value in the store names the old place. The reader has to resolve both
layouts, because that is what stops the move leaving a picture unresolvable, and the move itself is a reviewed
run-once step rather than something that happens during a start.

Second, **the entitlement widens and the storage controls must not widen with it**. The specification grants the
picture entitlement to Setup Lead and Plant Manager, and the screen that changes the picture root and the key folder
belongs to IT Department and Developer. Those are two permission keys, and the picture-cache panel is gated on the
wrong one today: it is on the picture entitlement, so widening that entitlement would hand a Setup Lead the folder
this machine copies pictures from. The gate moves in the same change.

Third, the **local copy rule changes shape for parts only**. Today startup mirrors the whole picture root onto the
machine. The specification asks for a part's picture to be copied the first time that part is drawn, and at part
scale that is a different mechanism, not a setting.

## Summary

Give every part the application can name a picture of its own, kept apart by system so a Visual part and a WIP part
that share a number are two pictures, and draw it on every surface that names a part. The pictures live under the
existing configurable root, re-laid-out as one folder per collection and, inside the part collections, one folder per
part-number prefix with a catch-all beside them; the folders stay a setting and a stored value stays relative to it,
which is what lets the same record resolve on a machine that mapped a drive letter and one that did not.

The store already holds what the feature needs: `config_images_locations` carries a scope, an item key and a relative
path, the entitlement key `permission.settings.part_pictures` exists, the storage service already writes relative
paths and archives what it replaces, and `ImagePicturePolicy` already decides what may be drawn. The work is two new
scopes, one audit table, one refusal rule, the re-layout and its move, a part-picture resolver that copies on first
draw, and the surfaces. Only the parts of the application that could not name a picture before are new code; the
screens that set one are the existing picture editor pointed at a new key.

Stack choices are the project's own and need no new dependency: WinUI 3 on Windows App SDK 2.3.1, MySQL 5.7 through
the existing `IMySqlHelperServer`, and the file save picker the Windows App SDK already ships.

## Project Structure

```text
Database/
  Tables/17_config_images_locations/            scope comment widened to the five scopes; the unique key untouched
  Tables/33_config_images_locations_history/    new: one row per set or replace, carrying what it replaced
  StoredProcedures/sp_config_images_locations_insert/    gains the history row for a first picture
  StoredProcedures/sp_config_images_locations_update/    gains the history row carrying the replaced path
  StoredProcedures/sp_config_images_locations_history_get/   new: the change record for one part
  StoredProcedures/sp_config_images_locations_scope_paths_get/  new: every stored path for one scope
  StoredProcedures/sp_config_images_locations_paths_move/    new: the re-layout rewrite, reported by affected rows
  Seeds/seed_permission_role_baselines/         setup_lead and plant_manager join permission.settings.part_pictures
  Seeds/seed_picture_layout_move/               new: rewrites each recorded path to the new layout, paired rollback
  Bootstrap/create_database.sql                 the history table on the fresh-store path
  Bootstrap/update_table_descriptions.sql       the history table, guarded, for a store that already exists
  LocalSettings/  Files/                        no change: no statement text leaves Database/
  AllTables.sql  AllSPs.sql  AllSeeds.sql       regenerated in the same change
  Validation/part_pictures_schema/              new: the table, the five scopes and the new procedures
MTM_Waitlist.Shared/
  Helpers/PartPictureLayout.cs                  new: the one place the collections, the prefixes and the
                                                catch-all are spelled, and the one place a file name is made safe
  Helpers/AppStoragePaths.cs                    the three collection names, and a resolve that reads both layouts
  Helpers/ImageCachePaths.cs                    the two part cache folders, beside the two mirrored ones
  Services/PartPictureCacheStore.cs             new: copy one part's picture on first draw, and never remove
  Services/ImageCacheSyncService.cs             leaves the two part collections to the store above
MTM_Waitlist.Core/
  Contracts/Services/IPartPictureResolver.cs    new: the reader every surface calls, declared where all readers see it
MTM_Waitlist.Settings/
  Services/PartPictureResolver.cs               new: resolves system, part number and family to a file, or nothing
  Services/IPartPictureWriteService.cs          new: set, replace, refuse, and record
  Services/PartPictureWriteService.cs
  Services/PartPictureCoverageService.cs        new: the parts with no picture, and the export rows
  Models/PartPictureSystem.cs                   new: the two part systems, never the part number alone
  Models/PartPictureRow.cs                      new: one row of the picture screen
  Models/PartPictureMissRow.cs                  new: one row of the missing-picture list
  Models/PartPictureSetResult.cs                new: a typed answer, so a refusal says which part it collides with
  ViewModels/PartPictureManagerViewModel.cs     the screen, on the existing picture-editor base
  ViewModels/SettingsViewModel.cs               two new panels: the part-picture screen's entry point, and the
                                                storage folders with the retention period
  Models/ConfigSettingValue.cs                  image_storage.archive_keep_days is now owned by the screen
Module_Settings/Views/
  PartPictureManagerPage.xaml (+ .xaml.cs)      new page: search, one part, set or replace, the missing list, export
  ImageOverrideEditorControl.xaml               reused as the editor, unchanged
  SettingsPage.xaml                             the two dunnage type lists become cards; two new panels
Module_Setup/Views/
  SetupPartSelectionPage.xaml                   the part list becomes cards carrying each part's picture
  SetupReviewPage.xaml                          the subordinate-parts rows gain the part's picture
Module_Waitlist/Views/
  NewRequestDiePage.xaml                        the die card gains the part's picture
  NewRequestSummaryPage.xaml                    the confirmation step draws the part's picture
  NewRequestPreviewPage.xaml                    the same list, drawn the same way
  NewRequestDetailsPage.xaml                    the remaining answers become cards
  NewRequestWorkCenterPage.xaml                 the building choice becomes a flyout of cards
Module_Waitlist/Controls/WaitlistLineCardView.xaml   the card draws the part's picture, not the request type's
Module_Core/Views/ShellPage.xaml                the building choice becomes a flyout of cards
MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs     EffectiveImagePath is the part's picture
MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestComponentOption.cs  ImagePath is the part's picture
MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs   CanonicalCategory delegates to PartPictureLayout
MTM_Waitlist.Startup/Services/StartupCoordinator.cs   the archive cleanup joins the cache step
ViewModels/ShellViewModel.cs                    the building flyout's entries, without the building in effect
Services/DependencyInjection/ServiceRegistrationExtensions.cs  the new services and the one new page route
Strings/en-us/Resources.resw                    every new label, action and refusal
appsettings.json                                ImageStorage.ArchiveKeepDays moves to ninety
tools/Move-PartPictureLayout.ps1                new: the run-once move, beside the existing capture tools
MTM_Waitlist.Tests/Module_Shared/               PartPictureLayout, AppStoragePaths, PartPictureCacheStore
MTM_Waitlist.Tests/Module_Settings/             the resolver, the write service, the coverage service, the two
                                                screens, the widened parity fixture, live procedure checks
MTM_Waitlist.Tests/Module_Setup/ Module_Waitlist/  the converted surfaces' markup and view-model rules
```

**Structure Decision**: the layout and the local copy rule live in `MTM_Waitlist.Shared`, because the app, Setup,
Settings, the waitlist libraries and the test project all see it; the resolver and the write path live in
`MTM_Waitlist.Settings` beside `ImageLocationService`, behind interfaces declared in `MTM_Waitlist.Core`, which is how
`IWorkCenterImageService` and `IImageLocationService` already work; and the app project stays the composition root
holding every view.

## Constitution Check

| Principle | Verdict | Why |
| --- | --- | --- |
| I. Spec-First, Verified Delivery | PASS | This plan follows a written specification of 38 requirements and seven user stories, and the spec's three open questions are answered in `research.md` rather than left to implementation. No work item is ticked without a build, a test or a verified check behind it. |
| II. Live Data Integrity — Internal Stores Are Never Mocked | PASS, with a note | Every picture row is read and written live in `mtm_waitlist`; there is no demo mode and no stand-in row, and the one stand-in the specification names is removed rather than added (the component card's placeholder). The note: the Visual parts in the missing-picture list are an **external read**, so they go through the existing Visual read path and its cached fallback, and a Visual part the fallback cannot supply is reported as unavailable rather than invented. |
| III. Stored-Procedure-First Database Discipline | PASS | One new table, four new procedures and two changed procedures, one changed seed, one new seed, plus `Bootstrap/create_database.sql`, the guarded `Bootstrap/update_table_descriptions.sql`, and the aggregates `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`, each artifact with `create.sql` and `rollback.sql` in the same change. The picture write stays on the existing procedure path so one write owns one picture, no statement text enters application code, and the move reports affected rows through the non-query seam. |
| IV. MCP-First, Grounded Decisions | PASS | The repository was read through Serena, and the one platform API this feature adds — the file save picker used by the export — was checked against Microsoft Learn rather than recalled; `research.md` names the source, which was the Learn code-sample search because the docs search tool was reported disabled in this session. |
| V. WinUI 3 Platform Conformance | PASS, with a note | `Microsoft.UI.Xaml` throughout, CommunityToolkit.Mvvm generators, navigation through the existing `PageService` and `NavigationService`, every new string localised, no new converter unregistered in `App.xaml`. The note: the card template this feature copies from `NewRequestComponentPage.xaml` pins a fixed 150 by 170 tile; the shared template this feature introduces uses `Min*` and `Max*` bounds instead, so eight new card lists do not multiply a fixed-size layout. The component page's own markup is left as shipped. |
| VI. Evidence-Based Verification Gates | PASS | The gates are the documented ones: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` with zero warnings and zero errors, the full suite green, the live-database integration checks for the new procedures, the policy and markup scans extended rather than weakened, and the moved layout proved by resolving a picture of each kind from the new place and from the un-moved place. |
| VII. Readable, End-User-Facing Delivery | PASS | Applies to every chat reply made while this plan is executed. |
| Additional Constraints — Security & Secrets | PASS | No credential is involved, hardcoded, logged or displayed. The write path names a part number in a refusal message and stores nothing else about the person beyond the existing actor column and the new history row's actor and timestamp. |
| Additional Constraints — External Integration & Cache Boundaries | PASS | Infor Visual is read and never written. The Visual parts this feature lists come through the existing read shapes and their settled-verdict fallback, so a reachable-but-empty answer stays a real answer. The local picture cache is a file cache of this application's own pictures, not a mirror of external data, so the atomic snapshot rule is untouched. |
| Additional Constraints — Documentation & Extensibility | PASS | `Database/Database-Ruleset.md`, `FEATURES.md` and `CHANGELOG.md` move with the change, the file-drop convention the specification depends on is written down where an operator will find it, and the retired wording the change makes wrong — the component card's "until part pictures exist" — is removed rather than left behind. |
