using API.Models.Entities;

namespace API.Services.Interfaces;

public interface ICompanyService
{
    Task<Company> UpsertCompanyAsync(Company company);
    Task<Company?> GetCompanyByIdAsync(Guid? id);
    Task<Company?> GetCompanyByTinAsync(string? tin);
}
