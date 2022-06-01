using API.Models;

namespace API.Services;

public interface IEnergiDataService
{
    Task<EmissionsDataResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo);
}