# Contract: the headless validation command

`out/validate.js` is how a checkout is analysed and judged without VS Code open on it. It is built from
`src/validate.ts` by `npm run compile` and needs nothing but Node: no editor, no new dependency, and no
inference call.

Eight invocations. The measuring one is what a checkout's recorded result comes from; `--expected` is
what writes the record it is measured against, from the same inspection `--inspect` prints.
`--compatibility` and `--request` came with the compatibility check: one issues the verdict before any
profile work, and one turns that verdict into a request file.

## `--inspect`

```bash
node out/validate.js <checkout> --inspect
node out/validate.js <checkout> --inspect --json
```

Walks the checkout, reads the files the built-in selection covers, and prints the structural and
convention evidence a profile author needs. This is the supported inspection: the setup prompt quotes
the same derivation, and `--expected` writes a record from it, so the prompt, the record and this
output cannot describe one repository three ways.

Printed, in this order: the compatibility verdict and its facts, exactly as `--compatibility` prints
them, then `files read: <n> C#, <n> XAML`, then one section per kind — `name suffixes`,
`name prefixes`, `namespace segments`, `calls`, `base types`, `method attributes`, `field attributes`
and `xaml root tags` — each item on its own indented line. An empty section prints `none`.

With `--json`, one JSON object is printed instead of those sections:
`{ compatibility, facts, limits, files, evidence }`, in that key order.

Nothing is decided here: no layer is assigned, no link is drawn and no finding is computed. Nothing is
written, nothing inside the checkout changes, and no editor is needed.

| Exit | Meaning |
|---|---|
| 0 | The folder was read. It may declare nothing at all; that is still an answer. |
| 2 | No evidence could be gathered: the folder could not be listed. One `inspection:` line says so. |

## `--expected`

```bash
node out/validate.js <checkout> --expected specs/023-extension-repo-profile/expected/<slug>.json
```

Produces the expected-answer record the measuring mode judges a checkout against, in the shape
`contracts/expected-record.schema.json` describes. It takes the checkout's inspection for the
declared types — each named by its `Namespace.Type`, with its checkout-relative file and the layer the
checkout's compiled rules give it — and one analysis of the same checkout, on a fresh read, for the
view-to-view-model links and the findings. It never reads a recorded result, so the record cannot be a
transcript of the run it judges.

Ordered throughout: types by name, links by view then view model, findings by kind then names. The
`source` sentence names this surface and the extension version and records whether the conventions
came from the checkout's profile or from the built-in rules. No absolute path, duration or timestamp
is written, so the record is byte-identical on a second run.

Printed: `repository:`, `types:`, `by layer:`, `links:`, `findings:`, `by finding kind:`, then the
record's path.

| Exit | Meaning |
|---|---|
| 0 | The record was written. Read its counts against `baseline.md` before measuring anything. |
| 2 | No record was written: the folder could not be read, or the analysis threw. |

A record is written once per checkout, before its first measured run, and is never edited after a run
has been compared with it. A mismatch is fixed by re-reading the code, never by copying the run into
the record.

## `--compatibility`

```bash
node out/validate.js <checkout> --compatibility
```

Walks the checkout without opening a file, builds the structure facts, and prints the verdict.

Printed, in this order: `compatibility: compatible` or `compatibility: incompatible`, then the facts
(`C# files`, `XAML files`, `projects`, `solutions`), then one line per reason as
`cannot analyse <structure>: <sentence>`, then one line per evidence item indented under it. Nothing is
written and nothing is changed, so this is the proof that a verdict precedes any file.

| Exit | Meaning |
|---|---|
| 0 | `compatible`: profile work may proceed. |
| 3 | `incompatible`, with every reason named. |
| 2 | `unknown`: the structure could not be read. One note says so, and profile work proceeds as it does today. |

## `--request`

```bash
node out/validate.js <checkout> --request
```

The yes answer to the offer, for use without a person present. Runs the verdict, and when it is
incompatible writes the request file to
`extensions/csharp-xaml-graph/update_prompts/<slug>/request.md` from the extension's own root and prints
its path. On a compatible checkout it writes nothing and says so.

| Exit | Meaning |
|---|---|
| 0 | The request was written, or the checkout was compatible and nothing was needed. |
| 2 | No verdict could be reached, so no request was written. |

Without `--request` there is no yes answer, which is exactly the no answer: nothing is written. That
is what lets the same code serve a person and a script without a mode that waits for input.

## `--prompt`

```bash
node out/validate.js <checkout> --prompt
```

Analyses the checkout with no profile, derives the evidence the analysis already gathers, and prints the
same setup prompt the **Set up Repository Profile** command puts in chat. Exit code 0.

This is the text a fresh-context assistant is given, with the checkout's path. Nothing else is handed
over: the prompt names the profile file, the questions, and the reply shape.

## `--check`

```bash
node out/validate.js <checkout> --check <reply-file>
node out/validate.js <checkout> --check <reply-file> --record-attempt
```

Reads the reply, takes the first fenced block if there is one and otherwise the whole text, and parses it
with the same parser the import command uses. Prints one line per problem. Writes nothing, unless
`--record-attempt` is given: then it appends `{ sha256, problems }` to
`specs/023-extension-repo-profile/results/<slug>.attempts.json`, so the first check of a reply can be
told apart from a correction.

| Exit | Meaning |
|---|---|
| 0 | The reply parses and records at least one declaration: the extension would accept it. |
| 1 | The reply was refused. Each problem is printed, and nothing has been written. |

The attempts record is what makes SC-011 measurable: `--record` passes the `import` condition only when
that file holds exactly one attempt and it is clean. Deleting it starts a fresh first attempt, and that
deletion is visible in git.

## `--record` and `--result`

```bash
node out/validate.js <checkout> --record specs/023-extension-repo-profile/expected/<slug>.json \
                                 --result specs/023-extension-repo-profile/results/<slug>.json
```

Reads `<checkout>/csharp-xaml-graph.profile.json`, compiles its conventions, selects files by the
profile's `include` and `exclude`, analyses, compares the run with the record item by item, runs
`node out/selftest.js <checkout>` for the invariants, and writes the result.

Printed, in this order: the three counts and the per-label counts, the five conditions each PASS or FAIL,
every mismatch on its own line as `<item> <name>: expected <expected>, got <actual>`, and a final
`VERDICT: pass` or `VERDICT: fail`. Then the result file's path.

| Exit | Meaning |
|---|---|
| 0 | Verdict pass: all five conditions hold. |
| 1 | Verdict fail: at least one item is named above. |
| 2 | Not run: the checkout or the record is missing, the profile could not be read, or the analysis threw. No result file is written, so a moved checkout is recorded as not run rather than as a pass. |

What it never does: write into the checkout, edit the profile, reorder or summarise the mismatch list, or
record a result it could not produce.

## `--compare`

```bash
node out/validate.js --compare <first-result> <second-result>
```

Compares two result files on their measurements, their mismatch lists and their conditions. It ignores
`profile.sha256`, because the two runs it exists for are a fresh assistant's profile and the recorded one,
which may differ in text and still express the same conventions.

| Exit | Meaning |
|---|---|
| 0 | `IDENTICAL`: the measurements, the mismatches and the conditions agree. |
| 1 | `DIFFERENT`, with the first field that differs named. |

## What a caller may rely on

- No mode requires `vscode`, an open folder, or a running editor.
- The only files any mode writes are the `--result` path it was given, the attempts record under
  `--record-attempt`, and the request file under `--request`. None of them is inside the checkout.
- `--compatibility` and `--check` without `--record-attempt` write nothing at all, which is what makes
  them safe to run against a repository whose files must not change.
- The output is plain lines in a fixed order, so a person can read it and a script can grep it.
- The result is byte-identical for equivalent runs: no timestamp, no duration, no absolute path, sorted
  lists throughout.
- No mode waits for input, so every mode works unattended.
