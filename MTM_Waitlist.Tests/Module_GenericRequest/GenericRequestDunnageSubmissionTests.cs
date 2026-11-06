using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What a raised generic dunnage request records on itself.
/// </summary>
/// <remarks>
/// The card's second line names the dunnage part from <c>input_value</c>, and the picture, the type and whether the
/// part is still offered ride the request's existing answers column. Nothing in the schema changes, so these
/// assertions are the whole of the guarantee that a dunnage request can name what it is for (FR-005, FR-008,
/// FR-009).
/// </remarks>
[TestClass]
public sealed class GenericRequestDunnageSubmissionTests
{
    [TestMethod]
    public async Task Service_GenericRequestDunnageSubmissionTests_ADunnageRequestCarriesTheChosenPartAndItsFourAnswers()
    {
        var reader = new FakeDunnageCatalogueReader
        {
            Parts = [FakeDunnageCataloguePart.Of("DNG-1", typeName: "Rack")],
        };
        var data = CreateLive(reader, out var requests);

        var result = await data.SubmitAsync(DunnageDraft());

        Assert.IsTrue(result.IsRaised, result.FailureMessage);
        Assert.AreEqual(1, requests.Submitted.Count);

        var draft = requests.Submitted[0];
        Assert.AreEqual(
            "DNG-1",
            draft.InputValue,
            "The card's second line names the dunnage part from this column, so it has to hold the part.");

        var recorded = GenericRequestDunnageAnswer.TryRead(draft.Answers);
        Assert.IsNotNull(recorded, "The four dunnage answers ride the request's own answers.");
        Assert.AreEqual("DNG-1", recorded!.PartNumber);
        Assert.AreEqual("Rack", recorded.TypeName);
        Assert.IsTrue(recorded.Offered, "A part the catalogue still holds is recorded as offered.");
    }

    [TestMethod]
    public async Task Service_GenericRequestDunnageSubmissionTests_AnItemThatAsksForNoDunnageRecordsNeither()
    {
        var reader = new FakeDunnageCatalogueReader
        {
            Parts = [FakeDunnageCataloguePart.Of("DNG-1")],
        };
        var data = CreateLive(reader, out var requests);

        var draft = DunnageDraft();
        draft.RequiresDunnage = false;
        draft.Answer = "the stated answer";

        await data.SubmitAsync(draft);

        var submitted = requests.Submitted[0];
        Assert.AreEqual("the stated answer", submitted.InputValue, "An item that asks no dunnage keeps its own answer.");
        Assert.IsNull(
            GenericRequestDunnageAnswer.TryRead(submitted.Answers),
            "An item that asks for no dunnage records no dunnage at all.");
    }

    [TestMethod]
    public async Task Service_GenericRequestDunnageSubmissionTests_APartGoneFromTheCatalogueIsKeptAndRecordedAsNotOffered()
    {
        var reader = new FakeDunnageCatalogueReader
        {
            Parts = [FakeDunnageCataloguePart.Of("DNG-OTHER", typeName: "Tote")],
        };
        var data = CreateLive(reader, out var requests);

        await data.SubmitAsync(DunnageDraft("DNG-GONE"));

        var submitted = requests.Submitted[0];
        Assert.AreEqual(
            "DNG-GONE",
            submitted.InputValue,
            "The part the requester chose is kept rather than silently replaced by something else (FR-009).");

        var recorded = GenericRequestDunnageAnswer.TryRead(submitted.Answers);
        Assert.IsNotNull(recorded);
        Assert.AreEqual("DNG-GONE", recorded!.PartNumber);
        Assert.IsFalse(recorded.Offered, "A part the catalogue no longer holds is recorded as no longer offered.");
    }

    [TestMethod]
    public async Task Service_GenericRequestDunnageSubmissionTests_AReadThatFailsKeepsThePartAndDoesNotClaimItIsGone()
    {
        var reader = new FakeDunnageCatalogueReader { Failure = new InvalidOperationException("the store is down") };
        var data = CreateLive(reader, out var requests);

        await data.SubmitAsync(DunnageDraft());

        var recorded = GenericRequestDunnageAnswer.TryRead(requests.Submitted[0].Answers);
        Assert.IsNotNull(recorded);
        Assert.IsTrue(
            recorded!.Offered,
            "A read that failed proves nothing, so the part is not marked gone on the strength of an outage.");
    }

    private static GenericRequestLiveStepData CreateLive(
        FakeDunnageCatalogueReader reader,
        out RecordingWaitlistRequestService requests)
    {
        requests = new RecordingWaitlistRequestService();

        return new GenericRequestLiveStepData(
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
            reader,
            new FakeLuggerArrangementService());
    }

    private static GenericRequestDraft DunnageDraft(string partNumber = "DNG-1") =>
        new()
        {
            WorkCenter = "WC-1",
            Building = "B-1",
            Category = "Dunnage",
            ItemCode = "deliver-dunnage",
            ItemName = "Deliver Dunnage",
            RequiresDunnage = true,
            DunnageType = "Rack",
            DunnagePart = partNumber,
        };
}
