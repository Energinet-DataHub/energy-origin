using API.Models;

namespace API.Repository;

public class AffiliationRepository(ApplicationDbContext context) : GenericRepository<Affiliation>(context);
public interface IAffiliationRepository : IGenericRepository<Affiliation>;
