using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Models;

namespace API.Cvr.Api.Clients.Cvr;

public class CvrClient
{
    private readonly HttpClient client;

    public CvrClient(HttpClient client)
    {
        this.client = client;
    }

    public Task<Root?> CvrNumberSearch(CvrNumber cvr)
    {
        return CvrNumberSearch(new List<CvrNumber> { cvr });
    }

    public async Task<Root?> CvrNumberSearch(IEnumerable<CvrNumber> cvrNumbers)
    {
        var cvrNumbersArray = JsonSerializer.Serialize(cvrNumbers);

        var postBody = $@"{{
                            ""query"": {{
                                ""bool"": {{
                                    ""must"": [
                                        {{
                                            ""terms"": {{
                                                ""Vrvirksomhed.cvrNummer"": {cvrNumbersArray}
                                            }}
                                        }}
                                    ]
                                }}
                            }}
                        }}";

        var content = new StringContent(postBody, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("cvr-permanent/virksomhed/_search", content);

        return await res.Content.ReadFromJsonAsync<Root>();
    }
}
