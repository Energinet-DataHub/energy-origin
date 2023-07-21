using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.AuthorizePolicies;
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
    [HttpPut]
    [Route("terms/acceptUser")]
    public async Task<IActionResult> AcceptUserAsync(
        ILogger<TermsController> logger,
        IHttpContextAccessor accessor,
        IUserDescriptorMapper mapper,
        IUserService userService,
        ICompanyService companyService,
        IHttpClientFactory clientFactory,
        IOptions<DataSyncOptions> dataSyncOptions,
        IOptions<TermsOptions> termsOptions,
        [FromBody] AcceptUserTermsRequest acceptUserTermsRequest)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

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

        var userTerms = user.UserTerms.FirstOrDefault(x => x.Type == acceptUserTermsRequest.TermsType);
        if (userTerms == null)
        {
            userTerms = new UserTerms()
            {
                Type = acceptUserTermsRequest.TermsType,
            };
            user.UserTerms.Add(userTerms);
        }
        userTerms.AcceptedVersion = termsOptions.Value.PrivacyPolicyVersion;

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
            termsOptions.Value.PrivacyPolicyVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return NoContent();
    }

    [Authorize(Policy = nameof(OrganizationOwnerPolicy))]
    [HttpPut]
    [Route("terms/acceptCompany")]
    public async Task<IActionResult> AcceptCompanyAsync(
        ILogger<TermsController> logger,
        IUserDescriptorMapper mapper,
        IUserService userService,
        IOptions<TermsOptions> termsOptions,
        [FromBody] AcceptCompanyTermsRequest acceptedCompanyTermsVersion)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var user = await userService.GetUserByIdAsync(descriptor.Id);

        var companyTerms = user!.Company!.CompanyTerms.FirstOrDefault(x => x.Type == acceptedCompanyTermsVersion.TermsType);
        if (companyTerms == null)
        {
            companyTerms = new CompanyTerms()
            {
                Type = acceptedCompanyTermsVersion.TermsType,
            };
            user.Company.CompanyTerms.Add(companyTerms);
        }
        companyTerms.AcceptedVersion = termsOptions.Value.TermsOfServiceVersion;

        await userService.UpsertUserAsync(user);

        logger.AuditLog(
            "{User} updated accepted Terms of service {Versions} at {TimeStamp}.",
            user.Id,
            termsOptions.Value.TermsOfServiceVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return NoContent();
    }
}
