using API.Models;

namespace API.Repository;

public class ConsentRepository(ApplicationDbContext context) : GenericRepository<Consent>(context);
public interface IConsentRepository : IGenericRepository<Consent>;
