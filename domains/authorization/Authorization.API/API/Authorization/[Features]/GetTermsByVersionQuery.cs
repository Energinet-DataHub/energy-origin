using System.Threading;
using System.Threading.Tasks;
using API.Models;
using MediatR;

namespace API.Authorization._Features_;

public record GetTermsByVersionQuery(string Version) : IRequest<Terms>;

public class GetTermsByVersionQueryHandler : IRequestHandler<GetTermsByVersionQuery, Terms>
{
    private readonly ITermsRepository _termsRepository;

    public GetTermsByVersionQueryHandler(ITermsRepository termsRepository)
    {
        _termsRepository = termsRepository;
    }

    public async Task<Terms> Handle(GetTermsByVersionQuery request, CancellationToken cancellationToken)
    {
        return await _termsRepository.GetByVersionAsync(request.Version, cancellationToken);
    }
}
