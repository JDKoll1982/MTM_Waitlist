# Contract: The Verification Record and the Boundary Guard

**Feature**: [spec.md](../spec.md) | **Data model**: [data-model.md](../data-model.md)

Two consumers read this feature's output: a reviewer reading `verification.md`, and the build reading the
guard. Each has a fixed shape.

## C1. A verification record is one entry per screen

`verification.md` carries one entry per screen, and each entry carries: the screen, the live read behind it,
the three presentations compared, the tests that prove each of them, and the outcome.

The ten screens are the steps this flow shows: work center, category, item, part, dunnage, scrap, die,
details, summary, result. A screen whose step the draft can skip still needs an entry, because a deferred
chunk's screen has to report its answer source as unavailable rather than vanish from the record.

An entry with no evidence for a presentation is incomplete. Nothing in the record may claim a screen was
compared without naming what proved it.

## C2. Outcomes are exactly two

`matches` when the live presentation is the approved one. A recorded disagreement, with what differed and
which chunk owns the correction, when it is not.

No third outcome. "Probably fine" and "looked at it" are not outcomes, and an unresolved disagreement that
ships is the failure FR-013 names.

## C3. The boundary guard fails the build, by name

The guard fails when a `Temp` artefact appears in any project the solution names, and its message names the
offending file and the offending identifier. A message that says only that something matched is not
conforming: a reviewer has to be able to open the named place.

## C4. Exemptions are exact and reasoned

An exemption is an exact repository-relative path, or an exact path plus one exact declared name. Each
carries a written reason. No pattern, suffix or directory exemption exists, and an exemption added without a
reason fails review.

## C5. The deliberate-offence case stays

The guard's own test keeps a case that feeds it one offending declaration and one offending resource key and
asserts both are named, plus a case where an unrelated constant is not flagged. That case is what makes
SC-002's "verified by deliberately doing so once" a fact rather than an intention.
