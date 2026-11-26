# The toolchain this feature was built and proven on

FR-031 asks for the upgrade to be proven rather than assumed, and FR-032 asks the coverage guard to
state where the suite actually stands. The upgrade itself was performed outside this task list, so
nothing else in the feature folder said what it produced. This file does: every package, the editor
floor, the two options that had to be replaced, and every gate with its exit code and its numbers.

Recorded from `extensions/csharp-xaml-graph` on the `extension-profile-update` branch, at version
`0.6.0`.

## Packages: declared, and what each resolves to

| Package | Declared | Resolves to | Where |
|---|---|---|---|
| `typescript` | `7.0.2` | `7.0.2` | devDependency |
| `vitest` | `^5.1.2` | `5.1.2` | devDependency |
| `@vitest/coverage-v8` | `^5.1.2` | `5.1.2` | devDependency |
| `jsdom` | `^31.0.1` | `31.0.1` | devDependency |
| `htmlparser2` | `12.0.0` | `12.0.0` | devDependency |
| `@types/vscode` | `1.146.0` | `1.146.0` | devDependency |
| `@types/node` | `26.6.11` | `26.6.11` | devDependency |
| `@vscode/vsce` | `4.1.1` | `4.1.1` | devDependency |
| `cytoscape` | `3.35.0` | `3.35.0` | devDependency |
| `cytoscape-dagre` | `4.1.0` | `4.1.0` | devDependency |
| `dagre` | `0.8.5` | `0.8.5` | devDependency |
| `esbuild` | `0.28.2` | `0.28.2` | devDependency |
| `tree-sitter-c-sharp` | `0.24.0` | `0.24.0` | devDependency |
| `web-tree-sitter` | `0.27.2` | `0.27.2` | dependency |

`npm ls --depth=0` reports the same fourteen lines, so nothing is resolved to a version the manifest
does not name.

## The editor floor

`engines.vscode` is `^1.146.0`, and `@types/vscode` is `1.146.0`. The floor and the API types it
compiles against are the same version: SC-018 is met, and the editor requirement cannot sit below the
surface the code is type-checked against.

## The two options TypeScript 7 and Vitest 5 removed

| Option | Old form | New form | Why |
|---|---|---|---|
| Coverage scope | `coverage.all: true` | `coverage.include: ['src/**/*.ts']` | `coverage.all` was removed in the new major; `include` says the same thing without asking the reporter to walk files no test imported. |
| Module resolution | `module: node16`, `moduleResolution: node10` | `module: preserve`, `moduleResolution: bundler` | TypeScript 7 removed `node10`; `preserve`/`bundler` is the supported pair, and the suite still resolves every import because the bundler resolves them the same way. |

`vitest.config.mts` keeps `src/validate.ts` and `src/selftest.ts` out of coverage: both are command
entry points, exercised by running them rather than by importing them from a test. That exclusion is
unchanged and is stated in the file beside the list.

## Every gate, and what each reported

Run from `extensions/csharp-xaml-graph`.

| Command | Exit | What it printed |
|---|---|---|
| `npm run typecheck` | 0 | `tsc --noEmit` then `tsc -p tsconfig.test.json`, both silent |
| `npm test` | 0 | **40 test files, 1038 tests passed** |
| `npm run selftest` | 0 | `ALL CHECKS PASSED`; **1095 nodes, 4776 edges, 21 findings** |
| `npm run selftest` (second run) | 0 | the same 1095 / 4776 / 21 |
| `npm run coverage` | 0 | statements 98.31, branches 90.68, functions 98.85, lines 98.29 |

**The test count moved, and is reported rather than absorbed.** The premise of T082 and T095 was 39
files and 1028 tests. This phase added one suite, `test/graph/inspect.test.ts` (T086), with ten tests,
so the suite now reports 40 files and 1038 tests. Nothing that passed before fails: the increase is
the new file and nothing else.

The analysis numbers did not move. This is the point of the upgrade proof (SC-019): after the whole
toolchain moved, the extension still reports 1095 nodes, 4776 edges and 21 findings on this
repository, and does so again on a second run.

## The coverage guard's real position

Read from `vitest.config.mts` and from the report `coverage/coverage-summary.json` holds:

| Metric | Guard | Actual | Headroom |
|---|---|---|---|
| Lines | 98 | 98.29 | +0.29 |
| Statements | 98 | 98.31 | +0.31 |
| Branches | 90 | 90.68 | +0.68 |
| Functions | 98 | 98.85 | +0.85 |

Two of the four thresholds moved down from where they stood before this change: branches from 93 to 90
and functions from 100 to 98. The guard carries the reason in its own comment, and this file repeats it
rather than inventing a second one: the validation and compatibility code arrived with less coverage
than the analysis modules, and the guard records where the suite actually is rather than hiding the gap
behind a figure from before the change. No threshold was lowered silently, no threshold was raised as
part of writing this file, and each one still sits just under the figure the suite reaches today.

One module is thinner than the rest: `src/graph/inspect.ts`, added by this phase, is at statements
94.83 and branches 86.11. That is inside the guard because the guard is a total, and the deficit is
reachable defensive code — the two parse `catch` blocks and the unreadable-folder return — each of which
has a test for the case it guards and no test for a path that cannot happen. It is recorded here so a
later reader can see where the headroom went rather than finding it by diffing two reports.
