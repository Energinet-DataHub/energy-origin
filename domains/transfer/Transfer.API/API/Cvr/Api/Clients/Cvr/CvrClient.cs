using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Models;

namespace API.Cvr.Api.Clients.Cvr;

public interface ICvrClient
{
    Task<Root?> CvrNumberSearch(IEnumerable<CvrNumber> cvrNumbers);
}

public class CvrClient(HttpClient client) : ICvrClient
{
    private const int BatchSize = 25;

    public async Task<Root?> CvrNumberSearch(IEnumerable<CvrNumber> cvrNumbers)
    {
        var from = 0;
        var cvrArray = cvrNumbers.ToArray();
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < cvrNumbers.Count(); i += BatchSize)
        {
            var batch = cvrArray.Skip(i).Take(BatchSize);
            var serializedBatch = JsonSerializer.Serialize(batch);
            var body = GetBody(serializedBatch, from, BatchSize);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            tasks.Add(client.PostAsync("cvr-permanent/virksomhed/_search", content));

        }

        var responses = await Task.WhenAll(tasks);

        var result = new Root
        {
            hits = new HitsRoot
            {
                hits = []
            }
        };

        foreach (var response in responses)
        {
            var root = await response.Content.ReadFromJsonAsync<Root>();
            if (root?.hits?.hits is not null)
            {
                result.hits.hits.AddRange(root.hits.hits);
            }
        }

        return result;

    }

    private static string GetBody(string cvrNumbersArray, int from, int size)
    {
        return $@"{{
                    ""from"": {from},
                    ""size"": {size},
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
    }
}
