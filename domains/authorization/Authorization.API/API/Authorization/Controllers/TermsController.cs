using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TermsController(IMediator mediator) : ControllerBase
{
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptTerms([FromBody] AcceptTermsDto acceptTermsDto)
    {
        var isValid = await mediator.Send(new OrganizationStateQuery(acceptTermsDto.Tin));

        if (isValid)
            return Ok(new TermsResponseDto(true));

        var latestTerms = await mediator.Send(new GetLatestTermsQuery());

        if (acceptTermsDto.TermsVersion != latestTerms.Version)
            return Ok(new TermsResponseDto(false, latestTerms.Text, latestTerms.Version));

        var createResult = await mediator.Send(new CreateOrganizationAndUserCommand(
            acceptTermsDto.Tin,
            acceptTermsDto.OrganizationName,
            acceptTermsDto.UserIdpId,
            acceptTermsDto.UserName,
            latestTerms.Version
        ));

        return Ok(new TermsResponseDto(true, CreateResult: createResult));
    }
}

public record AcceptTermsDto(
    string Tin,
    string OrganizationName,
    Guid UserIdpId,
    string UserName,
    string TermsVersion
);

public record TermsResponseDto(
    bool Accepted = false,
    string? TermsText = null,
    string? TermsVersion = null,
    CreateOrganizationAndUserCommandResult? CreateResult = null
);
