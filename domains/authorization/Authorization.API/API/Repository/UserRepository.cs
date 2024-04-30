using API.Models;

namespace API.Repository;

public interface IUserRepository : IGenericRepository<User>;
public class UserRepository(ApplicationDbContext context) : GenericRepository<User>(context), IUserRepository;
