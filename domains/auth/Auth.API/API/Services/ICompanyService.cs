using API.Models.Entities;

namespace API.Services;

public interface ICompanyService
{
    Task<Company> UpsertCompanyAsync(Company company);
    Task<Company?> GetCompanyByIdAsync(Guid? companyId);
    Task<Company?> GetCompanyByTinAsync(string? tin);
}
