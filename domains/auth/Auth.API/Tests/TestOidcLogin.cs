using API.Services;
using Tests.Resources;
using Xunit;
using Xunit.Categories;

namespace Tests;


[UnitTest]
public sealed class TestOidcLogin
{

    [Fact]
    public void Oidc_Redirect_success()
    {
        //Arrange

        EnvironmentVariable.EnvironmentVariables();

        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var expectedNextUrl = "OIDCURL?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE%2BSCOPE1&state=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmZV91cmwiOiJodHRwOi8vdGVzdC5lbmVyZ2lvcHJpbmRlbHNlLmRrIiwicmV0dXJuX3VybCI6Imh0dHBzOi8vZGVtby5lbmVyZ2lvcHJpbmRlbHNlLmRrL2Rhc2hib2FyZCIsInRlcm1zX2FjY2VwdGVkIjoiRmFsc2UiLCJ0ZXJtc192ZXJzaW9uIjoiMCIsImlkX3Rva2VuIjoiIiwidGluIjoiIiwiaWRlbnRpdHlfcHJvdmlkZXIiOiIiLCJleHRlcm5hbF9zdWJqZWN0IjoiIn0.0QgNo_mOGnthS5R4lYTONYC7xmOtywdldWrDXCma8uQ&language=en&idp_params=%7B%22nemid%22%3A%7B%22amr_values%22%3A%22AMRVALUES%22%7D%7D";
        var OidcLogin = new SignaturGruppen();

        //Act

        var res = OidcLogin.CreateRedirecthUrl(feUrl, returnUrl);

        //Assert
        Assert.NotNull(res.NextUrl);
        Assert.NotEmpty(res.NextUrl);
        Assert.Equal(expectedNextUrl, res.NextUrl);
    }
}
