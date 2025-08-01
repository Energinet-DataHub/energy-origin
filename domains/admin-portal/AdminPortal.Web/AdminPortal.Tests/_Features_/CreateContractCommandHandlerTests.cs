using System;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class CreateContractCommandHandlerTests
{
    [Fact]
    public async Task GivenCreateContract_WhenOrganizationIsValidWithNormalOrganization_ThenContractIsCreated()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";
        var gsrn = "GSRN1234567890";
        var organizationName = "Test Organization";

        var mockContractService = Substitute.For<IContractService>();
        mockContractService.CreateContracts(Arg.Any<CreateContracts>())
            .Returns(new ContractList
            {
                Result =
                    [
                        new Contract
                        {
                            Id = Guid.NewGuid(),
                            Gsrn = gsrn,
                            StartDate = 1700000000,
                            EndDate = 1720000000,
                            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            MeteringPointType = MeteringPointTypeResponse.Consumption,
                            Technology = new AdminPortal.Services.Technology
                            {
                                AibFuelCode = "FuelCode",
                                AibTechCode = "TechCode"
                            }
                        }
                    ]
            });

        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationId, organizationName, organizationTin, "Normal")]));

        var command = new CreateContractCommand
        {
            Contracts =
            [
                new CreateContractItem { Gsrn = gsrn, StartDate = 1700000000, EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = organizationId,
        };

        var handler = new CreateContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await mockAuthorizationService.Received(1).GetOrganizationsAsync(Arg.Any<CancellationToken>());
        await mockContractService.Received(1).CreateContracts(Arg.Is<CreateContracts>(x =>
            x.Contracts.Count == 1 &&
            x.Contracts[0].Gsrn == gsrn &&
            x.MeteringPointOwnerId == command.MeteringPointOwnerId &&
            x.OrganizationTin == organizationTin &&
            x.OrganizationName == organizationName &&
            x.IsTrial == false));
    }

    [Fact]
    public async Task GivenCreateContract_WhenOrganizationIsValidWithTrialOrganization_ThenContractIsCreated()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";
        var gsrn = "GSRN1234567890";
        var organizationName = "Test Organization";

        var mockContractService = Substitute.For<IContractService>();
        mockContractService.CreateContracts(Arg.Any<CreateContracts>())
            .Returns(new ContractList
            {
                Result =
                    [
                        new Contract
                        {
                            Id = Guid.NewGuid(),
                            Gsrn = gsrn,
                            StartDate = 1700000000,
                            EndDate = 1720000000,
                            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            MeteringPointType = MeteringPointTypeResponse.Consumption,
                            Technology = new AdminPortal.Services.Technology
                            {
                                AibFuelCode = "FuelCode",
                                AibTechCode = "TechCode"
                            }
                        }
                    ]
            });

        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationId, organizationName, organizationTin, "Trial")]));

        var command = new CreateContractCommand
        {
            Contracts =
            [
                new CreateContractItem { Gsrn = gsrn, StartDate = 1700000000, EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = organizationId,
        };

        var handler = new CreateContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await mockAuthorizationService.Received(1).GetOrganizationsAsync(Arg.Any<CancellationToken>());
        await mockContractService.Received(1).CreateContracts(Arg.Is<CreateContracts>(x =>
            x.Contracts.Count == 1 &&
            x.Contracts[0].Gsrn == gsrn &&
            x.MeteringPointOwnerId == command.MeteringPointOwnerId &&
            x.OrganizationTin == organizationTin &&
            x.OrganizationName == organizationName &&
            x.IsTrial == true));
    }

    [InlineData("2b198070-373e-4e3e-9bf6-5e94b96987d7")]
    [Theory]
    public async Task GivenCreateContract_WhenOrganizationIsNotValid_ThenContractIsNotCreated(string organizationId)
    {
        var mockContractService = Substitute.For<IContractService>();

        var organizationIdFromService = new Guid("65f76bf5-b308-48af-bca7-f8c36620abd4");
        var organizationTinFromService = "12345678";
        var organizationName = "Test Organization";
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationIdFromService, organizationName, organizationTinFromService, "Normal")]));

        var gsrn = "GSRN1234567890";
        var command = new CreateContractCommand
        {
            Contracts =
            [
                new CreateContractItem { Gsrn = gsrn, StartDate = 1700000000, EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = new Guid(organizationId),
        };

        var handler = new CreateContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act/Assert
        await Assert.ThrowsAsync<BusinessException>(async () => await handler.Handle(command, TestContext.Current.CancellationToken));
    }
}
