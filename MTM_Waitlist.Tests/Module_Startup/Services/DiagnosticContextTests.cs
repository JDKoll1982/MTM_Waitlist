using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The gathered context: the runtime, view and store objects are built by the seam rather than supplied by a
/// call site, a store fault carries the provider's own diagnostics, and nothing on the never-write list reaches
/// any of them (`contracts/logging-contract.md` §1.3, §6; FR-034 to FR-036, SC-015).
/// </summary>
/// <remarks>
/// The provider is a double rather than the real one, because what is proved is that the seam reads a provider's
/// members without the logging module referencing the provider, which is the whole reason it reads them by name.
/// </remarks>
[TestClass]
public sealed class DiagnosticContextTests
{
    private static readonly DateTime ProcessStarted = DateTime.UtcNow.AddMinutes(-3);

    [TestMethod]
    public void Build_CarriesTheRuntimeTheSeamGathered_NotTheCaller()
    {
        // Arrange
        var entry = Entry();

        // Act
        var payload = JsonNode.Parse(DiagnosticContext.Build(entry, null, null, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted))!;

        // Assert: FR-034 — a call site supplies none of this, so a call site cannot forget it.
        var runtime = payload["runtime"]!;
        Assert.IsNotNull(runtime["framework"], "the runtime was not described");
        Assert.IsNotNull(runtime["osDescription"], "the operating system was not described");
        Assert.IsNotNull(runtime["processArchitecture"], "the process architecture was not described");
        Assert.IsNotNull(runtime["processId"], "the process identity was not described");
        Assert.IsNotNull(runtime["windowsAppSdkVersion"], "the Windows App SDK version was not described");

        var uptime = long.Parse(runtime["uptimeSeconds"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsTrue(uptime >= 170, $"the application's uptime was recorded as {uptime} seconds");
    }

    [TestMethod]
    public void Build_WhenTheCallerNamedAControl_CarriesItUnderTheViewContext()
    {
        // Arrange
        var entry = Entry(target: "MachineSetupWindow/SaveButton");

        // Act
        var ui = JsonNode.Parse(DiagnosticContext.Build(entry, null, null, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted))!["ui"]!;

        // Assert: FR-035 — the control the caller could name is recorded, and nothing is read from a live window.
        Assert.AreEqual("MachineSetupWindow/SaveButton", ui["control"]!.GetValue<string>());
        Assert.IsNull(ui["window"], "the seam read a window it must not touch during shutdown");
        Assert.IsNull(ui["screen"]);
    }

    [TestMethod]
    public void Build_WhenTheFaultCameFromTheStore_CarriesTheProvidersOwnDiagnostics()
    {
        // Arrange: a provider exception of the shape MySqlConnector raises.
        var fault = new StubProviderException("Duplicate entry for @p_display_name=Shop floor station")
        {
            ErrorCode = 1062,
            SqlState = "23000",
            Number = 1062,
        };

        // Act
        var database = JsonNode.Parse(DiagnosticContext.Build(Entry(), fault, null, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted))!["database"]!;

        // Assert: FR-036 — the provider's own codes, read by name so the module stays provider-independent.
        Assert.AreEqual("1062", database["errorCode"]!.GetValue<string>());
        Assert.AreEqual("23000", database["sqlState"]!.GetValue<string>());
        Assert.AreEqual("1062", database["number"]!.GetValue<string>());
        Assert.AreEqual("mtm_waitlist", database["database"]!.GetValue<string>());
        Assert.AreEqual("sp_ops_startup_logs_insert", database["storedProcedure"]!.GetValue<string>());
    }

    [TestMethod]
    public void Build_WhenTheFaultCameFromTheStore_RecordsNoStatementValues()
    {
        // Arrange: a provider message carrying a statement's parameters.
        var fault = new StubProviderException("Near '@p_display_name=Shop floor station' at line 1");

        // Act
        var json = DiagnosticContext.Build(Entry(), fault, null, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted);

        // Assert: SC-015 — no statement fingerprint may contain a parameter value, and no connection text may be
        // written at all (§6). The store's alias is null for exactly that reason.
        var database = JsonNode.Parse(json)!["database"]!;
        var fingerprint = database["statementFingerprint"];
        Assert.IsTrue(
            fingerprint is null || !fingerprint.GetValue<string>().Contains("Shop floor station", StringComparison.Ordinal),
            "a statement fingerprint carried a parameter value");

        Assert.IsFalse(json.Contains("Shop floor station", StringComparison.Ordinal), "a parameter value reached the payload");
        Assert.IsFalse(json.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase), "connection text reached the payload");
        Assert.IsNull(database["alias"], "a server alias is a convenience; a connection string is a credential");
    }

    [TestMethod]
    public void Build_WhenAStructuredPropertyNamesASecret_DropsTheValue()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["workCenter"] = "Press 4",
            ["password"] = "a-credential-the-test-owns",
            ["sessionToken"] = "a-token-the-test-owns",
        };

        // Act
        var json = DiagnosticContext.Build(Entry(), null, properties, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted);

        // Assert: §6 forbids the value and the seam removes it, so a call site cannot smuggle one in.
        Assert.IsFalse(json.Contains("a-credential-the-test-owns", StringComparison.Ordinal), "a credential reached the payload");
        Assert.IsFalse(json.Contains("a-token-the-test-owns", StringComparison.Ordinal), "a token reached the payload");

        var projected = JsonNode.Parse(json)!["properties"]!;
        Assert.AreEqual("Press 4", projected["workCenter"]!.GetValue<string>(), "an innocent property was dropped with the secret");
        Assert.AreEqual(2, projected["valuesRemoved"]!.GetValue<int>(), "the dropped values were not accounted for");
    }

    [TestMethod]
    public void Build_WhenTheKeyFilePathIsTheOnlySecretAdjacentValue_WritesItRatherThanTheKey()
    {
        // Arrange: the shared key file's path is the one secret-adjacent value §6 permits, so it is recorded
        // while nothing read out of it ever is.
        var properties = new Dictionary<string, object?> { ["keyFilePath"] = @"\\mtmanu-fs01\Expo Drive\MTM_Application_Keys\MTM_AUTH_USER_SECRET_KEY.txt" };

        // Act
        var json = DiagnosticContext.Build(Entry(), null, properties, "mtm_waitlist", "sp_ops_startup_logs_insert", ProcessStarted);

        // Assert: the path is what a support reader needs to see which key a decryption failure used.
        StringAssert.Contains(json, "MTM_AUTH_USER_SECRET_KEY.txt");
    }

    /// <summary>An entry as a call site describes one: the severity, the module, the message and the action.</summary>
    private static LogEntry Entry(string? target = null)
        => new(
            LogSeverity.Error,
            "Startup",
            "the launch did not finish",
            "Failure",
            "launch",
            null,
            null,
            null,
            target,
            null);

    /// <summary>
    /// A provider's exception, shaped the way MySqlConnector's is, so the seam can be shown to read its members
    /// by name without the logging module referencing the provider.
    /// </summary>
    private sealed class StubProviderException(string message) : Exception(message)
    {
        public int ErrorCode { get; init; }

        public string? SqlState { get; init; }

        public int Number { get; init; }
    }
}
