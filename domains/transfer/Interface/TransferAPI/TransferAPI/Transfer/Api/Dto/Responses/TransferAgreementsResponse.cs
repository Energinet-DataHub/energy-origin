using System.Collections.Generic;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementsResponse(List<TransferAgreementDto> Result);
