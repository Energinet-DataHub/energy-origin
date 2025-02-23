using System.Linq;
using System.Threading.Tasks;
using AdminPortal.Dtos;

namespace AdminPortal.Services;
public interface IAggregationService
{
    Task<ActiveContractsResponse> GetActiveContractsAsync();
}

public class ActiveContractsService : IAggregationService
{
    private readonly IAuthorizationFacade _authorizationFacade;
    private readonly ICertificatesFacade _certificatesFacade;

    public ActiveContractsService(IAuthorizationFacade authorizationFacade, ICertificatesFacade certificatesFacade)
    {
        _authorizationFacade = authorizationFacade;
        _certificatesFacade = certificatesFacade;
    }

    public async Task<ActiveContractsResponse> GetActiveContractsAsync()
    {

        var organizations = await _authorizationFacade.GetOrganizationsAsync();
        var contracts = await _certificatesFacade.GetContractsAsync();

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
