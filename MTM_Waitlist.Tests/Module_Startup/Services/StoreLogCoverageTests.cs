using System.Diagnostics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Tests.Module_Logging;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The store covers every launch: a released build records at least one entry per completed launch, where the
/// retired debug-only path recorded none, and a fault the store refuses is recorded nowhere on the machine
/// (SC-003, SC-016, FR-025, FR-037; plan D6, D27).
/// </summary>
/// <remarks>
/// <para>
/// Two facts have to hold for SC-003 and only one of them is behavioural. The first is that the seam is
/// unconditional: the type it replaced carried a conditional attribute, so a Release build compiled its calls and
/// their arguments out and a shop-floor machine recorded nothing at all. A test compiled in Debug cannot observe
/// that by running, so it is read from the members themselves, which is the only place the attribute would be.
/// </para>
/// <para>
/// The second is that the launch's own end runs through that seam. It does: the host writes the launch's outcome
/// through the same static façade the whole application uses, and this file proves both halves of that, the call
/// site and the path it reaches the store by.
/// </para>
/// <para>
/// No test here touches a live database. The store is the logging module's own recording double, which answers
/// whether the write arrived rather than pretending one did.
/// </para>
/// </remarks>
[TestClass]
public sealed class StoreLogCoverageTests
{
    /// <summary>Whether the file-write probe found anything under the retired log directory before the test.</summary>
    private static readonly string LocalLogRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MTM_Waitlist",
        "Logs");

    [TestCleanup]
    public void DetachTheSeam() => AppLog.Configure(null);

    [TestMethod]
    public void TheSeam_IsUnconditional_SoAReleaseBuildRecordsWhatADebugBuildWouldHaveCompiledOut()
    {
        // Arrange: every type the write path passes through, from the call site's façade to the seam.
        var types = new[] { typeof(AppLog), typeof(ILogService), typeof(LogService) };

        // Act
        var conditional = types
            .SelectMany(type => type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(member => member.GetCustomAttribute<ConditionalAttribute>() is not null)
            .Select(member => member.Name)
            .ToList();

        // Assert: a conditional member is compiled out of a Release build along with its arguments, which is
        // exactly why the retired type recorded nothing on a shop-floor machine (SC-003, plan D6).
        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            conditional,
            "a conditional member on the write path would leave the store empty in the released build");
    }

    [TestMethod]
    public async Task ALaunchThatEnds_RecordsItsOutcomeThroughTheSeam_AndTheStoreReceivesIt()
    {
        // Arrange
        var store = new RecordingLogStore();
        var seam = new LogService(new FakeMachineFacts(), new FakePersonIdentity(), new StoreLogWriter(store));
        AppLog.Configure(seam);

        // Act: the host's own line for a launch that reached its outcome, raised through the façade the whole
        // application writes through.
        AppLog.Info("Launch", "The launch ended at MainScreens.");

        await seam.FlushAsync(CancellationToken.None);

        // Assert: at least one entry per completed launch, in the store, where the released build recorded none.
        Assert.AreEqual(1, store.Writes.Count, "a completed launch recorded nothing in the store");
        Assert.AreEqual("sp_ops_startup_logs_insert", store.Writes[0].Procedure);
        Assert.AreEqual("Launch", store.Writes[0].Value("p_module"));
        Assert.IsNotNull(store.Writes[0].Value("p_host_id"), "the entry named no machine");
        Assert.IsNotNull(store.Writes[0].Value("p_actor_kind"), "the entry named no kind of actor");
    }

    [TestMethod]
    public void TheHost_RecordsTheLaunchsOutcomeThroughTheStaticSeam()
    {
        // Arrange: the app host is the one place a launch's end is recorded, and it is a WinUI application class
        // that no unit test can instantiate, so its call site is read rather than run.
        var host = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "App.xaml.cs");

        Assert.IsTrue(File.Exists(host), $"the app host was not found at '{host}'.");

        var text = File.ReadAllText(host);

        // Assert: the outcome line is raised through the seam, so a launch that ends always leaves an entry.
        StringAssert.Contains(
            text,
            "AppLog.Info(\"Launch\", $\"The launch ended at {outcome}.\")",
            "the host's launch-outcome line does not go through the logging seam");
    }

    [TestMethod]
    public async Task AWriteTheStoreRefuses_LeavesNoLocalLogFileBehind()
    {
        // Arrange: the store refuses every write, which is the outage SC-016 describes.
        var before = FilesUnder(LocalLogRoot);

        var store = new RecordingLogStore { WriteFailure = new InvalidOperationException("the store did not answer") };
        var seam = new LogService(new FakeMachineFacts(), new FakePersonIdentity(), new StoreLogWriter(store));
        AppLog.Configure(seam);

        // Act
        AppLog.Error("StoreReachability", new InvalidOperationException("the store did not answer"), "The store did not answer.");
        await seam.FlushAsync(CancellationToken.None);

        // Assert: the entry was dropped rather than written anywhere else. Nothing on the machine is a substitute
        // record, and no local log file is produced (FR-025, FR-037, SC-016).
        Assert.AreEqual(1, store.Writes.Count, "the refused entry never reached the writer");
        CollectionAssert.AreEqual(before, FilesUnder(LocalLogRoot), "a local log file appeared where none may be produced");
    }

    /// <summary>
    /// Lists what lives under a folder, tolerating a folder that does not exist.
    /// </summary>
    /// <remarks>
    /// The retired file-based path wrote beneath the local application data folder, so an empty answer here is
    /// the machine keeping nothing. A developer whose machine still holds logs from the build before the rebuild
    /// is not a failure: what is asserted is that this test adds nothing to them.
    /// </remarks>
    private static List<string> FilesUnder(string folder) =>
        Directory.Exists(folder)
            ? [.. Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)]
            : [];

    /// <summary>This machine, with the two facts the seam reads at write time.</summary>
    private sealed class FakeMachineFacts : IMachineFacts
    {
        public string Hostname => "test-workstation";

        public string? MacAddress => "aa-bb-cc-dd-ee-ff";

        public MTM_Waitlist.Module_Core.Models.ComputerRecord? RegisteredComputer => null;

        public bool IsRegistered => true;

        public bool HardwareIdentityReadable => true;
    }

    /// <summary>Nobody signed in, so the entry is attributed to the application rather than to a person.</summary>
    private sealed class FakePersonIdentity : IPersonIdentity
    {
        public long UserId => 0;

        public string SignInName => string.Empty;

        public string DisplayName => string.Empty;

        public string? EmployeeNumber => null;

        public string CurrentRoleCode => string.Empty;

        public IReadOnlyList<string> HeldRoleCodes => Array.Empty<string>();

        public bool IsSignedIn => false;

        public bool Holds(string roleCode) => false;
    }
}
