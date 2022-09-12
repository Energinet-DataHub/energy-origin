using API.Models;

namespace API.Repository;

public interface ICompanyStorage
{
    public Task<Company?> CompanyByTin(string tin);
}
