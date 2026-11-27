# Contract: declared configuration

Owners: DEP-008, DEP-009, DEP-004, SEC-001, AI-003, AI-002, DEV-002.
Carried by: W6, W7, W8, W1, W29, W4, W5.

These are files a machine reads, so their element and property names are the interface. Every identifier
below is copied from the spec's Verbatim Constraints and must not be recased or renamed.

## Central package management (W6)

File: `Directory.Packages.props`, at the repository root, new in this change.

- Property `ManagePackageVersionsCentrally` set to `true`.
- One `PackageVersion` item per package id. After W6 the version of a package appears in this file and
  nowhere else.
- A per-project exception is declared as `VersionOverride` on that project's `PackageReference`. An exception
  is visible as an exception; no project silently holds a different version of one package.

Constraint that governs the change: a `PackageReference` must not carry a `Version` attribute once this file
exists. NuGet raises NU1008 for one that does.

Inherited version to relocate, not retire: `System.Security.Cryptography.Pkcs`, currently
`<PackageReference Include="System.Security.Cryptography.Pkcs" Version="9.0.21" />` at
`Directory.Build.props:77`. It becomes a `PackageVersion` in this file in W6, and DEP-005 retires it in W11.

No other version-carrying item may remain in `Directory.Build.props`. That file keeps the application
`Version`, the `NoWarn`, and `UseRidGraph` until W17.

## Package source mapping (W7)

File: `NuGet.Config`, tracked at the repository root since `598c66cd`.

- Keep the existing `packageSources` entry for `nuget.org`.
- Add `packageSourceMapping`, with a `nuget.org` package pattern set covering every package id the solution
  declares.
- No wildcard fallback pattern. A second feed does not exist, and a wildcard would defeat the guard.

The contract a clone may rely on: a package whose id is not mapped to a feed is refused at restore, and the
feed set is the same on every clone.

## Credential surface (W1)

File: `appsettings.json`, tracked.

- Every credential value is an empty string. That covers `WaitlistDatabaseOptions.ConnectionString`,
  `ReceivingDatabaseOptions.ConnectionString`, and `InforVisualDatabaseOptions.Server`, `.User` and
  `.Password`.
- The shape is kept. Every binding, key path and option class that exists today still exists.
- `ConnectionStringEnvironmentVariable` stays and remains the single documented way a host is told which
  variable supplies its value. The two defaults in the tree are
  `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` and `MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING`.
- The plant's Infor Visual values have no equivalent property. Their environment-first resolution stays as it
  is, and their absence is a degradation, not a refusal.

The contract a host may rely on: a host with no value for an internal store does not start silently. It stops
naming the variable that is missing, and it never substitutes an empty string or a placeholder.

## Repository tool servers (W29)

File: `.mcp.json`, tracked at the repository root.

- `microsoft-learn`, transport HTTP, endpoint `https://learn.microsoft.com/api/mcp`.
- `context7`, transport stdio, launched through `npx @upstash/context7-mcp`.
- Both are declared or neither is. A team that cannot reach either endpoint rejects AI-003 rather than
  declaring half of it.

Prerequisites recorded next to the declaration, because a fresh clone has to know them: Node.js on the
machine for `context7`, and outbound HTTPS to `learn.microsoft.com`, to `registry.npmjs.org` and to the
Context7 service. Neither server reads the repository; both expose documentation search only.

## Interpreter declaration (W4)

File: `.github/hooks/speckit.json`.

Both arms of both hooks invoke `python`. The bash arms currently invoke `python3`, which does not exist on
this platform, so they silently do nothing. The identifier is `python`, never `python3`.

## Documentation path that must stay exact (W5)

`MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1`, cited at
`docs/operations/operations-mock-service-install-script.md:79,103-112` and
`docs/operations/operations-database-live-and-mock-updates.md:211`. Both the script and both cited locations
exist; the documented path does not match them.
