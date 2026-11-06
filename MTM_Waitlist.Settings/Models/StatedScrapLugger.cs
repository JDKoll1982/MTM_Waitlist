namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// What a stated scrap type resolves to, through the same arrangement routine the work-order scrap card calls.
/// </summary>
/// <param name="LuggerLocation">The lugger's name, or <c>null</c> when none is assigned and when the arrangement was not read.</param>
/// <param name="LuggerImagePath">The named lugger's picture, or empty when it has none.</param>
/// <param name="ArrangementRead">Whether the arrangement read answered.</param>
/// <remarks>
/// It encodes the three outcomes the card has to tell apart: a named lugger (<c>true</c>, a name), a configuration
/// gap (<c>true</c>, <c>null</c>) and an unreadable arrangement (<c>false</c>, <c>null</c>). It carries no state of
/// its own and reads nothing; the caller that holds one decides how to word it.
/// </remarks>
public sealed record StatedScrapLugger(string? LuggerLocation, string LuggerImagePath, bool ArrangementRead);
