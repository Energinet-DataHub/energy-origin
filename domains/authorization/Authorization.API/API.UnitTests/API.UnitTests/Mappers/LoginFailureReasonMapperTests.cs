using API.Authorization.Controllers;
using API.Models;

namespace API.UnitTests.Mappers;

public class LoginFailureReasonContractTests
{
    [Theory]
    [InlineData("normal", OrganizationStatus.Trial, "a1b2c3d4-e111-4444-aaaa-aaaaaaaaaaaa")]
    [InlineData("trial", OrganizationStatus.Normal, "b2c3d4e5-e222-5555-bbbb-bbbbbbbbbbbb")]
    [InlineData("trial", OrganizationStatus.Deactivated, "c3d4e5f6-e333-6666-cccc-cccccccccccc")]
    [InlineData("normal", OrganizationStatus.Deactivated, "c3d4e5f6-e333-6666-cccc-cccccccccccc")]
    [InlineData("normal", null, "e5f6g7h8-e444-7777-dddd-dddddddddddd")]
    [InlineData("trial", null, "e5f6g7h8-e444-7777-dddd-dddddddddddd")]
    [InlineData("invalid", null, "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee")]
    [InlineData("", OrganizationStatus.Normal, "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee")]
    public void LoginFailureReasons_HaveExpectedGuids(string loginType, OrganizationStatus? status, string expected)
    {
        var failureGuid = (loginType.ToLowerInvariant(), status) switch
        {
            ("normal", OrganizationStatus.Trial) => LoginFailureReasons.NormalLoginForTrialOrg,
            ("trial", OrganizationStatus.Normal) => LoginFailureReasons.TrialLoginForNormalOrg,
            (_, OrganizationStatus.Deactivated) => LoginFailureReasons.OrgIsDeactivated,
            ("normal", _) or ("trial", _) => LoginFailureReasons.NoMatchingCase,
            _ => LoginFailureReasons.UnknownLoginType
        };

        Assert.Equal(expected, failureGuid);
    }
}
