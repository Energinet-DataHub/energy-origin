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
using FluentAssertions;
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
        result.Should().NotBeNull();
        result.Result.Should().BeEmpty();
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
        result.Result.Should().HaveCount(1);
        var item = result.Result.First();
        item.GSRN.Should().Be(data[0].GSRN);
        item.MeteringPointOwner.Should().Be(data[0].MeteringPointOwner);
        item.MeteringPointType.Should().Be(MeteringPointType.Production);
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
        result.Result.Should().HaveCount(1, "both records share the same GSRN");
        var singleItem = result.Result.Single();
        singleItem.MeteringPointOwner.Should().Be(sameOwnerOfMultipleContracts, "it has the newest Created");
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
        result.Result.Should().HaveCount(2, "the record with EndDate in the past should be excluded");

        result.Result.Should().ContainSingle(c => c.GSRN == activeContract1.GSRN);
        result.Result.Should().ContainSingle(c => c.GSRN == activeContract2.GSRN);
        result.Result.Should().NotContain(c => c.GSRN == expiredContract.GSRN);
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
        result.Result.Should().HaveCount(2, because: "there should be one newest record per GSRN");

        var gsrn1Result = result.Result.SingleOrDefault(x => x.GSRN == gsrn1);
        gsrn1Result.Should().NotBeNull();
        gsrn1Result!.MeteringPointOwner.Should().Be(newerOwnerForGsrn1, "we want the newest record by Created for gsrn1");
        gsrn1Result.MeteringPointType.Should().Be(MeteringPointType.Consumption);

        var gsrn2Result = result.Result.SingleOrDefault(x => x.GSRN == gsrn2);
        gsrn2Result.Should().NotBeNull();
        gsrn2Result!.MeteringPointOwner.Should().Be(ownerForGsrn2, "there is only one contract for gsrn2");
        gsrn2Result.MeteringPointType.Should().Be(MeteringPointType.Production);
    }
}
