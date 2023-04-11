using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
            throw new ArgumentException($"The user cannot accept terms version '{acceptedTermsVersion.Version}', when they had previously accepted version '{descriptor.AcceptedTermsVersion}'.");
        }

        var company = await companyService.GetCompanyByTinAsync(descriptor.Tin);
        if (company == null && descriptor.Tin != null)
        {
            company = new Company()
            {
                Name = descriptor.CompanyName!,
                Tin = descriptor.Tin!
            };
        }

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
            };
            await userService.InsertUserAsync(user);
            user.Company = company;
            user.UserProviders = UserProvider.ConvertDictionaryToUserProviders(descriptor.ProviderKeys);
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;
        await userService.UpsertUserAsync(user);

        var relationUri = options.Value.Uri.AbsoluteUri;
        if (relationUri != null && AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
        {
            var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = authentication;

            // NOTE/TODO: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
            //            However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
            var result = await client.PostAsJsonAsync<Dictionary<string, object?>>($"{relationUri}/relations", new()
            {
                { "ssn", null },
                { "tin", descriptor.Tin }
            });

            if (!result.IsSuccessStatusCode)
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {subject}", descriptor.Subject);
            }
        }

        await Task.Delay(5000);

        return NoContent();
    }
}
