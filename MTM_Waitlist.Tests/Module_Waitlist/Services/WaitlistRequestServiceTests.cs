using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

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
    public async Task SubmitAsync_RejectsIncompleteDraftAsync()
    {
        var service = new WaitlistRequestService();

        var result = await service.SubmitAsync(new WaitlistRequestDraft(), allowDuplicate: false);

        Assert.AreEqual(WaitlistRequestSubmitStatus.ValidationFailure, result.Status);
        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    [TestMethod]
    public async Task Reset_ClearsSessionRequestsAsync()
    {
        var service = new WaitlistRequestService();

        await service.SubmitAsync(CreateDraft(), allowDuplicate: false);
        service.Reset();

        Assert.AreEqual(0, service.GetActiveRequests().Count);
    }

    private static WaitlistRequestDraft CreateDraft() => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Press 12",
        RequestType = "Coil",
        Subtype = "Wrong Coil",
        InputValue = "Wrong material at press",
    };
}