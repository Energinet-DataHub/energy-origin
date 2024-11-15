using API.Models;

namespace API.Repository;

public interface IServiceProviderTermsRepository : IGenericRepository<ServiceProviderTerms>;

public class ServiceProviderTermsRepository(ApplicationDbContext context) : GenericRepository<ServiceProviderTerms>(context), IServiceProviderTermsRepository;
