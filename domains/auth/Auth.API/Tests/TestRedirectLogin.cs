using API.Services;
using API.Models;
using Tests.Resources;
using Xunit;
using Xunit.Categories;
using Moq;
using Microsoft.AspNetCore.Http.Extensions;

namespace Tests;


[UnitTest]
public sealed class TestRedirectLogin
{
    private readonly Mock<ISignaturGruppen> _moqSignaturGruppen;

    public TestRedirectLogin()
    {
        _moqSignaturGruppen = new Mock<ISignaturGruppen>();
    }

    [Fact]
    public void Oidc_Redirect_success()
    {
        //Arrange

        AddEnvironmentVariables.EnvironmentVariables();

        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState(feUrl, returnUrl);

        var expectedNextUrl = "?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE%2BSCOPE1&state=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmZV91cmwiOiJodHRwOi8vdGVzdC5lbmVyZ2lvcHJpbmRlbHNlLmRrIiwicmV0dXJuX3VybCI6Imh0dHBzOi8vZGVtby5lbmVyZ2lvcHJpbmRlbHNlLmRrL2Rhc2hib2FyZCIsInRlcm1zX2FjY2VwdGVkIjoiRmFsc2UiLCJ0ZXJtc192ZXJzaW9uIjoiMCIsImlkX3Rva2VuIjoiIiwidGluIjoiIiwiaWRlbnRpdHlfcHJvdmlkZXIiOiIiLCJleHRlcm5hbF9zdWJqZWN0IjoiIn0.0QgNo_mOGnthS5R4lYTONYC7xmOtywdldWrDXCma8uQ&language=en";

        //Act
        var OidcLogin = new OidcService();
        var res = OidcLogin.CreateAuthorizationRedirectUrl("code", feUrl, state, "en");

        //Assert
        Assert.NotNull(res);
        Assert.NotEmpty(res);
        Assert.IsType<QueryBuilder>(res);
        Assert.Equal(expectedNextUrl, res.ToString());
    }

    [Fact]
    public void SignaturGruppen_Redirect_success()
    {
        //Arrange

        AddEnvironmentVariables.EnvironmentVariables();

        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState(feUrl, returnUrl);

        var expectedNextUrl = "?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE%2BSCOPE1&state=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmZV91cmwiOiJodHRwOi8vdGVzdC5lbmVyZ2lvcHJpbmRlbHNlLmRrIiwicmV0dXJuX3VybCI6Imh0dHBzOi8vZGVtby5lbmVyZ2lvcHJpbmRlbHNlLmRrL2Rhc2hib2FyZCIsInRlcm1zX2FjY2VwdGVkIjoiRmFsc2UiLCJ0ZXJtc192ZXJzaW9uIjoiMCIsImlkX3Rva2VuIjoiIiwidGluIjoiIiwiaWRlbnRpdHlfcHJvdmlkZXIiOiIiLCJleHRlcm5hbF9zdWJqZWN0IjoiIn0.0QgNo_mOGnthS5R4lYTONYC7xmOtywdldWrDXCma8uQ&language=en&idp_params=%7B%22nemid%22%3A%7B%22amr_values%22%3A%22AMRVALUES%22%7D%7D";

        _moqSignaturGruppen.Setup(x => x.CreateRedirecthUrl(feUrl, returnUrl))
            .Returns(new LoginResponse(expectedNextUrl));

        //Act
        var res = _moqSignaturGruppen.Object.CreateRedirecthUrl(feUrl, returnUrl);

        //Assert
        Assert.NotNull(res.NextUrl);
        Assert.NotEmpty(res.NextUrl);
        Assert.IsType<LoginResponse>(res);
        Assert.Equal(expectedNextUrl, res.NextUrl);
    }
}
