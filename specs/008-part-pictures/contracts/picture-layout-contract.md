# Contract: The Picture Layout, the Stored Value and the File Name

The literal layout the owner dictated, and the rules that produce a path from a part. Every identifier below is
copied from the specification exactly. Nothing here renames, re-cases, pluralises or paraphrases one, because these
strings are the contract: a file name is how a person finds a picture by hand, and the move and the bulk file-drop
path both depend on the name being the name they already know.

## The share, under the configured root

```text
<configured picture root>/
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

`<configured picture root>` is the setting `image_storage.shared_folder_path`, whose shipped default is
`AppStoragePaths.ImagesRootDefault`. It is per-machine configuration and is never compiled in (FR-009).

| Identifier | Kind | Meaning |
| --- | --- | --- |
| `Waitlist` | collection folder | everything the application pictures for its own screens |
| `Visual` | collection folder | the Infor Visual parts |
| `WIP` | collection folder | the WIP floor parts |
| `request_item` | kind folder inside `Waitlist` | the request items. The application's existing scope name, kept verbatim. |
| `request_category` | kind folder inside `Waitlist` | the category families. The application's existing scope name, kept verbatim. |
| `work_center` | kind folder inside `Waitlist` | the work centres. The application's existing scope name, kept verbatim. |
| `MMC` | family folder | part numbers that begin `MMC` |
| `MMF` | family folder | part numbers that begin `MMF` |
| `FGT` | family folder | part numbers that begin `FGT` |
| `Categorized Parts` | catch-all family folder | every part number no prefix above recognises |
| `Archive` | sub-folder | the replaced pictures kept beside the picture they replaced. Taken from `AppStoragePaths.ArchiveFolderName`, unchanged. |

The three kind folders exist so that two kinds can never resolve to one file — the item `other` beside the category
`Other` differ only by letter case, and a file name cannot tell them apart, so neither is renamed and each keeps its
own folder (FR-038).

## The stored value

`config_images_locations.image_path` holds a path **relative to the configured root**, collection and family folder
included (FR-004, FR-010). Forward slashes, and the same value resolves on a machine that mapped a drive letter and
on one that did not.

```text
Visual/MMC/MMC0001000.png
WIP/Categorized Parts/12345-6.png
Waitlist/request_item/pickup-coil.png
Waitlist/request_category/Other.png
Waitlist/work_center/100-03.png
```

A value that is rooted, or that names the pre-move place, is still read — that tolerance is what makes the move safe
to do in any order (FR-036, research D13).

## The scope strings

| Store `scope` | Collection | Meaning |
| --- | --- | --- |
| `request_item` | `Waitlist` | one request item, keyed by its Item code |
| `request_category` | `Waitlist` | one category family, keyed by its Category code |
| `work_center` | `Waitlist` | one work centre, keyed by its numeric id |
| `visual_part` | `Visual` | **new.** one Infor Visual part, keyed by its part number |
| `wip_part` | `WIP` | **new.** one WIP floor part, keyed by its part number |

## The file name

The file name is the part number, with the characters a file name cannot hold replaced, and the picture's own
extension.

| Rule | Statement |
| --- | --- |
| Base name | The part number (FR-006). |
| Unsafe characters | Every character in `Path.GetInvalidFileNameChars()` is replaced with `_`. This is the rule the storage service already applies to an item id, unchanged. |
| Length | 190 characters at most, which is what `scope_item_id` holds. Longer is refused with the limit stated and is never stored truncated (FR-008). |
| Extension | The chosen file's own extension, which the existing acceptance list already limits to `.png`, `.jpg` and `.jpeg`. |
| Collision, same name | A name that matches another part **in the same scope and the same family folder** refuses the save and names the part that holds it (FR-007). |
| Collision, case only | Compared case-insensitively, so two part numbers that differ only by letter case are two parts competing for one file, and the second is refused and names the first (FR-007's edge case). |
| Collision, across systems or kinds | Impossible by construction: `Visual` and `WIP` are different collections and the three `Waitlist` kinds are different folders. The same file name in two folders is two pictures and both are read for their own parts only (FR-002, FR-038). |

## The family folder

Decided by the part number's prefix (FR-012), so a part whose stored category disagrees with its number still finds
its picture. Compared case-insensitively, so `mmc0001000` lands in `MMC`.

```text
MMC…  ->  MMC
MMF…  ->  MMF
FGT…  ->  FGT
anything else, including a Component  ->  Categorized Parts
```

## The local copy

| Identifier | Meaning |
| --- | --- |
| `<cache root>` | The configured cache folder, defaulting to `%LOCALAPPDATA%\MTM_Waitlist\ImageCache` — `ImageCachePaths.DefaultCacheRoot`, unchanged. |
| the part collections' cache folders | **New, one per part collection**, so the same file name in `Visual` and in `WIP` cannot be one copy. They sit beside the existing mirrored folders and are named for the collection they hold. |
| `waitlist`, `dunnage` | The two folders `ImageCacheSyncService` already mirrors at startup. The part collections are **not** among them (FR-030). |

The copy rule is the existing one, applied on first draw instead of at startup: copy when the copy is missing, or its
size or last-written time differs from the source's; never copy when it is unchanged; never delete a copy because the
source answered with nothing (FR-030, FR-031, FR-032).

## What is never drawn

One picture, named once: `ImagePicturePolicy.NoImagePath`, with `ImagePicturePolicy.NoImagePackUri` as the packaged
form. No family artwork and no category artwork may stand in for a part that has no picture (FR-014, FR-015, FR-020).
The acceptance rule is the existing one — exists, readable, at least 48 by 48 pixels, square within 0.02 — and is
neither relaxed here nor tightened elsewhere (FR-013, FR-034).
