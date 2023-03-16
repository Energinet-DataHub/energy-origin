using API.Models.DTOs;
using API.Models.Entities;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<IActionResult> AcceptTermsAsync(
        IClaimsWrapperMapper claimsWrapperMapper,
        IUserService userService,
        ICompanyService companyService,
        [FromBody] AcceptTermsDTO acceptedTermsVersion)
    {
        var claimsWrapper = claimsWrapperMapper.Map(User) ?? throw new NullReferenceException($"ClaimsWrapperMapper failed: {User}");

        User user;
        if (claimsWrapper.Id is null)
        {
            user = new User
            {
                Name = claimsWrapper.Name,
                ProviderId = claimsWrapper.ProviderId,
                AllowCPRLookup = claimsWrapper.AllowCPRLookup,
                Company = await companyService.GetCompanyByTinAsync(claimsWrapper.Tin) ?? new Company()
                {
                    Name = claimsWrapper.CompanyName,
                    Tin = claimsWrapper.Tin
                }
            };
        }
        else
        {
            var id = claimsWrapper.Id.Value;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        return NoContent();
    }
}
