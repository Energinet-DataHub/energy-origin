using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class RemoveOrganizationClientsCommandHandlerTest
{
    private readonly FakeClientRepository _fakeRepo;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly RemoveOrganizationClientsCommandHandler _sut;

    public RemoveOrganizationClientsCommandHandlerTest()
    {
        _fakeRepo = new FakeClientRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _sut = new RemoveOrganizationClientsCommandHandler(_fakeRepo, _fakeUnitOfWork);
    }

    [Fact]
    public async Task GivenNoClients_WhenRemovingAllOrganizationClients_ThenReturnOk()
    {
        var cmd = new RemoveOrganizationClientsCommand(Any.Guid());
        await _sut.Handle(cmd, CancellationToken.None);
    }

    [Fact]
    public async Task GivenClients_WhenRemovingAllOrganizationClients_AllClientsAreRemoved()
    {
        var orgId = Any.Guid();
        var client1 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client1.SetOrganization(OrganizationId.Create(orgId));
        var client2 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client2.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client1, CancellationToken.None);
        await _fakeRepo.AddAsync(client2, CancellationToken.None);

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.True(_fakeUnitOfWork.Committed);
        Assert.DoesNotContain(client1, _fakeRepo.Query().ToList());
        Assert.DoesNotContain(client2, _fakeRepo.Query().ToList());
    }

    [Fact]
    public async Task GivenUnrelatedClient_WhenRemovingAllOrganizationClients_UnrelatedClientsAreNotRemoved()
    {
        var orgId = Any.Guid();
        var unrelatedClient = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        unrelatedClient.SetOrganization(Any.OrganizationId());

        await _fakeRepo.AddAsync(unrelatedClient, CancellationToken.None);

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.Contains(unrelatedClient, _fakeRepo.Query().ToList());
    }
}
