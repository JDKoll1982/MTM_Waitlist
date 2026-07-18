using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.Core.Contracts.Services;

// Remove this class once your pages/features are using your data.
public interface ISampleDataService
{
    Task<IEnumerable<SampleOrder>> GetContentGridDataAsync(string building);
}
