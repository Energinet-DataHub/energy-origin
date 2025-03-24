using API.Authorization.Controllers;
using API.Models;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using ClientType = API.Models.ClientType;

namespace API.UnitTests;

public class Any
{
    public static User User()
    {
        return API.Models.User.Create(IdpUserId(), Name());
    }

    public static IdpId IdpId()
    {
        return API.ValueObjects.IdpId.Create(Guid());
    }

    public static IdpUserId IdpUserId()
    {
        return API.ValueObjects.IdpUserId.Create(Guid());
    }

    public static Terms Terms()
    {
        return API.Models.Terms.Create(1);
    }

    public static Whitelisted Whitelisted(Tin? tin = null)
    {
        return API.Models.Whitelisted.Create(tin ?? Tin());
    }

    public static UserName Name()
    {
        return UserName.Create("Test Testesen");
    }

    public static Guid Guid()
    {
        return System.Guid.NewGuid();
    }

    public static Tin Tin()
    {
        return EnergyOrigin.Domain.ValueObjects.Tin.Create(IntString(8));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }

    public static OrganizationName OrganizationName()
    {
        return EnergyOrigin.Domain.ValueObjects.OrganizationName.Create("Wind turbines'R'us");
    }

    public static Organization Organization(Tin? tin = null, OrganizationName? organizationName = null)
    {
        return API.Models.Organization.Create(tin ?? Tin(), organizationName ?? OrganizationName());
    }

    public static IdpClientId IdpClientId()
    {
        return new IdpClientId(Guid());
    }

    public static Client Client()
    {
        return API.Models.Client.Create(IdpClientId(), new ClientName("ClientName"), ClientType.External,
            "https://redirect.url");
    }

    public static Organization OrganizationWithClient(Tin? tin = null, Client? client = null)
    {
        var organization = Organization(tin);

        organization.Clients.Add(client ?? Client());
        organization.AcceptServiceProviderTerms();

        return organization;
    }

    public static MitIdUserinfoResponse MitIdUserinfoResponse()
    {
        return new MitIdUserinfoResponse("idp", "idpId", "loa", "lal", "professionalId", "loginName", "loginLal",
            "loginOrgName", "loginCvr", "loginNemIdRid", "loginGivenName", "loginFamilyName", "loginEmail", "loginAge",
            "loginCprUuid", "loginDateOfBirth", "hasCpr", "sub");
    }
}
