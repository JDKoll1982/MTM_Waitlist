namespace MTM_Waitlist.Models;

public sealed class StartupState
{
    public bool IsBusy { get; set; } = true;

    public string StatusText { get; set; } = "Preparing startup checks...";

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Username { get; set; } = string.Empty;

    public bool ConfigurationLoaded { get; set; }

    public string ConfigurationFolder { get; set; } = string.Empty;

    public string ConfigurationFile { get; set; } = string.Empty;

    public string CurrentRole { get; set; } = string.Empty;

    public bool IsDeveloper => string.Equals(CurrentRole, "Developer", StringComparison.OrdinalIgnoreCase);
}
