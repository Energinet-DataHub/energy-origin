using API.Models;
using API.ValueObjects;

namespace API.UnitTests;

public class Any
{
    public static User User()
    {
        return API.Models.User.Create(IdpId(), IdpUserId(), Name());
    }

    public static IdpId IdpId()
    {
        return API.ValueObjects.IdpId.Create(Guid());
    }

    public static IdpUserId IdpUserId()
    {
        return API.ValueObjects.IdpUserId.Create(Guid());
    }

    public static Username Name()
    {

        return API.ValueObjects.Username.Create("Test Testesen");
    }

    public static Guid Guid()
    {
        return System.Guid.NewGuid();
    }

    public static Tin Tin()
    {
        return API.ValueObjects.Tin.Create("12345678");
    }

    public static OrganizationName OrganizationName()
    {
        return API.ValueObjects.OrganizationName.Create("Wind turbines'R'us");
    }

    public static Organization Organization()
    {
        return API.Models.Organization.Create( Tin(), OrganizationName());
    }

    public static IdpClientId IdpClientId()
    {
        return new IdpClientId(Guid());
    }

    public static Consent Consent()
    {
        return API.Models.Consent.Create(Organization(), Client(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    public static Client Client()
    {
        return API.Models.Client.Create(IdpClientId(), Role.External, "https://redirect.url");
    }
}
