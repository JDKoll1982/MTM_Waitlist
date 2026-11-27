using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What a raised generic die request records, and that several chosen dies raise several requests
/// (020-generic-wc-die-search FR-011, FR-012).
/// </summary>
/// <remarks>
/// The request's one value column holds one die, so several dies can never be folded into one entry: each chosen die
/// is its own request, and each carries the die's own answer so its card can name the die and where it lives.
/// </remarks>
[TestClass]
public sealed class GenericRequestDieSubmissionTests
{
    [TestMethod]
    public async Task Service_GenericRequestDieSubmissionTests_SubmitAsync_ThreeChosenDiesRaiseThreeRequests()
    {
        var requests = new RecordingGenericRequestSubmitter();
        var data = CreateLive(requests);

        var result = await data.SubmitAsync(DieDraft(
            Die("FGT0602-01", "U-B0-01"),
            Die("FGT0602-02", "U-B0-02"),
            Die("FGT0603-01", "U-B1-01")));

        Assert.IsTrue(result.IsRaised, result.FailureMessage);
        Assert.AreEqual(3, result.RaisedCount, "one request per chosen die (FR-011).");
        Assert.AreEqual(3, requests.Submitted.Count);
        CollectionAssert.AreEqual(
            new[] { "FGT0602-01", "FGT0602-02", "FGT0603-01" },
            requests.Submitted.Select(draft => draft.InputValue).ToArray(),
            "each request's own value is the die it is for, which is what keeps them apart on the queue");
    }

    [TestMethod]
    public async Task Service_GenericRequestDieSubmissionTests_SubmitAsync_EachRequestRecordsItsOwnDieAndLocation()
    {
        var requests = new RecordingGenericRequestSubmitter();
        var data = CreateLive(requests);

        await data.SubmitAsync(DieDraft(Die("FGT0602-01", "U-B0-01"), Die("FGT0602-02", "U-B0-02")));

        var first = GenericRequestDieAnswer.TryRead(requests.Submitted[0].Answers);
        var second = GenericRequestDieAnswer.TryRead(requests.Submitted[1].Answers);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreEqual("FGT0602-01", first!.DieNumber);
        Assert.AreEqual("U-B0-01", first.DieLocation);
        Assert.AreEqual("FGT0602-02", second!.DieNumber, "each request names its own die, not the first one (FR-012).");
        Assert.AreEqual("U-B0-02", second.DieLocation);
    }

    [TestMethod]
    public async Task Service_GenericRequestDieSubmissionTests_SubmitAsync_ADieWithNoLocationRecordsNoLocationKey()
    {
        var requests = new RecordingGenericRequestSubmitter();
        var data = CreateLive(requests);

        await data.SubmitAsync(DieDraft(Die("FGT0602-01", string.Empty)));

        var recorded = requests.Submitted[0].Answers;

        Assert.IsNotNull(recorded);
        Assert.IsTrue(recorded!.ContainsKey(GenericRequestDieAnswer.DieNumberKey));
        Assert.IsFalse(
            recorded.ContainsKey(GenericRequestDieAnswer.DieLocationKey),
            "an unknown location is stored as no answer, so the card can tell absent from empty (FR-006).");
    }

    [TestMethod]
    public async Task Service_GenericRequestDieSubmissionTests_SubmitAsync_OneChosenDieStillRaisesOneRequest()
    {
        var requests = new RecordingGenericRequestSubmitter();
        var data = CreateLive(requests);

        var result = await data.SubmitAsync(DieDraft(Die("FGT0602-01", "U-B0-01")));

        Assert.AreEqual(1, result.RaisedCount, "the ordinary single-request path is unchanged (FR-011).");
        Assert.AreEqual(1, requests.Submitted.Count);
    }

    private static GenericRequestDieOption Die(string number, string location) =>
        new(number, "DIE", location);

    private static GenericRequestDraft DieDraft(params GenericRequestDieOption[] dies)
    {
        var draft = new GenericRequestDraft
        {
            WorkCenter = "WC-1",
            Building = "B-1",
            Category = "Pickup",
            ItemCode = "pickup-die",
            ItemName = "Die",
            RequiresDie = true,
        };

        draft.SelectedDies.AddRange(dies);
        draft.DieNumber = dies.Length == 0 ? string.Empty : dies[0].DieNumber;

        return draft;
    }

    private static GenericRequestLiveStepData CreateLive(IGenericRequestSubmitter requests) =>
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
            new StubGenericRequestDieData(),
            new FakeDunnageCatalogueReader(),
            new FakeLuggerArrangementService());
}
