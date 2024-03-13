using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using API.MeteringPoints.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public static class DbContextExtensension
{
    public static async Task<T> RepeatedlyFirstOrDefaultAsyncUntil<T>(this DbContext dbContext,
        Func<T, bool> condition, TimeSpan? timeLimit = null) where T : class
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var entities = await dbContext.Set<T>().FirstOrDefaultAsync();

            if (entities != null && condition(entities))
                return entities;

            await Task.Delay(TimeSpan.FromMilliseconds(100));

        } while (stopwatch.Elapsed < limit);

        throw new Exception($"Entity not found within the time limit ({limit.TotalSeconds} seconds)");
    }
}
