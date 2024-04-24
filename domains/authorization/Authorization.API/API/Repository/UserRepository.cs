using API.Models;

namespace API.Repository;

public class UserRepository(ApplicationDbContext context) : GenericRepository<User>(context);
public interface IUserRepository : IGenericRepository<User>;
