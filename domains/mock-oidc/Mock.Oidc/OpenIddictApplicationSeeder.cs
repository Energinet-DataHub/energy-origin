using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Mock.Oidc.Models;

namespace Mock.Oidc;

public class OpenIddictApplicationSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ClientDescriptor[] _clients;

    public OpenIddictApplicationSeeder(IServiceProvider serviceProvider, ClientDescriptor[] clients)
    {
        _serviceProvider = serviceProvider;
        _clients = clients;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var descriptors = _clients.Select(c => new OpenIddictApplicationDescriptor
        {
            ClientId = c.ClientId,
            ClientSecret = c.ClientSecret,
            RedirectUris = { new Uri(c.RedirectUri) },
            Type = ClientTypes.Confidential,
            Permissions =
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
                Permissions.Scopes.Roles,
                "scp:nemid",
                "scp:mitid",
                "scp:ssn",
                "scp:userinfo_token"
            }
        });

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