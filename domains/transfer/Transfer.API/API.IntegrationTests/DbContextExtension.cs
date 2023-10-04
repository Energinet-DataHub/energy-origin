using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests;

public static class DbContextExtension
{
    public static async Task<List<T>> RepeatedlyQueryUntilCountIsMet<T>(this DbContext dbContext, int count, TimeSpan? timeLimit = null)
        where T : class
    {
        var limit = timeLimit ?? TimeSpan.FromSeconds(15);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var entities = await dbContext.Set<T>().ToListAsync();
            if (entities.Count() == count)
                return entities;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception($"Entity not found within the time limit ({limit.TotalSeconds} seconds)");
    }

    public static async Task TruncateTransferAgreementsTable(this ApplicationDbContext dbContext)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement)).GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{agreementsTable}\" CASCADE");
    }

    public static async Task TruncateTransferAgreementHistoryTables(this ApplicationDbContext dbContext)
    {
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry)).GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{historyTable}\"");
    }
}
