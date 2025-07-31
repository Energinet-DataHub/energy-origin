using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class CreateContractCommandHandlerTests
{
    [Fact]
    public async Task GivenCreateContract_WhenOrganizationIsValid_ThenContractIsCreated()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";
        var gsrn = "GSRN1234567890";

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
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationId, "Test Organization", organizationTin, "Normal")]));

        var command = new CreateContractCommand
        {
            Contracts =
            [
                new CreateContractItem { Gsrn = gsrn, StartDate = 1700000000, EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = organizationId,
            OrganizationTin = organizationTin,
            OrganizationName = "Test Organization",
            IsTrial = false
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
            x.OrganizationTin == command.OrganizationTin &&
            x.OrganizationName == command.OrganizationName &&
            x.IsTrial == command.IsTrial));
    }



    [InlineData("65f76bf5-b308-48af-bca7-f8c36620abd4", "12345677")]
    [InlineData("2b198070-373e-4e3e-9bf6-5e94b96987d7", "12345678")]
    [Theory]
    public async Task GivenCreateContract_WhenOrganizationIsNotValid_ThenContractIsNotCreated(string organizationId, string tin)
    {
        var mockContractService = Substitute.For<IContractService>();

        var organizationIdFromService = new Guid("65f76bf5-b308-48af-bca7-f8c36620abd4");
        var organizationTinFromService = "12345678";
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationIdFromService, "Test Organization", organizationTinFromService, "Normal")]));

        var gsrn = "GSRN1234567890";
        var command = new CreateContractCommand
        {
            Contracts =
            [
                new CreateContractItem { Gsrn = gsrn, StartDate = 1700000000, EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = new Guid(organizationId),
            OrganizationTin = tin,
            OrganizationName = "Test Organization",
            IsTrial = false
        };

        var handler = new CreateContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act/Assert
        await Assert.ThrowsAsync<BusinessException>(async () => await handler.Handle(command, TestContext.Current.CancellationToken));
    }
}
