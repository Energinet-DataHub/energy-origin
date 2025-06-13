using API.Models;
using EnergyOrigin.Setup.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(API.IntegrationTests.Setup.DatabaseServerFixture))]

namespace API.IntegrationTests.Setup;

public sealed class DatabaseServerFixture : IAsyncLifetime
{
    public static DatabaseServerFixture Instance { get; private set; } = null!;
    private readonly PostgreSqlContainer _pg =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithReuse(true)
            .WithLabel("reuse-id", "eotrace")
            .Build();

    public DatabaseServerFixture() => Instance = this;
    public string BaseConnection => _pg.GetConnectionString();
    public string TemplateDbName { get; } = $"tpl_{Guid.NewGuid():N}";

    public async ValueTask InitializeAsync()
    {
        await _pg.StartAsync();

        await using var admin = new NpgsqlConnection(BaseConnection);
        await admin.OpenAsync();
        await using (var cmd = admin.CreateCommand())
        {
            cmd.CommandText = $"""CREATE DATABASE "{TemplateDbName}";""";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = $"""ALTER DATABASE "{TemplateDbName}" IS_TEMPLATE true;""";
            await cmd.ExecuteNonQueryAsync();
        }

        var tplConn = BaseConnection.Replace("Database=postgres",
                          $"Database={TemplateDbName}") +
                      ";Pooling=false";

        await new DbMigrator(
                tplConn,
                typeof(Program).Assembly,
                NullLogger<DbMigrator>.Instance)
            .MigrateAsync();

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(tplConn)
            .Options;

        await using (var db = new ApplicationDbContext(opts))
        {
            if (!await db.Terms.AnyAsync())
            {
                db.Terms.Add(Terms.Create(1));
                await db.SaveChangesAsync();
            }
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(tplConn));
    }

    public ValueTask DisposeAsync() => new(_pg.DisposeAsync().AsTask());
}
