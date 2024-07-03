using API.Authorization._Features_;
using API.Authorization.Controllers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace API.UnitTests.Controller;

public class AuthorizationControllerTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly AuthorizationController _controller;

    public AuthorizationControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<AuthorizationController>>();
        _controller = new AuthorizationController(_mediator);
    }

    [Fact]
    public async Task GetConsentForUser_HappyPath_ReturnsCorrectUserAuthorizationResponse()
    {
        var request = new AuthorizationUserRequest(
            Sub: Guid.NewGuid(),
            Name: "Test User",
            OrgCvr: "12345678",
            OrgName: "Test Organization"
        );

        var commandResult = new GetConsentForUserCommandResult(
            request.Sub,
            request.Name,
            "User",
            request.OrgName,
            new List<Guid> { Guid.NewGuid() },
            "dashboard production meters certificates wallet",
            true
        );

        _mediator.Send(Arg.Any<GetConsentForUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var actionResult = await _controller.GetConsentForUser(_logger, request);

        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<UserAuthorizationResponse>().Subject;

        response.Should().BeEquivalentTo(new UserAuthorizationResponse(
            commandResult.Sub,
            commandResult.SubType,
            commandResult.OrgName,
            commandResult.OrgIds,
            commandResult.Scope,
            commandResult.TermsAccepted
        ));

        await _mediator.Received(1).Send(
            Arg.Is<GetConsentForUserCommand>(cmd =>
                cmd.Sub == request.Sub &&
                cmd.Name == request.Name &&
                cmd.OrgName == request.OrgName &&
                cmd.OrgCvr == request.OrgCvr),
            Arg.Any<CancellationToken>()
        );
    }
}
