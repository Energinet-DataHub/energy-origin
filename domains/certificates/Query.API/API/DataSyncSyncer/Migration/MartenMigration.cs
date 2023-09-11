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
        await MigrateContracts();
    }

    private async Task MigrateContracts()
    {
        logger.LogInformation("Migrate contracts...");
        var existingContractCount = await dbContextHelper.GetContractCount() == 0;
        if (existingContractCount)
        {
            var contracts = await martenHelper.GetContracts();

            logger.LogInformation("Got {count} contracts from Marten", contracts.Count);

            await dbContextHelper.SaveContracts(contracts);

            logger.LogInformation("Migrated contracts");
        }
        else
        {
            logger.LogInformation("Contracts already exists, no migration performed");
        }
    }
}
