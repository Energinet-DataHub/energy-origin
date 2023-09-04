using System;
using System.Threading.Tasks;
using API.Data;
using API.Extensions;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/connection-invitations")]
public class ConnectionInvitationsController : ControllerBase
{
    private readonly IConnectionInvitationRepository connectionInvitationRepository;

    public ConnectionInvitationsController(IConnectionInvitationRepository connectionInvitationRepository)
        => this.connectionInvitationRepository = connectionInvitationRepository;

    [ProducesResponseType(typeof(string), 200)]
    [HttpPost]
    public async Task<ActionResult> CreateConnectionInvitation()
    {
        var companySenderId = Guid.Parse(User.FindSubjectGuidClaim());
        var companySenderTin = User.FindSubjectTinClaim();

        var newInvitation = new ConnectionInvitation
        {
            SenderCompanyId = companySenderId,
            SenderCompanyTin = companySenderTin,
        };

        var addedInvitation = await connectionInvitationRepository.AddConnectionInvitation(newInvitation);

        return Ok(new { connectionInvitationId = addedInvitation.Id });
    }
}
