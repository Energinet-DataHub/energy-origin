using AdminPortal.API.Models;

namespace AdminPortal.API.Services;

public class AggregationService
{
    private readonly HttpClient _httpClient;

    public AggregationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MeteringPoint>> GetMeteringPointsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<Api1Response>("api/authorization/portal/first-party-organizations");
        return response?.Result ?? new List<MeteringPoint>();
    }

    public async Task<List<Organization>> GetOrganizationsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<Api2Response>("api/certificates/portal/contracts");
        return response?.Result ?? new List<Organization>();
    }

    public List<AggregatedData> AggregateData(List<MeteringPoint> meteringPoints, List<Organization> organizations)
    {
        var organizationsDict = organizations.ToDictionary(o => o.OrganizationId);

        var groupedMeteringPoints = meteringPoints
            .GroupBy(mp => mp.Gsrn)
            .Select(g => g.OrderByDescending(mp => mp.Created).First())
            .ToList();

        var aggregatedData = new List<AggregatedData>();

        foreach (var mp in groupedMeteringPoints)
        {
            if (organizationsDict.TryGetValue(mp.MeteringPointOwnerId, out var org))
            {
                aggregatedData.Add(new AggregatedData
                {
                    Gsrn = mp.Gsrn,
                    MeteringPointType = mp.MeteringPointType,
                    OrganizationId = org.OrganizationId,
                    OrganizationName = org.OrganizationName,
                    Tin = org.Tin
                });
            }
        }

        return aggregatedData;
    }
}
