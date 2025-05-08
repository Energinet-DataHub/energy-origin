using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Services;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminPortal.Tests.Controllers;

public class CvrControllerTest
{
    [Fact]
    public async Task GivenValidTin_WhenGettingCompanyInformation_ThenReturnsSuccessfulResponse()
    {
        var tin = "12345678";
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var token = await factory.ExtractAntiforgeryTokenAsync(client, "/WhitelistedOrganizations");
        client.DefaultRequestHeaders.Add("RequestVerificationToken", token);

        var response = await client.GetAsync($"/cvr/company/{tin}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CvrCompanyInformationDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(tin, result.Tin);
    }
}
