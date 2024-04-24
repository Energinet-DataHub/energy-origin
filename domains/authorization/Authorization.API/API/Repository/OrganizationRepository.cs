using API.Models;

namespace API.Repository;

public class OrganizationRepository(ApplicationDbContext context) : GenericRepository<Organization>(context);
public interface IOrganizationRepository : IGenericRepository<Organization>;

