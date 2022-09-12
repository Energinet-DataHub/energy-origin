using API.Models;

namespace API.Repository;

public interface IUserStorage
{
    public Task<User?> UserByOidcReferences(string subject, string provider);
}
