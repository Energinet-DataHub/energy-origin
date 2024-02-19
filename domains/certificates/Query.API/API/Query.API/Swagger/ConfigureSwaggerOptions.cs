using System;
using System.IO;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Query.API.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IWebHostEnvironment environment;
    private readonly IApiVersionDescriptionProvider provider;
    private readonly IConfiguration configuration;

    public ConfigureSwaggerOptions(
        IApiVersionDescriptionProvider provider,
        IWebHostEnvironment environment,
        IConfiguration configuration
        )
    {
        this.environment = environment;
        this.provider = provider;
        this.configuration = configuration;
    }

    public void Configure(SwaggerGenOptions options)
    {
        options.OperationFilter<SwaggerDefaultValues>();
        options.SupportNonNullableReferenceTypes();
        var xmlFilePath = Path.Combine(AppContext.BaseDirectory, "documentation.xml");
        options.IncludeXmlComments(xmlFilePath);
        options.DocumentFilter<AddContractsTagDocumentFilter>();

        if (environment.IsDevelopment())
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\""
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        }
        foreach (var description in provider.ApiVersionDescriptions)
        {
            var termsOfServiceUri = configuration["TermsOfServiceUri"];

            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"Certificates Query API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                TermsOfService = new Uri(termsOfServiceUri!),
                Description = description.IsDeprecated ? "This API version has been deprecated." : ""
            });
        }
    }
}
