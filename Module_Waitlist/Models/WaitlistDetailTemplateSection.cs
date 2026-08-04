using System.Collections.ObjectModel;

namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistDetailTemplateSection
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public ObservableCollection<WaitlistDetailTemplateField> Fields { get; } = new();
}

public sealed class WaitlistDetailTemplateField
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}