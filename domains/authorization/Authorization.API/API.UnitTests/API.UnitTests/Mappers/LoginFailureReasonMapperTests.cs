using API.Authorization.Controllers;
using API.Models;

namespace API.UnitTests.Mappers;

public class LoginFailureReasonContractTests
{
    [Theory]
    [InlineData("normal", OrganizationStatus.Trial, "a1b2c3d4-e111-4444-aaaa-aaaaaaaaaaaa - Trial Organization is not allowed to log in as a Normal Organization - Please log in as Trial Organization, or contact support, if you think this is an error")]
    [InlineData("trial", OrganizationStatus.Normal, "b2c3d4e5-e222-5555-bbbb-bbbbbbbbbbbb - Normal Organization is not allowed to log in as a Trial organization - Please log in as Normal Organization, or contact support, if you think this is an error")]
    [InlineData("trial", OrganizationStatus.Deactivated, "c3d4e5f6-e333-6666-cccc-cccccccccccc - Organization is deactivated - Please contact support, if you think this is an error")]
    [InlineData("normal", OrganizationStatus.Deactivated, "c3d4e5f6-e333-6666-cccc-cccccccccccc - Organization is deactivated - Please contact support, if you think this is an error")]
    [InlineData("normal", null, "e5f6g7h8-e444-7777-dddd-dddddddddddd - Unknown login type specified in request - Have you configured your client correctly?")]
    [InlineData("trial", null, "e5f6g7h8-e444-7777-dddd-dddddddddddd - Unknown login type specified in request - Have you configured your client correctly?")]
    [InlineData("invalid", null, "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee - Unhandled Exception")]
    [InlineData("", null, "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee - Unhandled Exception")]
    public void LoginFailureReasons_HaveExpectedGuids(string loginType, OrganizationStatus? status, string expected)
    {
        var failureGuid = (loginType.ToLowerInvariant(), status) switch
        {
            ("normal", OrganizationStatus.Trial) => LoginFailureReasons.TrialOrganizationIsNotAllowedToLogInAsNormalOrganization,
            ("trial", OrganizationStatus.Normal) => LoginFailureReasons.NormalOrganizationsAreNotAllowedToLogInAsTrial,
            (_, OrganizationStatus.Deactivated) => LoginFailureReasons.OrganizationIsDeactivated,
            ("normal", _) or ("trial", _) => LoginFailureReasons.UnknownLoginTypeSpecifiedInRequest,
            _ => LoginFailureReasons.UnhandledException
        };

        Assert.Equal(expected, failureGuid);
    }
}
