using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using Xunit;

namespace API.IntegrationTests.Transfer.Api._Features_;

public class DisableWalletCommandTests
{
    private readonly IHDAlgorithm _algorithm;

    public DisableWalletCommandTests()
    {
        _algorithm = new Secp256k1Algorithm();
    }

    [Fact]
    public async Task GivenCommand_WhenNonDisabledWallet_DisableWallet()
    {
        var orgId = Guid.NewGuid();

        var walletClientMock = Substitute.For<IWalletClient>();
        var walletId = Guid.NewGuid();

        walletClientMock.GetWallets(Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result =
                [
                    new WalletRecord
                    {
                        Id = walletId,
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = null
                    }
                ]
            }
        );

        var sut = new DisableWalletCommandCommandHandler(walletClientMock);

        var cmd = new DisableWalletCommand(OrganizationId.Create(orgId));
        await sut.Handle(cmd, new CancellationToken());

        await walletClientMock.Received(1).DisableWallet(Arg.Is<Guid>(x => x.Equals(walletId)), Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenCommand_WhenDisabledWallet_DisableWalletNotCalled()
    {
        var orgId = Guid.NewGuid();

        var walletClientMock = Substitute.For<IWalletClient>();
        var walletId = Guid.NewGuid();

        walletClientMock.GetWallets(Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result =
                [
                    new WalletRecord
                    {
                        Id = walletId,
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = 1234
                    }
                ]
            }
        );

        var sut = new DisableWalletCommandCommandHandler(walletClientMock);

        var cmd = new DisableWalletCommand(OrganizationId.Create(orgId));
        await sut.Handle(cmd, new CancellationToken());

        await walletClientMock.DidNotReceive().DisableWallet(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenCommand_WhenNoWallet_DisableWalletNotCalled()
    {
        var orgId = Guid.NewGuid();

        var walletClientMock = Substitute.For<IWalletClient>();

        walletClientMock.GetWallets(Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 0,
                    Limit = 1,
                    Offset = 0,
                    Total = 0
                },
                Result = []
            }
        );

        var sut = new DisableWalletCommandCommandHandler(walletClientMock);

        var cmd = new DisableWalletCommand(OrganizationId.Create(orgId));
        await sut.Handle(cmd, new CancellationToken());

        await walletClientMock.DidNotReceive().DisableWallet(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenCommand_WhenMultipleNonDisabledWallets_DisableAllWallets()
    {
        var orgId = Guid.NewGuid();

        var walletClientMock = Substitute.For<IWalletClient>();

        walletClientMock.GetWallets(Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result =
                [
                    new WalletRecord
                    {
                        Id = Guid.NewGuid(),
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = null
                    },
                    new WalletRecord
                    {
                        Id = Guid.NewGuid(),
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = null
                    }
                ]
            }
        );

        var sut = new DisableWalletCommandCommandHandler(walletClientMock);

        var cmd = new DisableWalletCommand(OrganizationId.Create(orgId));
        await sut.Handle(cmd, new CancellationToken());

        await walletClientMock.Received(2).DisableWallet(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenCommand_WhenNonDisabledWalletAndDisabledWallet_DisableNonDisabledWallet()
    {
        var orgId = Guid.NewGuid();

        var walletClientMock = Substitute.For<IWalletClient>();
        var walletId = Guid.NewGuid();

        walletClientMock.GetWallets(Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo
                {
                    Count = 1,
                    Limit = 1,
                    Offset = 0,
                    Total = 1
                },
                Result =
                [
                    new WalletRecord
                    {
                        Id = walletId,
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = null
                    },
                    new WalletRecord
                    {
                        Id = Guid.NewGuid(),
                        PublicKey = _algorithm.GenerateNewPrivateKey().Neuter(),
                        DisabledDate = 1234
                    }
                ]
            }
        );

        var sut = new DisableWalletCommandCommandHandler(walletClientMock);

        var cmd = new DisableWalletCommand(OrganizationId.Create(orgId));
        await sut.Handle(cmd, new CancellationToken());

        await walletClientMock.Received(1).DisableWallet(Arg.Is<Guid>(x => x.Equals(walletId)), Arg.Is<Guid>(x => x.Equals(orgId)), Arg.Any<CancellationToken>());
    }
}
