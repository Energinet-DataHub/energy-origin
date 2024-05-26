using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AccessControl.API.Swagger;

public class ProblemDetailsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (!context.SchemaRepository.Schemas.ContainsKey("ProblemDetails"))
            context.SchemaRepository.Schemas.Add("ProblemDetails", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new() { Type = "string" },
                    ["title"] = new() { Type = "string" },
                    ["status"] = new() { Type = "integer", Format = "int32" },
                    ["detail"] = new() { Type = "string" },
                    ["instance"] = new() { Type = "string" }
                }
            });
    }
}
