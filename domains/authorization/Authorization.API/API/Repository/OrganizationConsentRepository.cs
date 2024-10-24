using API.Models;

namespace API.Repository;

public interface IOrganizationConsentRepository : IGenericRepository<OrganizationConsent>;

public class OrganizationOrganizationConsentRepository(ApplicationDbContext context) : GenericRepository<OrganizationConsent>(context), IOrganizationConsentRepository;
