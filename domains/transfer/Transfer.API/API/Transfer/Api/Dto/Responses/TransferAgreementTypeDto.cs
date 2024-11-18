using System;
using System.Text.Json.Serialization;
using DataContext.Models;

namespace API.Transfer.Api.Dto.Responses;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransferAgreementTypeDto
{
    TransferAllCertificates = 0,
    TransferCertificatesBasedOnConsumption = 1,
}

public static class TransferAgreementTypeMapper
{
    public static TransferAgreementTypeDto MapCreateTransferAgreementType(TransferAgreementType modelType)
    {
        return modelType switch
        {
            TransferAgreementType.TransferAllCertificates => TransferAgreementTypeDto.TransferAllCertificates,
            TransferAgreementType.TransferCertificatesBasedOnConsumption => TransferAgreementTypeDto.TransferCertificatesBasedOnConsumption,
            _ => throw new ArgumentOutOfRangeException($"Unable to map transfer agreement type value {modelType}")
        };
    }
}
