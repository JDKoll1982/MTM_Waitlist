namespace MTM_Waitlist.Models;

public sealed class StartupLoggingOptions
{
    public const string CentralizedDestinationSettingKey = "Startup.Logging.CentralizedDestination";

    public string HostedVmLogDirectory { get; set; } = "MTM_Waitlist/Logs/Startup";

    public string CentralizedDestination { get; set; } = string.Empty;

    public int RetentionDays { get; set; } = 14;

    public int MaxDirectorySizeMb { get; set; } = 250;

    public int ChannelCapacity { get; set; } = 4096;

    public int ForwardRetryCount { get; set; } = 2;
}
