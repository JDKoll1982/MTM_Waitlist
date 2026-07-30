using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.Core.Contracts.Services;

public interface ISampleDataService
{
    Task<IEnumerable<SampleOrder>> GetContentGridDataAsync(string building);
}
