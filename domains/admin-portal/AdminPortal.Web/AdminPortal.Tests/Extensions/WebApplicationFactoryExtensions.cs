using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPortal.Tests.Extensions;

public static class WebApplicationFactoryExtensions
{
    public static T GetRequiredScopedService<T>(this WebApplicationFactory<Program> factory)
        where T : notnull
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public static T? GetOptionalScopedService<T>(this WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetService<T>();
    }
}
