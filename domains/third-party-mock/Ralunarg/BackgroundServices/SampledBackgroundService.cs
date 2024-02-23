using Microsoft.Extensions.Hosting;
using Ralunarg.HttpClients;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ralunarg.BackgroundServices;

public class SampledBackgroundService : BackgroundService
{
    private readonly TokenClient _tokenClient;
    private readonly TeamsClient _teamClient;

    public SampledBackgroundService(TokenClient tokenClient, TeamsClient teamClient)
    {
        _tokenClient = tokenClient;
        _teamClient = teamClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var token = await _tokenClient.GetToken();

            var result = await _teamClient.PostInChannel(token);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
