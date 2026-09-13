namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One declared field of an Item's page — an element of <c>detail_fields_json</c> on the Item's
/// configuration row. Shape fixed by
/// specs/004-unified-card-item-picker/data-model.md §4.
/// <para>
/// The field set is <b>mutable by design</b> (FR-015): adding, removing or reordering a field is a one-row
/// data edit, and no test may assert today's contents. What tests assert is the <i>mechanism</i> — that
/// fields are read from configuration, laid out in the declared order, with the declared value types and
/// labels.
/// </para>
/// <para>
/// The JSON key stays <c>order</c>; no bare <c>order</c> column is ever introduced
/// (Database/Database-Ruleset.md).
/// </para>
/// </summary>
public sealed class RequestItemFieldDefinition
{
    /// <summary>Where the field's value comes from: the active job, the captured answer, or a fixed value.</summary>
    public static class Sources
    {
        /// <summary>The value comes from the requesting job (part number, description, location, …).</summary>
        public const string Job = "job";

        /// <summary>The value is the one answer the flow captured.</summary>
        public const string Answer = "answer";

        /// <summary>The value is fixed for the Item (e.g. the requesting work centre).</summary>
        public const string Fixed = "fixed";
    }

    /// <summary>The resource key or literal shown beside the value.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The declared value type — one of the approved <see cref="RequestItemValueType"/> names.</summary>
    public RequestItemValueType ValueType { get; init; } = RequestItemValueType.String;

    /// <summary>One of <see cref="Sources"/>. An unrecognised source is a configuration problem.</summary>
    public string Source { get; init; } = Sources.Job;

    /// <summary>Position in the declared order; the page renders fields in this order.</summary>
    public int Order { get; init; }

    /// <summary>Whether the page blocks confirmation without this field.</summary>
    public bool IsRequired { get; init; }
}
