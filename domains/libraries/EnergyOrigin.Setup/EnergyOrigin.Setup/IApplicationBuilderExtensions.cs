using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace EnergyOrigin.Setup;

public static class IApplicationBuilderExtensions
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
}
