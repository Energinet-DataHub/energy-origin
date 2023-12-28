using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

    public async Task<Root?> CvrNumberSearch(CvrNumber cvr)
    {
        var postBody = @"{
                            ""query"": {
                                ""bool"": {
                                    ""must"": [
                                        {
                                            ""term"": {
                                                ""Vrvirksomhed.cvrNummer"":" + cvr + @"
                                            }
                                        }
                                    ]
                                }
                            }
                        }";

        var content = new StringContent(postBody, Encoding.UTF8, "application/json");

        var res = await client.PostAsync("cvr-permanent/virksomhed/_search", content);

        return await res.Content.ReadFromJsonAsync<Root>();
    }
}
