using API.Models;
using API.Models.EnergiDataService;

namespace API.Services;

public interface IEnergiDataService
{
    Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo);
    Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo);
}
