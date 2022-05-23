using API.Models;

namespace API.Services;

public interface IEnergiDataService
{
    Task<DeclarationProduction> GetDeclarationProduction(DateTime dateFrom, DateTime dataTo, Aggregation aggregation);
}