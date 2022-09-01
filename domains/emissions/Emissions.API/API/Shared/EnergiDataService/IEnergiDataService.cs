using API.Shared.EnergiDataService.Models;

namespace API.Shared.EnergiDataService;

public interface IEnergiDataService
{
    Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo);

    Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo);
}
