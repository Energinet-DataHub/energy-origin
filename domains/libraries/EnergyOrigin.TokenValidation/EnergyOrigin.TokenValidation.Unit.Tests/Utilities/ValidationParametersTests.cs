using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class ValidationParametersTests
{
    [Fact]
    public void Constructor_ShouldSetRoleClaimKey_Always()
    {
        var encoded = "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF3TENkQmwrekdJei9Va0xHVEtmaAplc0F0bUUxQUIrL25lQ0djU1VmU3h1Z2tkc3J3NXBuYmlteGtyZnU2RGxpSmtDTXZMUkZPOHRLZHhnYjN1ZWxPCm96U2ZpS2h3Nm9NRmQzWEd4dUpLeWljUVFFRVVwdkxmS3AxN2NmTE1aRG44T1lPYmQ2Q3lXVjBvczIzb2cyZVkKcmtvRzg1UzdOQ2JQeVJUSVF3VUJvNGw0bE9RWnNpNjgwaXdvU0ErVWRFL25La0dGVGxSOERYTW45NUR2V1hacQpkdDRjMXlPbmpMRmRyYTRLODRlblZ5Wm56SlQvRkFRVGRJTG9zLzhOVnlQM3pOY21sWmZmZDJUZlp5T3NRRHVhClVKQzd0eFlUYm50UlpZMnd5clVXaTZkblFOSkFwdmx5ZEN5QnpibmM3MW9Mc2NXQXpGZjdnSzRRSWI4SVNjWEQKUlFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg==";
        var pem = Convert.FromBase64String(encoded);

        var item = new ValidationParameters(pem);

        Assert.Equal(UserClaimName.Roles, item.RoleClaimType);
    }

    [Fact]
    public void Constructor_ShouldSetParameters_WhenPemIsCorrectFormat()
    {
        var encoded = "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF3TENkQmwrekdJei9Va0xHVEtmaAplc0F0bUUxQUIrL25lQ0djU1VmU3h1Z2tkc3J3NXBuYmlteGtyZnU2RGxpSmtDTXZMUkZPOHRLZHhnYjN1ZWxPCm96U2ZpS2h3Nm9NRmQzWEd4dUpLeWljUVFFRVVwdkxmS3AxN2NmTE1aRG44T1lPYmQ2Q3lXVjBvczIzb2cyZVkKcmtvRzg1UzdOQ2JQeVJUSVF3VUJvNGw0bE9RWnNpNjgwaXdvU0ErVWRFL25La0dGVGxSOERYTW45NUR2V1hacQpkdDRjMXlPbmpMRmRyYTRLODRlblZ5Wm56SlQvRkFRVGRJTG9zLzhOVnlQM3pOY21sWmZmZDJUZlp5T3NRRHVhClVKQzd0eFlUYm50UlpZMnd5clVXaTZkblFOSkFwdmx5ZEN5QnpibmM3MW9Mc2NXQXpGZjdnSzRRSWI4SVNjWEQKUlFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg==";
        var pem = Convert.FromBase64String(encoded);

        var validationParameters = new ValidationParameters(pem);

        Assert.NotNull(validationParameters.IssuerSigningKey);
        Assert.True(validationParameters.ValidateIssuerSigningKey);
    }

    [Fact]
    public void Constructor_ShouldThrowError_WhenPemIsNotCorrect()
    {
        var encoded = "LS0tLS1JUkIJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF3TENkQmwrekdJei9Va0xHVEtmaAplc0F0bUUxQUIrL25lQ0djU1VmU3h1Z2tkc3J3NXBuYmlteGtyZnU2RGxpSmtDTXZMUkZPOHRLZHhnYjN1ZWxPCm96U2ZpS2h3Nm9NRmQzWEd4dUpLeWljUVFFRVVwdkxmS3AxN2NmTE1aRG44T1lPYmQ2Q3lXVjBvczIzb2cyZVkKcmtvRzg1UzdOQ2JQeVJUSVF3VUJvNGw0bE9RWnNpNjgwaXdvU0ErVWRFL25La0dGVGxSOERYTW45NUR2V1hacQpkdDRjMXlPbmpMRmRyYTRLODRlblZ5Wm56SlQvRkFRVGRJTG9zLzhOVnlQM3pOY21sWmZmZDJUZlp5T3NRRHVhClVKQzd0eFlUYm50UlpZMnd5clVXaTZkblFOSkFwdmx5ZEN5QnpibmM3MW9Mc2NXQXpGZjdnSzRRSWI4SVNjWEQKUlFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg==";
        var pem = Convert.FromBase64String(encoded);

        Assert.Throws<ArgumentException>(() => new ValidationParameters(pem));
    }

    [Fact]
    public void Constructor_ShouldThrowError_WhenPemIsEmptyByteArray() => Assert.Throws<ArgumentException>(() => new ValidationParameters(Array.Empty<byte>()));
}
