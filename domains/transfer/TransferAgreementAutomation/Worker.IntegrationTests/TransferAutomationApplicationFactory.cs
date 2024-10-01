using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EnergyTrackAndTrace.Test.Testcontainers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using TransferAgreementAutomation.Worker;
using Xunit;

namespace Worker.IntegrationTest;

public class TransferAutomationApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainer postgresContainer = new();

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ProjectOrigin:WalletUrl", "http://foo");
        builder.UseSetting("Otlp:ReceiverEndpoint", "http://foobar");

        var connectionStringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = postgresContainer.ConnectionString
        };
        builder.UseSetting("Database:Host", (string)connectionStringBuilder["Host"]);
        builder.UseSetting("Database:Port", (string)connectionStringBuilder["Port"]);
        builder.UseSetting("Database:Name", (string)connectionStringBuilder["Database"]);
        builder.UseSetting("Database:User", (string)connectionStringBuilder["Username"]);
        builder.UseSetting("Database:Password", (string)connectionStringBuilder["Password"]);

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.First(s => s.ImplementationType == typeof(TransferAgreementsAutomationWorker)));
        });
    }

    public Task InitializeAsync() => postgresContainer.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync() => await postgresContainer.DisposeAsync();
}
