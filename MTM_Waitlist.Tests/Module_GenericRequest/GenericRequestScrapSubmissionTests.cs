using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What a raised generic scrap request records on itself.
/// </summary>
/// <remarks>
/// The stated type rides the request's existing answers column under the one <c>scrap.type</c> key, and the value
/// the request is about stays in <c>input_value</c>. Nothing in the schema changes, so these assertions are the
/// whole of the guarantee that a scrap request can name what it is for (FR-005, FR-011).
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapSubmissionTests
{
    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_AScrapRequestRecordsTheStatedTypeInItsOwnWording()
    {
        var data = CreateLive(out var requests);

        var result = await data.SubmitAsync(ScrapDraft(scrapType: "5052 Aluminum"));

        Assert.IsTrue(result.IsRaised, result.FailureMessage);
        Assert.AreEqual(1, requests.Submitted.Count);

        var recorded = GenericRequestScrapAnswer.TryRead(requests.Submitted[0].Answers);
        Assert.IsNotNull(recorded, "A scrap request records its stated type on its own answers.");
        Assert.AreEqual(
            "5052 Aluminum",
            recorded!.ScrapType,
            "The wording the requester chose is kept, not the catalogue's spelling (FR-011).");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_AScrapDraftWithNoStatedTypeRecordsNoScrapKey()
    {
        var data = CreateLive(out var requests);

        await data.SubmitAsync(ScrapDraft(scrapType: string.Empty));

        Assert.IsNull(
            GenericRequestScrapAnswer.TryRead(requests.Submitted[0].Answers),
            "A scrap request that stated no type records no scrap key at all.");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_AnItemThatAsksForNoScrapRecordsNoneEvenWithALeftoverType()
    {
        var data = CreateLive(out var requests);

        await data.SubmitAsync(ScrapDraft(scrapType: "Steel", requiresScrap: false));

        Assert.IsNull(
            GenericRequestScrapAnswer.TryRead(requests.Submitted[0].Answers),
            "An item that asks for no scrap records no scrap answer, even when a type is left on the draft.");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapSubmissionTests_InputValueStillCarriesTheStatedAnswerNotTheScrapType()
    {
        var data = CreateLive(out var requests);

        await data.SubmitAsync(ScrapDraft());

        var submitted = requests.Submitted[0];
        Assert.AreEqual(
            "the stated answer",
            submitted.InputValue,
            "The scrap type is not stored in input_value; the value the request is about is.");
    }

    private static GenericRequestLiveStepData CreateLive(out RecordingWaitlistRequestService requests)
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
            new FakeDunnageCatalogueReader(),
            new FakeLuggerArrangementService());
    }

    private static GenericRequestDraft ScrapDraft(string scrapType = "5052 Aluminum", bool requiresScrap = true) =>
        new()
        {
            WorkCenter = "WC-1",
            Building = "B-1",
            Category = "Scrap",
            ItemCode = "pickup-scrap",
            ItemName = "Pickup Scrap",
            RequiresScrap = requiresScrap,
            ScrapType = scrapType,
            Answer = "the stated answer",
        };
}
