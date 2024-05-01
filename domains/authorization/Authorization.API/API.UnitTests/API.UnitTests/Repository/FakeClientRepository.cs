using API.Models;
using API.Repository;

namespace API.UnitTests.Repository;

public class FakeClientRepository : FakeGenericRepository<Client>, IClientRepository
{

}
