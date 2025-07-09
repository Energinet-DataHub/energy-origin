using API.Authorization._Features_.Internal;
using API.Models;
using API.Services;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace API.UnitTests._Features_.Internal;

public class RemoveOrganizationClientsCommandHandlerTest
{
    private readonly FakeClientRepository _fakeRepo;
    private readonly FakeUnitOfWork _fakeUnitOfWork;
    private readonly IGraphServiceClientWrapper _fakeGraphServiceClientWrapper;
    private readonly ILogger<RemoveOrganizationClientsCommandHandler> _logger;
    private readonly RemoveOrganizationClientsCommandHandler _sut;

    public RemoveOrganizationClientsCommandHandlerTest()
    {
        _fakeRepo = new FakeClientRepository();
        _fakeUnitOfWork = new FakeUnitOfWork();
        _fakeGraphServiceClientWrapper = Substitute.For<IGraphServiceClientWrapper>();
        _logger = Substitute.For<ILogger<RemoveOrganizationClientsCommandHandler>>();
        _sut = new RemoveOrganizationClientsCommandHandler(_fakeRepo, _fakeUnitOfWork, _fakeGraphServiceClientWrapper, _logger);
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

    [Fact]
    public async Task GivenClients_WhenRemovingAllOrganizationClients_AzureB2CAppRegistrationsAreDeleted()
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

        await _fakeGraphServiceClientWrapper.Received(1).DeleteApplication(client1.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>());
        await _fakeGraphServiceClientWrapper.Received(1).DeleteApplication(client2.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenClients_WhenAzureB2CDeletionFails_OtherClientsAreStillProcessed()
    {
        var orgId = Any.Guid();
        var client1 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client1.SetOrganization(OrganizationId.Create(orgId));
        var client2 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client2.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client1, CancellationToken.None);
        await _fakeRepo.AddAsync(client2, CancellationToken.None);

        _fakeGraphServiceClientWrapper
            .DeleteApplication(client1.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Azure B2C error"));

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        await _fakeGraphServiceClientWrapper.Received(1).DeleteApplication(client1.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>());
        await _fakeGraphServiceClientWrapper.Received(1).DeleteApplication(client2.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>());

        Assert.DoesNotContain(client1, _fakeRepo.Query().ToList());
        Assert.DoesNotContain(client2, _fakeRepo.Query().ToList());
        Assert.True(_fakeUnitOfWork.Committed);
    }

    [Fact]
    public async Task GivenClients_WhenAllAzureB2CDeletionsFail_DatabaseCleanupStillProceeds()
    {
        var orgId = Any.Guid();
        var client1 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client1.SetOrganization(OrganizationId.Create(orgId));
        var client2 = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client2.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client1, CancellationToken.None);
        await _fakeRepo.AddAsync(client2, CancellationToken.None);

        _fakeGraphServiceClientWrapper
            .DeleteApplication(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Azure B2C error"));

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        await _fakeGraphServiceClientWrapper.Received(2).DeleteApplication(Arg.Any<string>(), Arg.Any<CancellationToken>());

        Assert.DoesNotContain(client1, _fakeRepo.Query().ToList());
        Assert.DoesNotContain(client2, _fakeRepo.Query().ToList());
        Assert.True(_fakeUnitOfWork.Committed);
    }

    [Fact]
    public async Task GivenClients_WhenRemovingAllOrganizationClients_LoggingIsPerformed()
    {
        var orgId = Any.Guid();
        var client = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client, CancellationToken.None);

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GivenClients_WhenAzureB2CDeletionFails_ErrorLoggingIsPerformed()
    {
        var orgId = Any.Guid();
        var client = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client, CancellationToken.None);

        _fakeGraphServiceClientWrapper
            .DeleteApplication(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Azure B2C error"));

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GivenEmptyOrganizationId_WhenRemovingAllOrganizationClients_NoClientsAreRemoved()
    {
        var validOrgId = Any.Guid();
        var cmd = new RemoveOrganizationClientsCommand(validOrgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.False(_fakeUnitOfWork.Committed);
        await _fakeGraphServiceClientWrapper.DidNotReceive().DeleteApplication(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenClients_WhenRemovingAllOrganizationClients_TransactionIsCommitted()
    {
        var orgId = Any.Guid();
        var client = Client.Create(Any.IdpClientId(), Any.ClientName(), ClientType.External, "", false);
        client.SetOrganization(OrganizationId.Create(orgId));

        await _fakeRepo.AddAsync(client, CancellationToken.None);

        var cmd = new RemoveOrganizationClientsCommand(orgId);

        await _sut.Handle(cmd, CancellationToken.None);

        Assert.True(_fakeUnitOfWork.Committed);
        Assert.DoesNotContain(client, _fakeRepo.Query().ToList());
        await _fakeGraphServiceClientWrapper.Received(1).DeleteApplication(client.IdpClientId.Value.ToString(), Arg.Any<CancellationToken>());
    }
}
