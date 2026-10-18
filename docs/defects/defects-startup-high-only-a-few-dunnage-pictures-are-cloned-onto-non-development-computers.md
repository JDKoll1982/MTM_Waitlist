# High — Startup — Only a few dunnage pictures are cloned onto non-development computers

| Field | Value |
| --- | --- |
| **Criticality** | High — dunnage pictures are missing on shop-floor machines, and the one line the person is given names the wrong thing |
| **Feature area** | Startup / local picture cache — the folder the dunnage mirror writes into |
| **Type** | A destination chosen by probing another application's install folder, with no check that this account may write in it |
| **Found** | 2026-10-09, from the report "on non development computers i am getting an error when trying to clone the image folders locally … only a few dunnage images actually store" |
| **Status** | **FIXED.** The destination must now be a folder this account can write in, an unusable folder is reported as itself rather than as an unreadable share, one picture that will not copy no longer abandons the rest, and the refusal is recorded to the store from every computer. **Proof:** `MTM_Waitlist.Tests/Module_Shared/Services/ImageCacheSyncServiceTests`, `MTM_Waitlist.Tests/Module_Shared/Helpers/ImageCachePathsTests`, `MTM_Waitlist.Tests/Module_Startup/Services/PictureCacheStepTests`. **Remainder owned by: none.** |
| **Files** | `MTM_Waitlist.Shared/Helpers/ImageCachePaths.cs`, `MTM_Waitlist.Shared/Services/ImageCacheSyncService.cs`, `MTM_Waitlist.Shared/Services/IImageCacheSyncService.cs`, `MTM_Waitlist.Startup/Services/PictureCacheStep.cs`, `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` |
| **Also recorded as** | A related, unfixed finding — the fixed-name probe in `ImageStorageService.IsShareAccessibleAsync` — is in §5 |

---

## 1. Summary

The dunnage pictures are read from the shared root and mirrored onto each computer at startup. The folder the
mirror writes **into** was not the configured cache folder and was not this application's own; it was whichever of
three paths happened to exist:

```csharp
public static string ResolveDunnageCacheFolder() =>
    ReceivingApplicationDunnageCaches.FirstOrDefault(Directory.Exists)   // ← existence only
        ?? Path.Combine(LocalCacheRoot, DunnageFolderName);
```

The three candidates are the MTM Receiving Application's own cache folders — beside this executable, under
`%LOCALAPPDATA%`, and **under `%Program Files%`**. The last of those is the normal place for that application on a
production PC, and it is where a standard account may **read** and may **not write**: `Directory.Exists` answers
`true`, so the mirror chooses it, and then every `File.Copy` fails with `UnauthorizedAccessException`.

Two further faults hid it, which is why it presented as "a few pictures store and there is an error":

1. `Synchronize` catches every exception from a source and records the source in `SourcesSkipped`, which the splash
   screen renders as **"The picture share could not be read"**. The share was fine. The one clue the person reading
   the screen gets points at the share.
2. One unwritable file ended the whole walk for that source, so a folder being read-only produced no copies at all
   rather than almost all of them — and nothing anywhere said how many had failed.

## 2. Impact

- On any computer where the receiving application is installed machine-wide, the dunnage folder keeps only the
  pictures an elevated installer left there, and never gains another one. That is the reported "only a few dunnage
  images actually store".
- The failure is reported against the share, so time is spent on a share that is healthy.
- The configured cache folder — the one a Developer or IT Department sets in the Settings panel — had **no effect
  at all** on dunnage pictures wherever a receiving-application folder existed, so the panel promised something it
  did not do.
- The failure was never recorded: it reached the in-memory launch feed only, and that feed dies with the window.
  A shop-floor machine could therefore fail for months with nothing in `ops_startup_logs` to find.

## 3. Evidence

| Item | Evidence |
| --- | --- |
| The destination ignored writability | `ImageCachePaths.ResolveDunnageCacheFolder()` used `FirstOrDefault(Directory.Exists)`. |
| The third candidate is under `Program Files` | `ImageCachePaths.ReceivingApplicationDunnageCaches`, third entry is `%ProgramFiles%\MTM_Receiving_Application\Assets\DunnageImages`. |
| The share was not the fault, but was named as one | `ImageCacheSyncService.Synchronize`'s `catch (Exception) { skipped.Add(source.Label); }`, rendered by `PictureCacheStep` as "The picture share could not be read…". |
| A working copy on a development machine | Measured 2026-10-09 with `tools/probe-dunnage-folders.ps1`: the shared root holds **168** pictures (`Parts` 133, `Types` 16, `Temp` 6, 13 loose at the root); this machine's own cache holds **162** — every picture except the share's `Temp` folder, which is skipped by design. Both receiving-application candidates are **absent** on this machine, so the destination here is this application's own folder, which is writable. The clone is complete here and cannot be complete where the candidates exist. |
| The failing folder cannot be seen from the store | `ops_startup_logs` held **0** rows when checked on 2026-10-09, so the evidence of the failure had already been purged; the defect was proved from the code and by the regression test in §4 instead. |
| The settings promise was false | `SettingsViewModel.SavePictureCacheFolderAsync` stores `image_cache.folder_path` and says "Pictures will be cached in {folder}", while `ResolveDunnageCacheFolder` never consulted it whenever a candidate existed. |

## 4. The fix

1. **The destination must be writable.** `ImageCachePaths.IsFolderWritable` asks by creating and deleting a uniquely
   named probe file — the only answer that is true for the account the application is running as — and
   `ResolveDunnageCacheFolder` now requires it before sharing the receiving application's folder, falling back to
   `{configured cache root}\dunnage` when no candidate qualifies. The answer is settled once per process, because
   this is read on the path that resolves a picture for a card.
2. **The two faults are kept apart.** `ImageCacheSyncResult` gained `DestinationsUnavailable`, and the mirror
   prepares and verifies the destination **before** it walks the source. A folder that cannot be written to is now
   reported as the folder, and the line no longer sends anyone to the share.
3. **One bad picture no longer abandons the rest.** `File.Copy` failures are counted into `NotCopied` and the walk
   continues, so a locked file costs one picture rather than the whole run.
4. **The refusal is recorded, not only shown.** `PictureCacheStep` records the destination failure through the log
   seam (`AppLog.Error`), so it reaches `ops_startup_logs` from every computer instead of dying with the feed.

**The regression test failed first.** `Service_ImageCacheSyncServiceTests_Service_ImageCacheSyncServiceTests_AnUnwritableDestinationIsNotReportedAsAnUnreachableShare` was run against the
unfixed mirror and reported:

```text
Assert.AreEqual failed. Expected:<0>. Actual:<1>. the share was readable, so it is not the share that could not be read
```

After the fix the whole suite reports `Total tests: 1929, Passed: 1887, Skipped: 42, Failed: 0`.

## 5. What is still open

- **Not confirmed on the affected PC.** Which of the two receiving-application candidates exists there has not been
  inspected; the fix is correct under either, and under neither, because it no longer depends on the answer. The
  next launch on such a machine will now say which folder it could not write to, in the log store.
- **A related probe has the same class of fault and is untouched.**
  `MTM_Waitlist.Settings/Services/ImageStorageService.IsShareAccessibleAsync` writes a **fixed** name,
  `_access_check_.tmp`. Two machines saving a picture at the same instant can have one delete the other's probe
  file, and the second then reports a healthy share as inaccessible. It is a different code path (saving pictures,
  not cloning them) and was left alone deliberately rather than widened into this fix. The new probe uses a unique
  name and is the pattern to copy.
