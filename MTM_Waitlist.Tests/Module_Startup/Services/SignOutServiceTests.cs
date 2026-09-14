using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// US6 (FR-026, FR-034, SC-013). Signing out must <b>end</b> the session: the five keys the sign-in path reads
/// are cleared, and only then is the application relaunched, so the restarted instance asks for credentials
/// instead of restoring the session. A relaunch that cannot be performed is reported in plain language rather
/// than leaving the person apparently signed in.
/// </summary>
/// <remarks>
/// The five keys are written out here as literals on purpose. Reading them from the production constant list
/// would make this test agree with whatever that list says, which is the one thing it must not do — the keys
/// are named by the specification, and the sign-in path's own reads are what they have to match.
/// </remarks>
[TestClass]
public sealed class SignOutServiceTests
{
    private static readonly string[] s_expectedKeys =
    {
        "Login.RememberPassword",
        "Login.RememberedUsername",
        "Login.RememberedPassword",
        "Startup.Session.Token",
        "Startup.Session.ExpiresUtc",
    };

    [TestMethod]
    public async Task SignOutAsync_ClearsEveryKeyTheSignInPathReads_BeforeTheRelaunchIsAskedFor()
    {
        var settings = new RecordingLocalSettingsService();
        var lifecycle = new RecordingAppLifecycleService();
        var restarter = new RecordingAppProcessRestarter(settings) { Result = true };
        var service = new SignOutService(settings, restarter, lifecycle);

        var result = await service.SignOutAsync();

        Assert.IsTrue(result.Succeeded, "A relaunch that was performed is a sign-out that succeeded.");
        CollectionAssert.AreEquivalent(
            s_expectedKeys,
            settings.ResetKeys.Distinct().ToArray(),
            "Sign out must clear exactly the keys the sign-in path reads; a key left behind is a session that comes back (FR-034).");
        Assert.IsTrue(
            restarter.EveryKeyWasAlreadyCleared,
            "The order is the requirement: the keys must be gone before the relaunch is asked for, or the restarted instance reads them and restores the session (FR-034).");
        Assert.AreEqual(1, lifecycle.ExitCallCount, "The signed-in process must leave once the replacement is running.");
    }

    [TestMethod]
    public async Task SignOutAsync_WhenTheRelaunchIsRefused_ReportsItInPlainLanguage_AndDoesNotExit()
    {
        var settings = new RecordingLocalSettingsService();
        var lifecycle = new RecordingAppLifecycleService();
        var restarter = new RecordingAppProcessRestarter(settings) { Result = false };
        var service = new SignOutService(settings, restarter, lifecycle);

        var result = await service.SignOutAsync();

        Assert.IsFalse(result.Succeeded, "A refused relaunch must not be reported as a sign-out that worked (FR-026).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), "The person must be told something (FR-026).");
        Assert.AreNotEqual(
            SignOutService.RestartFailedMessageKey,
            result.Message,
            "The person must be shown the sentence, never the bare resource key (FR-022).");
        Assert.AreEqual(0, lifecycle.ExitCallCount, "The process must not exit while the person is being told the restart failed.");
        CollectionAssert.AreEquivalent(
            s_expectedKeys,
            settings.ResetKeys.Distinct().ToArray(),
            "A refused restart still ends the session: leaving the keys behind would leave the person apparently signed in (FR-034).");
    }

    [TestMethod]
    public async Task SignOutAsync_WhenTheRelaunchThrows_ReportsItRatherThanLettingItEscape()
    {
        var settings = new RecordingLocalSettingsService();
        var lifecycle = new RecordingAppLifecycleService();
        var restarter = new RecordingAppProcessRestarter(settings) { ThrowOnRestart = true };
        var service = new SignOutService(settings, restarter, lifecycle);

        var result = await service.SignOutAsync();

        Assert.IsFalse(result.Succeeded, "A restart that threw must be reported, not raised into the shell (FR-026, FR-034).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), "The report must say something the person can act on.");
        Assert.AreEqual(0, lifecycle.ExitCallCount, "Nothing was started, so nothing may be exited.");
    }

    [TestMethod]
    public async Task SignOutAsync_OnAComputerWhereNothingWasRemembered_StillRelaunches()
    {
        // The behaviour must not depend on a remembered credential existing (spec Edge Cases).
        var settings = new RecordingLocalSettingsService();
        var lifecycle = new RecordingAppLifecycleService();
        var restarter = new RecordingAppProcessRestarter(settings) { Result = true };
        var service = new SignOutService(settings, restarter, lifecycle);

        var result = await service.SignOutAsync();

        Assert.IsTrue(result.Succeeded, "Signing out on a computer where nothing was remembered still restarts the application.");
        Assert.AreEqual(1, restarter.RestartCallCount, "The relaunch is the sign-out, not a side effect of one.");
        Assert.AreEqual(1, lifecycle.ExitCallCount, "The signed-in process still leaves.");
    }

    /// <summary>Records every key reset, and what the store holds at any moment.</summary>
    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _settings = new(StringComparer.Ordinal);

        public List<string> ResetKeys { get; } = new();

        public bool Holds(string key) => _settings.ContainsKey(key);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            ResetKeys.Add(key);
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }

    /// <summary>A restarter that records the state of the store at the moment it is asked to relaunch.</summary>
    private sealed class RecordingAppProcessRestarter : IAppProcessRestarter
    {
        private readonly RecordingLocalSettingsService _settings;

        public RecordingAppProcessRestarter(RecordingLocalSettingsService settings)
        {
            _settings = settings;
        }

        public int RestartCallCount { get; private set; }

        public bool Result { get; init; }

        public bool ThrowOnRestart { get; init; }

        /// <summary>True only when every key the sign-in path reads was already gone when the relaunch was asked for.</summary>
        public bool EveryKeyWasAlreadyCleared { get; private set; }

        public bool Restart()
        {
            RestartCallCount++;
            EveryKeyWasAlreadyCleared = !s_expectedKeys.Any(_settings.Holds);

            if (ThrowOnRestart)
            {
                throw new InvalidOperationException("The application could not be started.");
            }

            return Result;
        }
    }

    private sealed class RecordingAppLifecycleService : IAppLifecycleService
    {
        public int ExitCallCount { get; private set; }

        public void Exit() => ExitCallCount++;

        public void ShowLoginWindowAndCloseSplash()
        {
        }

        public void ShowMainWindowAndCloseSplash()
        {
        }

        public void ShowMainWindowAndCloseLoginWindow()
        {
        }
    }
}
