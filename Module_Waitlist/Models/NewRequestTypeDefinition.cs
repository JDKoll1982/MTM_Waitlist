namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class NewRequestTypeDefinition
{
    public string RequestType { get; set; } = string.Empty;

    public string Control { get; set; } = string.Empty;

    public string Flow { get; set; } = "direct-to-confirmation";

    public List<string> CenterDataGridFields { get; set; } = new();

    public List<NewRequestSubtypeDefinition> Subtypes { get; set; } = new();
}
