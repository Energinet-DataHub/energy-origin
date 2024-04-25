using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;

namespace API.Services;

public class UserSignUpService(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IAffiliationRepository affiliationRepository,
    IUnitOfWork unitOfWork)
    : IUserSignUpService
{
    public async Task ProcessUserSignUpAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var idpId = jwtToken.Claims.FirstOrDefault(c => c.Type == "idp_identity_id")?.Value;
        var idpUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_cpr_uuid")?.Value;
        var idpOrganizationId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_persistent_professional_id")?.Value;
        var tin = jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_cvr")?.Value;
        var organizationName = jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_org_name")?.Value;
        var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_given_name")?.Value + " " + jwtToken.Claims.FirstOrDefault(c => c.Type == "nemlogin_family_name")?.Value;

        if (string.IsNullOrEmpty(idpId) || string.IsNullOrEmpty(idpUserId) || string.IsNullOrEmpty(idpOrganizationId) || string.IsNullOrEmpty(tin) || string.IsNullOrEmpty(organizationName) || string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Required claims are missing.");
        }

        // Begin transaction
        await unitOfWork.BeginTransactionAsync();

        try
        {
            // Create or update the organization
            var organization = new Organization
            {
                IdpId = Guid.Parse(idpId),
                IdpOrganizationId = Guid.Parse(idpOrganizationId),
                Tin = new Tin(tin),
                OrganizationName = new OrganizationName(organizationName)
            };
            await organizationRepository.AddAsync(organization);

            // Create or update the user
            var user = new User
            {
                IdpId = Guid.Parse(idpId).ToString(),
                IdpUserId = idpUserId,
                Name = name
            };
            await userRepository.AddAsync(user);

            // Create the affiliation
            var affiliation = new Affiliation(user, organization);
            await affiliationRepository.AddAsync(affiliation);

            // Commit the transaction
            await unitOfWork.CommitAsync();
        }
        catch (Exception)
        {
            // Rollback the transaction in case of failure
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
