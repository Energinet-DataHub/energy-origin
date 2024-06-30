using System.Threading.Tasks;
using API.Authorization._Features_;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TermsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TermsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptTerms([FromBody] AcceptTermsDto acceptTermsDto)
        {
            // Check if organization exists and terms are accepted
            var organization = await _mediator.Send(new GetOrganizationByTinQuery(acceptTermsDto.Tin));
            var terms = await _mediator.Send(new GetTermsByVersionQuery(acceptTermsDto.TermsVersion));

            if (organization == null || !organization.TermsAccepted)
            {
                // Return terms text if not accepted
                if (organization == null || organization.TermsVersion != terms.Version)
                {
                    return Ok(new { TermsText = terms.Text });
                }

                // Accept terms and create organization, user, and affiliation
                var result = await _mediator.Send(new CreateOrganizationAndUserCommand(
                    acceptTermsDto.Tin,
                    acceptTermsDto.OrganizationName,
                    acceptTermsDto.UserIdpId,
                    acceptTermsDto.UserName,
                    acceptTermsDto.TermsVersion
                ));

                return Ok(result);
            }

            return Ok(new { Message = "Terms already accepted" });
        }
    }
}
