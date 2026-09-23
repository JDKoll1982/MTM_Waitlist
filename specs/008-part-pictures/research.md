# Phase 0 Research: A Picture for Every Part

The specification settles what the feature must do and, in its Brainstorm Log, the literal layout the owner dictated.
What it leaves open are the decisions below: three of them are the spec's own open questions, the rest are the
choices this plan has to make before the data model and the contracts can be written.

Every claim about this repository below was read from it. The one claim about the platform that could not be read
from it was checked against Microsoft Learn and is named as such.

---

## D1. Where a part picture is recorded

**Decision**: reuse `config_images_locations`, adding two scopes to its existing three: `visual_part` and
`wip_part`. A part picture is one row per scope and part number, exactly as a request item's picture is one row per
scope and item code today.

**Rationale**: that table already carries everything this feature needs — a scope, an item key, a path stored
relative to a configurable root, an active flag, the actor and the timestamps — and four mechanisms are already built
on it: `IImageOverrideReadService` and `IImageOverrideWriteService`, `ImageStorageService` with its relative writes
and its archive, `ImageOverrideDialogViewModel` with its row list and batched commit, and the
`permission.settings.part_pictures` entitlement. The unique key `uq_config_images_locations_scope_item` already
enforces one picture per scope and key, which is FR-006 exactly. Two new scope values cost two strings; a second
table would cost a second copy of all five mechanisms and a second migration.

**Alternatives considered**: a dedicated `config_part_pictures` table, rejected because it duplicates the read
service, the write service, the storage service, the editor base and the entitlement, and because two tables that
mean the same thing drift; a column on the job's part rows, rejected because FR-003 requires one picture per part
wherever the part is named, and a job is one place a part is named.

## D2. Where the change record lives

**Decision**: a new table, `config_images_locations_history`. One row per set or replace, carrying the picture it
replaced, the picture that replaced it, the actor and the UTC timestamp.

**Rationale**: FR-027 asks for who, when and what the previous picture was, and asks for it to be readable. The
picture table's own actor and timestamp columns say who touched the row last and not what it held before, so they
cannot answer the question for a second replacement. A `*_history` table is the shape the schema rules already
prescribe for auditability, and the write that produces a row is the same transaction as the write that changes the
picture, so a change is never recorded without happening or happening without being recorded.

**Alternatives considered**: a `previous_image_path` column on the picture row, rejected because it holds one step
and loses the actor and the time of each earlier change; a row in `config_settings_history`, rejected because that
table logs settings values and a picture is not a setting.

## D3. The layout of the picture share

**Decision**: take the owner's dictated layout literally. Under the existing configurable root:

```text
<configured root>/
  Waitlist/
    request_item/
    request_category/
    work_center/
  Visual/
    MMC/
    MMF/
    FGT/
    Categorized Parts/
  WIP/
    MMC/
    MMF/
    FGT/
    Categorized Parts/
```

A stored value stays relative to the root, so a request item's picture is `Waitlist/request_item/pickup-coil.png`
and a Visual part's is `Visual/MMC/MMC0001000.png`.

**Rationale**: three things decide this and all three are already settled somewhere. The root is a setting and a
stored value is relative to it (FR-009, FR-010), which is what `AppStoragePaths.ResolvePicturePath` already
implements and what the storage service already writes. The collection folders are the three the specification names
in FR-004, and `Waitlist`, `Visual` and `WIP` are the owner's own words. The three kind folders inside `Waitlist` are
the application's existing scope names — `request_item`, `request_category`, `work_center` — kept verbatim, so the
move is the insertion of one folder level rather than a rename of three, and so FR-038's case-only collision between
the item `other` and the category `Other` is settled by folder rather than by touching either name. The archive
sub-folder each scope already has moves with its scope and keeps working, because `AppStoragePaths.ArchiveFolderName`
is relative to the picture it replaced.

**Alternatives considered**: category-named family folders (`Coil`, `Flatstock`, `Die`, `Component`), rejected
because the owner dictated the prefix-named layout verbatim and because `Component` would then be both a category and
the catch-all; one flat folder per collection, rejected because FR-005 requires a family folder and FR-038 requires
the kinds apart.

## D4. What decides the family folder

**Decision**: the part number's prefix, through one mapping owned by a new shared type, `PartPictureLayout`:
`MMC` to `MMC`, `MMF` to `MMF`, `FGT` to `FGT`, and anything else to `Categorized Parts`. The existing
`ActiveJobItemResolverService.CanonicalCategory` delegates to it rather than keeping its own copy.

**Rationale**: the mapping already exists in this repository and is already authoritative by prefix — `MMC` is a
coil, `MMF` is flatstock, `FGT` is a die, and a part whose stored category disagrees is still placed by its number,
which is FR-012's rule and is already how the Setup review groups parts. The one thing that must not happen is a
second copy of the mapping that can drift from the first, so the rule moves to the shared project that Setup, the
waitlist libraries and the test project all reference.

**Alternatives considered**: reading the part's stored category, rejected because FR-012 says the number decides and
because a mis-tagged part — the sample `MMF0001154` tagged `Component` — would look in a folder its picture is not
in.

## D5. The job's own per-part picture value (OQ-5)

**Decision**: leave it alone and keep reading it from nowhere. The part picture is the only answer a surface reads.

**Rationale**: the value is always empty and is copied forward on every save, so it carries no information a reader
could use, and giving it a meaning would create a second answer for one part, which is how a card and a page end up
disagreeing. The specification's own assumption records the same conclusion.

**Alternatives considered**: adopting it as a per-job override, rejected because the specification asks for one
picture per part per system and a job is not a system.

## D6. Which stored picture root is the truth (OQ-3)

**Decision**: the store's value wins, as it does today, and that is what the storage screen shows and lets an
entitled account correct. Where the store's value and this machine's `appsettings.json` disagree, the screen says so
in one line and offers the store's value for editing rather than silently preferring either.

**Rationale**: the cascade already implemented in `ImageStorageConfigurationResolver` is store first, then
`appsettings.json`, then the compiled default, and a screen that resolved differently from every reader would be the
one thing worse than the disagreement it fixes. The specification records the disagreement as the thing this screen
exists to settle, and settling it means showing it.

**Alternatives considered**: making `appsettings.json` win, rejected because it would let a machine read a share
every other machine does not; hiding the disagreement, rejected because the screen's whole purpose is to make the
site's layout inspectable.

## D7. How long a part number may be (OQ-4)

**Decision**: the limit is the store's own, 190 characters, which is what `config_images_locations.scope_item_id`
holds. A longer number is refused when its picture is set, with the limit stated, and is never stored truncated.

**Rationale**: FR-008 asks for the limit to be stated and for a truncation to be impossible, and the only limit this
application can state without inventing one is the width of the column that would have to hold the number. Measuring
the longest number in real use needs a live read of Infor Visual, which this planning step does not have; until that
is done the column width is the honest ceiling, and a refusal at 190 is a refusal at a number no real part has
reached.

**Alternatives considered**: the legacy 260-character path limit, rejected as far larger than the store can hold, so
the store would fail first with a worse message; no limit, rejected because the specification asks for one.

## D8. Copying a part's picture to the machine

**Decision**: the two part collections leave the startup mirror and are copied by a new on-demand store. The first
time a part is drawn, its picture is copied into this machine's cache; a later draw of an unchanged picture copies
nothing; a source that cannot be reached is recorded and leaves the existing copy in place; an empty source removes
nothing.

**Rationale**: FR-030 asks for exactly this, and the existing mechanism cannot serve it: `ImageCacheSyncService`
mirrors two whole trees at startup, which is right for the item, work-centre, category and dunnage pictures, whose
count is in the hundreds, and wrong for part pictures, whose count is the number of parts the site owns, copied at
every start onto every machine. The copy rule is the same rule the mirrored folders already use — copy when the copy
is missing or its size or write time differs from the source's — so there is one caching behaviour with two triggers
rather than two behaviours.

**Alternatives considered**: mirroring everything at startup and leaving FR-030 unsatisfied, rejected because the
requirement exists and the cost is real; a per-start partial mirror of only the parts a cache does not hold,
rejected because that still walks the whole part tree at every start.

## D9. The period a replaced picture is kept

**Decision**: one setting, `image_storage.archive_keep_days`, which already exists as a store key with an
`appsettings.json` default. The shipped default becomes ninety days, the storage screen owns it, and a cleanup step
that runs with the startup cache step removes archived pictures older than the setting.

**Rationale**: FR-037 asks for a setting defaulting to ninety days, and FR-011 asks for the replaced picture to be
removed once that period has passed. The key already exists and is already read through the existing resolver, so the
work is the default, the screen and the cleanup, not a new setting. One period for the one archive is right because
the archive folder is one folder per scope, and two periods over one folder is how a cleanup deletes something a
reader still expects to find.

**Alternatives considered**: a second setting for part pictures only, rejected as two answers over one folder; a
constant, rejected because FR-037 says a setting.

## D10. Which entitlement opens the storage folders

**Decision**: `permission.settings.storage_paths`, which is the IT Department and Developer entitlement FR-017
names. The picture-cache panel's gate moves from `permission.settings.part_pictures` to that same entitlement in this
change.

**Rationale**: FR-025 widens the picture entitlement to Setup Lead and Plant Manager, and FR-017 keeps the folders on
IT Department and Developer. The picture-cache panel chooses the folder this machine copies pictures from, which is a
storage control, and it is gated on the picture entitlement today only because the two happened to have the same
members. After the widening they do not, and a Setup Lead holding the picture entitlement would be able to point this
machine's picture cache at any folder. The two keys exist and mean different things; this change makes the gates
match what the keys say.

**Alternatives considered**: leaving the cache panel where it is and accepting the widening, rejected because it
hands a role a control FR-017 reserves; gating the new screen on both keys, rejected because it would take the
picture screen away from the two roles FR-025 just granted it.

## D11. Which parts the missing-picture list covers

**Decision**: the parts this application can name: the WIP parts the floor inventory holds, read through the
existing `WipFloorInventoryService`, and the Visual parts the existing Visual read shapes return, read through the
existing path and its cached fallback. Stored picture rows are read by one new procedure per scope and are subtracted
from those two sets. The screen also accepts any part number typed by hand, so a part that is in neither current read
can still be pictured.

**Rationale**: FR-026 asks for the parts that have no picture and FR-001 defines the universe as the parts the
application can name, which is these two sets and not a full external catalogue. Reading a complete Infor Visual part
catalogue would mean a read shape this application does not have, over an external system it only ever reads through
the shapes it has; adding one is a change to the Visual boundary that this feature has no requirement to make. The
hand-typed path is what makes the gap safe: nothing is unpicturable, only unlisted.

**Alternatives considered**: a cross-database join between `mtm_waitlist` and `mtm_wip_application_winforms`,
rejected because it crosses the store boundary the schema rules keep and because it would still not reach Visual; a
new full part-catalogue read, rejected as an unrequired change to an external boundary.

## D12. How the missing-picture list is exported

**Decision**: through `Microsoft.Windows.Storage.Pickers.FileSavePicker`, constructed with the app window's
identity, writing a comma-separated file of part number, system, family folder and the path the picture would be
stored at.

**Rationale**: FR-026 asks for the list to be exportable and this is the Windows App SDK's own save picker for a
desktop app. It was verified against Microsoft Learn rather than recalled, and the source is named:
<https://learn.microsoft.com/windows/apps/develop/files/pickers-save-file#save-a-document-with-filesavepicker>,
which shows `new FileSavePicker(this.AppWindow.Id)` followed by `PickSaveFileAsync()` and a `System.IO` write of the
chosen path. This project references Windows App SDK 2.3.1, so the type is available. The Learn code-sample search
supplied that source, because the Learn docs search tool reported itself disabled in this session.

**Call shape confirmed 2026-09-23 (implement T002).** The same page was re-read from Microsoft Learn and the
prerequisite it states is the one thing that could have moved the decision: `Microsoft.Windows.Storage.Pickers`
requires **Windows App SDK 1.8 or later**, and it names the legacy route for anything older. This project references
2.3.1, which is above that floor, so the constructor takes the window identity and no
`WinRT.Interop.InitializeWithWindow` call is needed. The verified shape the export task uses verbatim:

```csharp
using Microsoft.Windows.Storage.Pickers;
var savePicker = new FileSavePicker(this.AppWindow.Id)
{
    SuggestedFileName = "missing-part-pictures",
    FileTypeChoices = { { "CSV", new List<string> { ".csv" } } },
    DefaultFileExtension = ".csv",
};
var result = await savePicker.PickSaveFileAsync();
if (result is not null) { System.IO.File.WriteAllText(result.Path, csv); }
```

`PickSaveFileAsync` answers a `PickFileResult` whose `Path` is written with `System.IO`, and a cancelled picker
answers `null` rather than throwing. (Source: Microsoft Learn, fetched with `fetch_webpage` because the Learn docs
MCP tool reported itself disabled in this session.)

**Alternatives considered**: the older `Windows.Storage.Pickers.FileSavePicker`, which needs a separate
`WinRT.Interop.InitializeWithWindow.Initialize` call with the window handle — the pattern
`Module_Settings/Views/ImageOverrideEditorControl.xaml.cs` already uses for its open picker — rejected because the
newer type takes the window identity in its constructor and is the one the platform now documents; writing the file
without a picker, rejected because choosing where an export lands is the person's to decide.

## D13. Moving what is already stored

**Decision**: two things, and neither is optional. The reader resolves **both** layouts: a stored value that names
the old place is read from the old place, so no picture is unresolvable while the move is incomplete. And the move
itself ships as a run-once tool plus a paired SQL artifact that rewrites the recorded paths, each with its reverse.

**Rationale**: FR-036 says no picture may be left unresolvable and none may need to be set again by hand. A reader
that understands one layout cannot make that promise, because the files and the rows move at different moments and
the sequence can be interrupted between them. A reader that understands both makes the promise unconditionally, and
the move becomes a reviewed step rather than a startup side effect, which the schema rules require: schema and data
changes are hand-maintained, reviewed and promoted, and are never applied on application start.

**Alternatives considered**: moving at first start, rejected because it is exactly the startup mutation the rules
forbid; a one-way rewrite with a maintenance window, rejected because an interruption between the file move and the
row rewrite leaves pictures unresolvable, which is the failure FR-036 names.

## D14. Refusing a picture whose file name belongs to another part

**Decision**: comparison is case-insensitive and scoped to one system and one family folder. Before a picture is
stored, the target name is compared against the names already recorded for the same scope and folder; a match against
a different part number refuses the save and names the part that holds the name. The same check refuses two part
numbers that differ only by case.

**Rationale**: FR-007 asks for the refusal and for it to name the part it collides with, FR-008's edge case asks
for the case-only collision to be refused rather than silently overwritten, and a file system that is not
case-sensitive is the reason. Scoping to one system and one folder is right because FR-002 and FR-038 put the two
systems and the three kinds in different folders precisely so they cannot collide.

**Alternatives considered**: letting the later save overwrite, rejected because it silently destroys a picture and
makes two part numbers one file; disambiguating by appending a suffix, rejected because the file name is how a person
finds a picture by hand and the bulk file-drop path in FR-028 depends on the name being the name they know.

## D15. The card every converted list uses

**Decision**: one shared card template for the lists FR-021 converts, using `MinWidth` and `MinHeight` with
`MaxWidth` and `MaxHeight` bounds rather than fixed dimensions, and drawing a picture through the existing
`ResolvedImagePathToSourceConverter` so an entry with no picture shows the one shared placeholder and stays
selectable.

**Rationale**: FR-021 converts four lists and FR-023 requires every one of them to draw the placeholder rather than
an empty tile, so one template with one picture rule is what keeps four lists from answering the same question four
ways — which is the defect `ImagePicturePolicy` was created to end. Layout-critical containers must not be pinned to
hardcoded pixel boundaries, so the template takes the existing component card's shape with bounds instead of a fixed
size.

**Alternatives considered**: copying the component card's markup into each converted list, rejected because four
copies of one card drift; a fixed 150 by 170 tile, rejected because it is the fixed-size pattern the constitution
rules out for a layout-critical container.
