using System.IO;
using System.Linq;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace EnergyOrigin.Setup.Swagger;

public static class SwaggerApplicationBuilderExtensions
{
    public static void AddSwagger(this IApplicationBuilder app, IWebHostEnvironment env, string subsystemName)
    {
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseSwagger(o =>
        {
            o.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
            o.RouteTemplate = "api-docs/" + subsystemName + "/{documentName}/swagger.json";
        });
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

    public static void BuildSwaggerYamlFile(this WebApplication app, IWebHostEnvironment env, string filename, string apiVersion = ApiVersions.Version1)
    {
        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        var swagger = swaggerProvider.GetSwagger(apiVersion);

        using var stringWriter = new StringWriter();
        var yamlWriter = new OpenApiYamlWriter(stringWriter);

        swagger.SerializeAsV3(yamlWriter);
        yamlWriter.Flush();
        File.WriteAllText(Path.Combine(env.ContentRootPath, filename), stringWriter.ToString());
    }
}
