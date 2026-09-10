namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The four MySQL stores the on-host service backs up and (for emergencies) restores.
/// </summary>
/// <remarks>
/// This is a <b>fixed</b> set, not an extension point — an unknown store key is a configuration
/// error. Infor Visual (SQL Server) is deliberately absent: it is read-only to us and is not
/// backed up by this tool.
/// </remarks>
public enum BackupStore
{
    /// <summary>The application's own store — always live, never mocked.</summary>
    MtmWaitlist,

    /// <summary>The floor / WIP store — always live, never cached (FR-018).</summary>
    MtmWipApplicationWinforms,

    /// <summary>The receiving store — always live, never cached (FR-018).</summary>
    MtmReceivingApplication,

    /// <summary>The Infor Visual mirror cache — disposable and wholesale-replaced.</summary>
    MtmMock
}
