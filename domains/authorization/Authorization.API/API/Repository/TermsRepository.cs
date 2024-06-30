using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public interface ITermsRepository : IGenericRepository<Terms>
{
    Task<Terms> GetLatestAsync(CancellationToken cancellationToken);
}

public class TermsRepository(ApplicationDbContext context) : GenericRepository<Terms>(context), ITermsRepository
{
    public async Task<Terms> GetLatestAsync(CancellationToken cancellationToken)
    {
        return (await Context.Set<Terms>()
            .OrderByDescending(t => t.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken))!;
    }
}

