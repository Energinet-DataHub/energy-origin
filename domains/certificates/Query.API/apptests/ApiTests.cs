using System.Net.Http.Json;
using System.Threading.Tasks;
using src.Controllers;
using Xunit;

namespace apptests;

public class ApiTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public ApiTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetList_AppStarted_ReturnsFiveCertificates()
    {
        var client = factory.CreateClient();
        var certificatesResponse = await client.GetFromJsonAsync<CertificateList>("v1/certificates");
        
        Assert.Equal(5, certificatesResponse.Result.Count);
    }
}
