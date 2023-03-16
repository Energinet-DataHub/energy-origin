using API.Models.Entities;

namespace API.Repositories;
public interface ICompanyRepository
{
    Task<Company> UpsertCompanyAsync(Company company);
    Task<Company?> GetCompanyByIdAsync(Guid id);
    Task<Company?> GetCompanyByTinAsync(string tin);
}
