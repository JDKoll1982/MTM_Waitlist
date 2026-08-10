namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class NewRequestTypeDefinition
{
    public string RequestType { get; set; } = string.Empty;

    public string Control { get; set; } = string.Empty;

    public List<NewRequestSubtypeDefinition> Subtypes { get; set; } = new();
}
