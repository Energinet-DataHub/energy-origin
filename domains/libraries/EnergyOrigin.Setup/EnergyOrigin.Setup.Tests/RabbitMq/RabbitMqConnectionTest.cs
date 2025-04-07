using System.Net;
using EnergyOrigin.Setup.Health;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.RabbitMq;
using EnergyOrigin.Setup.Tests.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;
using RabbitMqContainer = Testcontainers.RabbitMq.RabbitMqContainer;

namespace EnergyOrigin.Setup.Tests.RabbitMq;

public class RabbitMqConnectionTest : IAsyncLifetime
{
    private const string ServiceBaseAddress = "http://localhost:5001";
    private const string RabbitMqUsername = "guest";
    private const string RabbitMqPassword = "guest";

    private readonly PostgresContainer _postgresContainer = new();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management")
        .WithUsername(RabbitMqUsername)
        .WithPassword(RabbitMqPassword)
        .WithPortBinding(25672, 5672)
        .WithPortBinding(35672, 15672)
        .Build();

    [Fact]
    public async Task GivenMassTransitConfig_WhenConfiguringHost_HealthCanBeConfiguredToCheckRabbitMqConnectivity()
    {
        // Given application stack WITHOUT RabbitMQ running
        var databaseInfo = await StartPostgresDatabase();
        var builder = ConfigureWebAppBuilder(databaseInfo);
        await using var app = builder.Build();
        app.MapDefaultHealthChecks();
        await app.StartAsync(TestContext.Current.CancellationToken);

        // Confirm health check is not OK (RabbitMQ is not started)
        await WaitForHealthEndpointResponse(HttpStatusCode.ServiceUnavailable);

        // When starting RabbitMQ
        await _rabbitMqContainer.StartAsync(TestContext.Current.CancellationToken);
        VerifyRabbitMqIsOpen();

        // Health check becomes OK
        await WaitForHealthEndpointResponse(HttpStatusCode.OK);

        // When stopping RabbitMQ
        await _rabbitMqContainer.StopAsync(TestContext.Current.CancellationToken);

        // Health check remains OK due to MassTransit outbox buffering (it’s not forced “down”)
        await WaitForHealthEndpointResponse(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenMassTransitConfig_WhenSendingMessages_MessagesAreSentAndReceivedWhenRabbitMqConnectivityIsUp()
    {
        // Given application stack WITH active message producer
        await _rabbitMqContainer.StartAsync(TestContext.Current.CancellationToken);
        VerifyRabbitMqIsOpen(); // <-- Official approach: ensure readiness

        var databaseInfo = await StartPostgresDatabase();
        var builder = ConfigureWebAppBuilder(databaseInfo);
        builder.Services.AddHostedService<TestMessageProducer>();
        await using var app = builder.Build();
        app.MapDefaultHealthChecks();
        await app.StartAsync(TestContext.Current.CancellationToken);

        // Health check is OK (RabbitMQ started)
        await WaitForHealthEndpointResponse(HttpStatusCode.OK);

        // When stopping RabbitMQ
        await _rabbitMqContainer.StopAsync(TestContext.Current.CancellationToken);

        // The producer can still send messages (outbox buffering)
        await TestMessageProducer.MessagesSentTask;

        // When starting RabbitMQ again
        await _rabbitMqContainer.StartAsync(TestContext.Current.CancellationToken);
        VerifyRabbitMqIsOpen(); // <-- Official approach again

        // Outbox messages should now be consumed
        await WaitForMessagesConsumed();
    }

    private void VerifyRabbitMqIsOpen()
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString())
        };
        using var connection = connectionFactory.CreateConnection();
        Assert.True(connection.IsOpen, "Expected RabbitMQ container to accept connections but it was not open.");
    }

    public class TestMessageProducer(IServiceScopeFactory scopeFactory) : BackgroundService
    {
        public static Task MessagesSentTask { get; private set; } = Task.Delay(TimeSpan.FromMinutes(1));

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MessagesSentTask = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                await using var dbContext = scope.ServiceProvider.GetService<TestDbContext>()!;
                var publishEndpoint = scope.ServiceProvider.GetService<IPublishEndpoint>()!;

                foreach (var i in Enumerable.Range(0, 10))
                {
                    var msg = new TestMessage($"TestMessage({i})");
                    await publishEndpoint.Publish(msg, stoppingToken);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }, stoppingToken);

            return Task.CompletedTask;
        }
    }

    public class TestMessage(string msg)
    {
        public string Msg { get; private set; } = msg;
    }

    public class TestMessageConsumer : IConsumer<TestMessage>
    {
        public static int MessageConsumedCount = 0;

        public Task Consume(ConsumeContext<TestMessage> context)
        {
            Interlocked.Increment(ref MessageConsumedCount);
            return Task.CompletedTask;
        }
    }

    private static WebApplicationBuilder ConfigureWebAppBuilder(DatabaseInfo databaseInfo)
    {
        var dbMigrator = new DbMigrator(databaseInfo.ConnectionString, typeof(DbMigratorTest).Assembly, NullLogger<DbMigrator>.Instance);
        dbMigrator.MigrateAsync("20250116-0001-InboxOutbox.sql").Wait();

        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<RabbitMqOptions>(options =>
        {
            options.Host = "localhost";
            options.Port = 25672;
            options.Username = RabbitMqUsername;
            options.Password = RabbitMqPassword;
        });
        builder.Services.AddMassTransitAndRabbitMq<TestDbContext>(x =>
        {
            x.AddConsumer<TestMessageConsumer>();
        });
        builder.Services.AddDbContext<TestDbContext>(options =>
        {
            options.UseNpgsql(databaseInfo.ConnectionString);
        });
        builder.Services.AddHealthChecks();
        builder.AddSerilog();

        builder.Configuration.GetSection("urls").Value = ServiceBaseAddress;
        return builder;
    }

    private async Task<DatabaseInfo> StartPostgresDatabase()
    {
        await _postgresContainer.InitializeAsync();
        var databaseInfo = await _postgresContainer.CreateNewDatabase();
        return databaseInfo;
    }

    private async Task WaitForHealthEndpointResponse(HttpStatusCode expectedStatusCode)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(ServiceBaseAddress) };
        const int timeoutSeconds = 120;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

        HttpResponseMessage? healthResponse = null;

        while (DateTimeOffset.UtcNow < timeout)
        {
            healthResponse = await httpClient.GetAsync("health");
            if (healthResponse.StatusCode == expectedStatusCode) return;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new Exception(
            $"Health endpoint did not return status code ({expectedStatusCode}) within {timeoutSeconds} seconds. " +
            $"Last response status code was {healthResponse?.StatusCode}.");
    }

    private async Task WaitForMessagesConsumed()
    {
        const int expectedMessageCount = 10;
        const int timeoutSeconds = 60;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTimeOffset.UtcNow < timeout)
        {
            if (TestMessageConsumer.MessageConsumedCount >= expectedMessageCount) return;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new Exception($"Did not consume {expectedMessageCount} messages within {timeoutSeconds} seconds.");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}
