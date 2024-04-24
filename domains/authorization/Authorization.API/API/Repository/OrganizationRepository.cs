using API.Models;

namespace API.Repository;

public interface IOrganizationRepository : IGenericRepository<Organization>;
public class OrganizationRepository(ApplicationDbContext context) : GenericRepository<Organization>(context), IOrganizationRepository;
