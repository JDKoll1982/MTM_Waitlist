namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IBuildingSelectionService
{
    event EventHandler? BuildingChanged;

    IReadOnlyList<string> Buildings { get; }

    string SelectedBuilding { get; set; }
}
