using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.AuthorizePolicies;
using API.Utilities.Interfaces;
using API.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut]
    [Route("terms/user/accept/{version}")]
    public async Task<IActionResult> AcceptUserTermsAsync(
        ILogger<TermsController> logger,
        IHttpContextAccessor accessor,
        IUserDescriptorMapper mapper,
        IUserService userService,
        ICompanyService companyService,
        IHttpClientFactory clientFactory,
        IOptions<DataSyncOptions> dataSyncOptions,
        [FromRoute] int version)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        if (descriptor.AcceptedPrivacyPolicyVersion > version)
        {
            throw new ArgumentException($"The user cannot accept privacy policy version '{version}', when they had previously accepted version '{descriptor.AcceptedPrivacyPolicyVersion}'.");
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

        var user = await userService.GetUserByIdAsync(descriptor.Id);
        if (user == null)
        {
            user = new User
            {
                Id = descriptor.Id,
                Name = descriptor.Name,
                AllowCprLookup = descriptor.AllowCprLookup,
            };
            await userService.InsertUserAsync(user);
            user.Company = company;
            user.UserProviders = UserProvider.ConvertDictionaryToUserProviders(descriptor.ProviderKeys);
        }

        var type = UserTermsType.PrivacyPolicy;
        var userTerms = user.UserTerms.FirstOrDefault(x => x.Type == type);
        if (userTerms == null)
        {
            userTerms = new UserTerms()
            {
                Type = type,
            };
            user.UserTerms.Add(userTerms);
        }
        userTerms.AcceptedVersion = version;

        await userService.UpsertUserAsync(user);

        var relationUri = dataSyncOptions.Value.Uri?.AbsoluteUri.TrimEnd('/');
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
                logger.LogWarning("AcceptTerms: Unable to create relations for {Subject}", descriptor.Subject);
            }
        }

        logger.AuditLog(
            "{User} updated accepted Privacy policy {Versions} at {TimeStamp}.",
            user.Id,
            userTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }

    [Authorize(Policy = nameof(OrganizationOwnerPolicy))]
    [HttpPut]
    [Route("terms/company/accept/{version}")]
    public async Task<IActionResult> AcceptCompanyAsync(
        ILogger<TermsController> logger,
        IUserDescriptorMapper mapper,
        IUserService userService,
        [FromRoute] int version)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var user = await userService.GetUserByIdAsync(descriptor.Id);

        if (descriptor.AcceptedTermsOfServiceVersion > version)
        {
            throw new ArgumentException($"The user cannot accept terms of service version '{version}', when they had previously accepted version '{descriptor.AcceptedTermsOfServiceVersion}'.");
        }

        var type = CompanyTermsType.TermsOfService;
        var companyTerms = user!.Company!.CompanyTerms.FirstOrDefault(x => x.Type == type);
        if (companyTerms == null)
        {
            companyTerms = new CompanyTerms()
            {
                Type = type,
            };
            user.Company.CompanyTerms.Add(companyTerms);
        }
        companyTerms.AcceptedVersion = version;

        await userService.UpsertUserAsync(user);

        logger.AuditLog(
            "{User} updated accepted Terms of service {Versions} at {TimeStamp}.",
            user.Id,
            companyTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }
}
