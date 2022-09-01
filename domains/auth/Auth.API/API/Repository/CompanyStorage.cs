using API.Models;

namespace API.Repository;

public class CompanyStorage : ICompanyStorage
{
    public Task<Company?> CompanyByTin(string tin) => Task.FromResult<Company?>(null);  //FIXME implement this
}
