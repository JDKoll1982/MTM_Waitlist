using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// Maps <see cref="BackupStore"/> to its MySQL database name, and to the words an operator sees.
/// </summary>
public static class BackupStoreExtensions
{
    /// <summary>Returns the MySQL database name for the store.</summary>
    public static string ToDatabaseName(this BackupStore store) => store switch
    {
        BackupStore.MtmWaitlist => "mtm_waitlist",
        BackupStore.MtmWipApplicationWinforms => "mtm_wip_application_winforms",
        BackupStore.MtmReceivingApplication => "mtm_receiving_application",
        BackupStore.MtmMock => "mtm_mock",
        _ => throw new ArgumentOutOfRangeException(nameof(store), store, "Unknown backup store.")
    };

    /// <summary>
    /// Returns the store's own connection-string environment variable, so the store resolves its target the same
    /// way whether it is being read, backed up, or restored. The shared fallback variable still applies when this
    /// one is unset — see <see cref="MySqlConnectionStringResolver"/>. A store with no variable of its own returns
    /// <see langword="null"/> and resolves through the shared one alone.
    /// </summary>
    public static string? ToConnectionStringEnvironmentVariable(this BackupStore store) => store switch
    {
        BackupStore.MtmWaitlist => MySqlConnectionStringResolver.WaitlistConnectionStringEnvironmentVariable,
        BackupStore.MtmWipApplicationWinforms => MySqlConnectionStringResolver.WipApplicationConnectionStringEnvironmentVariable,
        BackupStore.MtmReceivingApplication => MySqlConnectionStringResolver.ReceivingApplicationConnectionStringEnvironmentVariable,
        BackupStore.MtmMock => MySqlConnectionStringResolver.MockConnectionStringEnvironmentVariable,
        _ => null
    };

    /// <summary>
    /// Returns the name an operator sees for the store. The database name is a fallback, not the label:
    /// an internal schema name is not something the person taking a backup should have to read.
    /// </summary>
    public static string ToDisplayName(this BackupStore store) =>
        Resolve($"Service_Store.{store.ToDatabaseName()}", store.ToDatabaseName());

    /// <summary>Returns the one-line note that says what the store holds.</summary>
    public static string ToDescription(this BackupStore store) =>
        Resolve($"Service_StoreDescription.{store.ToDatabaseName()}", string.Empty);

    /// <summary>Returns every store in the fixed set.</summary>
    public static IReadOnlyList<BackupStore> All { get; } =
    [
        BackupStore.MtmWaitlist,
        BackupStore.MtmWipApplicationWinforms,
        BackupStore.MtmReceivingApplication,
        BackupStore.MtmMock
    ];

    /// <summary>
    /// Resolves a resource key, falling back to the supplied text when the key is not authored (which is
    /// how <c>GetLocalized</c> reports a missing resource).
    /// </summary>
    private static string Resolve(string key, string fallback)
    {
        var value = key.GetLocalized();
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }
}
