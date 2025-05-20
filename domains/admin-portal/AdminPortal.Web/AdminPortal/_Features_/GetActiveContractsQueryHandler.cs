using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;
public class GetActiveContractsQuery : IRequest<GetActiveContractsResponse>
{
}

public class GetActiveContractsQueryHandler : IRequestHandler<GetActiveContractsQuery, GetActiveContractsResponse>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICertificatesService _certificatesService;

    public GetActiveContractsQueryHandler(IAuthorizationService authorizationService, ICertificatesService certificatesService)
    {
        _authorizationService = authorizationService;
        _certificatesService = certificatesService;
    }

    public async Task<GetActiveContractsResponse> Handle(GetActiveContractsQuery request, CancellationToken cancellationToken)
    {
        var organizations = (await _authorizationService.GetOrganizationsAsync(cancellationToken)).Result;
        var contracts = (await _certificatesService.GetContractsHttpRequestAsync()).Result;

        var meteringPoints = contracts
            .Join(organizations,
                contract => contract.MeteringPointOwner,
                org => org.OrganizationId.ToString(),
                (contract, org) => new MeteringPoint
                {
                    GSRN = contract.GSRN,
                    MeteringPointType = contract.MeteringPointType,
                    OrganizationName = org.OrganizationName,
                    Tin = org.Tin,
                    Created = contract.Created,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate
                })
            .ToList();

        return new GetActiveContractsResponse
        {
            Results = new ResultsData { MeteringPoints = meteringPoints }
        };
    }
}
