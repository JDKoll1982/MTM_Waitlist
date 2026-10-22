# Image naming requirements

Every folder the application reads pictures from, what belongs in it, and exactly what each file must be called.
Written for the person placing artwork by hand on the share, because a file the application cannot find is a
picture nobody sees — and it fails silently, into the shared placeholder.

This document is the naming **contract**. The words in it are compared character-for-character by the code, so a
renamed folder or a re-cased file name stops working without any error being raised.

- Layout contract in code: `MTM_Waitlist.Shared/Helpers/PartPictureLayout.cs`
- Storage paths and defaults: `MTM_Waitlist.Shared/Helpers/AppStoragePaths.cs`
- Picture acceptance rule: `MTM_Waitlist.Shared/Helpers/ImagePicturePolicy.cs`,
  `MTM_Waitlist.Settings/Models/ImageValidation.cs`
- Folder settings live on **Settings → Pictures → Where the picture files are kept** in the application.

---

## 1. The two roots

| Root | Setting | Default |
| --- | --- | --- |
| Picture folder | `image_storage.shared_folder_path` | `\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images` |
| Dunnage root | `dunnage.root_path` | `\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\Shared\Images\Dunnage` |

The dunnage root is separate from everything below and is described in §4. **Everything in §2 and §3 sits under
the picture folder.**

The pictures are looked for in the picture folder through the setting, not through a mapped drive, so a machine
with no `X:` mapping still finds them.

---

## 2. Part pictures — the `Visual` and `WIP` collections

Two collections hold part pictures, one per source system. A part picture is keyed by **the system and the part
number together**, so the same part number in both systems is two files in two folders:

```
<picture folder>\Visual\<family>\<PARTNUMBER>.<ext>
<picture folder>\WIP\<family>\<PARTNUMBER>.<ext>
```

### The family folder

The family comes from the **part number's prefix**, in this order. The prefix is authoritative: a part whose
recorded category disagrees with its number is still filed by its number.

| Prefix | Family | Folder | Example |
| --- | --- | --- | --- |
| `MMC` | Coil | `MMC` | `Visual\MMC\MMC0001000.png` |
| `MMF` | Flatstock | `MMF` | `Visual\MMF\MMF500.png` |
| `FGT` | Die | `FGT` | `Visual\FGT\FGT00023.png` |
| *(none of the above)* | — | `Categorized Parts` | `Visual\Categorized Parts\ABC-1234.png` |

`Categorized Parts` is the catch-all. A part with no known prefix goes there rather than becoming unpicturable,
so **never invent a new family folder** — an unrecognised folder is simply never read.

Prefix matching is case-insensitive, so `mmc0001000` and `MMC0001000` are the same family and the same file.

### The file name

- The **part number itself**, verbatim, then an accepted extension.
- Nothing is added: no suffixes such as `-front`, `-labeled` or `-1`, and no spaces inserted into the number.
- Characters a file name cannot hold are replaced with `_`. This is the only substitution the application makes,
  and it happens on both the write and the read so the two always agree.
- Part numbers are at most **190 characters**. A longer one is refused when its picture is set rather than stored
  truncated.

### Accepted extensions

`.png`, `.jpg`, `.jpeg` — in that order when more than one exists for a part, so a machine holding two of them
draws the same one every time. `.webp`, `.bmp` and `.gif` are **not** accepted as part pictures.

---

## 3. The application's own pictures — the `Waitlist` collection

`Waitlist` holds everything the application pictures for its own screens. Inside it, the folder is the
**scope name**, spelled exactly:

```
<picture folder>\Waitlist\request_item\<ITEMCODE>.png
<picture folder>\Waitlist\request_category\<CATEGORY>.png
<picture folder>\Waitlist\work_center\<WORKCENTERCATALOGID>.png
```

| Folder | Keyed by | Example |
| --- | --- | --- |
| `request_item` | Request Item code | `Waitlist\request_item\pickup-coil.png` |
| `request_category` | Category code | `Waitlist\request_category\Pickup.png` |
| `work_center` | Work centre's catalog row id | `Waitlist\work_center\12.png` |

These three names are lower-case here and are compared case-insensitively, but keep the spelling above: the same
folder name in two cases is two folders on the share.

### 3.1 Hopper reference pictures — `Waitlist\hopper`

The hopper size pictures are the one group keyed by **something other than a part number**: they picture the size
a person chose, so they are named after the size word.

```
<picture root>\Waitlist\hopper\Small.png
<picture root>\Waitlist\hopper\Medium.png
<picture root>\Waitlist\hopper\Large.png
```

On this site the picture root is `\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist
Application\Images` (reached as `X:\MH_RESOURCE\...\Images` on a machine that maps `X:`), so the three files sit at
`X:\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images\Waitlist\hopper`.

Rules specific to this folder:

- **Exactly three files, exactly these names.** `Small`, `Medium`, `Large`, each capitalised as written, with the
  `.png` extension. These three words are the size answers the New Request wizard records, and the same words are
  what the card resolves, so a file named `MEDIUM.png`, `Medium Hopper.png` or `Med.png` is never found.
- **The extension is `.png`.** The part-picture rule above accepts three extensions; this folder expects `.png`.
- The folder is **configurable** on Settings → Pictures → *Where the picture files are kept*, under *Hopper picture
  folder*, and the box has a **Browse** button that opens a folder dialog at whatever the box currently holds. Its
  shipped default is the share's own `...\Images\Waitlist\hopper` path, written in full rather than relative to the
  picture folder, so a machine that has never opened the panel still draws the size pictures. A relative path is
  still read beneath the picture folder, and an absolute path is used as given. Do not leave it blank — the panel
  refuses an empty value.
- If a size's picture is missing or unusable, the card and the wizard draw the shared placeholder rather than a
  blank. Nothing is substituted, and nothing is logged as a fault.
- These pictures are held to the picture rules in §5, like every other picture.

---

## 4. Dunnage pictures — the dunnage root

Dunnage artwork lives under its **own root**, not the picture folder:

```
<dunnage root>\Types\<TYPE NAME>.png
<dunnage root>\Parts\<PART NAME>.png
```

Paths are recorded **relative** to that root, as `Types/DunnageType-Boxes.png` or `Parts/Steel Rack.png`, and the
file name is the type's or part's own display name. Spaces are allowed here (unlike part numbers, which have
none). The root can be overridden for a machine by the `MTM_DUNNAGE_IMAGE_ROOT` environment variable.

---

## 5. The rules every picture must pass

A file that fails any of these is treated as **no picture at all** — the application draws
`Assets/Placeholders/default-no-image.png` and reports nothing. This is deliberate: a bad picture must not become
a broken frame or a substituted value.

| Rule | Value |
| --- | --- |
| Shape | Square, within ±2% (`TargetAspectRatio 1.0`, tolerance `0.02`) |
| Minimum side | 48 pixels |
| Maximum side | 2048 pixels |
| Accepted types | `.png`, `.jpg`, `.jpeg` (part and application pictures) |
| Minimum file size | 1 KB (1024 bytes) |
| Maximum file size | 10 MB (10,485,760 bytes) |

The file's own header is read to check the format and the pixel dimensions — the picture is not decoded — so a
file with a correct name and a corrupt body is refused rather than half-drawn.

---

## 6. The one that is not a picture: key files

The key folder holds `<keyName>.txt` — a text file named after its key, with the key's contents inside. This is
the only non-picture file the folders are expected to contain; do not place other loose files in the picture
folders, because a walk of the share treats the part collections as belonging to this machine's own copy store.

---

## 7. Checklist before you leave a file on the share

1. Is it under the right root — the picture folder for part/hopper/application pictures, the dunnage root for
   dunnage?
2. Is it in the right folder, spelled exactly as the tables above spell it?
3. Is the file name the part number, the Item/Category code, the size word, or the dunnage name — with **no**
   suffix added?
4. Is the extension one of the accepted ones (and `.png` for hopper sizes)?
5. Is it square, between 48 and 2048 pixels a side, and between 1 KB and 10 MB?
6. Did you keep the case? `Large.png` and `large.png` are the same size to the code, but two different files to
   the share — pick the documented spelling and stay with it.
