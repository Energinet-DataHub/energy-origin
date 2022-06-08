using API.Models;

namespace API.Services;

public interface IEmissionDataService
{
    Task<EmissionsDataResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo);

    Task<DeclarationProduction> GetDeclarationProduction(DateTime dateFrom, DateTime dataTo);
}
