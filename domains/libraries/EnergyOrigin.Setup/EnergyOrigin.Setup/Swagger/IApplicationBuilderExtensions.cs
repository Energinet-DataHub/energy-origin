using System.IO;
using System.Linq;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Swashbuckle.AspNetCore.Swagger;

namespace EnergyOrigin.Setup.Swagger;

public static class SwaggerApplicationBuilderExtensions
{
    public static void AddSwagger(this IApplicationBuilder app, IWebHostEnvironment env, string subsystemName)
    {
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwagger(o => o.RouteTemplate = "api-docs/" + subsystemName + "/{documentName}/swagger.json");
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(x => x.GroupName))
                    {
                        options.SwaggerEndpoint(
                            $"/api-docs/{subsystemName}/{description.GroupName}/swagger.json",
                            $"API v{description.GroupName}");
                    }
                });
        }
    }

    public static void BuildSwaggerYamlFile(this WebApplication app, IWebHostEnvironment env, string filename, string apiVersion = ApiVersions.Version20240515)
    {
        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        var swagger = swaggerProvider.GetSwagger(apiVersion);

        File.WriteAllText(
            Path.Combine(env.ContentRootPath, filename),
            swagger.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0)
        );
    }
}
