using API.Models;

namespace API.Services;

public interface IEnergyDataService
{
    Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo);

    Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTime dateFrom, DateTime dateTo);
}
