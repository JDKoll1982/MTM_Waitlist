# What's Left — Plain-Language Summary

**For:** everyone who uses the MTM Waitlist system — operators, material handlers, supervisors, plant
managers, and whoever administers the machine it runs on.

**No technical background needed.** This is the everyday-language companion to `OPEN-TASKS.md`, which is the
same information written for the people who work in the code. If you want the technical detail, read that one.

**Last updated:** 11 September 2026.

---

## The short version

The Waitlist app is in good shape. The big piece of recent work — a **spare copy of Infor Visual data** so the
screens keep working when Infor Visual is unavailable — is built, running, and refreshing successfully.

Four things are still open. **Two of them need a decision from you**, one you can fix yourself in ten seconds,
and one is a long-term measurement that just needs time to run.

Nothing here is broken in a way that stops you working today.

---

## What's working right now

- **The app's own data is always live.** Waitlist requests, WIP, and Receiving are read and written straight to
  the database. Nothing is ever faked or substituted.
- **If Infor Visual goes down, the screens keep working.** The app quietly switches to a saved copy of that
  data, and switches back on its own when Infor Visual returns. There is no switch to remember and nothing to
  press.
- **The spare copy is now being kept up to date successfully.** All five sets of Infor Visual data refresh
  properly — this was broken and was fixed on 11 September.
- **The service that keeps the spare copy warm has a proper window again,** with Settings and Status pages laid
  out in the same style as the main app.

---

## Things you may need to do

### 1. Show the service's tray icon (10 seconds, no tools needed)

The background service has **no ordinary window**. You reach it from a small icon near the clock. Windows
hides brand-new tray icons by default, so that icon is currently tucked away — which makes a perfectly healthy
service look like it isn't running.

**To show it:**

1. Click the small **`∧`** arrow on the taskbar (next to the clock), **or**
2. Go to **Settings → Personalization → Taskbar → Other system tray icons** and switch on
   **`MTM_Waitlist.Mock.Service`**.

Then click the icon to open the service's **Status** page (is everything up to date?) and **Settings** page.

### 2. Decide how the service's password is handled

**This one needs your decision, and it's currently blocking things.**

The background service protects its status page — and lets other programs ask it to refresh — using a
password. Right now that password is **created automatically, but there is no way to see it.** Not for you,
not for IT, and not for the programs that need it.

The result: **nobody can check the service's status, and nothing can trigger a refresh on demand.** The
service is running fine — you just can't ask it anything.

**Two options, either is fine:**

- **Show it once when it's created** so the person setting up the machine can write it down, or
- **Let the operator type in the password** they want, and the service stores that.

Just say which you prefer.

---

## Known problems, and what they mean for you

### Cached work orders can be incomplete (partly fixed)

When Infor Visual is unavailable, work-order lookups are served from the saved copy. Two problems were found:

- **Fixed:** some lookups could show a **different work order's information** than the live system would.
  Wrong data is worse than no data, so this mattered. Now a cached result either matches what the real system
  would return, or returns nothing.
- **Still open:** many work orders **can't be looked up at all**, because the app only accepts work-order
  numbers written in certain ways, and a large number of open orders use other formats. Fixing this means
  changing how the app accepts work-order numbers — which changes how people type them in. **That's a decision
  for you, not a quick repair.**

### If the service stops refreshing, nothing will tell you why

The service doesn't keep a log file. When something goes wrong, the reason is only visible to a developer
attached to the running program — so a problem stays invisible until somebody notices the data is stale.

The plan is to give the service a proper log file, so this becomes diagnosable instead of invisible.

### The full end-to-end test hasn't been run yet

The formal acceptance test — proving the fallback works with Infor Visual deliberately switched off, on a
signed-in machine, plus the backup-and-restore drill — has **not** been completed. It needs a specific setup
that isn't in place yet.

Separately, **two long-term measurements haven't started**, and they need **30 days of real time**:

- that at least **95% of scheduled refreshes** succeed, and
- that **every scheduled backup** produces a file that can actually be restored.

Because they need a month of wall-clock time, the sooner they're switched on the better. Nothing else is
waiting on them.

---

## What's planned next

These are the next pieces of work, roughly in the order they make sense. Each is a project in its own right.

| What | What it gives you |
| --- | --- |
| **Handlers can accept and finish jobs** | Accept / Complete / Release actions on each request. Accepting assigns the job to you automatically, it stays on the shared list, and only you can complete or release it. Handlers can add a note, and the shared list is sorted **most urgent first**. *(The due-time and overdue maths are already done.)* |
| **One consistent request card** | Every request shows as the same simple two-line card — like **"Deliver: Coil"** — instead of a long list of type names. Creating a request becomes **Category → Item** (Pickup / Deliver / Assist / Other), and you're only offered items the job actually has. |
| **Reporting for managers** | A Plant Manager dashboard: volumes, how quickly requests get filled, on-time versus late, cancellations and reasons, workload per handler, and how often parts were short at the time of the request. |
| **Cancellations and old-request housekeeping** | An admin view of who cancelled what, when, and why. Finished requests older than **90 days** drop off the main list but are **kept, never deleted**, so reporting stays complete. |
| **User management** | A new **Administration** area in Settings to add and edit users and their roles, search for them, and deactivate them — with protection against locking yourself out. |
| **A clearer start-up message** | If the main database can't be reached, show a plain message with **Retry** and **Cancel** instead of a technical error. |

---

## Things that are *not* being built

So nobody waits for them:

- **No request-type editor.** You won't get screens to add or change request types and subtypes. That was
  decided deliberately — the existing catalog is used as-is.
- **No "use test data" switch.** The fallback is **automatic** and always has been. There is no manual mode to
  switch on or off, and one won't be added. Where the app reads live data, it stays live.

---

## If something looks wrong

1. **Check the service is running** — click the `∧` arrow on the taskbar and look for
   `MTM_Waitlist.Mock.Service`. If it's missing, see "Show the service's tray icon" above.
2. **Open its Status page** from that icon to see when each set of data last refreshed.
3. **If the Status page can't be opened or asked anything**, that's the password problem described above —
   it needs the decision, not troubleshooting.

---

*This is a plain-language companion to `OPEN-TASKS.md`, which carries the same information with technical
detail, evidence, and identifiers. Where the two disagree, `OPEN-TASKS.md` is authoritative.*
