namespace MTM_Waitlist.Contracts.Services;

public interface IBuildingSelectionService
{
    event EventHandler? BuildingChanged;

    IReadOnlyList<string> Buildings { get; }

    string SelectedBuilding { get; set; }
}
