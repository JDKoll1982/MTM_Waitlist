using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The one mapping between a generic request and the queue's draft (024-dependency-modernization, MOD-007).
/// </summary>
/// <remarks>
/// The mapping is the whole of this seam and the only place a captured answer can quietly stop being written, so it is
/// asserted field by field against the request the queue actually holds rather than against the seam's own record. The
/// interactive pass over the flow needs a reachable store, so this is what proves the raise still carries everything.
/// </remarks>
[TestClass]
public sealed class GenericRequestSubmitterTests
{
    [TestMethod]
    public async Task Service_GenericRequestSubmitterTests_SubmitAsync_EveryCapturedValueReachesTheQueue()
    {
        var queue = new WaitlistRequestService();
        var submitter = new GenericRequestSubmitter(queue);
        var requestedUtc = DateTimeOffset.UtcNow;

        var result = await submitter.SubmitAsync(new GenericRequestSubmitRequest
        {
            Building = "Expo Drive",
            WorkCenter = "WC-1",
            Category = "Scrap",
            Item = "pickup-scrap",
            InputValue = "Galvanized Steel",
            Answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scrap.type"] = "Galvanized Steel",
            },
            ProvenanceWorkOrder = "WO-9001",
            Description = "The skid is full.",
            AcceptedWarnings = (string[])["The die is shared."],
            WorkCenterName = "PC-1",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            RequestedUtc = requestedUtc,
            IsOverdue = false,
        });

        Assert.AreEqual(GenericRequestSubmitStatus.Raised, result.Status, result.Message);
        Assert.IsTrue(result.RequestId is { } id && id != Guid.Empty, "a raised request names the row it created.");

        var recorded = queue.GetActiveRequests().Single();

        Assert.AreEqual(recorded.Id, result.RequestId, "the identifier handed back is the row the queue holds.");
        Assert.AreEqual("Expo Drive", recorded.Building);
        Assert.AreEqual("WC-1", recorded.WorkCenter);
        Assert.AreEqual("Scrap", recorded.Category);
        Assert.AreEqual("pickup-scrap", recorded.Item);
        Assert.AreEqual("Galvanized Steel", recorded.InputValue);
        Assert.AreEqual("Galvanized Steel", recorded.Answers?["scrap.type"], "the captured answers ride the answers column.");
        Assert.AreEqual("WO-9001", recorded.ProvenanceWorkOrder, "the job a filled request came from is provenance.");
        Assert.AreEqual("The skid is full.", recorded.Description);
        CollectionAssert.AreEqual(new[] { "The die is shared." }, recorded.AcceptedWarnings?.ToArray());
        Assert.AreEqual("PC-1", recorded.WorkCenterName);
        Assert.AreEqual("6229", recorded.RequesterEmployeeNumber);
        Assert.AreEqual("John Koll", recorded.RequesterEmployeeName);
        Assert.AreEqual(requestedUtc, recorded.RequestedUtc);

        // A generic request has no job and no work order anywhere: the flag is set for it, never a blank standing in.
        Assert.IsTrue(recorded.IsGeneric, "the mapping always raises a generic request.");
        Assert.AreEqual(string.Empty, recorded.ActiveSetupJobId, "a generic request never carries a job as its identity.");
        Assert.IsNull(recorded.TargetTimeUtc, "the store derives the due time from the request's own creation time.");
    }

    [TestMethod]
    public async Task Service_GenericRequestSubmitterTests_SubmitAsync_AQueueRefusalComesBackAsADuplicateWithTheQueuesWords()
    {
        var queue = new WaitlistRequestService();
        var submitter = new GenericRequestSubmitter(queue);
        var request = new GenericRequestSubmitRequest
        {
            Building = "Expo Drive",
            WorkCenter = "WC-1",
            Category = "Scrap",
            Item = "pickup-scrap",
            InputValue = "Steel",
            WorkCenterName = "PC-1",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
        };

        await submitter.SubmitAsync(request);
        var second = await submitter.SubmitAsync(request);

        Assert.AreEqual(
            GenericRequestSubmitStatus.DuplicateWarningRequired,
            second.Status,
            "the second identical request is refused by the queue, not raised twice.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(second.Message),
            "the queue's own sentence is what the screen shows, so it survives the mapping.");
        Assert.IsNull(second.RequestId, "a refusal raises nothing, so it names nothing.");
    }
}
