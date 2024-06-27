using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
[Authorize(Policy.B2CCvrClaim)]
[Authorize(Policy.B2CSubTypeUserPolicy)]
public class ConsentController(IMediator mediator, IdentityDescriptor identity) : ControllerBase
{
    /// <summary>
    /// Grants consent.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/consent/grant/")]
    public async Task<ActionResult> GrantConsent([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentRequest request)
    {
        await mediator.Send(new GrantConsentCommand(identity.Subject, identity.AuthorizedOrganizationIds.Single(),
            new IdpClientId(request.IdpClientId)));
        return Ok();
    }

    /// <summary>
    /// Get consent from a specific Client.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consent/grant/{clientId}")]
    public async Task<ActionResult> GetConsent([FromServices] ILogger<ConsentController> logger, [FromRoute] Guid clientId)
    {
        var result = await mediator.Send(new GetConsentQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Retrieves consents granted by the organization that a user is affiliated with. It will read the IdpUserId and OrgCvr claims from the user's session token, use those to query the database, and return a list of consents.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consents/")]
    [ProducesResponseType(typeof(UserOrganizationConsentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetConsent()
    {
        var queryResult = await mediator.Send(new GetUserOrganizationConsentsQuery(identity.Subject.ToString(), identity.OrganizationCvr!));

        var response = new UserOrganizationConsentsResponse(
            queryResult.Result.Select(item => new UserOrganizationConsentsResponseItem(item.ClientId, item.OrganizationId, item.ClientName, item.ConsentDate))
        );

        return Ok(response);
    }

    /// <summary>
    /// Deletes a consent.
    /// </summary>
    /// <param name="clientId">The ID of the client.</param>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <returns>No content if the deletion was successful, Not Found if the consent was not found.</returns>
    [HttpDelete("api/authorization/consents/{clientId}/{organizationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteConsent([FromRoute] Guid clientId, [FromRoute] Guid organizationId)
    {
        var userId = identity.Subject.ToString();
        var userOrgCvr = identity.OrganizationCvr;

        try
        {
            var result = await mediator.Send(new DeleteConsentCommand(clientId, organizationId, userId, userOrgCvr!));

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (UserNotAffiliatedWithOrganizationCommandException)
        {
            return Forbid();
        }
    }
}
