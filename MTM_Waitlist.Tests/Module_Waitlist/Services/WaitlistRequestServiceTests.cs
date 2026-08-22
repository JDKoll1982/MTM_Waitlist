using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class WaitlistRequestServiceTests
{
    [TestMethod]
    public async Task SubmitAsync_PersistsRequestAndReturnsSuccessAsync()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        var requests = service.GetActiveRequests("Expo Drive");
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual("Coil", requests[0].RequestType);
        Assert.AreEqual("Press 12", requests[0].WorkCenter);
    }

    [TestMethod]
    public async Task SubmitAsync_ReturnsDuplicateWarningThenAllowsOverrideAsync()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();

        await service.SubmitAsync(draft, allowDuplicate: false);
        var warning = await service.SubmitAsync(draft, allowDuplicate: false);
        var overrideResult = await service.SubmitAsync(draft, allowDuplicate: true);

        Assert.AreEqual(WaitlistRequestSubmitStatus.DuplicateWarningRequired, warning.Status);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, overrideResult.Status);
        Assert.AreEqual(2, service.GetActiveRequests("Expo Drive").Count);
    }

    [TestMethod]
    public async Task SubmitAsync_OnlyFlagsExactDuplicateRequests()
    {
        var service = new WaitlistRequestService();
        var draft = CreateDraft();
        var differentDetailDraft = new WaitlistRequestDraft
        {
            Building = draft.Building,
            WorkCenter = draft.WorkCenter,
            RequestType = draft.RequestType,
            Subtype = draft.Subtype,
            InputValue = "Different reason for the same request",
            ActiveSetupJobId = draft.ActiveSetupJobId,
            WorkstationName = draft.WorkstationName,
            RequesterEmployeeNumber = draft.RequesterEmployeeNumber,
            RequesterEmployeeName = draft.RequesterEmployeeName,
            RequestedUtc = DateTimeOffset.UtcNow,
            TargetTimeUtc = draft.TargetTimeUtc,
            IsOverdue = draft.IsOverdue,
            AssignedMaterialHandler = draft.AssignedMaterialHandler,
        };

        var firstResult = await service.SubmitAsync(draft, allowDuplicate: false);
        var secondResult = await service.SubmitAsync(differentDetailDraft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, firstResult.Status);
        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, secondResult.Status);
        Assert.AreEqual(2, service.GetActiveRequests("Expo Drive").Count);
    }

    [TestMethod]
    public async Task SubmitAsync_RejectsIncompleteDraftAsync()
    {
        var service = new WaitlistRequestService();

        var result = await service.SubmitAsync(new WaitlistRequestDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, result.Status);
        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task SubmitAsync_ReturnsPersistenceFailureWhenProductionBackendIsUnavailableAsync()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.InforVisualMockData"] = false,
            ["Feature.RecvMockData"] = false,
        });
        var sampleDataService = new SampleDataService(settings);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        var service = new WaitlistRequestService(settings, sampleDataService, mySqlHelperServer);

        var result = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.PersistenceFailure, result.Status);
        Assert.AreEqual("Production waitlist persistence is not configured or failed. Re-check the helper-server route and database contract.", result.Message);
        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task SubmitAsync_PreservesActiveJobAndRequesterMetadataAsync()
    {
        var service = new WaitlistRequestService();
        var targetTime = DateTimeOffset.UtcNow.AddMinutes(15);
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Coil",
            Subtype = "Wrong Coil",
            InputValue = "Wrong material at press",
            ActiveSetupJobId = "JOB-1001",
            WorkstationName = "Press 12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            TargetTimeUtc = targetTime,
            IsOverdue = false,
            AssignedMaterialHandler = "M. Lewis",
            CancellationReason = null,
        };

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, result.Status);
        Assert.IsNotNull(result.Request);
        Assert.AreEqual("JOB-1001", result.Request.ActiveSetupJobId);
        Assert.AreEqual("Press 12", result.Request.WorkstationName);
        Assert.AreEqual("6229", result.Request.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", result.Request.RequesterEmployeeName);
        Assert.AreEqual(targetTime, result.Request.TargetTimeUtc);
        Assert.IsFalse(result.Request.IsOverdue);
        Assert.AreEqual("M. Lewis", result.Request.AssignedMaterialHandler);
        Assert.IsNull(result.Request.CancellationReason);
    }

    [TestMethod]
    public async Task SubmitAsync_RequiresActiveSetupJobForSubmissionAsync()
    {
        var service = new WaitlistRequestService();
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Coil",
            Subtype = "Wrong Coil",
            InputValue = "Wrong material at press",
            ActiveSetupJobId = string.Empty,
            WorkstationName = "Press 12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var result = await service.SubmitAsync(draft, allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, result.Status);
        Assert.AreEqual("A valid active setup job is required before submitting a waitlist request.", result.Message);
    }

    [TestMethod]
    public void NewRequestFlowRules_VerifyEmployeeIdentity_ReturnsActiveEmployeeResult()
    {
        var result = NewRequestFlowRules.VerifyEmployeeIdentity("6229");

        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual("6229", result.EmployeeNumber);
        Assert.AreEqual("John Koll", result.EmployeeName);
    }

    [TestMethod]
    public void NewRequestFlowRules_VerifyEmployeeIdentity_RejectsInactiveOrUnknownEmployee()
    {
        var unknownResult = NewRequestFlowRules.VerifyEmployeeIdentity("999999");
        var inactiveResult = NewRequestFlowRules.VerifyEmployeeIdentity("0000");

        Assert.IsFalse(unknownResult.IsValid);
        Assert.IsFalse(inactiveResult.IsValid);
        Assert.IsFalse(inactiveResult.IsActive);
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateSelectedWorkCenter_RequiresActiveSelection()
    {
        var validResult = NewRequestFlowRules.ValidateSelectedWorkCenter("Press 12");
        var invalidResult = NewRequestFlowRules.ValidateSelectedWorkCenter(string.Empty);
        var blockedResult = NewRequestFlowRules.ValidateSelectedWorkCenter("No active job");

        Assert.IsTrue(validResult.IsValid);
        Assert.IsFalse(invalidResult.IsValid);
        Assert.IsFalse(blockedResult.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_FilterRequestTypesForActiveJob_HidesUnavailableMaterialTypes()
    {
        var requestTypes = new List<NewRequestTypeDefinition>
        {
            new() { RequestType = "Coil" },
            new() { RequestType = "Flatstock" },
            new() { RequestType = "Pickup" },
            new() { RequestType = "Other" },
        };

        var filtered = NewRequestFlowRules.ApplyActiveJobEligibility(requestTypes, hasCoilData: false, hasFlatstockData: true, hasPartData: true, hasWorkOrderData: true);

        Assert.AreEqual(3, filtered.Count);
        Assert.IsFalse(filtered.Any(item => string.Equals(item.RequestType, "Coil", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(filtered.Any(item => string.Equals(item.RequestType, "Flatstock", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(filtered.Any(item => string.Equals(item.RequestType, "Pickup", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateCurrentJobState_RequiresRestart_WhenWorkCenterIsNoLongerActive()
    {
        var valid = NewRequestFlowRules.ValidateCurrentJobState("Press 12", "JOB-1001");
        var stale = NewRequestFlowRules.ValidateCurrentJobState("No active job", "JOB-1001");
        var missingJob = NewRequestFlowRules.ValidateCurrentJobState("Press 12", string.Empty);

        Assert.IsTrue(valid.IsValid);
        Assert.IsFalse(stale.IsValid);
        Assert.IsFalse(missingJob.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidateActiveJobsForWorkCenter_RejectsMultipleActiveJobs()
    {
        var valid = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", new[] { "JOB-1001" });
        var multiple = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", new[] { "JOB-1001", "JOB-1002" });
        var missing = NewRequestFlowRules.ValidateActiveJobsForWorkCenter("Press 12", Array.Empty<string>());

        Assert.IsTrue(valid.IsValid);
        Assert.IsFalse(multiple.IsValid);
        Assert.IsFalse(missing.IsValid);
    }

    [TestMethod]
    public void NewRequestFlowRules_ShouldShowIntermediateSummary_ForNoSubtypeFlowsOnly()
    {
        var noSubtype = new NewRequestTypeDefinition { RequestType = "Pickup" };
        var withSubtype = new NewRequestTypeDefinition
        {
            RequestType = "Other",
            Subtypes =
            [
                new NewRequestSubtypeDefinition { Name = "General Text Entry" }
            ],
        };

        Assert.IsTrue(NewRequestFlowRules.ShouldShowIntermediateSummary(noSubtype, null));
        Assert.IsFalse(NewRequestFlowRules.ShouldShowIntermediateSummary(withSubtype, withSubtype.Subtypes[0]));
    }

    [TestMethod]
    public void NewRequestFlowRules_ValidatesWorkCenterSelectionAndSubtypeTextWorkflowSteps()
    {
        var validSelection = NewRequestFlowRules.ValidateSelectedWorkCenter("Press 12");
        var blockedSelection = NewRequestFlowRules.ValidateSelectedWorkCenter("No active job");

        var types = new List<NewRequestTypeDefinition>
        {
            new() { RequestType = "Pickup", Subtypes = new List<NewRequestSubtypeDefinition>() },
            new()
            {
                RequestType = "Other",
                Subtypes =
                [
                    new NewRequestSubtypeDefinition { Name = "General Text Entry", RequiresTextInput = true, PromptText = "Enter a short description", MinLength = 5, MaxLength = 200 },
                ],
            },
        };

        var filtered = NewRequestFlowRules.ApplyActiveJobEligibility(types, hasCoilData: true, hasFlatstockData: true, hasPartData: true, hasWorkOrderData: true);
        Assert.IsTrue(validSelection.IsValid);
        Assert.IsFalse(blockedSelection.IsValid);
        Assert.IsTrue(filtered.Any(item => item.RequestType.Equals("Pickup", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(filtered.Any(item => item.RequestType.Equals("Other", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(NewRequestFlowRules.ShouldShowIntermediateSummary(types[0], null));
        Assert.IsFalse(NewRequestFlowRules.ShouldShowIntermediateSummary(types[1], types[1].Subtypes[0]));
    }

    [TestMethod]
    public void NewRequestFlowRules_UsesTextInputRules_ForForkliftAndGeneralTextCases()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "waitlist-request-types.json"));
        var definitions = NewRequestFlowRules.ParseRequestTypes(json);

        var otherType = definitions.Single(item => string.Equals(item.RequestType, "Other", StringComparison.OrdinalIgnoreCase));
        var generalTextSubtype = otherType.Subtypes.Single(item => string.Equals(item.Name, "General Text Entry", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(generalTextSubtype.RequiresTextInput);
        Assert.AreEqual("Enter a short description", generalTextSubtype.PromptText);
        Assert.AreEqual(5, generalTextSubtype.MinLength);
        Assert.AreEqual(200, generalTextSubtype.MaxLength);

        var forkliftAssist = definitions.Single(item => string.Equals(item.RequestType, "Forklift Assist", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(forkliftAssist.RequiresTextInput);
        Assert.AreEqual("Enter description of why you need assistance", forkliftAssist.PromptText);
        Assert.AreEqual(5, forkliftAssist.MinLength);
        Assert.AreEqual(50, forkliftAssist.MaxLength);
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_WithSpecificSubtypeRules_ForPickupWrongCoilAndScrapEmpty()
    {
        var wrongCoil = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Coil",
            Subtype = "Wrong Coil",
            InputValue = "Wrong material at press",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var wrongCoilOrder = WaitlistViewViewModel.CreateSessionOrder(wrongCoil);
        Assert.AreEqual("Wrong coil", wrongCoilOrder.Fields.First(item => item.Label == "Requested coil").Value);

        var pickupOther = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Pickup",
            Subtype = "Pickup Other",
            InputValue = "Need an outside service",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(12),
            IsOverdue = false,
        };

        var pickupOtherOrder = WaitlistViewViewModel.CreateSessionOrder(pickupOther);
        Assert.AreEqual("Pickup Other", pickupOtherOrder.Fields.First(item => item.Label == "Subtype").Value);

        var scrapEmpty = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Scrap",
            Subtype = "Empty",
            InputValue = "Scrap cart empty",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(6),
            IsOverdue = false,
        };

        var scrapOrder = WaitlistViewViewModel.CreateSessionOrder(scrapEmpty);
        Assert.AreEqual("Not selected", scrapOrder.Fields.First(item => item.Label == "Scrap lugger").Value);
    }

    [TestMethod]
    public void WaitlistViewViewModel_PadsSessionOrderFields_ToFiveCardSlots()
    {
        var forklift = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Forklift Assist",
            InputValue = "HELP ME!!!",
            Status = "Pending",
        };
        var flatstock = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Flatstock",
            Status = "Pending",
        };
        var other = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Other",
            Subtype = "General Text Entry",
            InputValue = "Please assist",
            Status = "Pending",
        };
        var pickupOther = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Pickup",
            Subtype = "Pickup Other",
            Status = "Pending",
        };

        Assert.IsTrue(WaitlistViewViewModel.CreateSessionOrder(forklift).Fields.Count >= 5);
        Assert.IsTrue(WaitlistViewViewModel.CreateSessionOrder(flatstock).Fields.Count >= 5);
        Assert.IsTrue(WaitlistViewViewModel.CreateSessionOrder(other).Fields.Count >= 5);
        Assert.IsTrue(WaitlistViewViewModel.CreateSessionOrder(pickupOther).Fields.Count >= 5);
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_WithSubtypeSpecificPickupFgFields()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Pickup",
            Subtype = "Pickup FG",
            InputValue = "Finished goods request",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual("FG-10042", order.Fields.First(item => item.Label == "Part number").Value);
        Assert.AreEqual("Finished bracket assembly", order.Fields.First(item => item.Label == "Part description").Value);
        Assert.AreEqual("24 each", order.Fields.First(item => item.Label == "Quantity remaining").Value);
        Assert.AreEqual("Northstar Manufacturing", order.Fields.First(item => item.Label == "Customer").Value);
        Assert.AreEqual("PL-80421", order.Fields.First(item => item.Label == "Packlist").Value);
    }

    [TestMethod]
    public void WaitlistViewViewModel_CreatesSessionOrder_WithProperRemainingTimeAndOverdueState()
    {
        var request = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Coil",
            Subtype = "Wrong Coil",
            InputValue = "Wrong material at press",
            Status = "Pending",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(8),
            IsOverdue = false,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual("00:08", order.RemainingTimeText);
        Assert.IsFalse(order.IsOverdue);

        var overdueRequest = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Other",
            InputValue = "Late submission",
            Status = "Accepted",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
            IsOverdue = true,
        };

        var overdueOrder = WaitlistViewViewModel.CreateSessionOrder(overdueRequest);

        Assert.AreEqual("Overdue", overdueOrder.RemainingTimeText);
        Assert.IsTrue(overdueOrder.IsOverdue);
    }

    [TestMethod]
    public void WaitlistViewViewModel_OverdueRequestsRemainActiveInTheActiveList()
    {
        var activeOverdue = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            RequestType = "Pickup",
            InputValue = "Late demand",
            Status = "Accepted",
            TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsOverdue = true,
        };

        var order = WaitlistViewViewModel.CreateSessionOrder(activeOverdue);
        Assert.AreEqual("Overdue", order.RemainingTimeText);
        Assert.IsTrue(order.IsOverdue);
        Assert.AreEqual("Accepted", order.Status);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_ChangesPendingToAcceptedCompletedAndCanceled()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.TransitionStatusAsync(request.Id, "Accepted");
        var acceptedRow = service.GetActiveRequests("Expo Drive").FirstOrDefault(item => item.Id == request.Id);

        Assert.IsTrue(accepted);
        Assert.IsNotNull(acceptedRow);
        Assert.AreEqual("Accepted", acceptedRow!.Status);

        var completed = await service.TransitionStatusAsync(request.Id, "Completed");
        var completedRow = service.GetActiveRequests("Expo Drive").FirstOrDefault(item => item.Id == request.Id);
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed");

        Assert.IsTrue(completed);
        Assert.IsNull(completedRow);
        Assert.IsFalse(canceled);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_WhenCanceled_RecordsCancellationMetadata()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        var accepted = await service.TransitionStatusAsync(request.Id, "Accepted");
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        Assert.IsTrue(accepted);
        Assert.IsTrue(canceled);
        Assert.AreEqual(0, service.GetActiveRequests("Expo Drive").Count(item => item.Id == request.Id));

        var updated = service.GetRequest(request.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Canceled", updated!.Status);
        Assert.AreEqual("No longer needed", updated.CancellationReason);
        Assert.IsNotNull(updated.CanceledUtc);
        Assert.AreEqual("6229", updated.CanceledByEmployeeNumber);
    }

    [TestMethod]
    public async Task TransitionStatusAsync_WhenCanceled_RecordsMaterialHandlerNotificationPayload()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        await service.TransitionStatusAsync(request.Id, "Accepted");
        var canceled = await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        Assert.IsTrue(canceled);
        var updated = service.GetRequest(request.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Canceled", updated!.Status);
        Assert.AreEqual("No longer needed", updated.CancellationReason);
        Assert.AreEqual("6229", updated.CanceledByEmployeeNumber);
        Assert.IsNotNull(updated.CanceledUtc);
    }

    [TestMethod]
    public async Task RequestsChanged_IsRaised_WhenRequestIsSubmittedOrStatusTransitions()
    {
        var service = new WaitlistRequestService();
        var changeCount = 0;
        service.RequestsChanged += (_, _) => changeCount++;

        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        Assert.AreEqual(1, changeCount);

        _ = await service.TransitionStatusAsync(request.Id, "Accepted");
        Assert.AreEqual(2, changeCount);
    }

    [TestMethod]
    public async Task GetAuditTrail_RecordsRequestCreationAndLifecycleTransitions()
    {
        var service = new WaitlistRequestService();
        var submitResult = await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        var request = submitResult.Request!;

        await service.TransitionStatusAsync(request.Id, "Accepted");
        await service.TransitionStatusAsync(request.Id, "Canceled", "No longer needed", "6229");

        var auditTrail = service.GetAuditTrail(request.Id);
        Assert.AreEqual(3, auditTrail.Count);
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Created"));
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Accepted"));
        Assert.IsTrue(auditTrail.Any(item => item.EventType == "Canceled"));
    }

    [TestMethod]
    public async Task WaitlistViewViewModel_IgnoresStaleRefreshResults_WhenBuildingChanges()
    {
        var buildingSelectionService = new StubBuildingSelectionService("Expo Drive");
        var requestService = new WaitlistRequestService();
        var sampleDataService = new DelayedSampleDataService();
        var viewModel = new WaitlistViewViewModel(new NoOpNavigationService(), sampleDataService, buildingSelectionService, requestService);

        var expoTask = InvokeLoad(viewModel, "Expo Drive");
        await Task.Delay(25);
        buildingSelectionService.SelectedBuilding = "VITS";
        var vitsTask = InvokeLoad(viewModel, "VITS");

        await Task.WhenAll(expoTask, vitsTask);

        Assert.AreEqual(1, viewModel.Source.Count);
        Assert.AreEqual("VITS request", viewModel.Source[0].Title);
    }

    [TestMethod]
    public async Task WaitlistViewViewModel_RefreshesActiveList_WhenRequestIsSubmitted()
    {
        var buildingSelectionService = new StubBuildingSelectionService("Expo Drive");
        var requestService = new WaitlistRequestService();
        var sampleDataService = new DelayedSampleDataService();
        var viewModel = new WaitlistViewViewModel(new NoOpNavigationService(), sampleDataService, buildingSelectionService, requestService);

        // The view model only refreshes on RequestsChanged once it subscribes in
        // OnNavigatedTo; without this it never picks up the submitted request.
        viewModel.OnNavigatedTo(null!);
        await viewModel.RefreshAsync();
        var submitResult = await requestService.SubmitAsync(CreateDraft(), allowDuplicate: false);
        await Task.Delay(50);

        Assert.AreEqual(WaitlistRequestSubmitStatus.Success, submitResult.Status);
        Assert.IsTrue(viewModel.Source.Any(item => item.Title.Contains("Wrong Coil", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task Reset_ClearsSessionRequestsAsync()
    {
        var service = new WaitlistRequestService();

        await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        service.Reset();

        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    private static async Task InvokeLoad(WaitlistViewViewModel viewModel, string building)
    {
        await viewModel.RefreshAsync();
        await Task.Delay(10);
    }

    private static WaitlistRequestDraft CreateDraft() => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Press 12",
        RequestType = "Coil",
        Subtype = "Wrong Coil",
        InputValue = "Wrong material at press",
        ActiveSetupJobId = "JOB-1001",
        WorkstationName = "Press 12",
        RequesterEmployeeNumber = "6229",
        RequesterEmployeeName = "John Koll",
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        IsOverdue = false,
        AssignedMaterialHandler = "M. Lewis",
    };

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public StubBuildingSelectionService(string selectedBuilding)
        {
            SelectedBuilding = selectedBuilding;
        }

        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<string> Buildings => new[] { "Expo Drive", "VITS" };

        public string SelectedBuilding { get; set; }
    }

    private sealed class DelayedSampleDataService : ISampleDataService
    {
        public IReadOnlyList<object> GetSampleOrders(string? building = null)
        {
            var normalized = building ?? string.Empty;
            var title = normalized.Equals("VITS", StringComparison.OrdinalIgnoreCase) ? "VITS request" : "Expo request";
            return new object[]
            {
                new SampleOrder
                {
                    Id = normalized.Equals("VITS", StringComparison.OrdinalIgnoreCase) ? 101 : 100,
                    Title = title,
                    RequestedByName = "Current user",
                    RequestedPressName = normalized,
                    RemainingTimeText = "00:05",
                    IsOverdue = false,
                }
            };
        }
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return Task.FromResult((T?)value);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}