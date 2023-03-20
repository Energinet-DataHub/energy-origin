using System.ComponentModel.Design;
using API.Models.Entities;
using API.Repositories;

namespace API.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository companyRepository;

    public CompanyService(ICompanyRepository companyRepository) => this.companyRepository = companyRepository;

    public async Task<Company> UpsertCompanyAsync(Company company) => await companyRepository.UpsertCompanyAsync(company);
    public async Task<Company?> GetCompanyByIdAsync(Guid? companyId) => companyId is null ? null : await companyRepository.GetCompanyByIdAsync(companyId.Value);
    public async Task<Company?> GetCompanyByTinAsync(string? tin) => tin is null ? null : await companyRepository.GetCompanyByTinAsync(tin);
}
