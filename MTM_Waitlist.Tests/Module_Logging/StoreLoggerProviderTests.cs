using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_Mock;

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

    [TestMethod]
    public async Task Log_ACategoryLongerThanTheStoreHolds_IsCutWithAMarkerRatherThanLosingTheEntry()
    {
        // Arrange: a category longer than the column, built the way a deep namespace would be. Before the bound
        // existed the store refused such a write with "Data too long for column 'p_module'" and the entry was
        // lost, silently, because a logging failure never reaches the caller (FR-037).
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        using var provider = CreateProvider(writer);
        var category = string.Concat(Enumerable.Repeat("MTM_Waitlist.Module_Settings.Services.", 4))
            + "ImageStorageConfigurationResolver";

        Assert.IsTrue(
            category.Length > StoreLoggerProvider.MaximumModuleLength,
            "this case only proves anything for a category longer than the store holds");

        // Act
        provider.CreateLogger(category).LogInformation("Resolving the picture storage configuration");

        await writer.FlushAsync(CancellationToken.None);

        // Assert: the entry is written, and a cut says so rather than looking like the whole name.
        Assert.AreEqual(1, store.Writes.Count, "an over-long category must not cost the entry");

        var module = store.Writes[0].Value("p_module");

        Assert.AreEqual(StoreLoggerProvider.MaximumModuleLength, module?.Length, "the module must fit what the store holds");
        StringAssert.StartsWith(module, "…", "a cut has to be visible in the value");
        StringAssert.EndsWith(module, "ImageStorageConfigurationResolver", "the tail is the part a reader filters by");
    }

    [TestMethod]
    public async Task Log_TheCategoryThatTheColumnUsedToRefuse_IsCarriedWhole()
    {
        // The case that was found by hand on 2026-09-28: this category is 70 characters, the column and the
        // writer's parameter were 64, and every entry from this type was refused. It fits now, and it must arrive
        // uncut, because the panel's module filter matches the category exactly.
        var store = new RecordingLogStore();
        var writer = new StoreLogWriter(store);
        using var provider = CreateProvider(writer);
        var category = "MTM_Waitlist.Module_Settings.Services.ImageStorageConfigurationResolver";

        // Act
        provider.CreateLogger(category).LogInformation("Resolving the picture storage configuration");

        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, store.Writes.Count);
        Assert.AreEqual(category, store.Writes[0].Value("p_module"), "a category that fits must arrive exactly as it is");
    }

    [TestMethod]
    public void MaximumModuleLength_IsNoWiderThanTheColumnTheTableDeclares()
    {
        // The bound and the column have to agree, or every entry from a long category is refused again. This is
        // the check that was missing when the table said 64 and the tree already held a 77-character category.
        var declared = DeclaredModuleColumnLength();

        Assert.IsTrue(
            StoreLoggerProvider.MaximumModuleLength <= declared,
            $"the provider hands the store up to {StoreLoggerProvider.MaximumModuleLength} characters and the table "
                + $"holds {declared}, so an entry from a long category would be refused");
    }

    /// <summary>The width of the log table's <c>module</c> column, read from the artifact that creates it.</summary>
    private static int DeclaredModuleColumnLength()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Tables",
            "10_ops_startup_logs",
            "create.sql");

        var match = Regex.Match(File.ReadAllText(path), @"\bmodule VARCHAR\((?<width>\d+)\)");

        Assert.IsTrue(match.Success, "the log table's create artifact must declare the module column's width");

        return int.Parse(match.Groups["width"].Value, CultureInfo.InvariantCulture);
    }

    private static StoreLoggerProvider CreateProvider(StoreLogWriter writer) =>
        new(new LogService(new FakeMachineFacts { Hostname = "test-workstation" }, new FakePersonIdentity(), writer));
}
