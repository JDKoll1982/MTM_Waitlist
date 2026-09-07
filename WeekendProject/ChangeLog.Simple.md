# MTM Waitlist — What's New

> **🕐 Times for Shawn to enter**
>
> | Date | Day | Clock in | Clock out | ~Hours |
> |---|---|---|---:|---:|
> | 2026-09-05 | Sat | 9:00 AM | 11:00 AM | ~2h |
> | 2026-09-06 | Sun | 10:30 AM | 6:00 PM | ~7h |
>

>
> **Legend:**
>
> - ✅ Completed Feature/Update
> - 🚧 Partially Completed Feature/Update
>
> **Last updated:** 2026-09-06

## Where Infor Visual data comes from (outage fallback)

**Infor Visual data** is used thoughout the Waitlist Application. Normally the app
reads that data **live** from the **Infor Visual** database. If that database is **unreachable or
down**, the app can automatically fall back to **cached Infor Visual data** (presently a bundled sample)
so the screens still open and stay usable instead of failing or sitting empty. In short:

- **Infor Visual up** → the app shows **live Infor Visual data**.
- **Infor Visual down** → the app shows **cached Infor Visual data** so nothing breaks; when the
  database comes back it switches back to live automatically, as long as the Waitlist Database is active everything works as intended.
- You can also **switch between live and cached Infor Visual data manually** with the single
  **"Infor Visual data"** toggle under **Settings** (mainly for testing/demo without the real database;
  I still need to source the cached data from the visual database rather than the bundled sample).
- When the app is *automatically* falling back or recovering, a **notification** explains which Infor
  Visual data it's showing (cached, or live again).

## Features (1–19)

1. 🚧 **Request lifecycle with statuses + history.** Requests move through **Pending → Accepted →
   Completed** (or **Canceled**); every step is recorded with who/what/when and shown on the card.
   *(Statuses/audit + DB + service done; card **Accept/Complete/Release** actions remain — W07.)*
   — **~40 min**
2. ✅ **Cancel your own request.** Cancel a request you created while it's still waiting, kept with
   the reason (never deleted). — **~15 min**
3. ✅ **"My Requests" filter.** Header toggle shows only your requests, or all — visible only on the
   Waitlist screen. — **~20 min**
4. ✅ **Wait time on each card.** Every request shows how long it's waited, and overdue shows **bold
   red**. — **~20 min**
5. 🚧 **Notes on a request.** Requests carry a short free-text note. *(Save/persist done; handler
   edit UI remains — W07.)* — **~20 min**
6. ✅ **Clear empty states.** List/detail show a friendly "Nothing to show" instead of a blank area.
   — **~15 min**
7. ✅ **New Request creation flow.** Type/subtype selection, building + work center, details, and
   submit (driven by the real catalog). — **~45 min**
8. ✅ **Requester identity & selection.** Signed-in employee number/name is surfaced and selectable on
   a request. — **~25 min**
9. ✅ **Coil inventory** Added showing a list of locations where coils are stored for Coil Delivery Requests, will also be implemented for other request types such as Flatstock and other component parts. — **~45 min**
10. ✅ **"Requested coil" shows the actual coil** on the job (e.g. `MMC0001000`), not the action word (was shoing `Coil Goes Here` earlier). — **~20 min**
11. ✅ **Average coil weight from real receiving history**, pulls all coils received in via the MTM Receiving App, gets the average coil weight and returns it to the Application so material handlers can see in real time the average size of a coil without the need to look it up, helps determin if a larger forklift is needed. — **~30 min**
12. ✅ **New Request confirm shows coil details + coil gating.** Allows Press Operators to see that actual Coil number they are requesting. — **~25 min**
13. ✅ **Unified request-card layout + action-phrase titles.** All types share one tidy card (fixed
    square photo, title row, four detail rows, centered buttons, compact status badge); titles read as
    plain actions (e.g. **Deliver: Coil**, **Scrap: Empty**, **General Request**) Used strangly formatted titles before like `Coil\Bring` `Scrap\Emtpy` `NCM\Pickup`. — **~45 min**
14. ✅ **Press / work-center photo + image viewer.** Cards show the press photo; clicking either photo opens a larger view. — **~30 min**
15. ✅ **Detail location/part grid.** Sortable, pooled-weight inventory grid on the request detail.  — **~30 min**
16. 🚧 **Ignored-location settings.** **Settings → Operations** add/remove of hidden locations (e.g. `WC`, `NCM`, `SHIP`). *(Settings UI done; apply app-wide remains unimplemented.)* — **~25 min**
17. 🚧 **Urgency / due time for each request.** Every request sub-type (e.g. Deliver Coil) has a
    **max-allotted time** — how long it should reasonably take to fill — set by a Plant Manager under
    **Settings** (default ~30 min). The app uses that to show a **due time** for each request
    (submitted time + max allotted) and a **"time left"** countdown, switching to bold **red
    "Overdue"** once it passes due — so handlers can spot at a glance which requests are running late.
    *(Settings + due/overdue math are done; sorting the handler list "most urgent first" is still in
    progress.)* — **~35 min**
18. ✅ **New Request Alerts (per-user).** **Settings** toggle (default off) → toast on a new request +
    deep-link to the request detail. — **~40 min**
19. ✅ **Single "Infor Visual data" switch.** One **Settings** switch controls whether the app uses
    **live** or **cached/sample** Infor Visual data. — **~15 min**

---

## Time summary

**Measured (WakaTime, MTM_Waitlist, since yesterday → now):** ~8h 58m
**Rounded total used:** **9h 00m** (540 min)

| # | Feature | Status | Est. time |
|---|---------|:------:|----------:|
| 1 | Request lifecycle with statuses + history | 🚧 | 40 min |
| 2 | Cancel your own request | ✅ | 15 min |
| 3 | "My Requests" filter | ✅ | 20 min |
| 4 | Wait time on cards + overdue bold red | ✅ | 20 min |
| 5 | Notes on a request | 🚧 | 20 min |
| 6 | Clear empty states | ✅ | 15 min |
| 7 | New Request creation flow | ✅ | 45 min |
| 8 | Requester identity & selection | ✅ | 25 min |
| 9 | Coil pooled-weight inventory + "Quantity (lb)" grid | ✅ | 45 min |
| 10 | "Requested coil" shows actual coil | ✅ | 20 min |
| 11 | Average coil weight from receiving history | ✅ | 30 min |
| 12 | Confirm-screen coil details + coil gating | ✅ | 25 min |
| 13 | Unified card layout + action-phrase titles | ✅ | 45 min |
| 14 | Press photo + image viewer | ✅ | 30 min |
| 15 | Detail location/part grid | ✅ | 30 min |
| 16 | Ignored-location settings | 🚧 | 25 min |
| 17 | Urgency per-subtype allotment + due/overdue | 🚧 | 35 min |
| 18 | New Request Alerts toggle + toast + deep link | ✅ | 40 min |
| 19 | Single "Infor Visual data" switch | ✅ | 15 min |
| | **Total** | | **540 min = 9h 00m** |

---

## Still in progress

What I'm still actively working on in this area:

- **The "handler" side of the lifecycle (Accept / Complete / Release).** Statuses, history, and the
  operator actions behind them exist, but I still need the **Accept / Complete / Release** buttons on
  the cards so a handler can pick up and finish a request, plus the **note-editing UI**.
- **Apply ignored locations everywhere.** The **Settings → Operations** list of hidden locations is in,
  but I still need it to actually hide those locations from the Waitlist/Coil lists, the "Quantity in
  house" totals, and the Setup location lists.
- **Live location/part lookup.** Route the location grid to the **real** (non-mock) location/part
  source when Infor Visual is up, not just the sample data.
- **Order the handler list most-urgent-first.** The urgency math and per-sub-type time settings are in;
  I still need to sort the list a handler sees by most urgent first.
- **Analytics screen.** A supervisor-level analytics view (request volumes, wait times, on-time vs
  overdue, cancellations, handler workload).
- **Cancelled-request monitor / admin view.** A Developer/Admin view of cancelled requests
  (who/when/type/reason) and archiving of old completed/cancelled ones while keeping them for review.1
- **Cached Infor Visual data sourced from Infor Visual.** The cached/sample mode currently uses a
  bundled sample; I still need it to pull that data from the Infor Visual database rather than the
  bundled sample, and to finalize the automatic fall-back / recovery messaging.
- **Splash / database-unreachable messaging.** A clearer, non-technical "can't reach the database"
  message with Retry / Cancel instead of a bare error.
- **Settings freeze fix.** A UI freeze when opening **Settings** is being investigated and fixed.
