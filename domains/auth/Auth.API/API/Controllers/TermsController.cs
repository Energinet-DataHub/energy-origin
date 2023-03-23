using System.Net.Http.Headers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EnergyOrigin.TokenValidation.Models.Requests;
using API.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<IActionResult> AcceptTermsAsync(
        ILogger<TermsController> logger,
        IHttpContextAccessor accessor,
        IUserDescriptorMapper descriptMapper,
        IUserDescriptorMapper mapper,
        IUserService userService,
        ICompanyService companyService,
        IHttpClientFactory clientFactory,
        IOptions<DataSyncOptions> options,
        [FromBody] AcceptTermsRequest acceptedTermsVersion)
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

        if (AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
        {
            var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = authentication;

            // NOTE/TODO: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
            //            However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
            var result = await client.PostAsJsonAsync<Dictionary<string, object?>>($"{options.Value.Uri.AbsoluteUri}/relations", new()
            {
                { "ssn", null },
                { "tin", user.Company?.Tin }
            });

            if (!result.IsSuccessStatusCode)
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {subject}", user.Id); // TODO: This should be logging the subject when merged with "company changes".
            }
        }

        return NoContent();
    }
}
