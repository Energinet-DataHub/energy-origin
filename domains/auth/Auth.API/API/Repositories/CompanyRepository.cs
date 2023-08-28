using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
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
    public async Task<Company?> GetCompanyByTinAsync(string tin) => await dataContext.Companies.SingleOrDefaultAsync(x => x.Tin == tin);
}
