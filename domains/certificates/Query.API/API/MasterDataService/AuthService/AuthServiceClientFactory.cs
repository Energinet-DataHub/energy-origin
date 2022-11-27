using System;
using Microsoft.Extensions.DependencyInjection;

namespace API.MasterDataService.AuthService;

public class AuthServiceClientFactory
{
    private readonly IServiceProvider serviceProvider;

    public AuthServiceClientFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public AuthServiceClient CreateClient() => serviceProvider.GetRequiredService<AuthServiceClient>();
}
