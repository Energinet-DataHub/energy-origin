using System;
using System.Reflection;
using System.Threading.Tasks;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace EnergyOrigin.Setup.Migrations;

public class DbMigrator
{
    private readonly string _postgresConnectionString;
    private readonly Assembly _scriptAssembly;
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _sleepTime = TimeSpan.FromSeconds(5);

    public DbMigrator(string postgresConnectionString, Assembly scriptAssembly, ILogger logger)
    {
        _postgresConnectionString = postgresConnectionString;
        _scriptAssembly = scriptAssembly;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        await MigrateInternalAsync(null);
    }

    public async Task MigrateAsync(string migrationTarget)
    {
        await MigrateInternalAsync(migrationTarget);
    }

    private async Task MigrateInternalAsync(string? migrationTarget)
    {
        var filter = (string scriptName) =>
        {
            return scriptName.ToLower().EndsWith(".sql") &&
                   (migrationTarget is null || String.Compare(scriptName.ToLower(), migrationTarget.ToLower(), StringComparison.Ordinal) <= 0);
        };

        var upgradeEngine = DeployChanges.To
            .PostgresqlDatabase(_postgresConnectionString)
            .WithTransactionPerScript()
            .WithScriptsEmbeddedInAssembly(_scriptAssembly, filter)
            .LogTo(new LoggerWrapper(_logger))
            .WithExecutionTimeout(_timeout)
            .Build();

        await TryConnectToDatabaseWithRetry(upgradeEngine);

        EnsureDatabase.For.PostgresqlDatabase(_postgresConnectionString);

        var databaseUpgradeResult = upgradeEngine.PerformUpgrade();

        if (!databaseUpgradeResult.Successful)
        {
            throw databaseUpgradeResult.Error;
        }
    }

    private async Task TryConnectToDatabaseWithRetry(UpgradeEngine upgradeEngine)
    {
        var started = DateTime.UtcNow;
        while (!upgradeEngine.TryConnect(out string msg))
        {
            _logger.LogWarning("Failed to connect to database ({message}), waiting to retry in {sleepTime} seconds... ", msg,
                _sleepTime.TotalSeconds);
            await Task.Delay(_sleepTime);

            if (DateTime.UtcNow - started > _timeout)
                throw new TimeoutException($"Could not connect to database ({msg}), exceeded retry limit.");
        }
    }

    private sealed class LoggerWrapper : IUpgradeLog
    {
        private readonly ILogger _logger;

        public LoggerWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public void WriteError(string format, params object[] args)
        {
            _logger.LogError(format, args);
        }

        public void WriteInformation(string format, params object[] args)
        {
            _logger.LogInformation(format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            _logger.LogWarning(format, args);
        }
    }
}
