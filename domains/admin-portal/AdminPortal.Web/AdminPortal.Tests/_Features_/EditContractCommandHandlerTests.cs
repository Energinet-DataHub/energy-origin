using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class EditContractCommandHandlerTests
{
    [Fact]
    public async Task GivenEditContract_WhenOrganizationIsValid_ThenContractIsEdited()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationTin = "12345678";

        var mockContractService = Substitute.For<IContractService>();
        mockContractService.EditContracts(Arg.Any<EditContracts>()).Returns(Task.CompletedTask);

        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationId, "Test Organization", organizationTin, "Normal")]));

        var command = new EditContractCommand
        {
            Contracts =
            [
                new EditContractItem { Id = Guid.NewGuid(), EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = organizationId,
            OrganizationTin = organizationTin,
            OrganizationName = "Test Organization",
        };

        var handler = new EditContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await mockAuthorizationService.Received(1).GetOrganizationsAsync(Arg.Any<CancellationToken>());
        await mockContractService.Received(1).EditContracts(Arg.Is<EditContracts>(x =>
            x.Contracts.Count == 1 &&
            x.Contracts[0].Id == command.Contracts.First().Id &&
            x.MeteringPointOwnerId == command.MeteringPointOwnerId &&
            x.OrganizationTin == command.OrganizationTin &&
            x.OrganizationName == command.OrganizationName));
    }

    [InlineData("65f76bf5-b308-48af-bca7-f8c36620abd4", "12345677")]
    [InlineData("2b198070-373e-4e3e-9bf6-5e94b96987d7", "12345678")]
    [Theory]
    public async Task GivenEditContract_WhenOrganizationIsNotValid_ThenContractIsEdited(string organizationId, string tin)
    {
        // Arrange
        var mockContractService = Substitute.For<IContractService>();

        var organizationIdFromService = new Guid("65f76bf5-b308-48af-bca7-f8c36620abd4");
        var organizationTinFromService = "12345678";
        var mockAuthorizationService = Substitute.For<IAuthorizationService>();
        mockAuthorizationService.GetOrganizationsAsync(Arg.Any<CancellationToken>())
            .Returns(new GetOrganizationsResponse([new GetOrganizationsResponseItem(organizationIdFromService, "Test Organization", organizationTinFromService, "Normal")]));

        var command = new EditContractCommand
        {
            Contracts =
            [
                new EditContractItem { Id = Guid.NewGuid(), EndDate = 1720000000 }
            ],
            MeteringPointOwnerId = new Guid(organizationId),
            OrganizationTin = tin,
            OrganizationName = "Test Organization",
        };

        var handler = new EditContractCommandHandler(mockContractService, mockAuthorizationService);

        // Act/Assert
        await Assert.ThrowsAsync<BusinessException>(async () => await handler.Handle(command, TestContext.Current.CancellationToken));
    }
}
