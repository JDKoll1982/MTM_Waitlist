namespace MTM_Waitlist.Module_Shared.Services;

public sealed record TooltipPresentation(
    string Text,
    IReadOnlyList<string> AssociatedFiles,
    bool IsDeveloperMode);