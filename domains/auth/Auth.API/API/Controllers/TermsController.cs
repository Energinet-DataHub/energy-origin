using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        IUserService userService,
        ICompanyService companyService,
        IHttpClientFactory clientFactory,
        ICryptography cryptography,
        DataSyncOptions dataSyncOptions,
        RoleOptions roleOptions,
        TermsOptions termsOptions,
        OidcOptions oidcOptions,
        int version)
    {
        if (termsOptions.PrivacyPolicyVersion < version)
        {
            return BadRequest($"The user cannot accept {nameof(UserTermsType.PrivacyPolicy)} version '{version}', since the latest version is '{termsOptions.PrivacyPolicyVersion}'.");
        }

        var descriptor = new DecodableUserDescriptor(User, cryptography);

        var type = UserTermsType.PrivacyPolicy;
        var user = await userService.GetUserByIdAsync(descriptor.Id);
        var acceptedVersion = user?.UserTerms.SingleOrDefault(x => x.Type == type)?.AcceptedVersion ?? 0;

        if (acceptedVersion > version)
        {
            return BadRequest($"The user cannot accept {nameof(UserTermsType.PrivacyPolicy)} version '{version}', when they had previously accepted version '{acceptedVersion}'.");
        }

        var company = await companyService.GetCompanyByTinAsync(descriptor.Organization?.Tin);
        if (company == null && descriptor.Organization?.Tin != null)
        {
            company = new Company()
            {
                Id = oidcOptions.ReuseSubject ? descriptor.Id : null,
                Name = descriptor.Organization!.Name,
                Tin = descriptor.Organization!.Tin
            };
        }

        if (user == null)
        {
            user = new User
            {
                Id = descriptor.Id,
                Name = descriptor.Name,
                AllowCprLookup = descriptor.AllowCprLookup,
            };

            user.UserRoles.AddRange(roleOptions.RoleConfigurations.Where(x => x.IsDefault).ToList().Select(x =>
                new UserRole { Role = x.Key, UserId = descriptor.Id }
            ));

            await userService.InsertUserAsync(user);
            if (company != null)
            {
                user.CompanyId = company.Id;
            }
            user.Company = company;
            user.UserProviders = UserProvider.ConvertDictionaryToUserProviders(descriptor.ProviderKeys);
        }

        var userTerms = user.UserTerms.SingleOrDefault(x => x.Type == type);
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

        var relationUri = dataSyncOptions.Uri?.AbsoluteUri.TrimEnd('/');
        if (relationUri != null && AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
        {
            var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = authentication;

            // NOTE/TODO: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
            //            However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
            var result = await client.PostAsJsonAsync<Dictionary<string, object?>>($"{relationUri}/relations", new()
            {
                { "ssn", null },
                { "tin", descriptor.Organization?.Id }
            });

            if (result.IsSuccessStatusCode == false)
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {Subject}", descriptor.Subject);
            }
        }

        logger.AuditLog(
            "{User} updated accepted Privacy policy {Version} at {TimeStamp}.",
            user.Id,
            userTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }

    [Authorize(Roles = RoleKey.OrganizationAdmin, Policy = PolicyName.RequiresCompany)]
    [HttpPut]
    [Route("terms/company/accept/{version}")]
    public async Task<IActionResult> AcceptCompanyAsync(
        ILogger<TermsController> logger,
        IUserService userService,
        TermsOptions termsOptions,
        int version)
    {
        if (termsOptions.TermsOfServiceVersion < version)
        {
            return BadRequest($"The user cannot accept {nameof(CompanyTermsType.TermsOfService)} terms of service version '{version}', since the latest version is '{termsOptions.TermsOfServiceVersion}'.");
        }

        var descriptor = new UserDescriptor(User);
        var user = await userService.GetUserByIdAsync(descriptor.Id);

        var type = CompanyTermsType.TermsOfService;
        var acceptedVersion = user?.Company?.CompanyTerms.SingleOrDefault(x => x.Type == CompanyTermsType.TermsOfService)?.AcceptedVersion ?? 0;
        if (acceptedVersion > version)
        {
            return BadRequest($"The user cannot accept {nameof(CompanyTermsType.TermsOfService)} version '{version}', when they had previously accepted version '{acceptedVersion}'.");
        }

        var companyTerms = user!.Company!.CompanyTerms.SingleOrDefault(x => x.Type == type);
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
            "{User} updated accepted Terms of service {Version} at {TimeStamp}.",
            user.Id,
            companyTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }
}
