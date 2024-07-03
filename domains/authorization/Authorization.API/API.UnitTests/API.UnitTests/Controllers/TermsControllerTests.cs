using API.Authorization._Features_;
using API.Authorization.Controllers;
using API.Models;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace API.UnitTests.Controllers;

public class TermsControllerTests
{
    private readonly IMediator _mediator;
    private readonly TermsController _controller;

    public TermsControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new TermsController(_mediator);
    }

    [Fact]
    public async Task AcceptTerms_TermsAlreadyAccepted_ReturnsAcceptedTrue()
    {
        var acceptTermsDto = new AcceptTermsDto(
            "12345678",
            "Test Org",
            Guid.NewGuid(),
            "Test User",
            "1.0"
        );

        _mediator.Send(Arg.Any<OrganizationStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _controller.AcceptTerms(acceptTermsDto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var value = result.Value as TermsResponseDto;
        value!.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptTerms_TermsNotAccepted_ReturnsLatestTerms()
    {
        var acceptTermsDto = new AcceptTermsDto(
            "12345678",
            "Test Org",
            Guid.NewGuid(),
            "Test User",
            "1.0"
        );

        var latestTerms = new Terms("2.0");

        _mediator.Send(Arg.Any<OrganizationStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _mediator.Send(Arg.Any<GetLatestTermsQuery>(), Arg.Any<CancellationToken>())
            .Returns(latestTerms);

        var result = await _controller.AcceptTerms(acceptTermsDto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var value = result.Value as TermsResponseDto;
        value!.Accepted.Should().BeFalse();
        value.TermsVersion.Should().Be(latestTerms.Version);
    }

    [Fact]
    public async Task AcceptTerms_TermsAccepted_CreatesEntities()
    {
        var acceptTermsDto = new AcceptTermsDto(
            "12345678",
            "Test Org",
            Guid.NewGuid(),
            "Test User",
            "1.0"
        );

        _mediator.Send(Arg.Any<OrganizationStateQuery>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var latestTerms = new Terms(acceptTermsDto.TermsVersion);
        _mediator.Send(Arg.Any<GetLatestTermsQuery>(), Arg.Any<CancellationToken>())
            .Returns(latestTerms);

        var commandResult = new CreateOrganizationAndUserCommandResult(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mediator.Send(Arg.Any<CreateOrganizationAndUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await _controller.AcceptTerms(acceptTermsDto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var value = result.Value as TermsResponseDto;
        value!.Accepted.Should().BeTrue();
        value.CreateResult.Should().BeEquivalentTo(commandResult);
    }
}
