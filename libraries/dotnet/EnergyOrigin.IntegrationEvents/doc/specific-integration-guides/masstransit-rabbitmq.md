# Integration with RabbitMQ using MassTransit's Transactional Outbox Pattern

## Overview

Energy Track & Trace™ currently uses RabbitMQ, as the message broker for publishing and consuming integration events.
To ensure reliability and consistency between database operations and message publishing,
[MassTransit's](https://masstransit.io/) transactional outbox pattern is used,
using the MassTransit.EntityFrameworkCore, and MassTransit.RabbitMQ NuGet packages.

### Setup Guide

To start publishing integration event messages,
using [Transactional Outbox Configuration](https://masstransit.io/documentation/configuration/middleware/outbox),
then follow these steps:

**1. Install the following NuGet packages:**

```
Install-Package EnergyOrigin.IntegrationEvents
Install-Package MassTransit.RabbitMQ
Install-Package MassTransit.EntityFrameworkCore
```

**2. Add the following Configuration to your `appsettings.Development.json` file:**

```
"RabbitMq:Host": "localhost",
"RabbitMq:Port": "5672",
"RabbitMq:Username": "guest",
"RabbitMq:Password": "guest",
```

***Note:** The values here are for demonstration purposes only. RabbitMq values for production use,
should **NEVER** be committed to the repository. Use the deployment manifests,
in our IAC repository to specify the rabbitmq settings that should be used in production.
If you need help with this, ask a Coworker.

**3. Create a `RabbitMqOptions.cs` file:**

```csharp
public class RabbitMqOptions
{
    public const string RabbitMq = "RabbitMq";

    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int? Port { get; set; }
}
```

**4. Load the RabbitMqOptions and enable Masstransit by adding the following to `Program.cs`:**

```csharp
builder.Services.AddOptions<RabbitMqOptions>() //<---- This loads the configuration settings for RabbitMq
    .BindConfiguration(RabbitMqOptions.RabbitMq)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddMassTransit(o => // <---- This enables Transactional outbox via Masstransit, and EF core
{
    o.SetKebabCaseEndpointNameFormatter();

    o.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var url = $"rabbitmq://{options.Host}:{options.Port}";

        cfg.Host(new Uri(url), h =>
        {
            h.Username(options.Username);
            h.Password(options.Password);
        });
        cfg.ConfigureEndpoints(context);
    });
    o.AddEntityFrameworkOutbox<ApplicationDbContext>(outboxConfigurator =>
    {
        outboxConfigurator.UsePostgres();
        outboxConfigurator.UseBusOutbox();
    });
});
```
**5. Enable creation of migration files by adding the following to the modelbuilder in `ApplicationDbContext`:**

```csharp
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddTransactionalOutboxEntities(); //<---- Add this!
    }
```

**6. Generate the required migration files by running the following command, from solution root
(You will need to have the dotnet-ef tools, installed):**

```shell
dotnet ef migrations add AddOutbox --project Path/To/Your/Project.csproj --startup-project Path/To/Your/Project.csproj
```

This should generate a new migrationfile for 3 new tables called:

- InboxState
- OutboxState
- OutboxMessage

**7. Apply the migrations by running the following command, using the dotnet-ef tools:**

```shell
dotnet ef database update --project Path/To/Your/Project.csproj --startup-project Path/To/Your/Project.csproj
```

### Testing the integration

Refer to the [following documentation from MassTransit](https://masstransit.io/documentation/concepts/testing),
in order to test out your implementation:

***Note:** You may also refer to the [EnergyOrigin repository](https://github.com/Energinet-DataHub/energy-origin),
for inspiration on how to implement, and test the Transactional Outbox Pattern.
