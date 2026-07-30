namespace MTM_Waitlist.Module_DevTools.Models;

public sealed class RequestTypeDefinition
{
    public required string RequestTypeName { get; init; }

    public string? ImageFilePath { get; init; }

    public required IReadOnlyList<RequestTypeFieldDefinition> CardFields { get; init; }

    public required IReadOnlyList<RequestTypeFieldDefinition> DetailFields { get; init; }
}
