using System.Diagnostics;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
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
        IUserService userService,
        IHttpContextAccessor accessor,
        ICompanyService companyService,
        ICryptography cryptography,
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

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        await userService.UpdateTermsAccepted(user, descriptor, traceId);

        logger.AuditLog(
            "{User} updated accepted Privacy policy {Version} at {TimeStamp}.",
            user.Id,
            userTerms.AcceptedVersion,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );

        return Ok();
    }
}
