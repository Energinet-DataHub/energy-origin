using System;
using DataContext.Models;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string SenderName,
    string SenderTin,
    string ReceiverTin,
    TransferAgreementTypeDto Type);

public static class TransferAgreementDtoMapper
{
    public static TransferAgreementDto MapTransferAgreement(TransferAgreement transferAgreement)
    {
        return new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.EpochSeconds,
            EndDate: transferAgreement.EndDate?.EpochSeconds,
            SenderName: transferAgreement.SenderName.Value,
            SenderTin: transferAgreement.SenderTin.Value,
            ReceiverTin: transferAgreement.ReceiverTin.Value,
            Type: TransferAgreementTypeMapper.MapCreateTransferAgreementType(transferAgreement.Type));
    }
}
