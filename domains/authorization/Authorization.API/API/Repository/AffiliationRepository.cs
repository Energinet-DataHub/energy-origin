using API.Models;

namespace API.Repository;

public interface IAffiliationRepository : IGenericRepository<Affiliation>;

public class AffiliationRepository(ApplicationDbContext context) : GenericRepository<Affiliation>(context), IAffiliationRepository;

