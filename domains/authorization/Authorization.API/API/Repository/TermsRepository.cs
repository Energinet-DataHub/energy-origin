using API.Models;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API.Repository
{
    public interface ITermsRepository : IGenericRepository<Terms>
    {
        Task<Terms> GetByVersionAsync(string version, CancellationToken cancellationToken);
    }

    public class TermsRepository : GenericRepository<Terms>, ITermsRepository
    {
        public TermsRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Terms> GetByVersionAsync(string version, CancellationToken cancellationToken)
        {
            return await Context.Set<Terms>().FirstOrDefaultAsync(t => t.Version == version, cancellationToken);
        }
    }
}
