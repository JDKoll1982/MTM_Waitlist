using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the service's durable log file — the diagnostic that turns "the service is not refreshing and
/// nothing says why" into an answer (T148(c), FR-013).
/// </summary>
/// <remarks>
/// The point of these assertions is not that a string is written; it is that the record survives a
/// <c>Release</c> publish. The previous path was <c>StartupDebugLog</c>, whose calls carry
/// <c>[Conditional("DEBUG")]</c> and were therefore compiled out of the shipped build entirely, leaving
/// only <c>Debug.WriteLine</c> output that nothing on the host could read.
/// </remarks>
[TestClass]
public sealed class ServiceLogTests
{
    private string _root = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "mtm-service-log-tests", Guid.NewGuid().ToString("N"));
        ServiceLog.Configure(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        ServiceLog.Configure(null);

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [TestMethod]
    public void Write_CreatesTheDailyFileUnderTheConfiguredDirectory()
    {
        ServiceLog.Info("ServiceApp", "hello");

        var expected = Path.Combine(_root, $"{ServiceLog.FileNamePrefix}{DateTime.UtcNow:yyyy_MM_dd}.jsonl");

        Assert.IsTrue(File.Exists(expected), $"A durable daily file was expected at '{expected}'.");
    }

    [TestMethod]
    public void Write_RecordsLevelAreaAndMessageAsOneJsonLine()
    {
        ServiceLog.Info("ServiceApp", "refresh cycle started");

        var line = File.ReadAllLines(SingleLogFile()).Single();
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        Assert.AreEqual("INFO", root.GetProperty("Level").GetString());
        Assert.AreEqual("ServiceApp", root.GetProperty("Area").GetString());
        Assert.AreEqual("refresh cycle started", root.GetProperty("Message").GetString());
        Assert.IsTrue(root.GetProperty("Exception").ValueKind is JsonValueKind.Null);
        Assert.AreNotEqual(
            default(DateTimeOffset),
            root.GetProperty("TimestampUtc").GetDateTimeOffset(),
            "Every line must carry its own UTC timestamp.");
    }

    [TestMethod]
    public void Error_RecordsTheExceptionText()
    {
        ServiceLog.Error("ServiceApp", new InvalidOperationException("boom"), "the engines failed");

        var line = File.ReadAllLines(SingleLogFile()).Single();
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        Assert.AreEqual("ERROR", root.GetProperty("Level").GetString());
        StringAssert.Contains(root.GetProperty("Exception").GetString(), "InvalidOperationException");
        StringAssert.Contains(root.GetProperty("Exception").GetString(), "boom");
    }

    [TestMethod]
    public void Write_AppendsRatherThanReplacing()
    {
        ServiceLog.Info("ServiceApp", "first");
        ServiceLog.Info("ServiceApp", "second");

        Assert.AreEqual(2, File.ReadAllLines(SingleLogFile()).Length);
    }

    [TestMethod]
    public void Write_WhenTheDirectoryCannotBeCreated_DoesNotThrow()
    {
        // A path containing a reserved character cannot be created, which is the failure a durable logger has
        // to survive: the engines must keep running even when the log destination is unusable.
        ServiceLog.Configure(Path.Combine(_root, "inva|id"));

        ServiceLog.Info("ServiceApp", "this must not throw");
    }

    [TestMethod]
    public void FileLoggerProvider_RoutesContainerLogMessagesToTheDailyFile()
    {
        using var provider = new ServiceFileLoggerProvider();
        var logger = provider.CreateLogger("MTM_Waitlist.Mock.Service.RefreshEngine");

        logger.LogError(new InvalidOperationException("connection refused"), "Refresh cycle failed.");

        var line = File.ReadAllLines(SingleLogFile()).Single();
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        Assert.AreEqual("ERROR", root.GetProperty("Level").GetString());
        Assert.AreEqual(
            "MTM_Waitlist.Mock.Service.RefreshEngine",
            root.GetProperty("Area").GetString(),
            "The logger category becomes the log area, which is how a reader finds the failing component.");
        Assert.AreEqual("Refresh cycle failed.", root.GetProperty("Message").GetString());
        StringAssert.Contains(root.GetProperty("Exception").GetString(), "connection refused");
    }

    [TestMethod]
    public void FileLoggerProvider_IsEnabledForEverythingExceptNone()
    {
        using var provider = new ServiceFileLoggerProvider();
        var logger = provider.CreateLogger("any");

        Assert.IsTrue(logger.IsEnabled(LogLevel.Error));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.None));
    }

    private string SingleLogFile()
    {
        var files = Directory.GetFiles(_root, $"{ServiceLog.FileNamePrefix}*.jsonl");

        Assert.AreEqual(1, files.Length, "Exactly one daily file was expected.");

        return files[0];
    }
}
