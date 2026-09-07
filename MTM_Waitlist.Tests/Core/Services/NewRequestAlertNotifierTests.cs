using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class NewRequestAlertNotifierTests
{
    [TestMethod]
    public async Task Notify_WhenToggleOnAndPackaged_ShowsToastWithDeepLink()
    {
        var settings = new StubLocalSettings();
        settings.Set(NewRequestAlertService.SettingKeyName, true);
        var notifications = new RecordingNotificationService();
        var notifier = new NewRequestAlertNotifier(
            notifications,
            new NewRequestAlertService(settings));

        var id = Guid.NewGuid();
        var shown = await notifier.NotifyNewRequestAsync(id, "New request", "Coil request ready", isPackaged: true);

        Assert.IsTrue(shown);
        Assert.AreEqual(1, notifications.Payloads.Count);
        var payload = notifications.Payloads[0];
        StringAssert.Contains(payload, WaitlistRequestLink.ActionOpenRequest);
        StringAssert.Contains(payload, id.ToString("D"));
        StringAssert.Contains(payload, "New request");
        StringAssert.Contains(payload, "Coil request ready");
    }

    [TestMethod]
    public async Task Notify_WhenToggleOff_ShowsNothing()
    {
        var settings = new StubLocalSettings(); // toggle absent -> OFF
        var notifications = new RecordingNotificationService();
        var notifier = new NewRequestAlertNotifier(
            notifications,
            new NewRequestAlertService(settings));

        var shown = await notifier.NotifyNewRequestAsync(Guid.NewGuid(), "t", "b", isPackaged: true);

        Assert.IsFalse(shown);
        Assert.AreEqual(0, notifications.Payloads.Count);
    }

    [TestMethod]
    public async Task Notify_WhenUnpackaged_ShowsNothingEvenWhenToggleOn()
    {
        var settings = new StubLocalSettings();
        settings.Set(NewRequestAlertService.SettingKeyName, true);
        var notifications = new RecordingNotificationService();
        var notifier = new NewRequestAlertNotifier(
            notifications,
            new NewRequestAlertService(settings));

        var shown = await notifier.NotifyNewRequestAsync(Guid.NewGuid(), "t", "b", isPackaged: false);

        Assert.IsFalse(shown);
        Assert.AreEqual(0, notifications.Payloads.Count);
    }

    [TestMethod]
    public void BuildToastXml_EmbedsEscapedLaunchArguments()
    {
        var xml = NewRequestAlertNotifier.BuildToastXml("Title <&>", "Body \"x\"", "action=openrequest&request=abc");

        StringAssert.Contains(xml, "<toast launch=\"action=openrequest&amp;request=abc\">");
        StringAssert.Contains(xml, "<text>Title &lt;&amp;&gt;</text>");
        StringAssert.Contains(xml, "<text>Body &quot;x&quot;</text>");
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new();

        public void Set(string key, bool value) => _store[key] = value;

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (!_store.TryGetValue(key, out var raw) || raw is not T typed)
            {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult<T?>(typed);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }

    private sealed class RecordingNotificationService : IAppNotificationService
    {
        public List<string> Payloads { get; } = new();

        public void Initialize() { }

        public bool Show(string payload)
        {
            Payloads.Add(payload);
            return true;
        }

        public NameValueCollection ParseArguments(string arguments) => new();

        public void Unregister() { }
    }
}
