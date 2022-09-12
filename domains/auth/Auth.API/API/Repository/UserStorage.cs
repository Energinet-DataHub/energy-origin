using API.Models;

namespace API.Repository;

public class UserStorage : IUserStorage
{

    public Task<User?> UserByOidcReferences(string subject, string provider) => Task.FromResult<User?>(null);  //FIXME implement this https://app.zenhub.com/workspaces/fenris---team-board-616bc40121d71900140955f8/issues/energinet-datahub/energy-origin-issues/766
}
