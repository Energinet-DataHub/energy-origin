using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectOriginClients;

namespace API.Transfer.Api._Features_;

public record CreateTransferAgreementCommand(
    Guid ReceiverOrganizationId,
    Guid SenderOrganizationId,
    long StartDate,
    long? EndDate,
    string ReceiverTin, // TODO: Delete once we get info from Auth 🐉
    string ReceiverName,// TODO: Delete once we get info from Auth 🐉
    string SenderTin,   // TODO: Delete once we get info from Auth 🐉
    string SenderName   // TODO: Delete once we get info from Auth 🐉
    ) : IRequest<CreateTransferAgreementCommandResult>;
public record CreateTransferAgreementCommandResult(Guid TransferAgreementId, string SenderName, string SenderTin, string ReceiverTin, long StartDate, long? EndDate, TransferAgreementType Type);

public class CreateTransferAgreementCommandHandler(IUnitOfWork UnitOfWork, IProjectOriginWalletClient walletClient) : IRequestHandler<CreateTransferAgreementCommand, CreateTransferAgreementCommandResult>
{
    public async Task<CreateTransferAgreementCommandResult> Handle(CreateTransferAgreementCommand command, CancellationToken cancellationToken)
    {
        var taRepo = UnitOfWork.TransferAgreementRepo;

        var transferAgreement = new TransferAgreement
        {
            StartDate = UnixTimestamp.Create(command.StartDate),
            EndDate = command.EndDate.HasValue ? UnixTimestamp.Create(command.EndDate.Value) : null,
            SenderId = OrganizationId.Create(command.SenderOrganizationId),
            SenderName = OrganizationName.Create(command.SenderName),
            SenderTin = Tin.Create(command.SenderTin),
            ReceiverName = OrganizationName.Create(command.ReceiverName),
            ReceiverTin = Tin.Create(command.ReceiverTin)
        };

        var hasConflict = await taRepo.HasDateOverlap(transferAgreement, CancellationToken.None);
        if (hasConflict)
        {
            throw new TransferAgreementConflictException();
        }

        var wallets = await walletClient.GetWallets(command.ReceiverOrganizationId, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null) // TODO: This code should be deleted when we allign when and where we create a wallet. 🐉
        {
            var createWalletResponse = await walletClient.CreateWallet(command.ReceiverOrganizationId, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await walletClient.CreateWalletEndpoint(command.ReceiverOrganizationId, walletId.Value, CancellationToken.None);

        var externalEndpoint = await walletClient.CreateExternalEndpoint(command.SenderOrganizationId, walletEndpoint, command.SenderTin, CancellationToken.None);

        transferAgreement.ReceiverReference = externalEndpoint.ReceiverId;

        try
        {
            var result = await taRepo.AddTransferAgreement(transferAgreement, CancellationToken.None);

            await UnitOfWork.SaveAsync();

            return new CreateTransferAgreementCommandResult(result.Id, result.SenderTin.Value, result.SenderName.Value,
                result.ReceiverTin.Value, result.StartDate.EpochSeconds, result.EndDate?.EpochSeconds, result.Type);
        }
        catch (DbUpdateException)
        {
            throw new TransferAgreementConflictException();
        }
    }
}
