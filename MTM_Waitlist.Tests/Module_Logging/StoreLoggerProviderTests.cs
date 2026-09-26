using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// The <c>ILogger</c> surface: existing call sites reach the same store through the same seam, with no edit to a
/// call site and with their structured properties kept out of the message (contract §4, plan D5).
/// </summary>
[TestClass]
public sealed class StoreLoggerProviderTests
{
    [TestMethod]
    public async Task Log_ILoggerErrorReachesTheStoreThroughTheSameSeam()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        using var provider = CreateProvider(writer);
        var logger = provider.CreateLogger("MTM_Waitlist.Module_Waitlist.Services.WaitlistRequestService");

        // Act: an ordinary ILogger call site, written the way the application's own are.
        logger.LogError(
            new InvalidOperationException("the request could not be saved"),
            "The request {RequestId} could not be saved",
            4711);

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, store.Writes.Count);
        var write = store.Writes[0];

        Assert.AreEqual("sp_ops_startup_logs_insert", write.Procedure, "the ILogger surface must use the store's one writer");
        Assert.AreEqual(
            "MTM_Waitlist.Module_Waitlist.Services.WaitlistRequestService",
            write.Value("p_module"),
            "the category name must become the entry's module");
        Assert.AreEqual("error", write.Value("p_level"));
        Assert.AreEqual("Failure", write.Value("p_outcome"));
        Assert.AreEqual("The request 4711 could not be saved", write.Message);
        StringAssert.Contains(write.Value("p_error_type"), "InvalidOperationException");
        StringAssert.Contains(write.Value("p_exception_detail"), "the request could not be saved");
        Assert.AreEqual(64, write.Value("p_error_fingerprint")?.Length, "a fault must carry its grouping fingerprint");
    }

    [TestMethod]
    public async Task Log_StructuredProperties_LandInThePayloadRatherThanInTheMessage()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        using var provider = CreateProvider(writer);
        var logger = provider.CreateLogger("MTM_Waitlist.Module_Waitlist.Services.WaitlistRequestService");

        // Act
        logger.LogInformation("Request {RequestId} was submitted by {SubmittedBy}", 4711, "johnk");

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        var write = store.Writes[0];

        Assert.AreEqual("Request 4711 was submitted by johnk", write.Message);
        StringAssert.Contains(write.Value("p_payload_json"), "\"RequestId\":\"4711\"");
        StringAssert.Contains(write.Value("p_payload_json"), "\"SubmittedBy\":\"johnk\"");
        Assert.IsFalse(
            write.Message?.Contains("RequestId", StringComparison.Ordinal) ?? false,
            "the structured properties were flattened into the message instead of being carried separately");
    }

    [TestMethod]
    public async Task Log_LevelNone_IsNotRecordedAtAll()
    {
        // Arrange
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        using var provider = CreateProvider(writer);
        var logger = provider.CreateLogger("MTM_Waitlist.Module_Waitlist.Services.WaitlistRequestService");

        // Act
        Assert.IsFalse(logger.IsEnabled(LogLevel.None));
        logger.Log(LogLevel.None, new EventId(0), "nothing", null, (_, _) => "nothing");

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(0, store.Writes.Count);
    }

    private static StoreLoggerProvider CreateProvider(StoreLogWriter writer) =>
        new(new LogService(new FakeMachineFacts { Hostname = "test-workstation" }, new FakePersonIdentity(), writer));
}
