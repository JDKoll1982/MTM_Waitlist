namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A printer-friendly report document that can be handed to <see cref="Contracts.Services.IReportPrintService"/>.
/// </summary>
public sealed class PrintableReport
{
    public string Title { get; init; } = "Report";

    public string Subtitle { get; init; } = string.Empty;

    public IReadOnlyList<PrintableReportSection> Sections { get; init; } = Array.Empty<PrintableReportSection>();

    public IReadOnlyList<string> FooterLines { get; init; } = Array.Empty<string>();
}

public sealed class PrintableReportSection
{
    public string Title { get; init; } = string.Empty;

    /// <summary>Label/value rows rendered as a compact table.</summary>
    public IReadOnlyList<PrintableReportField> Fields { get; init; } = Array.Empty<PrintableReportField>();

    /// <summary>Monospace lines (for example, source file paths).</summary>
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
}

public sealed class PrintableReportField
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
