using System;
using System.IO;
using API.Query.API.Swagger;
using API.Query.API.v2023_11_27.ApiModels.Requests;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Query.API;

public static class Startup
{
    public static void AddQueryApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<CreateContractValidator>();
    }
}
