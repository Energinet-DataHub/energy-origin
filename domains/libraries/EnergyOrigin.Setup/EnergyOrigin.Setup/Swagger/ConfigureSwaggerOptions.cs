using System;
using System.IO;
using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnergyOrigin.Setup.Swagger;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, string subsystemName) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.OperationFilter<SwaggerDefaultValues>();
        options.SupportNonNullableReferenceTypes();
        var xmlFilePath = Path.Combine(AppContext.BaseDirectory, "documentation.xml");
        options.IncludeXmlComments(xmlFilePath);
        options.EnableAnnotations();

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme." +
                " \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below." +
                "\r\n\r\nExample: \"Bearer 1safsfsdfdfd\""
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

        foreach (var description in provider.ApiVersionDescriptions)
        {
            var descriptionText = new StringBuilder();

            descriptionText.Append(description.IsDeprecated
                ? "This API version has been deprecated."
                : "An example API.");

            if (description.SunsetPolicy is { } policy)
            {
                if (policy.Date is { } when)
                {
                    descriptionText.Append(" The API will be sunset on ")
                        .Append(when.Date.ToShortDateString())
                        .Append('.');
                }

                if (policy.HasLinks)
                {
                    descriptionText.AppendLine();

                    foreach (var link in policy.Links)
                    {
                        if (link.Type != "text/html") continue;
                        descriptionText.AppendLine();

                        if (link.Title.HasValue)
                        {
                            descriptionText.Append(link.Title.Value).Append(": ");
                        }

                        descriptionText.Append(link.LinkTarget.OriginalString);
                    }
                }
            }

            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"{subsystemName.FirstCharToUpper()} API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = descriptionText.ToString()
            });
        }
    }
}
