using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RegistryConnector;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
