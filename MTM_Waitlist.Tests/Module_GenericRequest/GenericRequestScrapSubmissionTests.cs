using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What a raised generic scrap request records on itself, and that the step needs no job to be answerable.
/// </summary>
/// <remarks>
/// A generic work center has no job, so the stated type is the only thing that decides where the scrap goes. It rides
/// the request's existing answers column, which is what lets the card name it after a restart (FR-001, FR-005).
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapSubmissionTests
{
    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_SubmitAsync_AScrapRequestRecordsTheStatedType()
    {
        var requests = new RecordingWaitlistRequestService();
        var data = CreateLive(requests);

        var result = await data.SubmitAsync(ScrapDraft("Galvanized Steel"));

        Assert.IsTrue(result.IsRaised, result.FailureMessage);
        Assert.AreEqual(1, requests.Submitted.Count);

        var recorded = GenericRequestScrapAnswer.TryRead(requests.Submitted[0].Answers);
        Assert.IsNotNull(recorded, "The card can only name the type if the request kept it (FR-005).");
        Assert.AreEqual("Galvanized Steel", recorded!.ScrapType);
        Assert.AreEqual(
            "Galvanized Steel",
            requests.Submitted[0].InputValue,
            "The stated type is what this request is about, so it is the detail a second request is told apart by (FR-005, FR-013).");
    }

    /// <summary>
    /// Two scrap requests for two different types at one work center are two requests, not one raised twice.
    /// </summary>
    /// <remarks>
    /// The queue refuses a second active request when its building, work center, item <b>and detail</b> all match.
    /// A scrap request that left the detail empty collided with every other scrap request at its work center, so the
    /// second was refused as a duplicate and the review sat in its failed state. Each type is its own pickup, with
    /// its own lugger (FR-013).
    /// </remarks>
    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_SubmitAsync_ASecondScrapTypeAtTheSameWorkCenterIsNotADuplicate()
    {
        var queue = new WaitlistRequestService();
        var data = CreateLive(queue);

        var first = await data.SubmitAsync(ScrapDraft("3003 Aluminum"));
        var second = await data.SubmitAsync(ScrapDraft("Steel"));

        Assert.IsTrue(first.IsRaised, first.FailureMessage);
        Assert.IsTrue(
            second.IsRaised,
            $"Two scrap types are two pickups, each with its own lugger (FR-013). The queue said: {second.FailureMessage}");
        Assert.AreEqual(
            2,
            queue.GetActiveRequests().Count,
            "both requests reached the queue rather than being folded into one");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_SubmitAsync_AQueueRefusalIsReportedAsADuplicateAndNotAFault()
    {
        var queue = new RecordingWaitlistRequestService
        {
            SubmitResult = _ => WaitlistRequestSubmitResult.DuplicateWarning(new WaitlistRequest
            {
                Id = Guid.NewGuid(),
                Building = "B-1",
                WorkCenter = "WC-1",
                Category = "Scrap",
                Item = "Scrap",
            }),
        };

        var data = CreateLive(queue);

        var result = await data.SubmitAsync(ScrapDraft("Steel"));

        Assert.IsFalse(result.IsRaised, "a refused request was not raised.");
        Assert.IsTrue(
            result.IsDuplicate,
            "A refusal is the queue answering, so the result step can show it as a duplicate rather than a fault (FR-019).");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(result.FailureMessage),
            "the queue's own sentence is what the screen shows, so it must survive the refusal.");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_ReadStepAvailabilityAsync_TheScrapStepIsAnswerableWithNoJobAnywhere()
    {
        var data = CreateLive(new RecordingWaitlistRequestService());

        var scrap = await data.ReadStepAvailabilityAsync(GenericRequestStep.Scrap);
        var die = await data.ReadStepAvailabilityAsync(GenericRequestStep.Die);

        Assert.AreEqual(
            GenericRequestPresentation.Populated,
            scrap.Presentation,
            "The scrap step's source is the plant's list, so it can be shown without a job read (FR-001).");
        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            die.Presentation,
            "The die step still waits on its own source, which this chunk does not build.");
    }

    private static GenericRequestLiveStepData CreateLive(IWaitlistRequestService requests) =>
        new(
            new StubWorkCenterCatalogService(),
            new StubNewRequestPickerService(),
            new StubRequestItemConfigurationService(),
            requests,
            new StubPersonIdentity(),
            new StubEmployeeDirectoryService
            {
                Found = new EmployeeIdentity { EmployeeNumber = "1234", DisplayName = "J. Koll", IsActive = true },
            },
            new StubGenericRequestPartData(),
            new FakeDunnageCatalogueReader(),
            new FakeLuggerArrangementService());

    private static GenericRequestDraft ScrapDraft(string scrapType) =>
        new()
        {
            WorkCenter = "WC-1",
            Building = "B-1",
            Category = "Scrap",
            ItemCode = "pickup-scrap",
            ItemName = "Scrap",
            RequiresScrap = true,
            ScrapType = scrapType,
        };
}
