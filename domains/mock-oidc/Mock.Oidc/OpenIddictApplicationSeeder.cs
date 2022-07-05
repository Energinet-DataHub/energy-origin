using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Mock.Oidc;

public class OpenIddictApplicationSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public OpenIddictApplicationSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        
        // TODO: To be read from configuration or yaml file?
        var descriptors = new[]
        {
            new OpenIddictApplicationDescriptor
            {
                ClientId = "energy-origin",
                ClientSecret = "secret_secret_secret", //TODO: Do we want to use this?
                RedirectUris = { new Uri("https://localhost:7124") },
                Type = ClientTypes.Confidential,  //TODO: Why not public?
                Permissions = //TODO: Are all these permissions needed?
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Logout,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.Implicit,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.ResponseTypes.CodeIdToken,
                    Permissions.ResponseTypes.CodeIdTokenToken,
                    Permissions.ResponseTypes.CodeToken,
                    Permissions.ResponseTypes.IdToken,
                    Permissions.ResponseTypes.IdTokenToken,
                    Permissions.Scopes.Address,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            }
        };
        
        foreach (var descriptor in descriptors)
        {
            if (await manager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is not null)
            {
                continue;
            }

            await manager.CreateAsync(descriptor, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}