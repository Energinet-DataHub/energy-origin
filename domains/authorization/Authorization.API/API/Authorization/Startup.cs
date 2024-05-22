using API.Authorization._Features_;
using API.Authorization.Controllers;
using EnergyOrigin.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace API.Authorization;

public static class Startup
{
    public static void AddAuthorizationApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwagger("authorization");
        services.AddSwaggerGen(c =>
        {
            //c.DocumentFilter<AddContractsTagDocumentFilter>();
        });
        services.AddHttpContextAccessor();
        services.AddScoped<EntityDescriptor>();

        //services.AddValidatorsFromAssemblyContaining<CreateContractValidator>();
    }
}
