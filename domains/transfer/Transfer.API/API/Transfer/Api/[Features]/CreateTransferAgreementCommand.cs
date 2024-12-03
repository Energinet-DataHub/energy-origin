using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
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
    TransferAgreementType Type

// TODO: Delete once we get info from Auth 🐉
) : IRequest<CreateTransferAgreementCommandResult>;
public record CreateTransferAgreementCommandResult(Guid TransferAgreementId, string SenderName, string SenderTin, string ReceiverTin, long StartDate, long? EndDate, TransferAgreementType Type);

public class CreateTransferAgreementCommandHandler(IUnitOfWork UnitOfWork, IProjectOriginWalletClient WalletClient, IAuthorizationClient AuthorizationClient) : IRequestHandler<CreateTransferAgreementCommand, CreateTransferAgreementCommandResult>
{
    private record GiverReceiverDto(OrganizationId SenderOrganizationId, OrganizationName SenderName, Tin SenderTin, OrganizationId ReceiverOrganizationId,  OrganizationName ReceiverName, Tin ReceiverTin);

    private GiverReceiverDto FromConsentResponse(UserOrganizationConsentsResponse consents, Guid SenderOrganizationId, Guid ReceiverOrganizationId)
    {
        var sender = consents.Result.First(c => c.GiverOrganizationId == SenderOrganizationId); // Todo Or self?
        var receiver = consents.Result.First(c => c.GiverOrganizationId == ReceiverOrganizationId); // Todo Or self?

        return new GiverReceiverDto(
            OrganizationId.Create(SenderOrganizationId),
            OrganizationName.Create(sender.GiverOrganizationName),
            Tin.Create(sender.GiverOrganizationTin),
            OrganizationId.Create(ReceiverOrganizationId),
            OrganizationName.Create(receiver.GiverOrganizationName),
            Tin.Create(receiver.GiverOrganizationTin));
    }

    public async Task<CreateTransferAgreementCommandResult> Handle(CreateTransferAgreementCommand command, CancellationToken cancellationToken)
    {
        var taRepo = UnitOfWork.TransferAgreementRepo;

        var consents = await AuthorizationClient.GetConsentsAsync();
        if(consents == null)
            throw new ApplicationException("Failed to get consents from authorization.");

        var authResponse = FromConsentResponse(consents, command.SenderOrganizationId, command.ReceiverOrganizationId);

        var transferAgreement = new TransferAgreement
        {
            StartDate = UnixTimestamp.Create(command.StartDate),
            EndDate = command.EndDate.HasValue ? UnixTimestamp.Create(command.EndDate.Value) : null,
            SenderId = authResponse.SenderOrganizationId,
            SenderName = authResponse.SenderName,
            SenderTin = authResponse.SenderTin,
            ReceiverId = authResponse.ReceiverOrganizationId,
            ReceiverName = authResponse.ReceiverName,
            ReceiverTin = authResponse.ReceiverTin,
            Type = command.Type
        };

        var hasConflict = await taRepo.HasDateOverlap(transferAgreement, CancellationToken.None);
        if (hasConflict)
        {
            throw new TransferAgreementConflictException();
        }

        var wallets = await WalletClient.GetWallets(command.ReceiverOrganizationId, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null) // TODO: This code should be deleted when we allign when and where we create a wallet. 🐉
        {
            var createWalletResponse = await WalletClient.CreateWallet(command.ReceiverOrganizationId, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await WalletClient.CreateWalletEndpoint(command.ReceiverOrganizationId, walletId.Value, CancellationToken.None);

        var externalEndpoint = await WalletClient.CreateExternalEndpoint(command.SenderOrganizationId, walletEndpoint, authResponse.SenderTin.Value, CancellationToken.None);

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

