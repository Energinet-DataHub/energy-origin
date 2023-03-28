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
        IHostEnvironment env,
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

        if (descriptor.AcceptedTermsVersion >= acceptedTermsVersion.Version)
        {
            throw new ArgumentException("The user has already accepted the same or a newer version of terms.");
        }

        var company = await companyService.GetCompanyByTinAsync(descriptor.Tin);

        User user;
        if (descriptor.Id is null)
        {
            user = new User
            {
                Name = descriptor.Name,
                AllowCPRLookup = descriptor.AllowCPRLookup,
                Company = descriptor.Tin is null ? null : company ?? new Company()
                {
                    Name = descriptor.CompanyName!,
                    Tin = descriptor.Tin!
                },
                UserProviders = UserProvider.GetUserProviders(descriptor.ProviderKeys)
            };
        }
        else
        {
            var id = descriptor.Id.Value;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        if (env.IsDevelopment() is false)
        {
            if (AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
            {
                var client = clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = authentication;

                // NOTE/TODO: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
                //            However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
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
