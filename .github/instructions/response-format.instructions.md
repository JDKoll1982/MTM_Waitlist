---
applyTo: "**/*"
---

# Response Format — Plain English, End-User Facing

How the agent talks in chat, for every task in this repo. Constitution Principle VII is the governing
rule; this file is its working detail. It governs the **chat channel only** — files, code, artifacts and
commit messages are unaffected.

## The rules

1. **Plain English, end-user facing.** Write for the person who asked for the work, not for a compiler
   log. Say what changed and what it means in ordinary words. No jargon walls, no acronyms that were
   never defined, no narration of your own reasoning, and no recapping the request back to the person.
2. **Never paste raw material.** No tool output, terminal transcripts, JSON payloads, stack traces,
   file dumps, diffs, or whole artifact bodies. If one line genuinely matters, quote that one line.
3. **One short line per step, not per keystroke.** Report progress as single sentences. Never paste the
   task list, never announce a command you are about to run, never repeat a step you already reported.
4. **Fences must be safe to render.** A code fence must start at **column 0**. Indenting a fence by four
   or more spaces turns it into an *indented code block* — the backticks then render as literal text and
   everything after them mis-renders (GitHub Flavored Markdown spec, "Indented Code Block (4 Spaces)").
   When the content contains a fence, wrap it in a **longer** run of backticks than the inner one. Never
   put a fence inside a list item. Never print a sample block that was shown to you as a template for a
   file you are writing.
5. **No machine directives in chat.** Do not print `EXECUTE_COMMAND:` or similar tokens unless the host
   explicitly requires them to run a step.
6. **Say what is left, honestly.** Unfinished work, skipped checks and residual risk are stated in plain
   language. "Done" is never used for work that was not verified.
7. **Anything the person must do comes last.** See below.

## The last thing in the reply

If anything needs the person after the chat session — approving a step, running a command, reinstalling
a database, restarting the app, clicking a button, supplying a credential — it goes in a **final section
headed exactly**:

```markdown
## What you need to do
```

as a short numbered list, one action per line, in the order they should do them. If there is nothing,
end the reply with this single line instead:

```text
Nothing needed from you.
```

An action must never be buried in the middle of a reply. If a reply would otherwise end mid-step with
something outstanding, the outstanding thing is still the last section.

## Why

A garbled, fence-broken transcript is not a cosmetic problem: it hides the decisions and the open work,
so the person cannot review or continue. Reading the transcript is how this repo's work is audited, and
an action buried mid-reply is an action that never happens.
