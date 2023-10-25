using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Shared.Data;
using API.Transfer.Api.Models;
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

    public static async Task TruncateTransferAgreementsTables(this ApplicationDbContext dbContext)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement))!.GetTableName();
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry))!.GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{agreementsTable}\" CASCADE");
        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{historyTable}\"");
    }

    public static async Task TruncateClaimSubjectsTables(this ApplicationDbContext dbContext)
    {
        var claimSubjectTable = dbContext.Model.FindEntityType(typeof(ClaimSubject))!.GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{claimSubjectTable}\" CASCADE");
    }

    public static async Task TruncateClaimSubjectHistoryTables(this ApplicationDbContext dbContext)
    {
        var claimSubjectTable = dbContext.Model.FindEntityType(typeof(ClaimSubjectHistory))!.GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{claimSubjectTable}\" CASCADE");
    }

    public static void RemoveAll<T>(this ApplicationDbContext dbContext, Func<ApplicationDbContext, DbSet<T>> getDbSet) where T : class
    {
        var dbSet = getDbSet(dbContext);
        dbSet.RemoveRange(dbSet.ToList());
        dbContext.SaveChanges();
    }
}
