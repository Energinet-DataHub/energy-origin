using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using API.Cvr.Models;
using Newtonsoft.Json;

namespace API.Cvr;

public class CvrClient
{
    private readonly HttpClient client;

    public CvrClient(HttpClient client)
    {
        this.client = client;
    }

    public async Task<Root?> CvrNumberSearch(string cvr)
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

        return JsonConvert.DeserializeObject<Root>(res.Content.ReadAsStringAsync().Result);
    }
}
