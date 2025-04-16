using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version1)]
[Authorize(Policy.FrontendOr3rdParty)]
[Route("api/authorization/clients/{clientId:guid}/credentials")]
public class CredentialController(
    IMediator mediator,
    AccessDescriptor accessDescriptor,
    IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateCredentialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Create credential",
        Description = """
                      Creates a single credential for a client and returns the client secret as part of the response.

                      It is only possible to have two credentials configured.
                      """
    )]
    public async Task<ActionResult<CreateCredentialResponse>> CreateCredential([FromRoute] Guid clientId)
    {
        if (!accessDescriptor.IsExternalClientAuthorized())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var commandResult =
            await mediator.Send(new CreateCredentialCommand(clientId, identityDescriptor.OrganizationId));

        var response = new CreateCredentialResponse(commandResult.Hint, commandResult.KeyId,
            commandResult.StartDateTime, commandResult.EndDateTime, commandResult.Secret);

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetCredentialsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Gets credentials",
        Description = """
                      Gets all credentials for a client. The client secret is not returned as part of the credential.

                      Use the KeyId that is returned in the response to delete a credential.
                      """
    )]
    public async Task<ActionResult<GetCredentialsResponse>> GetCredentials([FromRoute] Guid clientId)
    {
        if (!accessDescriptor.IsExternalClientAuthorized())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var queryResult = await mediator.Send(new GetCredentialsQuery(clientId, identityDescriptor.OrganizationId));

        var items = queryResult.Select(credential => new GetCredentialsResponseItem(
            credential.Hint,
            credential.KeyId,
            credential.StartDateTime, credential.EndDateTime)).ToList();

        return Ok(new GetCredentialsResponse(items));
    }

    [HttpDelete]
    [Route("{keyId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Deletes credential",
        Description = """
                      Deletes a single credential for a client.

                      Make sure you have another credential created before calling this endpoint, to guarentee that you can continue calling the Energy Track & Trace API.
                      """
    )]
    public async Task<ActionResult> DeleteCredential([FromRoute] Guid clientId, [FromRoute] Guid keyId)
    {
        if (!accessDescriptor.IsExternalClientAuthorized())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        await mediator.Send(new DeleteCredentialCommand(clientId, keyId, identityDescriptor.OrganizationId));

        return NoContent();
    }
}
