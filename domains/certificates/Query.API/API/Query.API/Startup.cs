using API.Query.API.Swagger;
using API.Reports;
using EnergyOrigin.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace API.Query.API;

public static class Startup
{
    public static void AddQueryApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwagger("certificates");
        services.AddSwaggerGen(c =>
        {
            c.DocumentFilter<AddContractsTagDocumentFilter>();
        });
        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<ApiModels.Requests.Internal.CreateContractValidator>();
        services.AddValidatorsFromAssemblyContaining<ApiModels.Requests.CreateContractValidator>();

        services.AddValidatorsFromAssemblyContaining<ApiModels.Requests.Internal.EditContractEndDateValidator>();
        services.AddValidatorsFromAssemblyContaining<ApiModels.Requests.EditContractEndDateValidator>();
        services.AddScoped<CertificatesSpreadsheetExporter>();
    }
}
