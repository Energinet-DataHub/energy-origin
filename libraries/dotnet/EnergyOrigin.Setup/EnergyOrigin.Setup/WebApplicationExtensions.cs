using Microsoft.AspNetCore.Builder;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace EnergyOrigin.Setup;

public static class WebApplicationExtensions
{
    public static void AddSwagger(this WebApplication app, string subsystemName)
    {
        app.UseSwagger(o => o.RouteTemplate = "api-docs/" + subsystemName + "/{documentName}/swagger.json");
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in app.DescribeApiVersions().OrderByDescending(x => x.GroupName))
                    {
                        options.SwaggerEndpoint(
                            $"/api-docs/{subsystemName}/{description.GroupName}/swagger.json",
                            $"API v{description.GroupName}");
                    }
                });
        }
    }
}
