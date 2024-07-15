using API.Models;

namespace API.Repository;

public interface ITermsRepository : IGenericRepository<Terms>;

public class TermsRepository(ApplicationDbContext context) : GenericRepository<Terms>(context), ITermsRepository;
