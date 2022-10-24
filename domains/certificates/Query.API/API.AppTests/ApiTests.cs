using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Models;
using Xunit;

namespace API.AppTests;

public class ApiTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public ApiTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetList_AppStarted_ReturnsCertificates()
    {
        var client = factory.CreateClient();
        var certificatesResponse = await client.GetFromJsonAsync<CertificateList>("certificates");

        const int numberOfMeteringPoints = 3;
        const int numberOfHours = 24;
        const int expected = numberOfMeteringPoints * numberOfHours;

        Assert.NotNull(certificatesResponse);
        Assert.Equal(expected, certificatesResponse.Result.Count);
    }
}
