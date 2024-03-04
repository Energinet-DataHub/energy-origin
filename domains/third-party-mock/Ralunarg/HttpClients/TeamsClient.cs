using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ralunarg.Models;

namespace Ralunarg.HttpClients;

public class TeamsClient
{
    private readonly HttpClient _httpClient;

    public TeamsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> PostInChannel(JwtToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "manual/paths/invoke")
        {
            Content = new StringContent("{\r\n    \"version\": \"4\",\r\n    \"groupKey\": \"{}:{alertname=\\\"high_memory_load\\\"}\",\r\n    \"status\": \"firing\",\r\n    \"receiver\": \"teams_proxy\",\r\n    \"groupLabels\": {\r\n        \"alertname\": \"high_memory_load\"\r\n    },\r\n    \"commonLabels\": {\r\n        \"alertname\": \"high_memory_load\",\r\n        \"monitor\": \"master\",\r\n        \"severity\": \"warning\"\r\n    },\r\n    \"commonAnnotations\": {\r\n        \"summary\": \"Server High Memory usage\"\r\n    },\r\n    \"externalURL\": \"http://docker.for.mac.host.internal:9093\",\r\n    \"alerts\": [\r\n        {\r\n            \"labels\": {\r\n                \"alertname\": \"high_memory_load\",\r\n                \"instance\": \"10.80.40.11:9100\",\r\n                \"job\": \"docker_nodes\",\r\n                \"monitor\": \"master\",\r\n                \"severity\": \"warning\"\r\n            },\r\n            \"annotations\": {\r\n                \"description\": \"10.80.40.11 reported high memory usage with 23.28%.\",\r\n                \"summary\": \"Server High Memory usage\"\r\n            },\r\n            \"startsAt\": \"2018-03-07T06:33:21.873077559-05:00\",\r\n            \"endsAt\": \"0001-01-01T00:00:00Z\"\r\n        }\r\n    ]\r\n}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return true;
        }

        throw new Exception("Something went wrong when posting in the channel!");
    }
}
