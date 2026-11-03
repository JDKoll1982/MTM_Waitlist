using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// The categories a generic request is raised under — the work-order wizard's own list, less the one it does not use.
/// </summary>
/// <remarks>
/// <para>
/// The two flows describe a request to the same handler, so a category cannot mean one thing at a work order and
/// something else at a generic one. The names are therefore read from the work-order wizard's own resource keys rather
/// than restated here: a change to that vocabulary reaches both screens, and neither can drift into names of its own.
/// </para>
/// <para>
/// <b>Assist is not offered.</b> The generic flow asks for what the requester needs brought or moved; a request for
/// help with a thing belongs to a job, where the work order says what is being handled.
/// </para>
/// <para>
/// The fallback is the plain word, which is what the work-order flow shows when a key does not resolve. That is also
/// what a host without those resources renders — the preview host ships its own resource map, and both flows still
/// read the same names there.
/// </para>
/// </remarks>
public static class GenericRequestCategoryCatalog
{
    /// <summary>
    /// The categories in the order the work-order wizard lists them (Pickup, Deliver, Other).
    /// </summary>
    public static IReadOnlyList<GenericRequestCategoryOption> All() =>
    [
        Option("Pickup", "Move a thing from another location to this work center."),
        Option("Deliver", "Deliver a thing to this work center."),
        Option("Other", "Anything else, described in your own words."),
    ];

    private static GenericRequestCategoryOption Option(string name, string description) =>
        new(name, ResolveName(name), description);

    /// <summary>
    /// The category's name as the work-order wizard spells it, or the plain word when that key is not in this host.
    /// </summary>
    private static string ResolveName(string name)
    {
        var key = $"RequestCategory_{name}.Name";
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? name
            : localized;
    }
}
