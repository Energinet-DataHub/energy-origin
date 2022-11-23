using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API;

public class AuthCallWorker : BackgroundService
{
    private readonly HttpClient client;
    private readonly ILogger<AuthCallWorker> logger;

    public AuthCallWorker(HttpClient client, ILogger<AuthCallWorker> logger)
    {
        this.client = client;
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var uri = "http://eo-auth/user/uuid?cvr=12121212";

        logger.LogInformation("uri: {uri}", uri);

        var response = await client.GetAsync(uri, stoppingToken);

        logger.LogInformation("status code: {statusCode}", response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(stoppingToken);

        logger.LogInformation("content: {content}", content);
    }
}
