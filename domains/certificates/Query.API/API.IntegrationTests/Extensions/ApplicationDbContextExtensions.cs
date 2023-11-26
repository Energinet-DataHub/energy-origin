using DataContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace API.IntegrationTests.Extensions;

public static class ApplicationDbContextExtensions
{
    public static void RemoveAll<T>(this ApplicationDbContext dbContext, Func<ApplicationDbContext, DbSet<T>> getDbSet) where T : class
    {
        var dbSet = getDbSet(dbContext);
        dbSet.RemoveRange(dbSet.ToList());
        dbContext.SaveChanges();
    }
}
