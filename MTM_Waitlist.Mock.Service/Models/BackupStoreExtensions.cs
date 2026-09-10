namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// Maps <see cref="BackupStore"/> to its MySQL database name.
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

    /// <summary>Returns every store in the fixed set.</summary>
    public static IReadOnlyList<BackupStore> All { get; } =
    [
        BackupStore.MtmWaitlist,
        BackupStore.MtmWipApplicationWinforms,
        BackupStore.MtmReceivingApplication,
        BackupStore.MtmMock
    ];
}
