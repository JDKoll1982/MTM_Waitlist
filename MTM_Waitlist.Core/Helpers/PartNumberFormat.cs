namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// The shape of the part numbers the plant records: a known prefix followed by a fixed-width number.
/// </summary>
/// <remarks>
/// The width moved here because two libraries that may not reference each other both need it: the incorrect-part
/// search that pads a typed term, and the die search that builds a die number from the digits alone
/// (024-dependency-modernization, MOD-007). A prefix list is not restated here — that stays with
/// <c>PartPictureLayout</c>, which is where the families are declared.
/// </remarks>
public static class PartNumberFormat
{
    /// <summary>The width a part number with a known prefix is padded to, in characters.</summary>
    public const int Width = 10;
}
