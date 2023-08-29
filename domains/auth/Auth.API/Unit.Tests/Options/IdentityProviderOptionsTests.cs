using API.Options;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests.Options;

public class IdentityProviderOptionsTests
{
    [Theory]
    [InlineData("openid ssn userinfo_token nemid", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemIdPrivate)]
    [InlineData("openid ssn userinfo_token nemid", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemIdProfessional)]
    [InlineData("openid ssn userinfo_token nemid private_to_business", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemIdPrivate, ProviderType.NemIdProfessional)]
    [InlineData("openid ssn userinfo_token mitid", "mitid", null, ProviderType.MitIdPrivate)]
    [InlineData("openid ssn userinfo_token nemlogin", "mitid_erhverv", null, ProviderType.MitIdProfessional)]
    [InlineData("openid ssn userinfo_token mitid nemlogin", "mitid mitid_erhverv", null, ProviderType.MitIdPrivate, ProviderType.MitIdProfessional)]
    [InlineData("openid ssn userinfo_token nemid mitid", "nemid mitid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitIdPrivate, ProviderType.NemIdPrivate)]
    [InlineData("openid ssn userinfo_token nemid nemlogin", "nemid mitid_erhverv", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitIdProfessional, ProviderType.NemIdProfessional)]
    [InlineData("openid ssn userinfo_token nemid private_to_business mitid nemlogin", "nemid mitid mitid_erhverv", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitIdPrivate, ProviderType.MitIdProfessional, ProviderType.NemIdPrivate, ProviderType.NemIdProfessional)]
    public void GetIdentityProviderArguments_ShouldReturnCorrectValues_WhenInvokedWithNemIdProviderOptions(string expectedScope, string expectedValues, string expectedParams, params ProviderType[] providerTypes)
    {
        var providerOptions = new IdentityProviderOptions
        {
            Providers = providerTypes.ToList()
        };

        var (actualScope, actualArguments) = providerOptions.GetIdentityProviderArguments();

        Assert.Equal(expectedScope, actualScope);
        Assert.Contains(new KeyValuePair<string, string>("idp_values", expectedValues), actualArguments);

        var ipdParams = actualArguments.Where(x => x.Key == "idp_params").Select(x => x.Value).SingleOrDefault();
        if (ipdParams != null)
        {
            Assert.True(!string.IsNullOrEmpty(expectedParams));
            Assert.Equal(expectedParams, ipdParams);
        }
        else
        {
            Assert.True(string.IsNullOrEmpty(expectedParams));
        }
    }

    [Fact]
    public void GetIdentityProviderArguments_ShouldThrowArgumentException_WhenProvidersListIsEmpty()
    {
        var providerOptions = new IdentityProviderOptions()
        {
            Providers = new List<ProviderType>()
        };

        Assert.Throws<ArgumentException>(() => providerOptions.GetIdentityProviderArguments());
    }
}
