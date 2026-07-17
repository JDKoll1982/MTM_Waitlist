using MTM_Waitlist.Contracts.Services;

namespace MTM_Waitlist.Services;

public class BuildingSelectionService : IBuildingSelectionService
{
    private string _selectedBuilding;

    public event EventHandler? BuildingChanged;

    public IReadOnlyList<string> Buildings { get; } = ["Expo Drive", "Vits Drive"];

    public string SelectedBuilding
    {
        get => _selectedBuilding;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (string.Equals(_selectedBuilding, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedBuilding = value;
            BuildingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public BuildingSelectionService()
    {
        _selectedBuilding = Buildings[0];
    }
}
