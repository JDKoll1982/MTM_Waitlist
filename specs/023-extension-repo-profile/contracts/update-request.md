# Contract: the request file written for an incompatible repository

When the extension cannot analyse a repository, it says which structures it cannot handle and asks
whether to start a new specification to make the extension compatible with it. Answering yes writes one
file. Answering no, or closing the dialog, writes nothing.

## Where it goes

```text
extensions/csharp-xaml-graph/update_prompts/{requestingRepo}/request.md
```

Both strings are pinned by the specification and are not to be re-cased or re-spelled:

- the folder `extensions/csharp-xaml-graph/update_prompts/`;
- the placeholder `{requestingRepo}`, standing for the name of the repository that requested the change.

The path is relative to the extension's own root, which is the folder holding the extension's
`package.json`. When the extension is developed in place that folder is `extensions/csharp-xaml-graph`, so
the path above is the path in the repository.

`{requestingRepo}` is the workspace folder's name through the same slug rule the validation records use:
lower case, every run of characters outside `a-z0-9` replaced by one `-`, leading and trailing `-`
removed. `MTM_Waitlist` becomes `mtm-waitlist`, which is also its record's name in `expected/`.

The file inside the folder is `request.md`, so the folder carries the identity and answering yes twice
leaves one current request rather than a stack.

## What it contains

In this order.

1. **A heading** naming the repository the request is for.
2. **The command line**, immediately under the heading, stating that the file is to be run with
   `/speckit-companion-specify`. The literal is pinned: same spelling, same case, same punctuation.
3. **One section per reason** from the compatibility verdict, in the verdict's order. Each section opens
   with the sentence the person was shown, so the file and the dialog cannot say different things.
4. **One subsection per required change**, under the reason it answers. Each states what must change in
   the extension, why, and the evidence from the requesting repository that justifies it — counts and
   repository-relative paths, never an absolute path, a machine name or a timestamp.
5. **A closing line** saying nothing in the requesting repository was created or changed.

A reason with no change is not allowed: a request that names a problem and asks for nothing is a dead
end. A change with no evidence is not allowed either: the evidence is what makes the change reviewable
without opening the other repository.

## Shape

```markdown
# Extension change request: <repository name>

Run this with `/speckit-companion-specify`.

<one sentence: what this repository is and why the extension could not profile it>

## <reason sentence, as shown to the person>

Evidence: <count>, <repository-relative path>

### <what must change, one line>

<one sentence saying what the extension would do differently, and for which structure.>

Evidence:
- <count or repository-relative path>
- <count or repository-relative path>

## <next reason sentence>

...

## What was not changed

Nothing in <repository name> was created or changed. This file is a request against the extension.
```

## What a reader may rely on

- The file is plain markdown, reviewed like any other committed document, and needs no new dependency to
  read.
- Re-answering yes replaces the file; it never appends a second request to the same folder.
- The two pinned strings appear verbatim, so a search for `/speckit-companion-specify` finds every
  request waiting to be run.
- Nothing is written before the offer is answered. The verdict that produces the reasons only reads.
