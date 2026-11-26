# Audit scopes

What to inspect, per phase. These are investigation categories, not a mandate to replace everything
found inside them.

## 1. Discovery checklist

### Instructions and AI context

`AGENTS.md` and nested copies, `CLAUDE.md`, `GEMINI.md`, `.github/copilot-instructions.md`,
`.github/instructions/**`, `.github/prompts/**`, `.github/agents/**`, `.github/skills/**`,
`.github/hooks/*.json`, `.vscode/mcp.json`, `.mcp.json`, `.specify/**`, `README.md`,
`CONTRIBUTING.md`, and any `.serena/` memory directory.

Record for each: scope, whether it is active, purpose, approximate size, duplication or conflict,
freshness, and the recommended action. A table is required; prose is not a substitute.

### Workspace inventory

Solutions (`.sln`, `.slnx`), projects, application types, languages, target frameworks, SDK and runtime
versions from `global.json` / `Directory.Build.props`, shared libraries, external services, database
engines, build systems, test frameworks, deployment assets.

Build and dependency manifests: `*.csproj`, `*.vbproj`, `*.fsproj`, `Directory.Build.props`,
`Directory.Build.targets`, `Directory.Packages.props`, `packages.config`, `global.json`, `NuGet.config`,
`packages.lock.json`, `package.json` and its lockfiles, `requirements.txt`, `pyproject.toml`,
`Cargo.toml`, `go.mod`, `pom.xml`, `build.gradle`, Dockerfiles, CI configuration.

Exclude generated trees, caches, build output, `node_modules`, `bin`, `obj`, and `venv`, except to read
their manifests and lockfiles. For leftovers that pollute a build, use
`git ls-files --others --ignored --exclude-standard`.

### Relationships

Project references, shared libraries, shared dependencies, central version management, duplicated
packages, version disagreements, shared configuration, cross-project interfaces, and dependencies that
reach more than one application. Produce the relationship map before judging any version.

## 2. Area A — dependency health

Inventory direct, transitive, development, build and test dependencies, analyzers, SDKs, framework
integrations, internal packages and deployment dependencies. Distinguish **declared** from **resolved**.
Flag unused, obsolete, redundant and unsupported packages.

For each package that matters, record: declared version, resolved version, latest stable, latest
compatible, recommended target, update type, deprecation and end-of-support status, known
vulnerabilities, breaking changes, migration requirements, notable features and performance changes,
and compatibility needs.

Source ranking: official registry and vendor documentation, then the official source repository,
specification and issue tracker, then maintainer examples, then community posts as supporting evidence
only. Record the URL and the date checked.

Never recommend a pre-release as a production default. Prefer the lowest version that removes a
vulnerability, to minimise churn.

## 3. Area B — code modernization

For each area, look for custom code that a framework-native API, an already-installed package, or a
first-party SDK does better.

| Area | Look for |
| --- | --- |
| Infrastructure | Logging, exception handling, diagnostics, configuration, DI, background work, scheduling, threading, async, retry, caching, resource lifetime |
| Data | Connection management, command execution, ORM, validation, serialization, mapping, transformation, bulk operations, transactions, pooling |
| UI | Custom controls, grids, search, filter, sort, paging, form validation, notifications, progress, shortcuts, accessibility, error dialogs, printing, reporting, import/export |
| Security | Authentication, authorization, password and token handling, encryption, transport security, input validation, output encoding, certificates, secrets, audit logging, permissions |
| Performance | Repeated computation, inefficient loops, redundant queries, excess allocation, blocking calls, redundant UI updates, repeated serialization or file I/O, network chatter |
| Dev infrastructure | Custom build scripts, manual formatting, manual test running, custom static analysis, duplicate validators, doc and code generation, git and release automation, dependency scripts |

For each candidate, compare: the current implementation, framework-native alternative, third-party
libraries, official SDK or API, already-installed packages, and partial-replacement options. Judge
performance, memory, security, reliability, compatibility, complexity, maintenance, readability,
testability, extensibility, UX, support horizon, licensing and package size.

Prefer the native capability. Do not add a package to replace a few lines of efficient, maintainable
code.

Separate legitimate business rules from duplicated infrastructure. Do not externalize a business rule
merely because it is hardcoded, and never move a secret somewhere less protected.

Classify every finding: `REPLACE`, `MODERNIZE`, `ADD`, `CONSOLIDATE`, `REMOVE`,
`NATIVE_ALTERNATIVE`, `KEEP`, `INVESTIGATE`.

## 4. Area C — AI development environment

### Context and instructions

Copilot instructions, `AGENTS.md` organisation, path-scoped instructions, prompt files, custom agents,
skills, indexing and symbol navigation, documentation retrieval, memory systems, duplicate or
conflicting rules, and context consumption. Goal: better accuracy at lower token cost.

### MCP servers

For each: installed vs configured vs running vs authenticated vs actually tested — report the true
state. Then coverage gaps to unused or missing integrations, redundancy, permission and data-access
needs, configuration complexity, and resource cost.

Vet any proposed server on: official project, maintenance activity, compatibility, security history,
permissions, data access, external code execution, configuration complexity, and real usefulness.
Prefer free and open-source. Be cautious with anything needing unrestricted filesystem, terminal,
network or credential access. Do not propose a server whose job native tooling already does.

### Extensions and tools

Underused features of what is installed; redundancy that creates two paths to one result; missing
tools that measurably help a coding agent (diagnostics, test runner, language server, git tooling,
security scanning, dependency management).

### Agent behaviour

Targets: code understanding, context selection, repo-wide reasoning, multi-project awareness, edit
accuracy, tool selection, doc retrieval, API-version accuracy, test generation, debugging, token
efficiency, latency, cross-agent consistency, autonomous execution.

Find the rules that would stop an agent from re-reading irrelevant files, reimplementing existing
functionality, using deprecated APIs, guessing versions, making inconsistent architectural choices,
duplicating utilities, generating unnecessary code, or missing project conventions.

## 5. Repo-specific areas

### 5.1 Architecture

Layering (XAML views only in the app's `Module_*/Views`, non-view code in the `MTM_Waitlist.*`
libraries, no view types leaking into libraries), DI registration completeness in
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`, services constructed by hand instead of
injected, the project reference graph (cycles, unused references, code in the wrong project), duplicated
abstractions across modules, and module boundaries that no longer earn their keep.

Dead code: unreferenced public types, unused resources, stale XAML, leftover temp projects under
`obj\`/`Temp*.Preview\`. Churn hotspots — the six files over 1,000 lines, found with `git log --since` —
are decomposition candidates, not targets by themselves. Also check naming consistency, wildcard usings,
and god classes.

### 5.2 Tests

Whether the `*AuditTests` guardrails still assert something real or have drifted into vacuous checks;
coverage of risky paths (persistence, the Visual read fallback, startup, navigation); slow or flaky
tests, including any needing a live MySQL host; missing tests for code changed in the audit window; and
adherence to the repo's test conventions.

### 5.3 Database

Stored-procedure coverage of application data operations, with `InlineSqlAuditTests` as evidence;
`create.sql` + `rollback.sql` pairs present for every schema artifact; drift between repo scripts and the
live stores (the read-only drift report, never `-Apply`); duplicate or unused procedures; transaction
scope and error handling; index coverage on hot predicates; unbounded selects and N+1 loops in
application code; and the `mtm_mock` mirror's shape coverage and freshness relative to the SQL source.

Treat every write to a plant-host store as irreversible for the last day — there are no binary logs — so
proposals that mutate a store need an explicit backup step.

### 5.4 Docs

Drift between `docs/` and the code (guides naming deleted scripts, instructions describing retired
tooling), duplicated guidance across `AGENTS.md`, `.github/copilot-instructions.md`,
`.github/instructions/**` and `docs/**`, links to files that no longer exist, and instruction claims that
must be re-verified against the code before they are repeated. Also check whether generated artifacts
under `docs/diagrams` and `docs/ui-designs` are current or marked stale.

### 5.5 Localization

`.resw` hygiene — keys referenced but missing, unused keys, duplicated values, keys used as display
text; hardcoded UI strings bypassing `"Key".GetLocalized()`; `x:Uid` versus explicit binding consistency;
and culture-correct formatting of dates, numbers and quantities in plant data.

## 6. MCP grounding

Claim nothing from memory that a tool can answer. All the NuGet tools return a plan and modify no
files, which suits a read-only audit; each needs absolute `solutionDirectory` and `projectPaths`.

| Tool | Serves |
| --- | --- |
| `NuGet-get_latest_package_version` | Latest stable per package, with the source it came from |
| `NuGet-update_package_version` | Exact-target plan for pinned upgrades, including the surrounding graph |
| `NuGet-upgrade_packages_to_latest` | Latest versions compatible with the target framework |
| `NuGet-fix_vulnerable_packages` | Smallest safe remediation for a known vulnerability |
| `NuGet-review_supply_chain_security` | Authoritative verdict on central package management, source mapping, audit and config validity |
| `NuGet-get_package_context` | Migration guidance from the package's own README or `AGENTS.md` |
| Context7 | Library API facts at the version in use — resolve the library ID first |
| Microsoft Learn | WinUI 3, Windows App SDK, packaging and platform behaviour |
| `csv-mcp-server` | Infor Visual schema exports under `docs/database`, tab-delimited, one folder deep |
| GitHub tools / `gh` | Advisories, release notes, issue trackers, commit history |

## 7. Cross-cutting analysis

### Security

Known CVEs, vulnerable or unsupported packages, unsafe APIs, outdated cryptography, insecure
configuration, supply-chain risk, package ownership and maintenance, license compatibility, and
weaknesses in custom implementations. State both the security gain and the new risk of a proposed
alternative.

### Performance

Startup, execution, CPU, memory, database, network, UI responsiveness, build time, test time. Every
performance claim needs a documented benchmark or a stated way to measure it. Never invent one.

### User experience

Usability, accessibility, workflow efficiency, responsiveness, search and filtering, validation, error
reporting, notifications, keyboard navigation, printing, feedback, data-entry efficiency, and
consistency across screens and applications. Only where the improvement makes practical sense for the
existing application.

### Maintainability

Duplication, shared utilities, project separation, dependency consistency, configuration
organisation, architecture, naming, abstraction quality, testability, documentation. Prefer reductions
in complexity over new abstractions.
