using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Authorization;

public class AddAuthorizationTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = "Authorization",
            Description = """
                          Authorization in Energy Track & Trace provides endpoints for getting organization permissions for 3rd party clients.
                          """
        });
    }
}
