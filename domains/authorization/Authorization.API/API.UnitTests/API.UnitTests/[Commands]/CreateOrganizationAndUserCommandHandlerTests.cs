using API.Authorization._Features_;
using API.Data;
using API.UnitTests.Repository;
using FluentAssertions;
using NSubstitute;

namespace API.UnitTests._Commands_;

public class CreateOrganizationAndUserCommandHandlerTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly FakeUserRepository _fakeUserRepository;
    private readonly FakeAffiliationRepository _fakeAffiliationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateOrganizationAndUserCommandHandler _handler;

    public CreateOrganizationAndUserCommandHandlerTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _fakeUserRepository = new FakeUserRepository();
        _fakeAffiliationRepository = new FakeAffiliationRepository();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateOrganizationAndUserCommandHandler(_unitOfWork, _fakeOrganizationRepository, _fakeUserRepository, _fakeAffiliationRepository);
    }

    [Fact]
    public async Task GivenValidRequest_WhenHandlingCommand_ThenCreatesEntities()
    {
        var request = new CreateOrganizationAndUserCommand(
            "12345678",
            "Test Org",
            Guid.NewGuid(),
            "Test User",
            "1.0"
        );

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.OrganizationId.Should().NotBeEmpty();
        result.UserId.Should().NotBeEmpty();
        result.AffiliationId.Should().Be(result.UserId);

        var organization = await _fakeOrganizationRepository.GetAsync(result.OrganizationId, CancellationToken.None);
        organization.Should().NotBeNull();
        organization.Tin.Value.Should().Be(request.Tin);
        organization.Name.Value.Should().Be(request.OrganizationName);
        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(request.TermsVersion);

        var user = await _fakeUserRepository.GetAsync(result.UserId, CancellationToken.None);
        user.Should().NotBeNull();
        user.IdpUserId.Value.Should().Be(request.UserIdpId);
        user.Name.Value.Should().Be(request.UserName);

        var affiliation = await _fakeAffiliationRepository.GetAsync(result.UserId, result.OrganizationId, CancellationToken.None);
        affiliation.Should().NotBeNull();
        affiliation.UserId.Should().Be(result.UserId);
        affiliation.OrganizationId.Should().Be(result.OrganizationId);

        await _unitOfWork.Received(1).BeginTransactionAsync();
        await _unitOfWork.Received(1).CommitAsync();
    }
}
