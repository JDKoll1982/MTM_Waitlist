# Release Notes

## Infor Visual Failover & Caching

### Automatic Failover

When Infor Visual cannot be reached — a network blip, a maintenance window, or a full outage — the application switches to the new Infor Visual cache database on its own. There is nothing for a user to turn on, no dialog to accept, and no error banner to dismiss.

#### What keeps working

The five Infor Visual reads the application depends on continue to return results (for now):

- Work-order lookup
- Operation sequences
- Subordinate parts
- Inventory locations
- Request disposition input

#### What the user experiences

When the cache is engaged, the user gets the same results from the cache database that they would get from Infor Visual. The screen looks and behaves normally; only the small status indicator (described below) hints that the data came from cache rather than directly from Infor Visual.

#### How the switch happens

Each running application probes the Infor Visual database itself, every 30 seconds, with a plain connectivity check. Two consecutive failed probes are required before the application decides the source is unreachable, so a single dropped packet never triggers a switch. Nothing has to be turned on, and no setting is involved.

While cached data is being served, probing backs off to once every 5 minutes, so an outage does not become a poll loop against a dead server. A single successful probe is enough to switch back, so recovery does not have to wait out a timer. Because each application decides for itself, two open clients can return to live data a few seconds apart.

The service on the host is not in that path. It does probe Infor Visual, but only so its status output can report whether the source is reachable — it does not tell clients when to switch, and the application does not wait to be told.

#### How often the cache is refreshed

The cache is refreshed by the service on the host, on a schedule configured there: **every 3 hours by default** — at 00:00, 03:00, 06:00, 09:00, 12:00, 15:00, 18:00 and 21:00 server-local time. The schedule is anchored to those clock times rather than counted from the last run, so a long or skipped cycle cannot drag it off them. An individual read shape may override the interval, and an operator can request an immediate refresh through the service's token-gated API.

This is deliberately **not** the same number as the detection interval above. Detection happens every 30 seconds; the cache is refreshed every 3 hours. Nothing refreshes the cache as a side effect of Infor Visual coming back — the next scheduled cycle (or an on-demand request) is what refreshes it.

---

## UI Indicator & Cache Logic

### InfoBar Indicator

While cached data is being served, a small notification bar appears inside the existing application shell. It tells the user two things: that Infor Visual is unreachable and cached data is in use, and how old that cached data is (the time of the last successful refresh, so the user can judge freshness).

#### It is purely informational

The bar has no buttons, no toggle, no link, and no command behind it. Clicking or tapping it does nothing. There is no way for a user to change a data mode, because there is no mode to change.

#### It is not dismissible

Because it reports a real condition rather than a one-off message, it cannot be closed while that condition is true. It currently spans the top of the shell's content area, above whatever screen is open.

#### It does not block work

It sits above the page content inside the existing shell rather than covering it, so it never hides or disables the work behind it, and it stretches to the available width so it stays readable as the window narrows.

#### Cached data is never refused for being old

The indicator reports the age; it does not stop the application from using the cached copy at any age.

### Auto-Clear

As soon as Infor Visual becomes reachable again, the indicator disappears on its own — no refresh of the page, no restart of the application, and no user action. Users do not have to remember that a fallback was in effect or manually clear a warning.

### Live Data Priority

Live data always wins.

- Whenever Infor Visual is reachable, the application reads from it — even if a perfectly good cached copy exists.
- A reachable Infor Visual that legitimately returns **no rows** is a valid empty result, and it is **not** replaced by cached data. Fallback covers an unreachable Infor Visual only — never "the answer is empty".
- If Infor Visual is unreachable and the cached data store is also unavailable, the affected screen reports the store as unavailable rather than showing a substitute or a stale answer without explanation.

---

## Background Refresh Service — Lightweight Server Application

### Atomic Updates

The cache stays correct even while it is being updated.

- A refresh writes the new snapshot into a staging table, then swaps it into place in a single step.
- Because the swap is atomic, a reader running at the exact moment of a refresh sees either the complete previous snapshot or the complete new one — never a mixture, and never an empty table.
- If a refresh fails or is interrupted partway through, the last good snapshot stays intact and in use. A failed refresh never leaves the cache partially written; the shape is refreshed again on its next scheduled cycle, or immediately if an operator asks for one.
- A single refresh cycle runs at a time; a second request while one is in progress is refused rather than queued on top of it.

### Decoupled Architecture

Refresh scheduling is owned entirely by the standalone background service, not by the client applications.

- The application never schedules or performs its own refresh.
- The application decides for itself that Infor Visual is down, by probing it directly; the service is not told to broadcast a switch and does not broadcast one.
- The service refreshes on its own schedule, and refreshes immediately when an authorized caller asks it to.
- The application remains fully functional when the service is not running — it simply serves whatever the cache already contains.
- There is no hard dependency in either direction: stopping or crashing the service does not break the application, and the application does not need to be open for the cache to stay fresh.

### Service Deployment

The service is a separate, unpackaged WinUI 3 application that runs on the database host itself.

- **Auto-start at logon.** It registers itself to start when the host logs in (a per-user `Run` key entry, deliberately not an MSIX-only startup task, because the service ships unpackaged). If the stored setting and the registry entry ever disagree at startup, the mismatch is reported rather than silently ignored.
- **Tray-only lifetime.** It has no main window. It sits quietly in the notification area and only opens a settings or status window when the operator asks for one; closing that window hides it rather than exiting.
- **Single instance.** Only one copy can run at a time. A second launch redirects to the running instance instead of starting a competing refresher.
- **Scheduled refresh engine.** Reads are refreshed on a configurable interval — 3 hours by default, on the eight slots anchored at local midnight — with per-shape overrides, so a shape that is cheap to refresh can run more often than one that is expensive.
- **Graceful degradation.** If Infor Visual is unreachable during a scheduled cycle, that cycle is skipped and logged, the previous snapshot is left untouched, and the next cycle is attempted on schedule. An outage produces a log entry, not a failure alarm.
- **Run records.** Each shape's last outcome and timestamp are recorded on the host in a durable local store — deliberately not in MySQL. Error text is sanitized and never contains the shared credential.
- **Observability.** The service records and surfaces, per item, the outcome and timestamp of the last refresh and the last backup, so an operator can tell at a glance whether the cache is keeping up.
- **Network API, token-gated.** An authorized caller can request an immediate refresh and retrieve status over the network using a shared token. Requests without it are refused and logged, and the token is never displayed, logged, echoed in a response, or written into a status payload.
- **Backups and restore.** The service also produces per-database scheduled backups of the Waitlist Application, WIP Application, Receiving Application, and Infor Visual cache databases — each configurable independently for enablement, schedule, retention, and destination. Emergency restore is host-only and confirmation-gated: it is never exposed over the network, and an unconfirmed request changes nothing.

#### When the backup tooling is missing

If the required backup tool is not available on the host, that condition is reported clearly and no partial or zero-length artifact is ever recorded as a success.
