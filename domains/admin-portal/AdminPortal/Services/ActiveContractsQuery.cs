using System.Linq;
using System.Threading.Tasks;
using AdminPortal.Dtos;

namespace AdminPortal.Services;
public interface IAggregationQuery
{
    Task<ActiveContractsResponse> GetActiveContractsAsync();
}

public class ActiveContractsQuery : IAggregationQuery
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICertificatesService _certificatesService;

    public ActiveContractsQuery(IAuthorizationService authorizationService, ICertificatesService certificatesService)
    {
        _authorizationService = authorizationService;
        _certificatesService = certificatesService;
    }

    public async Task<ActiveContractsResponse> GetActiveContractsAsync()
    {

        var organizations = await _authorizationService.GetOrganizationsAsync();
        var contracts = await _certificatesService.GetContractsAsync();

        var meteringPoints = contracts.Result
            .Join(organizations.Result,
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

        return new ActiveContractsResponse
        {
            Results = new ResultsData { MeteringPoints = meteringPoints }
        };
    }
}
