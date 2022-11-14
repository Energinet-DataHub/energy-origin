using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API;

internal class DeleteThisDatabaseCheckLaterWorker : BackgroundService
{
    private readonly IDocumentStore store;
    private readonly ILogger<DeleteThisDatabaseCheckLaterWorker> logger;

    public DeleteThisDatabaseCheckLaterWorker(IDocumentStore store, ILogger<DeleteThisDatabaseCheckLaterWorker> logger)
    {
        this.store = store;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
            logger.LogInformation("Database connection works!");
        }
        catch (Exception)
        {
            logger.LogError("Database connection fails");
        }
    }
}
