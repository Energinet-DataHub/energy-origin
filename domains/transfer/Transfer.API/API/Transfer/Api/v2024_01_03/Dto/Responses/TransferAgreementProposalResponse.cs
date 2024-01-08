using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Transfer.Api.v2024_01_03.Dto.Responses;

public record TransferAgreementProposalResponse(Guid Id, string SenderCompanyName, string? ReceiverTin, long StartDate, long? EndDate);

