using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using API.Query.API.ApiModels.Requests;
using API.UnitOfWork;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.WalletClient;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testing;
using Xunit;
using Technology = DataContext.ValueObjects.Technology;

namespace API.UnitTests.ContractService;

public class ContractServiceImplTest
{
    private readonly IMeteringPointsClient _meteringPointsClient = Substitute.For<IMeteringPointsClient>();
    private readonly IWalletClient _walletClient = Substitute.For<IWalletClient>();
    private readonly IStampClient _stampClient = Substitute.For<IStampClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ContractServiceImpl> _logger = Substitute.For<ILogger<ContractServiceImpl>>();

    [Fact]
    public async Task Create_GsrnNotFound_ReturnsGsrnNotFound()
    {
        // Arrange
        var meteringpointOwner = Guid.NewGuid();
        _meteringPointsClient
            .GetMeteringPoints(meteringpointOwner.ToString(), CancellationToken.None)
            .Returns(new MeteringPointsResponse([]));

        var contractService = new ContractServiceImpl(_meteringPointsClient, _walletClient, _stampClient, _unitOfWork, _logger);
        var gsrn = Any.Gsrn();

        var contracts = new CreateContracts([
            new CreateContract()
            {
                GSRN = gsrn.Value,
                StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        ]);

        // Act
        var result =  await contractService.Create(
            contracts,
            meteringpointOwner,
            Guid.NewGuid(),
            "testSubject",
            "testOrg",
            "testTin",
            CancellationToken.None);

        // Assert result is typeOf GsrnNotFound
        Assert.IsType<CreateContractResult.GsrnNotFound>(result);
    }

    [Fact]
    public async Task Create_CannotBeUsedForIssuingCertificates_ReturnsCannotBeUsedForIssuingCertificates()
    {
        // Arrange
        var meteringpointOwner = Guid.NewGuid();
        var gsrn = Any.Gsrn();

        _meteringPointsClient
            .GetMeteringPoints(meteringpointOwner.ToString(), CancellationToken.None)
            .Returns(new MeteringPointsResponse([
                new MeteringPoint(
                    gsrn.ToString(),
                    "DK1",
                    MeterType.Consumption,
                    new Address("Test", null, null, "Test", "Test", "Test"),
                    new API.ContractService.Clients.Technology(
                        "F01040100",
                        "T010000"),

                    false)
            ]));

        var contractService = new ContractServiceImpl(_meteringPointsClient, _walletClient, _stampClient, _unitOfWork, _logger);

        var contracts = new CreateContracts([
            new CreateContract()
            {
                GSRN = gsrn.Value,
                StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        ]);

        // Act
        var result =  await contractService.Create(
            contracts,
            meteringpointOwner,
            Guid.NewGuid(),
            "testSubject",
            "testOrg",
            "testTin",
            CancellationToken.None);

        // Assert result is typeOf GsrnNotFound
        Assert.IsType<CreateContractResult.CannotBeUsedForIssuingCertificates>(result);
    }

    [Fact]
    public async Task Create_OverlappingContracts_ReturnsContractAlreadyExists()
    {
        var meteringpointOwner = Guid.NewGuid();
        var gsrn = Any.Gsrn();

        _meteringPointsClient
            .GetMeteringPoints(meteringpointOwner.ToString(), CancellationToken.None)
            .Returns(new MeteringPointsResponse([
                new MeteringPoint(
                    gsrn.Value,
                    "DK1",
                    MeterType.Consumption,
                    new Address("Test", null, null, "Test", "Test", "Test"),
                    new API.ContractService.Clients.Technology(
                        "F01040100",
                        "T010000"),
                    true)
            ]));
        _unitOfWork.CertificateIssuingContractRepo.GetByGsrn(Arg.Is<List<string>>(l => l[0] == gsrn.Value), Arg.Any<CancellationToken>()).Returns(
            new List<CertificateIssuingContract>()
            {
                new()
                {
                    GSRN = gsrn.Value,
                    GridArea = "DK1",
                    MeteringPointType = MeteringPointType.Consumption,
                    MeteringPointOwner = meteringpointOwner.ToString(),
                    StartDate = DateTimeOffset.UtcNow.AddHours(-1),
                    EndDate = DateTimeOffset.UtcNow.AddDays(1),
                    Created = DateTimeOffset.UtcNow,
                    RecipientId = Guid.NewGuid(),
                    Technology = new Technology("F01040100", "T010000"),
                    ContractNumber = 1
                }
            }
        );

        var contractService = new ContractServiceImpl(_meteringPointsClient, _walletClient, _stampClient, _unitOfWork, _logger);

        var contracts = new CreateContracts([
            new CreateContract()
            {
                GSRN = gsrn.Value,
                StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
            }
        ]);

        var result =  await contractService.Create(
            contracts,
            meteringpointOwner,
            Guid.NewGuid(),
            "testSubject",
            "testOrg",
            "testTin",
            CancellationToken.None);

        Assert.IsType<CreateContractResult.ContractAlreadyExists>(result);
    }
}
