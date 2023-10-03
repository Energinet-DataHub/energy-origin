using API.Models.Entities;

namespace API.Services.Interfaces;

public interface ICompanyService
{
    Task<Company> InsertCompanyAsync(Company company);
    Task<Company?> GetCompanyByTinAsync(string? tin);
}
