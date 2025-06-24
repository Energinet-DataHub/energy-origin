using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;

namespace API.UnitTests._Features_.Internal;

public class RemoveOrganizationConsentsCommandHandlerTest
{
    private readonly FakeOrganizationConsentRepository _fakeRepo;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly RemoveOrganizationConsentsCommandHandler _sut;

    public RemoveOrganizationConsentsCommandHandlerTest()
    {
        _fakeRepo = new FakeOrganizationConsentRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _sut = new RemoveOrganizationConsentsCommandHandler(_fakeRepo, _fakeUnitOfWork);
    }

    [Fact]
    public async Task GivenNoConsents_WhenRemovingAllOrganizationConsents_ThenReturnOk()
    {
        var cmd = new RemoveOrganizationConsentsCommand(Any.Guid());
        await _sut.Handle(cmd, CancellationToken.None);
    }

    [Fact]
    public async Task GivenConsents_WhenRemovingAllOrganizationConsents_AllConsentsAreRemoved()
    {
        var orgId = Any.Guid();
        var receivedConsent = OrganizationConsent.Create(orgId, Any.Guid(), Any.DateTimeOffset());
        var givenConsent = OrganizationConsent.Create(Any.Guid(), orgId, Any.DateTimeOffset());

        await _fakeRepo.AddAsync(receivedConsent, CancellationToken.None);
        await _fakeRepo.AddAsync(givenConsent, CancellationToken.None);

        var cmd = new RemoveOrganizationConsentsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.True(_fakeUnitOfWork.Committed);
        Assert.DoesNotContain(receivedConsent, _fakeRepo.Query().ToList());
        Assert.DoesNotContain(givenConsent, _fakeRepo.Query().ToList());
    }

    [Fact]
    public async Task GivenUnrelatedConsent_WhenRemovingAllOrganizationConsents_UnrelatedConsentsAreNotRemoved()
    {
        var orgId = Any.Guid();
        var unrelatedConsent = OrganizationConsent.Create(Any.Guid(), Any.Guid(), Any.DateTimeOffset());

        await _fakeRepo.AddAsync(unrelatedConsent, CancellationToken.None);

        var cmd = new RemoveOrganizationConsentsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.Contains(unrelatedConsent, _fakeRepo.Query().ToList());
    }
}
