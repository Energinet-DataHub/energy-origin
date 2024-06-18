using API.Models;

namespace API.Repository;

public interface IConsentRepository : IGenericRepository<Consent>;

public class ConsentRepository(ApplicationDbContext context) : GenericRepository<Consent>(context), IConsentRepository;
