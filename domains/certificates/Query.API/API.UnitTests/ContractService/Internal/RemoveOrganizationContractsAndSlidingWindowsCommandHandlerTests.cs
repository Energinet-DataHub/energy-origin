using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Internal;
using API.ContractService.Repositories;
using API.MeasurementsSyncer.Persistence;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace API.UnitTests.ContractService.Internal;

public class RemoveOrganizationContractsAndSlidingWindowsCommandHandlerTests
{
    [Fact]
    public async Task GivenNoContractsExist_WhenCommand_ThenNoRemovalsArePerformed()
    {
        var organizationId = Guid.NewGuid();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);

        var mockContractRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockContractRepo.Query().Returns(Enumerable.Empty<CertificateIssuingContract>().AsQueryable().BuildMock());

        var mockSlidingWindowState = Substitute.For<ISlidingWindowState>();
        mockSlidingWindowState.Query()
            .Returns(Enumerable.Empty<MeteringPointTimeSeriesSlidingWindow>().AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockContractRepo);
        mockUnitOfWork.SlidingWindowState.Returns(mockSlidingWindowState);

        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await mockUnitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        mockContractRepo.DidNotReceive().RemoveRange(Arg.Any<IEnumerable<CertificateIssuingContract>>());
        mockSlidingWindowState.DidNotReceive()
            .RemoveRange(Arg.Any<IEnumerable<MeteringPointTimeSeriesSlidingWindow>>());
    }

    [Fact]
    public async Task GivenContractsExistButNoSlidingWindows_WhenCommand_ThenOnlyContractsAreRemoved()
    {
        var organizationId = Guid.NewGuid();
        var organizationIdString = organizationId.ToString();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);

        var gsrn1 = Any.Gsrn().Value;
        var gsrn2 = Any.Gsrn().Value;

        var contracts = new List<CertificateIssuingContract>
        {
            new CertificateIssuingContract { GSRN = gsrn1, MeteringPointOwner = organizationIdString },
            new CertificateIssuingContract { GSRN = gsrn2, MeteringPointOwner = organizationIdString }
        };

        var mockContractRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockContractRepo.Query().Returns(contracts.AsQueryable().BuildMock());

        var mockSlidingWindowState = Substitute.For<ISlidingWindowState>();
        mockSlidingWindowState.Query()
    .Returns(Enumerable.Empty<MeteringPointTimeSeriesSlidingWindow>().AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockContractRepo);
        mockUnitOfWork.SlidingWindowState.Returns(mockSlidingWindowState);


        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await mockUnitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        mockContractRepo.Received(1).RemoveRange(Arg.Do<IEnumerable<CertificateIssuingContract>>(actual =>
        {
            var list = actual.ToList();
            Assert.Equal(2, list.Count);
            Assert.All(list, item => Assert.Contains(item, contracts));
        }));
        mockSlidingWindowState.DidNotReceive()
            .RemoveRange(Arg.Any<IEnumerable<MeteringPointTimeSeriesSlidingWindow>>());
    }

    [Fact]
    public async Task GivenContractsAndSlidingWindowsExist_WhenCommand_ThenBothAreRemoved()
    {
        var organizationId = Guid.NewGuid();
        var organizationIdString = organizationId.ToString();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);

        var gsrn1 = Any.Gsrn().Value;
        var gsrn2 = "57" + new string('1', 16);

        var contracts = new List<CertificateIssuingContract>
        {
            new CertificateIssuingContract { GSRN = gsrn1, MeteringPointOwner = organizationIdString },
            new CertificateIssuingContract { GSRN = gsrn2, MeteringPointOwner = organizationIdString }
        };

        var gsrn1Obj = new Gsrn(gsrn1);
        var gsrn2Obj = new Gsrn(gsrn2);
        var timestamp = UnixTimestamp.Now();

        var slidingWindow1 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn1Obj, timestamp);
        var slidingWindow2 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn2Obj, timestamp);

        var slidingWindows = new List<MeteringPointTimeSeriesSlidingWindow> { slidingWindow1, slidingWindow2 };

        var mockContractRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockContractRepo.Query().Returns(contracts.AsQueryable().BuildMock());

        var mockSlidingWindowState = Substitute.For<ISlidingWindowState>();
        mockSlidingWindowState.Query().Returns(slidingWindows.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockContractRepo);
        mockUnitOfWork.SlidingWindowState.Returns(mockSlidingWindowState);

        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await mockUnitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        mockContractRepo.Received(1).RemoveRange(Arg.Do<IEnumerable<CertificateIssuingContract>>(actual =>
        {
            var list = actual.ToList();
            Assert.Equal(2, list.Count);
            Assert.All(list, item => Assert.Contains(item, contracts));
        }));
        mockSlidingWindowState.Received(1).RemoveRange(Arg.Is<IEnumerable<MeteringPointTimeSeriesSlidingWindow>>(
            s => s.Count() == 2));
    }

    [Fact]
    public async Task
        GivenMultipleContractsWithSameGsrn_WhenCommand_ThenAllContractsAreRemovedButSlidingWindowsOnlyOnce()
    {
        var organizationId = Guid.NewGuid();
        var organizationIdString = organizationId.ToString();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);

        var gsrn1 = Any.Gsrn();

        var contracts = new List<CertificateIssuingContract>
        {
            new CertificateIssuingContract { GSRN = gsrn1.ToString(), MeteringPointOwner = organizationIdString },
            new CertificateIssuingContract { GSRN = gsrn1.ToString(), MeteringPointOwner = organizationIdString }
        };

        var timestamp = UnixTimestamp.Now();

        var slidingWindow1 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn1, timestamp);

        var slidingWindows = new List<MeteringPointTimeSeriesSlidingWindow> { slidingWindow1 };

        var mockContractRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockContractRepo.Query().Returns(contracts.AsQueryable().BuildMock());

        var mockSlidingWindowState = Substitute.For<ISlidingWindowState>();
        mockSlidingWindowState.Query().Returns(slidingWindows.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockContractRepo);
        mockUnitOfWork.SlidingWindowState.Returns(mockSlidingWindowState);

        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await mockUnitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        mockContractRepo.Received(1).RemoveRange(Arg.Do<IEnumerable<CertificateIssuingContract>>(actual =>
        {
            var list = actual.ToList();
            Assert.Equal(2, list.Count);
            Assert.All(list, item => Assert.Contains(item, contracts));
        }));
        mockSlidingWindowState.Received(1).RemoveRange(Arg.Is<IEnumerable<MeteringPointTimeSeriesSlidingWindow>>(
            s => s.Count() == 1));
    }

    [Fact]
    public async Task GivenContractsWithDifferentGSRNs_WhenCommand_ThenOnlyMatchingSlidingWindowsAreRemoved()
    {
        var organizationId = Guid.NewGuid();
        var organizationIdString = organizationId.ToString();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);

        var gsrn1 = Any.Gsrn();
        var gsrn2 = Any.Gsrn();
        var gsrn3 = Any.Gsrn();

        var contracts = new List<CertificateIssuingContract>
        {
            new CertificateIssuingContract { GSRN = gsrn1.ToString(), MeteringPointOwner = organizationIdString },
            new CertificateIssuingContract { GSRN = gsrn2.ToString(), MeteringPointOwner = organizationIdString }
        };

        var timestamp = UnixTimestamp.Now();

        var slidingWindow1 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn1, timestamp);
        var slidingWindow2 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn2, timestamp);
        var slidingWindow3 = MeteringPointTimeSeriesSlidingWindow.Create(gsrn3, timestamp);

        var slidingWindows = new List<MeteringPointTimeSeriesSlidingWindow>
            { slidingWindow1, slidingWindow2, slidingWindow3 };

        var mockContractRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockContractRepo.Query().Returns(contracts.AsQueryable().BuildMock());

        var mockSlidingWindowState = Substitute.For<ISlidingWindowState>();
        mockSlidingWindowState.Query().Returns(slidingWindows.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockContractRepo);
        mockUnitOfWork.SlidingWindowState.Returns(mockSlidingWindowState);

        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        await mockUnitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        mockContractRepo.Received(1).RemoveRange(Arg.Do<IEnumerable<CertificateIssuingContract>>(actual =>
        {
            var list = actual.ToList();
            Assert.Equal(2, list.Count);
            Assert.All(list, item => Assert.Contains(item, contracts));
        }));

        mockSlidingWindowState.Received(1).RemoveRange(Arg.Do<IEnumerable<MeteringPointTimeSeriesSlidingWindow>>(actual =>
        {
            var list = actual.ToList();
            Assert.Equal(2, list.Count);
            Assert.DoesNotContain(list, sw => sw.GSRN == gsrn3.ToString());
        }));
    }

    [Fact]
    public async Task GivenTransactionFails_WhenCommand_ThenExceptionIsPropagated()
    {
        var organizationId = Guid.NewGuid();
        var command = new RemoveOrganizationContractsAndSlidingWindowsCommand(organizationId);
        var expectedException = new Exception("Transaction failed");

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();

        mockUnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(expectedException));

        var handler = new RemoveOrganizationContractsAndSlidingWindowsCommandHandler(mockUnitOfWork);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal("Transaction failed", exception.Message);
    }
}
