namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The data type of the value captured for a request item's Line 2 identifier / payload.
/// Type/Category/Item refactor (2026-09-07). Source: Request-Config-Template.csv (col 10).
/// </summary>
public enum RequestItemValueType
{
    /// <summary>A single text identifier (e.g. coil number, part number).</summary>
    String,

    /// <summary>A value chosen from a bounded set (enum).</summary>
    Enum,

    /// <summary>Free-form multi-word text (e.g. a described request / assistance).</summary>
    Text
}
