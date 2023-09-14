using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
}
