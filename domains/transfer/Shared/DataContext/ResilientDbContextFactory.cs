using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;

namespace DataContext;

public class ResilientDbContextFactory<TContext> : IDbContextFactory<TContext> where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _inner;
    private readonly ISyncPolicy _retryPolicy;

    public ResilientDbContextFactory(IDbContextFactory<TContext> inner)
    {
        _inner = inner;
        _retryPolicy = Policy
            .Handle<NpgsqlException>(IsTransient)
            .WaitAndRetry(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public TContext CreateDbContext()
    {
        return _retryPolicy.Execute(() => _inner.CreateDbContext());
    }

    private static bool IsTransient(NpgsqlException ex) => ex.SqlState switch
    {
        "57P01" => true, // node_shutdown
        "08006" => true, // connection_failure
        "08003" => true, // connection_does_not_exist
        "53300" => true, // too_many_connections
        _ => false
    };
}
