# Manual UI Smoke Checklist

- [ ] Launch the app successfully from the current branch.
- [ ] Confirm the startup/splash flow still appears as expected.
- [ ] Open the waitlist view and verify navigation works.
- [ ] Open the settings experience and verify options remain accessible.
- [ ] Verify no startup exceptions are logged during application launch.
- [ ] Confirm the app can be closed and reopened without a DI registration error.
- [ ] Select a valid Work Center and confirm the new-request flow opens.
- [ ] Cancel a request from the request-type flow and confirm the user returns to the waitlist without saving a draft.
- [ ] Choose a subtype-backed request and confirm the subtype selection is shown before confirmation.
- [ ] Enter a required text value and confirm the validation summary and field-level feedback appear as expected.
- [ ] Submit a valid request and confirm the new item appears in the active waitlist without restarting the app.
- [ ] Trigger a duplicate warning and confirm Continue and Cancel behave correctly.
- [ ] Cancel an accepted request and confirm the cancellation metadata and notification payload are surfaced to the material-handler flow.
- [ ] Verify the active waitlist still shows overdue items as active with the overdue state visible.
- [ ] Confirm the app handles stale work centers and invalid setup-job states with the restart-required messaging.
