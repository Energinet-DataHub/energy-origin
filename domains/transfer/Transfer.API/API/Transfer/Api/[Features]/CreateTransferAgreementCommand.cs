using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EnergyOrigin.WalletClient;

namespace API.Transfer.Api._Features_;

public record CreateTransferAgreementCommand(
    Guid ReceiverOrganizationId,
    Guid SenderOrganizationId,
    long StartDate,
    long? EndDate,
    TransferAgreementType Type
) : IRequest<CreateTransferAgreementCommandResult>;
public record CreateTransferAgreementCommandResult(Guid TransferAgreementId, string SenderName, string SenderTin, string ReceiverTin, long StartDate, long? EndDate, TransferAgreementType Type);

public class CreateTransferAgreementCommandHandler(IUnitOfWork UnitOfWork, IWalletClient WalletClient, IAuthorizationClient AuthorizationClient, IdentityDescriptor IdentityDescriptor) : IRequestHandler<CreateTransferAgreementCommand, CreateTransferAgreementCommandResult>
{
    public async Task<CreateTransferAgreementCommandResult> Handle(CreateTransferAgreementCommand command, CancellationToken cancellationToken)
    {
        var taRepo = UnitOfWork.TransferAgreementRepo;

        var consents = await AuthorizationClient.GetConsentsAsync();
        if (consents == null)
        {
            throw new Exception("Failed to get consents from authorization.");
        }

        (var SenderOrganizationId, var SenderTin, var SenderName) = GetOrganizationOnBehalfOf(command.SenderOrganizationId, consents);
        (var ReceiverOrganizationId, var ReceiverTin, var ReceiverName) = GetOrganizationOnBehalfOf(command.ReceiverOrganizationId, consents);

        var transferAgreement = new TransferAgreement
        {
            StartDate = UnixTimestamp.Create(command.StartDate),
            EndDate = command.EndDate.HasValue ? UnixTimestamp.Create(command.EndDate.Value) : null,
            SenderId = SenderOrganizationId,
            SenderName = SenderName,
            SenderTin = SenderTin,
            ReceiverId = ReceiverOrganizationId,
            ReceiverName = ReceiverName,
            ReceiverTin = ReceiverTin,
            Type = command.Type,
            IsTrial = IdentityDescriptor.IsTrial()
        };

        var hasConflict = await taRepo.HasDateOverlap(transferAgreement, CancellationToken.None);
        if (hasConflict)
        {
            throw new TransferAgreementConflictException();
        }

        var wallets = await WalletClient.GetWalletsAsync(command.ReceiverOrganizationId, CancellationToken.None);

        var walletId = wallets.Result.First().Id;

        var walletEndpoint = await WalletClient.CreateWalletEndpointAsync(walletId, command.ReceiverOrganizationId, CancellationToken.None);

        var externalEndpoint = await WalletClient.CreateExternalEndpointAsync(command.SenderOrganizationId, walletEndpoint, SenderTin.Value, CancellationToken.None);

        transferAgreement.ReceiverReference = externalEndpoint.ReceiverId;

        try
        {
            var result = await taRepo.AddTransferAgreement(transferAgreement, CancellationToken.None);

            await UnitOfWork.SaveAsync();

            return new CreateTransferAgreementCommandResult(result.Id, result.SenderName.Value, result.SenderTin.Value,
                result.ReceiverTin.Value, result.StartDate.EpochSeconds, result.EndDate?.EpochSeconds, result.Type);
        }
        catch (DbUpdateException)
        {
            throw new TransferAgreementConflictException();
        }
    }

    private (OrganizationId organizationId, Tin organizationTin, OrganizationName organizationName) GetOrganizationOnBehalfOf(Guid organizationIdOnBehalfOf, UserOrganizationConsentsResponse consents)
    {
        OrganizationId organizationId;
        Tin organizationTin;
        OrganizationName organizationName;

        if (IdentityDescriptor.OrganizationId == organizationIdOnBehalfOf)
        {
            organizationId = OrganizationId.Create(IdentityDescriptor.OrganizationId);
            organizationTin = Tin.Create(IdentityDescriptor.OrganizationCvr!);
            organizationName = OrganizationName.Create(IdentityDescriptor.OrganizationName);
        }
        else
        {
            (organizationId, organizationTin, organizationName) = consents!.GetCurrentOrganizationBehalfOf(organizationIdOnBehalfOf);
        }

        return (organizationId, organizationTin, organizationName);
    }
}
