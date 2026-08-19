using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Module_Shared.Models;

public sealed class ControlInspectorDetail
{
    public string Title { get; init; } = "Control details";

    public string Summary { get; init; } = string.Empty;

    public string ResourceKey { get; init; } = string.Empty;

    public string FallbackText { get; init; } = string.Empty;

    public string LocalizedTooltipText { get; init; } = string.Empty;

    public string ElementName { get; init; } = string.Empty;

    public string ElementType { get; init; } = string.Empty;

    public string AutomationId { get; init; } = string.Empty;

    public string Tag { get; init; } = string.Empty;

    public string DataContextType { get; init; } = string.Empty;

    public string ParentChain { get; init; } = string.Empty;

    public string DeclaringViewHint { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public bool IsHitTestVisible { get; init; }

    public string Visibility { get; init; } = string.Empty;

    public string ActualSize { get; init; } = string.Empty;

    public string RenderSize { get; init; } = string.Empty;

    public string Margin { get; init; } = string.Empty;

    public string Padding { get; init; } = string.Empty;

    public string StyleKey { get; init; } = string.Empty;

    public string CommandInfo { get; init; } = string.Empty;

    public string BindingHints { get; init; } = string.Empty;

    public IReadOnlyList<string> AssociatedFiles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ControlInspectorField> Fields { get; init; } = Array.Empty<ControlInspectorField>();

    public IReadOnlyList<ControlInspectorWorkflowStep> WorkflowSteps { get; init; } = Array.Empty<ControlInspectorWorkflowStep>();

    public IReadOnlyList<ControlInspectorSection> Sections { get; init; } = Array.Empty<ControlInspectorSection>();
}

public sealed class ControlInspectorField
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public sealed class ControlInspectorSection
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<ControlInspectorField> Fields { get; init; } = Array.Empty<ControlInspectorField>();
}

public sealed class ControlInspectorWorkflowStep
{
    public int Index { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Caption { get; init; } = string.Empty;

    /// <summary>True when a chevron should precede this step in the horizontal chart.</summary>
    public bool ShowChevron { get; init; } = true;
}

/// <summary>
/// XAML helper for binding chevron visibility in the workflow chart.
/// </summary>
public static class ControlInspectorWorkflowStepView
{
    public static Visibility ChevronVisibility(bool showChevron) =>
        showChevron ? Visibility.Visible : Visibility.Collapsed;
}
