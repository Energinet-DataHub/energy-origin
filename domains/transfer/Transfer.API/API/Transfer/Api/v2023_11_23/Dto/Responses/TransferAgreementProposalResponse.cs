using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Transfer.Api.v2023_11_23.Dto.Responses;

public record TransferAgreementProposalResponse(Guid Id, string SenderCompanyName, string? ReceiverTin, long StartDate, long? EndDate);

