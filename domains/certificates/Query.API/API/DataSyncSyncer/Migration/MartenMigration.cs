using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.DataSyncSyncer.Migration;

public class MartenMigration
{
    private readonly MartenHelper martenHelper;
    private readonly DbContextHelper dbContextHelper;
    private readonly ILogger<MartenMigration> logger;

    public MartenMigration(MartenHelper martenHelper, DbContextHelper dbContextHelper, ILogger<MartenMigration> logger)
    {
        this.martenHelper = martenHelper;
        this.dbContextHelper = dbContextHelper;
        this.logger = logger;
    }

    public async Task Migrate()
    {
        logger.LogInformation("Migrate contracts...");
        await MigrateContracts();

        logger.LogInformation("Migrate certificates...");
        await MigrateCertificates();
    }

    private async Task MigrateContracts()
    {
        var existingCount = await dbContextHelper.GetContractCount();
        if (existingCount == 0)
        {
            var contracts = await martenHelper.GetContracts();

            logger.LogInformation("Got {count} contracts from Marten", contracts.Count);

            await dbContextHelper.SaveContracts(contracts);
        }
        else
        {
            logger.LogInformation("Contracts already exists (has {count}), no migration performed", existingCount);
        }
    }

    private async Task MigrateCertificates()
    {
        var existingCount = await dbContextHelper.GetCertificatesCount();
        if (existingCount == 0)
        {
            var events = await martenHelper.GetEvents();

            logger.LogInformation("Got {count} certificates from Marten", events.Count);

            await dbContextHelper.SaveCertificates(events);
        }
        else
        {
            logger.LogInformation("Certificates already exists (has {count}), no migration performed", existingCount);
        }
    }
}
