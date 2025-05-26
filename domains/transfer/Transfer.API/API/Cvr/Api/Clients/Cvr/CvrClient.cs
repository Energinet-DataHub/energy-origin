using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Models;
using Microsoft.Extensions.Logging;

namespace API.Cvr.Api.Clients.Cvr;

public interface ICvrClient
{
    Task<Root?> CvrNumberSearch(IEnumerable<CvrNumber> cvrNumbers);
}

public class CvrClient(HttpClient client, ILogger<CvrClient> logger) : ICvrClient
{
    private const int From = 0;
    private const int BatchSize = 25;

    public async Task<Root?> CvrNumberSearch(IEnumerable<CvrNumber> cvrNumbers)
    {
        var cvrArray = cvrNumbers.ToArray();
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < cvrNumbers.Count(); i += BatchSize)
        {
            var batch = cvrArray.Skip(i).Take(BatchSize);
            var serializedBatch = JsonSerializer.Serialize(batch);
            var body = GetBody(serializedBatch, From, BatchSize);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            tasks.Add(client.PostAsync("cvr-permanent/virksomhed/_search", content));
        }

        var rootResult = new Root
        {
            hits = new HitsRoot
            {
                hits = []
            }
        };

        var responses = await Task.WhenAll(tasks);
        foreach (var response in responses)
        {
            var root = await response.Content.ReadFromJsonAsync<Root>();
            if (root?.hits?.hits is not null)
            {
                rootResult.hits.hits.AddRange(root.hits.hits);
            }
        }

        if (cvrArray.Length != rootResult.hits.hits.Count)
        {
            logger.LogWarning(
                    "The expected number of CVR records were not fetched. Expected {Expected}, Actual {Actual}",
                    cvrArray.Length, rootResult.hits.hits.Count);
        }

        return rootResult;
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
