using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Core.Models;

[TestClass]
public sealed class CoreModelsTests
{
    [TestMethod]
    public void StartupState_Defaults()
    {
        var state = new StartupState();

        Assert.IsTrue(state.IsBusy);
        Assert.AreEqual("Preparing startup checks...", state.StatusText);
        Assert.AreEqual(string.Empty, state.Username);
        Assert.AreEqual(string.Empty, state.CurrentRole);
        Assert.AreEqual(StartupState.SessionTokenSourceNone, state.SessionTokenSource);
        Assert.IsFalse(state.IsDeveloper);
        Assert.IsFalse(state.IsUserMatched);
        Assert.IsFalse(state.IsComputerRegistered);
    }

    [TestMethod]
    public void StartupState_IsDeveloper_IsCaseInsensitive()
    {
        Assert.IsTrue(new StartupState { CurrentRole = "Developer" }.IsDeveloper);
        Assert.IsTrue(new StartupState { CurrentRole = "developer" }.IsDeveloper);
        Assert.IsFalse(new StartupState { CurrentRole = "Operator" }.IsDeveloper);
    }

    [TestMethod]
    public void StartupState_SessionTokenSources_AreStable()
    {
        Assert.AreEqual("None", StartupState.SessionTokenSourceNone);
        Assert.AreEqual("Local", StartupState.SessionTokenSourceLocal);
        Assert.AreEqual("Database", StartupState.SessionTokenSourceDatabase);
    }

    [TestMethod]
    public void StartupResult_Success_SetsExpectedFlags()
    {
        var result = StartupResult.Success("Shell", "Done");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsBlocked);
        Assert.AreEqual("Shell", result.RouteTarget);
        Assert.AreEqual("Done", result.StatusMessage);
    }

    [TestMethod]
    public void StartupResult_Success_UsesDefaultMessage()
    {
        var result = StartupResult.Success("Shell");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Startup complete", result.StatusMessage);
    }

    [TestMethod]
    public void StartupResult_Blocked_SetsExpectedFlags()
    {
        var result = StartupResult.Blocked("Database unavailable");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual(string.Empty, result.RouteTarget);
        Assert.AreEqual("Database unavailable", result.StatusMessage);
    }

    [TestMethod]
    public void ComputerRecord_Defaults()
    {
        var record = new ComputerRecord();

        Assert.AreEqual(0L, record.Id);
        Assert.AreEqual(string.Empty, record.ComputerName);
        Assert.AreEqual(string.Empty, record.DisplayName);
        Assert.AreEqual(string.Empty, record.Description);
        Assert.AreEqual(string.Empty, record.MacAddressNormalized);
        Assert.IsFalse(record.IsRegistered);
    }

    [TestMethod]
    public void ComputerRecord_CanBeInitialized()
    {
        var record = new ComputerRecord
        {
            Id = 5,
            ComputerName = "johnspc",
            DisplayName = "John's Computer",
            Description = "Press 100-3",
            MacAddressNormalized = "AA:BB:CC",
            IsRegistered = true
        };

        Assert.AreEqual(5, record.Id);
        Assert.AreEqual("johnspc", record.ComputerName);
        Assert.AreEqual("John's Computer", record.DisplayName);
        Assert.AreEqual("Press 100-3", record.Description);
        Assert.AreEqual("AA:BB:CC", record.MacAddressNormalized);
        Assert.IsTrue(record.IsRegistered);
    }

    [TestMethod]
    public void ComputerGateCheck_StoresStatusAndOptionalRecord()
    {
        var check = new ComputerGateCheck(ComputerGateStatus.Missing);

        Assert.AreEqual(ComputerGateStatus.Missing, check.Status);
        Assert.IsNull(check.ExistingComputer);
    }

    [TestMethod]
    public void ComputerGateCheck_CanCarryExistingComputer()
    {
        var record = new ComputerRecord { ComputerName = "johnspc" };
        var check = new ComputerGateCheck(ComputerGateStatus.RenamedMachine, record);

        Assert.AreEqual(ComputerGateStatus.RenamedMachine, check.Status);
        Assert.AreSame(record, check.ExistingComputer);
    }

    [TestMethod]
    public void PrintableReport_Defaults()
    {
        var report = new PrintableReport();

        Assert.AreEqual("Report", report.Title);
        Assert.AreEqual(string.Empty, report.Subtitle);
        Assert.AreEqual(0, report.Sections.Count);
        Assert.AreEqual(0, report.FooterLines.Count);
    }

    [TestMethod]
    public void PrintableReport_CanBePopulated()
    {
        var report = new PrintableReport
        {
            Title = "Setup Report",
            Subtitle = "Press 100-3",
            Sections = new[]
            {
                new PrintableReportSection
                {
                    Title = "Pairing",
                    Fields = new[]
                    {
                        new PrintableReportField { Label = "Part", Value = "P-100" }
                    },
                    Lines = new[] { "C:\\data\\path.txt" }
                }
            },
            FooterLines = new[] { "Generated by MTM_Waitlist" }
        };

        Assert.AreEqual("Setup Report", report.Title);
        Assert.AreEqual("Press 100-3", report.Subtitle);
        Assert.AreEqual(1, report.Sections.Count);
        Assert.AreEqual("Pairing", report.Sections[0].Title);
        Assert.AreEqual("Part", report.Sections[0].Fields[0].Label);
        Assert.AreEqual("P-100", report.Sections[0].Fields[0].Value);
        Assert.AreEqual("C:\\data\\path.txt", report.Sections[0].Lines[0]);
        Assert.AreEqual("Generated by MTM_Waitlist", report.FooterLines[0]);
    }

    [TestMethod]
    public void StartupSessionSnapshot_Defaults()
    {
        var snapshot = new StartupSessionSnapshot();

        Assert.IsFalse(snapshot.IsUserMatched);
        Assert.IsFalse(snapshot.IsComputerRegistered);
        Assert.IsTrue(snapshot.IsComputerRegistrationAuthoritative);
        Assert.AreEqual(string.Empty, snapshot.CurrentRole);
        Assert.IsFalse(snapshot.HasDatabaseSession);
        Assert.IsNull(snapshot.DatabaseSessionExpiresUtc);
    }
}
