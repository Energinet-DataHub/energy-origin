using API.Authorization.Controllers;
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

        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = nameof(CredentialController).Replace("Controller", string.Empty),
            Description = """
                          The Credentials endpoints makes it possible to get all credentials tied to a client, create a credential and delete a credential.
                          A credential contains a client-secret, which can be used to get an access-token for the Energy Track & Trace API.

                          The client-secret is ONLY returned once, when the credential is initially created.

                          It is only possible for a client to have two credentials configured.
                          """
        });
    }
}
