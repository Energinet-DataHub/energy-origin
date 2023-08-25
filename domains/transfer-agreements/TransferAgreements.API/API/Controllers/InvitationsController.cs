using System;
using System.Threading.Tasks;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using API.Models;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationRepository invitationRepository;

    public InvitationsController(IInvitationRepository invitationRepository)
        => this.invitationRepository = invitationRepository;

    [ProducesResponseType(typeof(string), 200)]
    [HttpPost]
    public async Task<ActionResult> CreateInvitation()
    {
        var companySenderId = Guid.Parse(User.FindSubjectGuidClaim());
        var companySenderTin = User.FindSubjectTinClaim();

        var newInvitation = new Invitation
        {
            SenderCompanyId = companySenderId,
            SenderCompanyTin = companySenderTin,
        };

        await invitationRepository.AddInvitationToDb(newInvitation);

        var invitationId = newInvitation.Id;

        return Ok(new { result = invitationId });
    }
}
