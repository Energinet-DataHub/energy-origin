using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnergyOrigin.Setup.Swagger;

/*
 * Swagger understands the deprecated mark on controllers and marks these endpoints in swagger as deprecated
 * Sets default value for the EO_API_VERSION paramter to 20230101
 * Sets description for parameters, meaning EO_API_VERSION
 */
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
        {
            return;
        }

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

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata.Description;

            if (parameter.Schema.Default == null &&
                 description.DefaultValue != null &&
                 description.DefaultValue is not DBNull &&
                 description.ModelMetadata is { } modelMetadata)
            {
                var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
