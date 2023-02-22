using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Helpers;
using API.AppTests.Infrastructure;
using API.AppTests.Mocks;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class ContractBugTest :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IDisposable
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public ContractBugTest(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer)
    {
        dataSyncWireMock = new DataSyncWireMock(port: 9004);
        this.factory = factory;
        this.factory.MartenConnectionString  = martenDbContainer.ConnectionString; //If running docker compose setup, this line can be replaced with "host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;" and you remove the interface IClassFixture<MartenDbContainer> + "martenDbContainer" from constructor. This will run a bit faster
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
    }

    [Fact]
    public async Task Test()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        var client = factory.CreateAuthenticatedClient(subject);

        var startingContracts = await client.GetAsync("api/certificates/contracts");
        startingContracts.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var now = DateTimeOffset.Now;
        var responseMessages = await Task.WhenAll(
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(1).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(2).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(3).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(4).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(5).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(6).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(7).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(8).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(9).ToUnixTimeSeconds() }),
            client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now.AddMinutes(10).ToUnixTimeSeconds() })
        );
        //responseMessages.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Created));
        
        var contracts = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");
        contracts!.Result.Should().HaveCount(1);
    }

    public void Dispose() => dataSyncWireMock.Dispose();
}
