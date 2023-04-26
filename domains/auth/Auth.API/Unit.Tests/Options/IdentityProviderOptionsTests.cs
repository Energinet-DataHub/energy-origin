using API.Options;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests.Options;

public class IdentityProviderOptionsTests
{
    [Theory]
    [InlineData("openid ssn userinfo_token nemid", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemID_Private)]
    [InlineData("openid ssn userinfo_token nemid", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemID_Professional)]
    [InlineData("openid ssn userinfo_token nemid private_to_business", "nemid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.NemID_Private, ProviderType.NemID_Professional)]
    [InlineData("openid ssn userinfo_token mitid", "mitid", null, ProviderType.MitID_Private)]
    [InlineData("openid ssn userinfo_token nemlogin", "mitid_erhverv", null, ProviderType.MitID_Professional)]
    [InlineData("openid ssn userinfo_token mitid nemlogin", "mitid mitid_erhverv", null, ProviderType.MitID_Private, ProviderType.MitID_Professional)]
    [InlineData("openid ssn userinfo_token nemid mitid", "nemid mitid", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitID_Private, ProviderType.NemID_Private)]
    [InlineData("openid ssn userinfo_token nemid nemlogin", "nemid mitid_erhverv", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitID_Professional, ProviderType.NemID_Professional)]
    [InlineData("openid ssn userinfo_token nemid private_to_business mitid nemlogin", "nemid mitid mitid_erhverv", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}}""", ProviderType.MitID_Private, ProviderType.MitID_Professional, ProviderType.NemID_Private, ProviderType.NemID_Professional)]
    public void GetIdentityProviderArguments_ShouldReturnCorrectValues_WhenInvokedWithNemIdProviderOptions(string expectedScope, string expectedValues, string expectedParams, params ProviderType[] providerTypes)
    {
        var providerOptions = new IdentityProviderOptions
        {
            Providers = providerTypes.ToList()
        };

        var (actualScope, actualArguments) = providerOptions.GetIdentityProviderArguments();

        Assert.Equal(expectedScope, actualScope);
        Assert.Contains(new KeyValuePair<string, string>("idp_values", expectedValues), actualArguments);

        var ipdParams = actualArguments.Where(x => x.Key == "idp_params").Select(x => x.Value).FirstOrDefault();
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
