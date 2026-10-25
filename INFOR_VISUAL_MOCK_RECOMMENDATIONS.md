# Infor Visual data the cache does not carry yet

**Date:** 2026-10-23
**Audience:** plant operations, setup leads, and anyone deciding what the app should be able to show when Infor Visual is unreachable.

## What this document is

When Infor Visual cannot be reached, the app keeps working by answering from a kept-up copy of the Visual data it has already read. That copy is refreshed on the service host every three hours, so it is a real answer about the plant, just a few hours old — never sample or made-up data.

This document lists the Visual data the app reads that **has no such copy**, says which of those gaps are worth closing, and ranks them. Nothing here has been built; these are recommendations.

## The short version

| # | What is missing a copy | Who feels it today | Recommendation |
|---|---|---|---|
| 1 | The part catalogue (part numbers and descriptions) | Anyone correcting a wrong part number | **Do this first** |
| 2 | The Infor flag that says stock is non-conforming or on hold | Anyone relying on the "NCM" item type | **Do this second** |
| 3 | The names behind the codes the screens show (work centre, location, warehouse) | Anyone reading a card full of bare codes | Worth doing |
| 4 | Outside-service vendor and service names | Nobody yet — no screen shows them | Only when a screen needs it |

## What already has a kept copy

Eight kinds of Visual read are cached today. Every one of them is answered from the copy when Visual is down.

| The read | What it answers | Modules that use it |
|---|---|---|
| Work order lookup | A work order's part and work centre | Setup |
| Operation sequences | The routing steps for a job and part | Setup |
| Subordinate parts | The components and coils under a job | Setup |
| Work order search | Work orders by number, part, or customer | Setup |
| Inventory locations | Where a part's stock sits, and how much | Waitlist |
| Part warehouse quantities | A part's total stock and its finished-goods stock | Waitlist |
| Disposition input | Whether a job reads as Finished Goods, WIP, or Outside Service | Waitlist |
| Employee by number | An employee's name from their badge number | Startup |

That is eight cached answers out of the nine Visual reads the app makes. The ninth is gap 1 below.

## Gap 1 — The part catalogue has no kept copy

**Recommendation: close this gap. Highest value of anything in this document.**

**What is read today.** When someone corrects a wrong part number, the app searches Visual for the part whose number is exactly what they typed, or failing that every part whose number contains it. This is the one Visual read in the app that was deliberately left uncached, on the reasoning that a "contains" search across every part is not something you can hold a copy of.

**What that means in practice.** If Visual is down at that moment, the search cannot help. The person is told the search is unavailable and their typed value stands — so a typo can be carried into the request, and the request then fails or sits wrong until someone notices by hand.

**Why the reasoning no longer holds.** The catalogue itself is small: about **8,500** parts. Copying the whole catalogue is the same size as several copies we already keep, and the copy refreshes every three hours. Searching a copy is instant and needs no connection to Visual at all — the "contains" search becomes a filter over the copy rather than a question to Visual.

**A second benefit.** The search answer today carries part numbers only, so a person picking the right part chooses from a list of bare numbers. A catalogue copy can carry the part **description** too, so the correct part is recognisable instead of guessed.

**What happens once it exists.** A wrong part can be corrected while Visual is down, and after Visual comes back the search simply goes live again. The copy is never used to overrule a live answer, and a live answer that is legitimately empty still stands.

## Gap 2 — The non-conforming / on-hold flag is not read

**Recommendation: close this gap. Second priority.**

**What exists.** Infor Visual marks each piece of stock with a status: **A** available, **H** on hold, **U** unavailable. These codes are confirmed against the plant system, and a large amount of stock sits at the non-conforming locations under them.

**What the app does instead.** It decides "this is non-conforming stock" from **where the stock physically sits** — a location whose name begins with NCM. That is a good signal and it works, but it is a guess about Infor's intent rather than Infor's own answer. Stock that Infor has put on hold anywhere else reads as ordinary stock to us.

**Why it is worth reading.** A part that Infor has placed on hold should not be offered for return as ordinary stock or as finished goods. Reading the status makes that a fact we were told rather than a fact we inferred from a location name.

**What it takes.** This is not a new copy — it is one extra column on the inventory answer the app already caches. That is a smaller change than gap 1 but it still touches the answer's shape, so it needs the same care: the cache layout, the refresh, and the service redeploy all move together.

**Worth noting.** No screen shows this today, so the value is in preventing a wrong offer, not in a new display.

## Gap 3 — The screens show codes where they could show names

**Recommendation: worth doing, after gaps 1 and 2.**

Three small reference tables hold the meaning behind codes the app already displays:

| Visual table | Rows | What it would add |
|---|---|---|
| Work centres | about **240** | "100-12" becomes the work centre's real name |
| Locations | about **13,000** | "002/FG" becomes the location's description |
| Warehouses | **4** | "002" becomes the warehouse's description |

**The situation today.** Cards and setup screens show the code. Work centre names that the app does display come from our own waitlist database, not from Visual, so a work centre that exists in Visual but not in our list has no name to show.

**Why it is a single piece of work.** All three are small, change rarely, and carry no quantity, so one cached copy can hold all three without any freshness concern.

**What it is not.** It would not fix anything broken. It would make the screens easier to read and would let the app say plainly when a code is not a real work centre.

## Gap 4 — Outside-service vendor and service names

**Recommendation: wait. Nothing needs it yet.**

Visual knows which vendor and which service each subcontracted operation goes to — about **1,400** vendors and **8** services. The app already knows *that* a job is outside service (it now decides this from the routing, the same way the plant's own tag print does). It does not know or show *who* the work is with.

**Why wait.** No screen shows a vendor name today. Caching data nothing displays adds a copy to keep fresh and a redeploy to do, for no benefit yet. When a card or a detail view wants to say "Outside Service — Vendor X, Deburr", this becomes worth doing, and it is cheap at that point.

## What is not worth caching, and why

| Visual data | Rows | Why not |
|---|---|---|
| Inventory transactions | about **3.5 million** | Far larger than anything we cache, changes constantly, and no screen asks for it |
| Infor's own WIP inventory table | **empty** in this installation | Nothing to copy |
| Label type and label format tables | **16** | This is Visual's barcode label engine, not the shop tag the floor uses |
| Enumeration codes | about **700** | Fixed vocabulary that never moves in practice; our code constants are the better home |
| Production order type | **NULL** on every work order here | Cannot be used as a signal |
| Customer records | about **860** | The customer name we show already comes from the order allocation the work order search reads |
| Part site records | about **8,500** | Already read inside the subordinate parts answer, which is cached |

## How a new cached answer gets added

In plain terms, adding one cached answer is four things that must move together:

1. A read the app can run against Visual, and a matching bulk read the service can run.
2. Two copies of the answer in the cache database — one being filled while the other is still answering.
3. A read and a refresh for that answer in the cache database.
4. The declared answer added to the app's and the service's lists, the parity tests updated, and the service redeployed on its host.

Because the copies are swapped rather than emptied, an answer can be added without the service going down. The host rebuild is the only step that needs a person on the server.

## How this list was produced

Every Infor Visual read the application makes was located in the repository and matched against the eight cached answers. One read has no cached answer — the incorrect-part search, gap 1. The remaining gaps come from Visual data the app's own screens and rules already depend on but never reads, checked against the plant's schema export and the recorded live findings for this installation.

Two open questions, neither blocking:

- Gap 2 changes the shape of an existing answer. Whether the status belongs on the inventory answer or the disposition answer is a design choice that should be made before any code is written.
- Gap 1 assumes the catalogue copy carries descriptions. If the correction screen should keep showing numbers only, the copy is smaller still but the second benefit is gone.

## Decisions needed

1. Decide whether gaps 1 and 2 are approved, since both need a cache layout change and a service redeploy on the host.
2. For gap 2, pick which cached answer should carry the non-conforming status.
3. Approve or drop gaps 3 and 4; neither is needed for anything working today.
