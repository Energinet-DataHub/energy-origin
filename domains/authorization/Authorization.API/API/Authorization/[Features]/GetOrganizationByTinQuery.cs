using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record GetOrganizationByTinQuery(string Tin) : IRequest<Organization>;

public class GetOrganizationByTinQueryHandler : IRequestHandler<GetOrganizationByTinQuery, Organization>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationByTinQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<Organization> Handle(GetOrganizationByTinQuery request, CancellationToken cancellationToken)
    {
        return (await _organizationRepository.Query().FirstOrDefaultAsync(o => o.Tin.Value == request.Tin, cancellationToken))!;
    }
}
