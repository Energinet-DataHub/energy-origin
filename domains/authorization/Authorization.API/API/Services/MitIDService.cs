using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Authorization.Controllers;

namespace API.Services;

public interface IMitIDService
{
    Task<MitIdUserinfoResponse> GetUserinfo(string bearerToken);
}

public class MitIDService(HttpClient httpClient) : IMitIDService
{
    public async Task<MitIdUserinfoResponse> GetUserinfo(string bearerToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        var response = await httpClient.GetAsync("https://pp.netseidbroker.dk/op/connect/userinfo");

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<MitIdUserinfoResponse>())!;
    }
}
