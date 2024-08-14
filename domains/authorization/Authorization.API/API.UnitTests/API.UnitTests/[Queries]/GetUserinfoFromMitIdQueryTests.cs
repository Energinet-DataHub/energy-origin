using API.Authorization._Features_;
using API.Authorization.Controllers;
using API.Services;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Queries_;

public class GetUserinfoFromMitIdQueryTests
{
    [Fact]
    async Task GivenBearerToken_WhenGettingUserinfo_UserinfoReturned()
    {
        var userinfo = Any.MitIdUserinfoResponse();
        var mitIdService = Substitute.For<IMitIDService>();
        mitIdService.GetUserinfo(string.Empty).ReturnsForAnyArgs(userinfo);

        var handler = new GetUserinfoFromMitIdQueryHandler(mitIdService);
        var result = await handler.Handle(new GetUserinfoFromMitIdQuery("bearerToken"), CancellationToken.None);

        result.Sub.Should().Be(userinfo.Sub);
        result.Name.Should().Be(userinfo.NemloginName);
        result.Email.Should().Be(userinfo.NemloginEmail);
        result.OrgCvr.Should().Be(userinfo.NemloginEmail);
        result.OrgName.Should().Be(userinfo.NemloginOrgName);
    }
}
