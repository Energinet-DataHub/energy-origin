using API.Models;

namespace API.Repository;

public interface IWhitelistedRepository : IGenericRepository<Whitelisted>;

public class WhitelistedRepository(ApplicationDbContext context) : GenericRepository<Whitelisted>(context), IWhitelistedRepository;
