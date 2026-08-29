namespace MTM_Waitlist.Module_Startup.Models;

public sealed class ComputerRecord
{
    public long Id { get; init; }

    public string ComputerName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string MacAddressNormalized { get; init; } = string.Empty;

    public bool IsRegistered { get; init; }
}
