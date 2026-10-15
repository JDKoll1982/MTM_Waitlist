using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// The rule the fuzzy incorrect-part search applies to the term the operator types, before the live read is asked
/// about it (FR-082, D24).
/// </summary>
/// <remarks>
/// <para>
/// The term is upper-cased, and it is zero-padded to ten characters when it starts with a prefix the application
/// knows — <c>MMC</c> for coil, <c>MMF</c> for flatstock, <c>FGT</c> for die. The prefix is what says how wide the
/// number is, so <c>MMC1000</c> becomes <c>MMC0001000</c> and <c>MMF500</c> becomes <c>MMF0000500</c>, while a term
/// with no known prefix is upper-cased and otherwise left exactly as it was typed.
/// </para>
/// <para>
/// The prefix list is the application's own, read from <see cref="PartPictureLayout.KnownPartNumberPrefixes"/>
/// rather than restated here, so the search cannot recognize a different set of families from the rest of the
/// application. There was no existing zero-padding rule in the repository: this is where it is written down.
/// </para>
/// </remarks>
public static class PartMatchSearchInput
{
    /// <summary>The width a part number with a known prefix is padded to, in characters.</summary>
    public const int PartNumberWidth = 10;

    /// <summary>
    /// The term as the live read is asked about it: upper-cased, and zero-padded to
    /// <see cref="PartNumberWidth"/> characters when it starts with a known prefix.
    /// </summary>
    /// <param name="term">What the operator typed, or null.</param>
    /// <returns>
    /// The normalized term, or an empty string when nothing was typed. A term whose prefix is followed by anything
    /// other than digits is upper-cased and left alone rather than padded into a number it is not.
    /// </returns>
    public static string Normalize(string? term)
    {
        var trimmed = (term ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var normalized = trimmed.ToUpperInvariant();

        foreach (var prefix in PartPictureLayout.KnownPartNumberPrefixes)
        {
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var digits = normalized[prefix.Length..];
            if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
            {
                return normalized;
            }

            return prefix + digits.PadLeft(PartNumberWidth - prefix.Length, '0');
        }

        return normalized;
    }
}
