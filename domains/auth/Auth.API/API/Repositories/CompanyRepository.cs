using API.Models.Entities;
using API.Repositories.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ICompanyDataContext dataContext;

    public CompanyRepository(ICompanyDataContext dataContext) => this.dataContext = dataContext;

    public async Task<Company> UpsertCompanyAsync(Company company)
    {
        dataContext.Companies.Update(company);
        await dataContext.SaveChangesAsync();
        return company;
    }

    public async Task<Company?> GetCompanyByIdAsync(Guid id) => await dataContext.Companies.FirstOrDefaultAsync(x => x.Id == id);
    public async Task<Company?> GetCompanyByTinAsync(string tin) => await dataContext.Companies.FirstOrDefaultAsync(x => x.Tin == tin);
}
