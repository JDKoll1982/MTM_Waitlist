# Validation

The proofs that are not a test suite. Every command below was run from
`extensions/csharp-xaml-graph`, and every path in it is written as prose because no recorded file
carries a machine path.

## The incompatible repository: refused by name, and answered no by doing nothing

The repository used was a folder outside all four checkouts, at
`%TEMP%\csharp-xaml-graph-incompatible-023`, holding one file: `Sample.fsproj`. It has no C# and no
XAML, so it is the case the analysis cannot read at all.

**Run 1 — `node out/validate.js <folder> --compatibility`** exited **3** and printed:

```
compatibility: incompatible
C# files: 0
XAML files: 0
projects: 1
solutions: 0
cannot analyse no C# files: The analysis reads C# and XAML, and found no C# files in the files it reads, so there are no types to draw (0 XAML files read).
  0 C# files
  0 XAML files
cannot analyse no XAML files: The analysis reads C# and XAML, and found no XAML files in the files it reads, so no view-to-view-model binding can be drawn (0 C# files read).
  0 XAML files
  0 C# files
cannot analyse Sample.fsproj: The analysis reads .csproj projects and C# sources, and this repository is built as a .fsproj project (Sample.fsproj), which it does not read.
  1 .fsproj project
```

Three structures are named, each in a plain sentence, each with the evidence under it. No line says
"incompatible" without saying what is incompatible.

**Run 2 — the same command again, without `--request`, which is the no answer.** It exited **3** with
the same three reasons and wrote nothing. Checked immediately after it: `update_prompts/` did not exist
in the extension at all, no `request.md` existed anywhere under the extension, the folder's own listing
was byte-for-byte the same as before, and `Sample.fsproj` still hashed
`F5FEC380B88824A8C90623DFBB2CC0CCA66280B943026C00AD66272CB600F434`.

So a repository the extension cannot read is refused by name, and the answer that writes nothing
writes nothing — there is no mode that waits for input and none that creates a file to find out what
was asked.

## The yes answer: one current request, and the requesting folder still untouched

**`node out/validate.js <folder> --request`** printed the same verdict and its reasons, then
`request: update_prompts/csharp-xaml-graph-incompatible-023/request.md`, and exited **0**.

The file it wrote opens with `# Extension change request: csharp-xaml-graph-incompatible-023` — the
requesting repository, named from the folder — and its next text line states
`/speckit-companion-specify` verbatim. Beneath those: one section per reason the
verdict named (3), each opening with the sentence the person was shown; one subsection per required
change (4), each carrying at least one evidence line that names a count or a repository-relative
structure (7 evidence lines in total); and a closing section stating that nothing in the requesting
repository was created or changed.

**Run a second time** with the same command, it printed the same path and exited **0** again. The
extension then held exactly one file under `update_prompts/` — not two folders, not a second
`request.md` beside the first — so answering yes twice leaves one current request rather than a stack.

After both yes answers the requesting folder's listing and `Sample.fsproj`'s hash were unchanged, which
is the SC-013 claim: the request is a request against the extension, and the repository it is about is
read and never written.

## The four-of-four rollup

All four checkouts were profiled and measured. Each profile was written by an author working from that
checkout's setup prompt and its repository alone, with no other context, and each was accepted by
`--check` on its first attempt with zero problems.

| Checkout | Read | Types | Links | Findings | Record produced by | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| `MTM_Waitlist` (this repository) | 815 C# / 93 XAML | 1051 | 78 | 21 | `--expected`, profile applied | **pass** |
| `MTM_WIP` | 95 C# / 36 XAML | 301 | 18 | 12 | `--expected`, profile applied | **pass** |
| `WinUI3LearningLab` | 215 C# / 174 XAML | 237 | 6 | 1 | `--expected`, profile applied | **pass** |
| `MTM_Receiving_Application` | 1015 C# / 145 XAML | 1119 | 129 | 25 | `--expected`, profile applied | **pass** |

Every run passed all five conditions: `layers`, `links`, `findings`, `import` (one attempt, zero
problems) and `invariants` (`ALL CHECKS PASSED`). Measured sizes: 1051 nodes / 4380 edges,
1119 / 6306, 301 / 990 and 237 / 150.

**Determinism.** Each checkout was measured a second time against the same record and the two result
files were compared with `--compare`: all four printed `IDENTICAL`, so a repeat run reproduces the
measurements and the condition outcomes.

**What the profiles changed.** None of the four repositories uses this extension's built-in
conventions unmodified. `MTM_WIP` and `MTM_Receiving_Application` name everything by prefix
(`View_`, `ViewModel_`, `Service_`, `Dao_`), which the built-ins read as data or as plain models.
`WinUI3LearningLab` has no view-model namespace at all, so only the suffix can place its view models.
`MTM_Waitlist` splits `Services` and `Contracts` between collaborators and about forty records, so the
data rules have to win ahead of the namespace rules. Each profile states those facts and nothing else.

**The defect the bar found.** `MTM_Receiving_Application` initially failed on `invariants` alone:
three `binds` edges carried a line number from one file of a partial type and the file name of
another, so the evidence pointed past the end of the file it named. The cause was a rule that paired a
mention's line with the entry file instead of the mention's own file; the same shape existed in the
route-registration rule. Both now take the mention's file. After the fix the run passed, and this
repository's unprofiled analysis stayed at 1095 nodes / 4776 edges / 21 findings, so the fix corrected
evidence attribution without changing what was found.

**Only one file was added outside this repository.** Each checkout gained
`csharp-xaml-graph.profile.json` at its root and nothing else: `WinUI3LearningLab` and
`MTM_Receiving_Application` show exactly that one untracked file, and `MTM_WIP` shows it against the
49 modified and 26 untracked files it already carried before this run.

**A gap the profiles exposed.** Three of the four repositories apply `[ObservableProperty]` to a
partial property rather than a field, and the profile format can only say that an attribute generates
a member from a `method` or a `field`. That form declares the property name itself, so nothing is
missing from the analysis today, but the format cannot state it. It is recorded here rather than
worked around by adding a rule that would never fire.
