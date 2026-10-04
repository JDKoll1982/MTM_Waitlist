using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// What the seam captures rather than receives: the machine, the person and the fault are read at write time, so
/// a call site cannot omit them (FR-021, FR-034 to FR-036; contract §1.3).
/// </summary>
[TestClass]
public sealed class LogServiceTests
{
    [TestMethod]
    public async Task Error_MachinePersonAndFault_AreCapturedByTheSeamAndNotSuppliedByTheCallSite()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);

        var machine = new FakeMachineFacts
        {
            Hostname = "MTMFG-161",
            MacAddress = "aa-bb-cc-dd-ee-ff",
        };

        var person = new FakePersonIdentity
        {
            UserId = 7,
            SignInName = "JKoll",
            DisplayName = "John Koll",
            CurrentRoleCode = "developer",
        };

        var service = new LogService(machine, person, writer);

        // Act: the call site names the module, what happened, and the fault — nothing else.
        service.Error("Waitlist", "The request could not be saved.", new InvalidOperationException("the store refused the write"));

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, store.Writes.Count);
        var write = store.Writes[0];

        Assert.AreEqual("MTMFG-161", write.Value("p_host_id"));
        Assert.AreEqual("aa-bb-cc-dd-ee-ff", write.Value("p_mac_address"));
        Assert.AreEqual("user", write.Value("p_actor_kind"));
        Assert.AreEqual("7", write.Value("p_actor_id"), "the person is recorded by internal identifier, never by display name");
        Assert.AreEqual("Waitlist", write.Value("p_module"));
        Assert.AreEqual("error", write.Value("p_level"));
        Assert.AreEqual("Failure", write.Value("p_outcome"));
        Assert.AreEqual("System.InvalidOperationException", write.Value("p_error_type"));

        StringAssert.Contains(
            write.Value("p_payload_json"),
            "runtime",
            "the runtime context must be gathered by the seam");
        StringAssert.Contains(
            write.Value("p_exception_detail"),
            "the store refused the write",
            "the exception detail must be serialized by the seam");
    }

    [TestMethod]
    public async Task Error_CallerNamesNoAction_UsesTheModuleAsTheAction()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        // Act
        service.Error("Waitlist", "Something failed.");

        await writer.FlushAsync(CancellationToken.None);

        // Assert: event_action is NOT NULL, and an entry no one can filter by action is an entry no one can find.
        Assert.AreEqual("Waitlist", store.Writes[0].Value("p_event_action"));
    }

    [TestMethod]
    public async Task Error_NobodyIsSignedIn_RecordsTheActorAsSystem()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        var service = new LogService(new FakeMachineFacts(), new FakePersonIdentity(), writer);

        // Act: a fault raised during launch, before anyone has signed in.
        service.Critical("Startup", "The launch could not continue.", new InvalidOperationException("no store"));

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual("system", store.Writes[0].Value("p_actor_kind"));
        Assert.IsNull(store.Writes[0].Value("p_actor_id"));
        Assert.AreEqual("critical", store.Writes[0].Value("p_level"));
    }

    [TestMethod]
    public async Task Info_RaisedWithoutAFault_CarriesNoErrorTypeAndNoFingerprint()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        var service = CreateService(writer);

        // Act
        service.Info("Waitlist", "The list was refreshed.", "WaitlistViewPage");

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        var write = store.Writes[0];

        Assert.AreEqual("info", write.Value("p_level"));
        Assert.AreEqual("Success", write.Value("p_outcome"));
        Assert.IsNull(write.Value("p_error_type"));
        Assert.IsNull(write.Value("p_exception_detail"));
        Assert.IsNull(write.Value("p_error_fingerprint"));
        StringAssert.Contains(write.Value("p_payload_json"), "WaitlistViewPage", "the caller's target belongs in the payload");
    }

    private static LogService CreateService(StoreLogWriter writer) =>
        new(new FakeMachineFacts(), new FakePersonIdentity(), writer);
}
