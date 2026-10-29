using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The values the preview draws.
/// </summary>
/// <remarks>
/// The awkward set exists because the layout has to survive what the plant really types: a part number with a slash,
/// a scrap type in lower case, a work center name long enough to run out of room, and an outside-service part with
/// no quantity on hand.
/// </remarks>
internal static class TempPreviewValues
{
    private const string LongWorkCenter = "Horizontal Saw Line West — Building A, Bay 12, North Aisle";

    public static IReadOnlyList<GenericRequestWorkCenterOption> WorkCenters(TempPreviewScenario scenario) =>
        scenario.UsesAwkwardValues
            ?
            [
                new(LongWorkCenter, "Building A"),
                new("Waterjet 2", "Building B"),
            ]
            :
            [
                new("Laser 1", "Building A"),
                new("Brake Press 3", "Building A"),
                new("Waterjet 2", "Building B"),
            ];

    public static IReadOnlyList<GenericRequestCategoryOption> Categories() =>
    [
        new("Raw material", "Sheet, plate and bar stock held for the floor."),
        new("Weldment", "Parts built up from more than one piece."),
        new("Hardware", "Fasteners and small bought-in parts."),
    ];

    public static IReadOnlyList<GenericRequestItemOption> Items() =>
    [
        new("PART", "Stated part", NeedsDunnage: false, NeedsScrap: false, NeedsDie: false),
        new("DUNNAGE", "Dunnage", NeedsDunnage: true, NeedsScrap: false, NeedsDie: false),
        new("SCRAP", "Scrap type", NeedsDunnage: false, NeedsScrap: true, NeedsDie: false),
        new("DIE", "Die", NeedsDunnage: false, NeedsScrap: false, NeedsDie: true),
    ];

    public static IReadOnlyList<GenericRequestPartOption> Parts(TempPreviewScenario scenario) =>
        scenario.UsesAwkwardValues
            ?
            [
                new("5052/AL-1", "Aluminium sheet, 5052, one eighth inch", "42"),
                new("OUTSIDE/SVC-9", "Outside service part, quantity is agreed when ordered", string.Empty),
            ]
            :
            [
                new("12GA-CS-0001", "Cold rolled steel, twelve gauge", "180"),
                new("5052/AL-1", "Aluminium sheet, 5052, one eighth inch", "42"),
                new("OUTSIDE-SVC-9", "Outside service part", string.Empty),
            ];

    public static IReadOnlyList<GenericRequestSequenceOption> Sequences() =>
    [
        new("10", "Cut to length"),
        new("20", "Form and pierce"),
        new("30", "Finish and inspect"),
    ];

    public static IReadOnlyList<GenericRequestDunnageOption> Dunnage() =>
    [
        new("Aluminum sheet", "DUNN-ALU-01"),
        new("Aluminum sheet", "DUNN-ALU-02"),
        new("Corrugated", "DUNN-COR-02"),
    ];

    public static IReadOnlyList<GenericRequestScrapOption> Scrap(TempPreviewScenario scenario) =>
        scenario.UsesAwkwardValues
            ?
            [
                new("aluminum chips", 3),
                new("steel turnings", 0),
            ]
            :
            [
                new("Aluminum chips", 3),
                new("Steel turnings", 0),
                new("Mixed swarf", 6),
            ];

    public static IReadOnlyList<GenericRequestDieOption> Dies(string searchText, TempPreviewScenario scenario)
    {
        IReadOnlyList<GenericRequestDieOption> all =
        [
            new("D-1001", "Blank die for the left bracket", "Rack 4"),
            new("D-1002", "Pierce die for the cover plate", "Rack 4"),
            new("D-2010", "Form die, long description that runs past the width of the cell so the layout has to ellipsize it", "Rack 7"),
        ];

        return string.IsNullOrWhiteSpace(searchText)
            ? all
            : all.Where(die => die.DieNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || die.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
