# Publishing the application to the MTM Loader store

`scripts/store/publish-app-payload.ps1` publishes the application in the shape the MTM Loader
(`C:\Users\jkoll\source\repos\MTM_Loader_WinUI3`) expects, writes the application's `build-manifest.json`, and
verifies the result before anything is copied to the share.

The loader is not built from this repository, but it is what delivers this application to a floor machine, so the
publish shape is a contract between the two. Two of its documents define that contract:
[`01-Architecture/02-data-formats.md`](file:///C:/Users/jkoll/source/repos/MTM_Loader_WinUI3/docs/spec/01-Architecture/02-data-formats.md)
for the file shapes and
[`03-Platform/02-deployment-and-release.md`](file:///C:/Users/jkoll/source/repos/MTM_Loader_WinUI3/docs/spec/03-Platform/02-deployment-and-release.md)
for the publish and ordering rules.

## What the loader requires of this application

| Requirement | Where it is met |
|---|---|
| One folder per application under the store: `MTM Application Store\MTM Waitlist Application\` | The folder the script copies to, named by `-Destination` |
| Executable named `MTM_Waitlist.exe` in that folder, matching the catalogue's `executable` | The app host name from `MTM_Waitlist.csproj`; the script refuses a payload without it |
| `build-manifest.json` beside the payload, schema version 1 | Written by the script, last, from the files actually published |
| Unpackaged, self-contained .NET and Windows App SDK, no single-file, no trimming | `Properties/PublishProfiles/win-x64-selfcontained.pubxml` |
| The manifest lists every file with `path` (forward slashes), `size` and `sha256` | The script hashes the published folder |
| Nothing is advertised until the payload is complete | The manifest is written after every other file, and again last on the copy |
| A version is never reused with a different payload | The script refuses that case at the destination |
| The loader compares versions from the manifest; it never trusts a timestamp or a folder size | The manifest's `version` is the only place the published version is stated |

The catalogue entry the loader reads for this application is:

```json
{
  "id": "waitlist",
  "name": "MTM Waitlist Application",
  "caption": "Waitlist",
  "sourceFolder": "MTM Waitlist Application",
  "executable": "MTM_Waitlist.exe",
  "updateable": true
}
```

`-ApplicationId waitlist` and `-Executable MTM_Waitlist.exe` are the script's defaults; change them only if that
catalogue entry changes.

## What the script does

1. Reads the version from `Directory.Build.props`, the single place a person edits it.
2. Clears the payload folder and publishes through the loader-shaped profile. Clearing matters: `dotnet publish`
   adds to a folder and never removes what an earlier publish left, so a stale DLL would otherwise be hashed into
   the manifest and copied to every workstation.
3. Refuses an empty payload, or one without `MTM_Waitlist.exe`.
4. Hashes every file and writes `build-manifest.json` last, atomically.
5. Re-reads the manifest and verifies the folder against it, file by file: same set of paths, same sizes, same
   hashes.
6. With `-Destination`, copies the verified payload to the store, writes the manifest there last, and verifies the
   copy the same way.

It writes nothing outside the repository without `-Destination`, and it never touches a database.

## Usage

```powershell
# publish, write the manifest, verify. Changes nothing outside the repository.
./scripts/store/publish-app-payload.ps1

# then copy the verified payload to the store, as the deployment account
./scripts/store/publish-app-payload.ps1 `
  -Destination '\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Application Store\MTM Waitlist Application'
```

The script exits `0` on success and `2` when it refuses. A refusal names the reason, and nothing has been copied.

## What it refuses

| Refusal | Why |
|---|---|
| `-Version` disagrees with `Directory.Build.props` | Two places would declare the version, and one of them would be wrong |
| `-PayloadRoot` outside `bin\` | The folder is deleted before publishing, so a mistake must not be able to point at a real one |
| Empty payload, or no `MTM_Waitlist.exe` | The loader would mark the application unusable, or launch the wrong file |
| `-Destination` inside this repository | The store is a share, not a build folder |
| Store already holds this version with a different payload | Two workstations would hold different builds under one number |
| Store already holds a newer version | Publishing would offer every workstation an older build |

## After publishing

1. Bump `Directory.Build.props` and the two manifests (`app.manifest`, `Package.appxmanifest`) in the same change, so
   the published payload and the version the assemblies report agree.
2. Publish, then verify the payload hash the script prints in the release log.
3. The loader offers the update the next time someone starts it: it reads the manifest version, stages a copy,
   verifies every hash, and switches by one pointer change. A person running the old version keeps it until that
   switch, and the previous copy is kept as the rollback target.
