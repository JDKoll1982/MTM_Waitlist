using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist;

/// <summary>
/// The two surfaces a filled request is read on: its detail page, which names the job it was filled from, and its
/// card, which says nothing about a job (021 FR-012, FR-013).
/// </summary>
/// <remarks>
/// A filled request is still a generic request, so the card draws the generic one and carries no work-order cell.
/// The job appears on the detail page as provenance, beside the work order rather than in its place, and a request no
/// job filled draws neither.
/// </remarks>
[TestClass]
public sealed class WaitlistDetailJobFillProvenanceTests
{
    [TestMethod]
    public async Task ViewModel_WaitlistDetailJobFillProvenanceTests_OnNavigatedTo_AFilledRequestNamesTheJobItWasFilledFrom()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitAsync(requestService, provenanceWorkOrder: "WO-9001");

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(WaitlistViewViewModel.CreateSessionOrder(request).Id);

        var section = viewModel.TemplateSections.Single(candidate => candidate.Title == "Request");
        var provenanceLabel = "Waitlist_Detail_JobFill_Provenance.Text".GetLocalized();

        Assert.AreEqual(
            "WO-9001",
            SectionField(section, provenanceLabel),
            "the job is drawn as provenance on the request page (FR-013)");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistDetailJobFillProvenanceTests_OnNavigatedTo_ARequestNoJobFilledDrawsNoProvenanceRow()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitAsync(requestService, provenanceWorkOrder: null);

        var viewModel = new WaitlistViewDetailViewModel(
            new RecordingNavigationService(),
            new StubBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(WaitlistViewViewModel.CreateSessionOrder(request).Id);

        var section = viewModel.TemplateSections.Single(candidate => candidate.Title == "Request");
        var provenanceLabel = "Waitlist_Detail_JobFill_Provenance.Text".GetLocalized();

        Assert.IsTrue(
            section.Fields.All(field => field.Label != provenanceLabel),
            "a request no job filled says nothing about a job");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistDetailJobFillProvenanceTests_CreateSessionOrder_AFilledGenericRequestDrawsNoWorkOrderCell()
    {
        var requestService = new WaitlistRequestService();
        var request = await SubmitAsync(requestService, provenanceWorkOrder: "WO-9001");

        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.IsTrue(request.IsGeneric, "the request stays generic with a job behind it (FR-011)");
        Assert.IsTrue(
            order.Cells.All(cell => !cell.Label.Contains("Work order", StringComparison.OrdinalIgnoreCase)),
            "the generic card carries no work-order cell, filled or not (FR-012)");
    }

    private static string? SectionField(WaitlistDetailTemplateSection section, string label) =>
        section.Fields.FirstOrDefault(field => field.Label == label)?.Value;

    private static async Task<WaitlistRequest> SubmitAsync(WaitlistRequestService requestService, string? provenanceWorkOrder)
    {
        var draft = new WaitlistRequestDraft
        {
            Building = "Expo Drive",
            WorkCenter = "WC-1",
            Category = RequestCategory.Pickup.ToString(),
            Item = "pickup-coil",
            InputValue = "COIL-204",
            IsGeneric = true,
            ProvenanceWorkOrder = provenanceWorkOrder,
            WorkCenterName = "PC-1",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        var submit = await requestService.SubmitAsync(draft);
        Assert.IsNotNull(submit.Request, submit.Message);

        return submit.Request!;
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? BuildingsChanged
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<string> Buildings => ["Expo Drive"];

        public string SelectedBuilding { get; set; } = "Expo Drive";
    }
}
