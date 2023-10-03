using API.Models.Entities;

namespace API.Repositories.Interfaces;
public interface ICompanyRepository
{
    //TODO: This is never used
    //Task<Company> UpsertCompanyAsync(Company company);
    Task<Company?> GetCompanyByTinAsync(string tin);
}
