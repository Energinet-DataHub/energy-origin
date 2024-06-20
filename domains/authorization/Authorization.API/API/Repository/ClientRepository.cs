using API.Models;

namespace API.Repository;

public interface IClientRepository : IGenericRepository<Client>;

public class ClientRepository(ApplicationDbContext context) : GenericRepository<Client>(context), IClientRepository;
