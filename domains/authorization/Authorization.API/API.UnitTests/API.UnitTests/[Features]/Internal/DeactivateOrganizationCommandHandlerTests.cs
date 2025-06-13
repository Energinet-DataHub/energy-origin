using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;


namespace API.UnitTests._Features_.Internal;

public class DeactivateOrganizationCommandHandlerTests
{
    private readonly FakeOrganizationRepository _fakeRepo;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly DeactivateOrganizationCommandHandler _sut;

    public DeactivateOrganizationCommandHandlerTests()
    {
        _fakeRepo = new FakeOrganizationRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _sut = new DeactivateOrganizationCommandHandler(_fakeRepo, _fakeUnitOfWork);
    }

    [Fact]
    public async Task Given_NoOrganizationWithThatId_WhenHandling_ThenCommitStillCalled_AndNoException()
    {
        var cmd = new DeactivateOrganizationCommand(Guid.NewGuid());

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.True(_fakeUnitOfWork.Committed, "UnitOfWork should commit even if no org was found");
    }

    [Fact]
    public async Task Given_OrganizationInNormalState_WhenHandling_ThenStatusIsDeactivated_AndCommit()
    {
        var tin = Tin.Create("12345678");
        var name = OrganizationName.Create("Test Org");
        var org = Organization.Create(tin, name);
        await _fakeRepo.AddAsync(org, CancellationToken.None);

        var cmd = new DeactivateOrganizationCommand(org.Id);

        await _sut.Handle(cmd, CancellationToken.None);

        var updatedOrg = _fakeRepo.Query().First(o => o.Id == org.Id);
        Assert.Equal(OrganizationStatus.Deactivated, updatedOrg.Status);
        Assert.True(_fakeUnitOfWork.Committed, "UnitOfWork should commit after successful deactivate");
    }

    [Fact]
    public async Task Given_OrganizationInTrialState_WhenHandling_ThenThrowsBusinessException_AndDoesNotCommit()
    {
        var tin = Tin.Create("87654321");
        var name = OrganizationName.Create("Trial Org");
        var org = Organization.CreateTrial(tin, name); // trial status
        await _fakeRepo.AddAsync(org, CancellationToken.None);

        var cmd = new DeactivateOrganizationCommand(org.Id);

        await Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd, CancellationToken.None));
        Assert.False(_fakeUnitOfWork.Committed, "UnitOfWork should NOT commit when exception is thrown");
    }

    [Fact]
    public async Task Given_OrganizationAlreadyDeactivated_WhenHandling_ThenThrowsBusinessException_AndDoesNotCommit()
    {
        var tin = Tin.Create("55555555");
        var name = OrganizationName.Create("Deactivated Org");
        var org = Organization.Create(tin, name);
        org.Deactivate(); // manually set to Deactivated
        await _fakeRepo.AddAsync(org, CancellationToken.None);

        var cmd = new DeactivateOrganizationCommand(org.Id);

        await Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd, CancellationToken.None));
        Assert.False(_fakeUnitOfWork.Committed, "UnitOfWork should NOT commit when exception is thrown");
    }
}
