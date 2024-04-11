using DataContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace API.IntegrationTests.Extensions;

public static class TransferDbContextExtensions
{
    public static void RemoveAll<T>(this CertificateDbContext dbContext, Func<CertificateDbContext, DbSet<T>> getDbSet) where T : class
    {
        var dbSet = getDbSet(dbContext);
        dbSet.RemoveRange(dbSet.ToList());
        dbContext.SaveChanges();
    }
}
