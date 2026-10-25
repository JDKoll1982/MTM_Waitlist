# Coverage map: startup-launch

Which test proves each requirement in
[living-specs-capability-startup-launch.spec.md](./living-specs-capability-startup-launch.spec.md).
Paths are relative to the repository root; `file::Method` names the test that carries the proof.

| Requirement | Tests |
| --- | --- |
| Sign-in offers the last six people who signed in on this computer | `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs::ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenTheStoreHoldsThreePeopleOffersThemInTheOrderTheStoreSent`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs::ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenTheStoreCannotAnswerShowsTheFormWithoutThem`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs::ViewModel_SignInViewModelTests_LoadPreviousPeopleCommand_WhenThisComputerHasNoRecordAsksTheStoreNothing`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/SignInViewModelTests.cs::ViewModel_SignInViewModelTests_ChoosePreviousPersonCommand_WhenAPersonIsChosenFillsInTheUserNameAndNothingElse` |
| The offer of previous people is shown whole beside the sign-in form | `MTM_Waitlist.Tests/Module_Startup/Views/LaunchSurfaceMarkupTests.cs::UI_LaunchSurfaceMarkupTests_TheSignInSurface_PutsThePreviousPeopleToTheRightOfTheFormBehindADivider`; `MTM_Waitlist.Tests/Module_Startup/Views/LaunchSurfaceMarkupTests.cs::UI_LaunchSurfaceMarkupTests_ThePreviousPeople_OfferNoWayPastThePin`; `MTM_Waitlist.Tests/Module_Startup/Views/SignInPageFocusTests.cs::UI_SignInPageFocusTests_TheRememberedPerson_LeavesThePinToBeTyped` |
| Machine setup reads the computer's name from the computer | `MTM_Waitlist.Tests/Module_Startup/Services/MachineFactsServiceTests.cs::Service_MachineFactsServiceTests_Hostname_IsThisMachinesName`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs::ViewModel_MachineSetupViewModelTests_SignInAsync_WhenTheComputerHasAStoreRecordShowsTheComputersOwnNameAndItsStoredLocation`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs::ViewModel_MachineSetupViewModelTests_ComputerName_HasNoSetterAndNoFieldBindsToIt` |
| Machine setup asks only for where the computer stands | `MTM_Waitlist.Tests/Module_Startup/Views/LaunchSurfaceMarkupTests.cs::UI_LaunchSurfaceMarkupTests_TheSetupStage_ShowsTheComputersOwnNameWithNoFieldBehindIt`; `MTM_Waitlist.Tests/Module_Startup/ViewModels/MachineSetupViewModelTests.cs::ViewModel_MachineSetupViewModelTests_SaveAsync_WithNoLocationGivenStillConfiguresTheComputer` |
| What a computer stored before the location existed is read as its location | `MTM_Waitlist.Tests/Module_Startup/Services/MachineConfigurationServiceTests.cs::Service_MachineConfigurationServiceTests_GetStateAsync_WhenTheStoreHoldsAPreChangeRecordReadsWhatItStoredAsItsLocation` |

## Gaps this map does not close

The six-row cap is enforced in SQL, not in the client: `sp_auth_user_previous_sign_ins_list` ends in
`ORDER BY s.issued_utc DESC, s.id DESC LIMIT 6`
([create.sql](../../Database/StoredProcedures/sp_auth_user_previous_sign_ins_list/create.sql)). No test
exercises that procedure against a store holding more than six sign-ins, so the number six is proven by
reading the SQL, not by a test.
