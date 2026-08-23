Hello,

Here's what changed in MTM Waitlist this weekend.

## What's new

This weekend I focused on the Work Center screens. They look different and are easier to use.

### Work Center Selection

- Cards now show a real photo of each work center.
- Each card shows what's running: **Current Job**, **Part Number**, and **Last Updated**.
- The date updates automatically whenever a setup is saved.
- The selected card is highlighted with a blue outline and blue photo frame.
- Cards adjust to any screen size.

### Local vs. Other Work Centers

- Work centers are split into two groups: **Local Work Centers** (for your computer) and **Other Work Centers**.
- The list starts on your local work centers. The rest are one click away.
- "Hot Work Centers" is now called "Local Work Centers."
- The list now matches the real 25 work centers, split by building (Expo Drive and Vits Drive).

**How Local Work Centers work:** Each computer can have its own set of local work centers. For example, a PC near presses 100-14 and 100-18 would have those two presses set as its Local Work Centers, so the users working at those presses can quickly pick the right work center without scrolling through the full list.

### New Request

- The New Request work center screen uses the same new cards, so it matches Setup.
- Added search and a building filter.
- The header stays visible as you go through each step, so you always know where you are. This is where the Building Filter lives.

### Step Progress

- Added a step-by-step progress bar to the header for both Work Center Setup and New Request Workflows.
- You can always see which step you're on and how many are left.

### Dunnage & Scrap - New Request Workflow Step

- The step is now called **Dunnage & Scrap**. As this step use to only cover Adding Dunnage, it now also includes setting the scrap type for the Job.
- Added a "No Scrap" option.
- You must pick a scrap type before continuing.
- The app asks for confirmation before continuing with no dunnage.

## Why the above changes matter

- You can spot the right work center at a glance, with a real photo.
- You can see what's running and when it was last updated before you select it.
- Local work centers are front and center, so everyday use is faster.
- Setup and New Request look the same, so both flows feel familiar.
- The progress bar removes guesswork about what step comes next.
- Scrap and dunnage choices are now explicit, so nothing is left to chance.

## Next Proposed feature: User Management (pending approval/modification)

I am proposing a new **User Management** panel in Settings. It would let **Setup Lead and above**
create new users and edit existing users and their privileges directly in the app, instead of managing
accounts through the database the way I have it setup today. This is a must-have and can't be pushed to a later release.

**What it would do:**

- Add a new **Administration → User Management** section in Settings, visible to Production Lead and above.
- Create new users with a username, first/last name, employee ID, and an initial role.
- Edit existing users: change their role, display name, employee ID, activate/deactivate, or reset their password.
- Search users by username, display name, employee ID, or role.
- Track who made each change (audit trail).
- Keep role rules enforced: you can only assign a role up to your own, and you can't deactivate your own account.

**Why now:** Account and privilege management is currently handled outside the app. This brings it
in-house with clear role rules and a full audit trail. It's also a prerequisite for the next major
planned feature, the **Material Handler Workflow** — it needs to land first.

The full requirements are captured in **`User-Management-Clarifying-Questions.md`**, and the design was
stress-tested for edge cases in **`User-Management-Edge-Case-Report.md`** (including the fixes needed to
avoid app-crashing issues like unsafe background-thread UI updates and double-inserts). These two files
are written to be easy to follow during implementation — if anything is unclear, let me know and I can clarify.

**This feature is proposed for approval / modification before any implementation begins.**

## Next steps

- Get Photos of each Press (must be in a 1:1 format).
- Add Missing work-centers (can be done via the app quickly, already implemented)
- Review and approve or modify the proposed User Management feature before implementation.

Thank you,

[Your Name]
