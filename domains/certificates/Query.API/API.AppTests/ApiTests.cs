using System.Net;
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
    public async Task GetList_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var client = factory.CreateUnauthenticatedClient();
        var certificatesResponse = await client.GetAsync("certificates");

        Assert.Equal(HttpStatusCode.Unauthorized, certificatesResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_AuthenticatedUser_ReturnsCertificates()
    {
        var client = factory.CreateAuthenticatedClient();
        var certificatesResponse = await client.GetFromJsonAsync<CertificateList>("certificates");

        const int numberOfMeteringPoints = 3;
        const int numberOfHours = 24;
        const int expected = numberOfMeteringPoints * numberOfHours;

        Assert.NotNull(certificatesResponse);
        Assert.Equal(expected, certificatesResponse.Result.Count);
    }
}
