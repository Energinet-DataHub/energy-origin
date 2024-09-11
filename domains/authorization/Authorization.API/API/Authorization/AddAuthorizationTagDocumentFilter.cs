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
                          Authorization in Energy Track & Trace provides endpoints for getting organization permissions for 3rd party clients and logged in users.
                          Its also the service that is used a user can grant consent to a 3rd party client to access their data.
                          Finally it also internal API endpoints for returning access rights that populates JWT Tokens during login.
                          """
        });
    }
}
