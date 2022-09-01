using API.Models;

namespace API.Repository;

public class UserStorage : IUserStorage
{

    public Task<User?> UserByOidcReferences(string subject, string provider) => Task.FromResult<User?>(null);  //FIXME implement this
}
