using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.Authorization._Features_.Internal;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using OrganizationId = EnergyOrigin.Domain.ValueObjects.OrganizationId;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version1)]
[Authorize(Policy.Frontend)]
[ApiExplorerSettings(IgnoreApi = true)]
public class ConsentController(IMediator mediator, IdentityDescriptor identity) : ControllerBase
{
    /// <summary>
    /// Grant consent to 3rd party client
    /// </summary>
    [HttpPost]
    [Route("api/authorization/consent/client/grant/")]
    [SwaggerOperation(
        Summary = "Grant consent to 3rd party",
        Description = "Grant consent to 3rd party identified by the provided client id"
    )]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GrantConsentToClient([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentToClientRequest toClientRequest)
    {
        await mediator.Send(new GrantConsentToClientCommand(identity.Subject, identity.OrganizationCvr!, new IdpClientId(toClientRequest.IdpClientId)));
        return Ok();
    }

    /// <summary>
    /// Grant consent to organization
    /// </summary>
    [HttpPost]
    [Route("api/authorization/consent/organization/grant/")]
    [SwaggerOperation(
        Summary = "Grant consent to organization",
        Description = "Grant consent to organization identified by the provided organization id"
    )]
    [ProducesResponseType(typeof(void),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GrantConsentToOrganization([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentToOrganizationRequest request)
    {
        await mediator.Send(new GrantConsentToOrganizationCommand(identity.Subject, identity.OrganizationCvr!, OrganizationId.Create(request.OrganizationId)));
        return Ok();
    }

    /// <summary>
    /// Retrieves consents granted and received by the organization that a user is affiliated with. It will read the IdpUserId and OrgCvr claims from the users session token, use those to query the database, and return a list of consents.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consents/")]
    [ProducesResponseType(typeof(UserOrganizationConsentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetConsent()
    {
        var queryResult = await mediator.Send(new GetUserOrganizationConsentsQuery(identity.Subject, identity.OrganizationCvr!));

        var response = new UserOrganizationConsentsResponse(
            queryResult.Result.Select(item => new UserOrganizationConsentsResponseItem(item.ConsentId, item.GiverOrganizationId, item.GiverOrganizationTin, item.GiverOrganizationName, item.ReceiverOrganizationId, item.ReceiverOrganizationTin, item.ReceiverOrganizationName, item.ConsentDate))
        );

        return Ok(response);
    }

    /// <summary>
    /// Retrieves consents received by the organization that a user is affiliated with.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consents/organization/received")]
    [ProducesResponseType(typeof(UserOrganizationConsentsReceivedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetReceivedConsent()
    {
        var queryResult = await mediator.Send(new GetUserOrganizationConsentsReceivedQuery(identity.Subject.ToString(), identity.OrganizationCvr!));

        var response = new UserOrganizationConsentsReceivedResponse(
            queryResult.Result.Select(item => new UserOrganizationConsentsReceivedResponseItem(item.ConsentId, item.OrganizationId, item.OrganizationName, item.OrganizationTin ?? "", item.ConsentDate))
        );

        return Ok(response);
    }

    /// <summary>
    /// Deletes a consent, from the organization, which the user is affiliated with.
    /// </summary>
    /// <param name="consentId">The ID of the consent to delete.</param>
    /// <returns>No content if the deletion was successful, Not Found if the consent was not found.</returns>
    [HttpDelete("api/authorization/consents/{consentId}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteConsent([FromRoute] Guid consentId)
    {
        var idpUserId = identity.Subject;
        var userOrgCvr = identity.OrganizationCvr;

        await mediator.Send(new DeleteConsentCommand(consentId, idpUserId, userOrgCvr!));

        return NoContent();
    }
}
