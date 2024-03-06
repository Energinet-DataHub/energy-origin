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
using Relation.V1;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut]
    [Route("terms/user/accept/{version}")]
    public async Task<IActionResult> AcceptUserTermsAsync(
        ILogger<TermsController> logger,
        IUserService userService,
        IHttpContextAccessor accessor,
        ICompanyService companyService,
        ICryptography cryptography,
        RoleOptions roleOptions,
        TermsOptions termsOptions,
        OidcOptions oidcOptions,
        Relation.V1.Relation.RelationClient relationClient,
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
        var newCompany = company == null;
        if (company == null && descriptor.Organization?.Tin != null)
        {
            company = new Company
            {
                Id = oidcOptions.ReuseSubject ? descriptor.Organization!.Id : Guid.NewGuid(),
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

            if (newCompany)
            {
                user.Company = company;
            }
            else
            {
                user.CompanyId = company?.Id;
            }

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

        await userService.UpdateTermsAccepted(user);

        if (AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
        {
            var request = new CreateRelationRequest
            {
                Subject = descriptor.Subject.ToString(),
                Actor = descriptor.Id.ToString(),
                Ssn = "",
                Tin = descriptor.Organization?.Tin
            };
            try
            {
                var res = await relationClient.CreateRelationAsync(request, cancellationToken: CancellationToken.None);
                if (res.Success == false)
                {
                    logger.LogWarning("AcceptTerms: Unable to create relations for {Subject}. Error: {ErrorMessage}",
                        descriptor.Subject, res.ErrorMessage);
                }
            }
            catch (Exception e)
            {
                logger.LogError("AcceptTerms: Unable to create relations for {Subject}. Exception: {e}",
                                       descriptor.Subject, e);
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

        await userService.UpdateTermsAccepted(user);

        logger.AuditLog(
            "{User} updated accepted Terms of service {Version} at {TimeStamp}.",
            user.Id,
            companyTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }
}
