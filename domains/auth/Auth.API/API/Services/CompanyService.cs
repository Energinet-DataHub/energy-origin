using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository repository;

    public CompanyService(ICompanyRepository repository) => this.repository = repository;

    public async Task<Company> UpsertCompanyAsync(Company company) => await repository.UpsertCompanyAsync(company);
    public async Task<Company?> GetCompanyByIdAsync(Guid? id) => id is null ? null : await repository.GetCompanyByIdAsync(id.Value);
    public async Task<Company?> GetCompanyByTinAsync(string? tin) => tin is null ? null : await repository.GetCompanyByTinAsync(tin);
}
