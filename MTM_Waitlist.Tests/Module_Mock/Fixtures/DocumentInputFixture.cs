// DELIBERATE FIXTURE for DocumentInputAuditTests. DO NOT DELETE.
//
// This file exists so the FR-027 guard has something real to find when it runs against a
// deliberate fixture: the audit's own self-test points its scan at this directory and expects
// at least one hit, which is what proves the matcher matches rather than the tree merely being
// clean. The audit excludes this directory from its production scan, and nothing in the
// application reads this type.
//
// The two literals below are the documents of record from the design phase. They are named here
// only because the guard must be shown to catch a reference when one exists.
namespace MTM_Waitlist.Tests.Module_Mock.Fixtures;

/// <summary>A fixture that references the two design documents on purpose.</summary>
internal static class DocumentInputFixture
{
    /// <summary>The spreadsheet document of record, named only so the audit has a hit to find.</summary>
    internal const string SpreadsheetReference = "WeekendProject/Documents/Request-Config-Template.csv";

    /// <summary>The workflow document of record, named only so the audit has a hit to find.</summary>
    internal const string WorkflowReference = "WeekendProject/Documents/unified-item-picker-workflows.md";
}
