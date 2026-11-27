using System.Globalization;

namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// The coil weight as a person reads it: the machine's own number formatting, and a unit that comes from a resource
/// rather than from a literal in code (FR-024).
/// </summary>
/// <remarks>
/// <para>
/// A weight read from the receiving store is a number of pounds. Turning it into words belongs where the words are
/// composed, not in the read: the service that asks the store returns the figure, and this is the one place that
/// formats it, so every surface that shows a weight shows the same thing on the same machine
/// (024-dependency-modernization, UX-003).
/// </para>
/// <para>
/// The separator is the current culture's, so a machine that writes a decimal comma or groups with a space writes the
/// weight that way too. The unit is the <c>CoilWeight_Unit</c> entry in <c>Strings/en-us/Resources.resw</c>, which is
/// the translation point for every language the application gains.
/// </para>
/// </remarks>
public static class CoilWeightText
{
    /// <summary>The resource key holding the weight's unit, so a translation can change the word without a code change.</summary>
    public const string UnitKey = "CoilWeight_Unit";

    /// <summary>
    /// The weight in pounds as it is written on this machine, or empty when nothing was weighed. A missing resource
    /// leaves the number on its own rather than a wrong unit.
    /// </summary>
    /// <param name="pounds">The weight in whole pounds, or null when the store held no figure.</param>
    public static string From(decimal? pounds)
    {
        if (pounds is not { } value)
        {
            return string.Empty;
        }

        var number = value.ToString("N0", CultureInfo.CurrentCulture);
        var unit = UnitKey.GetLocalized();

        return string.Equals(unit, UnitKey, StringComparison.Ordinal) ? number : $"{number} {unit}";
    }
}
