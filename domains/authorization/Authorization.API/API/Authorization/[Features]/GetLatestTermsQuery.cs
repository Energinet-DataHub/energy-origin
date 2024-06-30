using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using MediatR;

namespace API.Authorization._Features_;

public record GetLatestTermsQuery() : IRequest<Terms>;

public class GetLatestTermsQueryHandler(ITermsRepository termsRepository)
    : IRequestHandler<GetLatestTermsQuery, Terms>
{
    public async Task<Terms> Handle(GetLatestTermsQuery request, CancellationToken cancellationToken)
    {
        return await termsRepository.GetLatestAsync(cancellationToken);
    }
}
