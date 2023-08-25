using System;
using System.IO;
using API.Query.API.ApiModels.Requests;
using API.Query.API.SwaggerGen;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace API.Query.API;

public static class Startup
{
    public static void AddQueryApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.OperationFilter<ResponseSchemaForBadRequestFilter>();
            o.SupportNonNullableReferenceTypes();
            o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "documentation.xml"));
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Certificates Query API"
            });
        });

        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<CreateContractValidator>();
    }
}
