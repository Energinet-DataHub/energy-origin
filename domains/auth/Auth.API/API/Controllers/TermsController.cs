using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
        IUserDescriptorMapper mapper,
        IUserService userService,
        ICompanyService companyService,
        IHttpClientFactory clientFactory,
        IOptions<DataSyncOptions> options,
        [FromBody] AcceptTermsRequest acceptedTermsVersion)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        if (descriptor.AcceptedTermsVersion > acceptedTermsVersion.Version)
        {
            throw new ArgumentException("The user has already accepted a newer version of terms.");
        }

        var company = await companyService.GetCompanyByTinAsync(descriptor.Tin);

        User user;
        if (descriptor.UserStored)
        {
            var id = descriptor.Id;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }
        else
        {
            user = new User
            {
                Id = descriptor.Id,
                Name = descriptor.Name,
                AllowCPRLookup = descriptor.AllowCPRLookup,
                Company = descriptor.Tin is null ? null : company ?? new Company()
                {
                    Name = descriptor.CompanyName!,
                    Tin = descriptor.Tin!
                },
                UserProviders = UserProvider.ConvertDictionaryToUserProviders(descriptor.ProviderKeys)
            };
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        if (options.Value.Uri?.AbsoluteUri?.IsNullOrEmpty() == false)
        {
            if (AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
            {
                var client = clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = authentication;

                // NOTE: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
                //       However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
                var result = await client.PostAsJsonAsync<Dictionary<string, object?>>($"{options.Value.Uri.AbsoluteUri}/relations", new()
                {
                    { "ssn", descriptor.Subject},
                    { "tin", descriptor.Tin }
                });

                if (!result.IsSuccessStatusCode)
                {
                    logger.LogWarning("AcceptTerms: Unable to create relations for {subject}", descriptor.Subject);
                }
            }
        }

        return NoContent();
    }
}
