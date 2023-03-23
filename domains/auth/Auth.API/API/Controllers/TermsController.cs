using API.Models.DTOs;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
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
        IUserDescriptorMapper mapper,
        IUserService userService,
        ICompanyService companyService,
        [FromBody] AcceptTermsDTO acceptedTermsVersion)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        User user;
        if (descriptor.Id is null)
        {
            user = new User
            {
                Name = descriptor.Name,
                AllowCPRLookup = descriptor.AllowCPRLookup,
                Company = descriptor.Tin is not null
                    ? await companyService.GetCompanyByTinAsync(descriptor.Tin) ?? new Company()
                    {
                        Name = descriptor.CompanyName!,
                        Tin = descriptor.Tin!
                    }
                    : null,
                UserProviders = descriptor.ProviderKeys
                    .Select(x => new UserProvider()
                    {
                        ProviderType = descriptor.ProviderType,
                        ProviderKeyType = x.Key,
                        UserProviderKey = x.Value
                    })
                    .ToList()
            };
        }
        else
        {
            var id = descriptor.Id.Value;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        return NoContent();
    }
}
