using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Transfer.Application.Configuration;

public static class Configuration
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(Assembly.GetExecutingAssembly());
    }
}
