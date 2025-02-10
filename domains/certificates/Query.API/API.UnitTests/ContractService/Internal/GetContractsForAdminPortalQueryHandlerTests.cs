using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Internal;
using API.ContractService.Repositories;
using API.UnitOfWork;
using DataContext.Models;
using DataContext.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace API.UnitTests.ContractService.Internal;

public class GetContractsForAdminPortalQueryHandlerTests
{
    [Fact]
    public async Task GivenNoContracts_WhenQuery_ThenReturnEmptyResult()
    {
        // Arrange
        var mockRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockRepo.Query().Returns(Enumerable.Empty<CertificateIssuingContract>().AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockRepo);

        var handler = new GetContractsForAdminPortalQueryHandler(mockUnitOfWork);

        // Act
        var result = await handler.Handle(new GetContractsForAdminPortalQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task GivenOnlySingleContractExists_WhenQuery_ThenReturnResultWithThatSingleRecord()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var data = new List<CertificateIssuingContract>
        {
            new ()
            {
                GSRN = Any.Gsrn().Value,
                MeteringPointOwner = Guid.NewGuid().ToString(),
                Created = now,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                MeteringPointType = MeteringPointType.Production
            }
        };

        var mockRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockRepo.Query().Returns(data.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockRepo);

        var handler = new GetContractsForAdminPortalQueryHandler(mockUnitOfWork);

        // Act
        var result = await handler.Handle(new GetContractsForAdminPortalQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result.Result);
        var item = result.Result.First();
        Assert.Equal(data[0].GSRN, item.GSRN);
        Assert.Equal(data[0].MeteringPointOwner, item.MeteringPointOwner);
        Assert.Equal(MeteringPointType.Production, item.MeteringPointType);
    }

    [Fact]
    public async Task GivenMultipleContractsWithSameGSRNsExist_WhenQuery_ThenOnlyReturnNewestActiveContractForTheGSRN()
    {
        // Arrange
        var sameGSRN = Any.Gsrn().ToString();
        var sameOwnerOfMultipleContracts = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var data = new List<CertificateIssuingContract>
        {
            new ()
            {
                GSRN = sameGSRN,
                MeteringPointOwner = sameOwnerOfMultipleContracts,
                Created = now.AddMinutes(-10),
                StartDate = now.AddDays(-5),
                EndDate = null,
                MeteringPointType = MeteringPointType.Production
            },
            new ()
            {
                GSRN = sameGSRN,
                MeteringPointOwner = sameOwnerOfMultipleContracts,
                Created = now,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(2),
                MeteringPointType = MeteringPointType.Production
            }
        };

        var mockRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockRepo.Query().Returns(data.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockRepo);

        var handler = new GetContractsForAdminPortalQueryHandler(mockUnitOfWork);

        // Act
        var result = await handler.Handle(new GetContractsForAdminPortalQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result.Result);
        var singleItem = result.Result.Single();
        Assert.Equal(sameOwnerOfMultipleContracts, singleItem.MeteringPointOwner);
    }

    [Fact]
    public async Task GivenMultipleContractsWhereSomeAreExpired_WhenQuery_OnlyListActiveContracts()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiredContract = new CertificateIssuingContract
        {
            GSRN = Any.Gsrn().Value,
            MeteringPointOwner = Guid.NewGuid().ToString(),
            Created = now.AddMinutes(-20),
            StartDate = now.AddDays(-10),
            EndDate = now.AddMinutes(-1),
            MeteringPointType = MeteringPointType.Production
        };
        var activeContract1 = new CertificateIssuingContract
        {
            GSRN = Any.Gsrn().Value,
            MeteringPointOwner = Guid.NewGuid().ToString(),
            Created = now,
            StartDate = now.AddDays(-2),
            EndDate = null,
            MeteringPointType = MeteringPointType.Consumption
        };
        var activeContract2 = new CertificateIssuingContract
        {
            GSRN = Any.Gsrn().Value,
            MeteringPointOwner = Guid.NewGuid().ToString(),
            Created = now,
            StartDate = now.AddDays(-3),
            EndDate = now.AddDays(5),
            MeteringPointType = MeteringPointType.Production
        };

        var data = new List<CertificateIssuingContract> { expiredContract, activeContract1, activeContract2 };

        var mockRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockRepo.Query().Returns(data.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockRepo);

        var handler = new GetContractsForAdminPortalQueryHandler(mockUnitOfWork);

        // Act
        var result = await handler.Handle(new GetContractsForAdminPortalQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Result.Count);

        Assert.Contains(result.Result, c => c.GSRN == activeContract1.GSRN);
        Assert.Contains(result.Result, c => c.GSRN == activeContract2.GSRN);
        Assert.DoesNotContain(result.Result, c => c.GSRN == expiredContract.GSRN);
    }

    [Fact]
    public async Task GivenMultipleContractsWithDifferentGSRNs_WhenQuery_ThenReturnOnlyNewestRecordPerGSRN()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var gsrn1 = Any.Gsrn().Value;
        var olderOwnerForGsrn1 = Guid.NewGuid().ToString();
        var newerOwnerForGsrn1 = Guid.NewGuid().ToString();

        var gsrn2 = Any.Gsrn().Value;
        var ownerForGsrn2 = Guid.NewGuid().ToString();

        var data = new List<CertificateIssuingContract>
        {
            // Older contract for gsrn1
            new ()
            {
                GSRN = gsrn1,
                MeteringPointOwner = olderOwnerForGsrn1,
                Created = now.AddHours(-2),
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(-1),
                MeteringPointType = MeteringPointType.Consumption
            },
            // Newer contract for gsrn1, with new owner (should be the one returned for gsrn1)
            new ()
            {
                GSRN = gsrn1,
                MeteringPointOwner = newerOwnerForGsrn1,
                Created = now.AddDays(-1),
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(5),
                MeteringPointType = MeteringPointType.Consumption
            },
            // A single contract for gsrn2
            new ()
            {
                GSRN = gsrn2,
                MeteringPointOwner = ownerForGsrn2,
                Created = now.AddHours(-1),
                StartDate = now,
                EndDate = now.AddDays(5),
                MeteringPointType = MeteringPointType.Production
            }
        };

        var mockRepo = Substitute.For<ICertificateIssuingContractRepository>();
        mockRepo.Query().Returns(data.AsQueryable().BuildMock());

        var mockUnitOfWork = Substitute.For<IUnitOfWork>();
        mockUnitOfWork.CertificateIssuingContractRepo.Returns(mockRepo);

        var handler = new GetContractsForAdminPortalQueryHandler(mockUnitOfWork);

        // Act
        var result = await handler.Handle(new GetContractsForAdminPortalQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Result.Count);

        var gsrn1Result = result.Result.SingleOrDefault(x => x.GSRN == gsrn1);
        Assert.NotNull(gsrn1Result);
        Assert.Equal(newerOwnerForGsrn1, gsrn1Result!.MeteringPointOwner);
        Assert.Equal(MeteringPointType.Consumption, gsrn1Result.MeteringPointType);

        var gsrn2Result = result.Result.SingleOrDefault(x => x.GSRN == gsrn2);
        Assert.NotNull(gsrn2Result);
        Assert.Equal(ownerForGsrn2, gsrn2Result!.MeteringPointOwner);
        Assert.Equal(MeteringPointType.Production, gsrn2Result.MeteringPointType);
    }
}
