using MassTransit;
using MessageRedeliveryPoc.MassTransit;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
builder.Logging.AddConsole();

// services.AddQuartz();
// services.AddQuartzHostedService(options =>
// {
//     options.WaitForJobsToComplete = true;
// });

services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("localhost", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
        // cfg.UseMessageScheduler(new Uri("rabbitmq://localhost/quartz"));
        // cfg.UseScheduledRedelivery(r => r.Interval(3, TimeSpan.FromMinutes(1)));
        cfg.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
        cfg.ConfigureEndpoints(context);
    });
    x.AddConsumer<MessageConsumer>();
});
services.AddHostedService<MessageProducer>();

var app = builder.Build();
app.UseHttpsRedirection();

app.Run();
