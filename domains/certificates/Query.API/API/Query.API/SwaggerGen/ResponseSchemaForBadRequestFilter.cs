using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Query.API.SwaggerGen;

public class ResponseSchemaForBadRequestFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses.TryGetValue("400", out var badRequestApiResponse))
        {
            var openApiMediaTypes = badRequestApiResponse.Content.Values;

            if (openApiMediaTypes.All(mediaType => mediaType.Schema.Reference.Id.Equals(nameof(ProblemDetails))))
            {
                context.SchemaRepository.Schemas.Remove(nameof(ProblemDetails));
                var validationProblemDetailsSchema =
                    context.SchemaGenerator.GenerateSchema(typeof(ValidationProblemDetails), context.SchemaRepository);

                foreach (var openApiMediaType in openApiMediaTypes)
                {
                    openApiMediaType.Schema.Reference = validationProblemDetailsSchema.Reference;
                }
            }
        }
    }
}
