using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.API.Dtos;

namespace AdminPortal.API.Services;
public interface IAggregationService
{
    Task<ActiveContractsResponse> GetActiveContractsAsync();
}

public class AggregationService : IAggregationService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AggregationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ActiveContractsResponse> GetActiveContractsAsync()
    {
        var firstPartyClient = _httpClientFactory.CreateClient("FirstPartyApi");
        var contractsClient = _httpClientFactory.CreateClient("ContractsApi");

        var organizations = await GetOrganizationsAsync(firstPartyClient);
        var contracts = await GetContractsAsync(contractsClient);

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

    private async Task<FirstPartyOrganizationsResponse> GetOrganizationsAsync(HttpClient client)
    {
        var response = await client.GetAsync("first-party-organizations/");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FirstPartyOrganizationsResponse>();
    }

    private async Task<ContractsForAdminPortalResponse> GetContractsAsync(HttpClient client)
    {
        var response = await client.GetAsync("internal-contracts");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>();
    }
}
