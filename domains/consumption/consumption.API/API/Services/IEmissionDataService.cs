using API.Models;

namespace API.Services;

public interface IEmissionDataService
{
    Task<EmissionsDataResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo);

    Task<ProductionEmission> GetProductionEmission(DateTime dateFrom, DateTime dateTo);
}
