using API.Models;

namespace API.Services;

public interface IEnergiDataService
{
    Task<EmissionsResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo);
}