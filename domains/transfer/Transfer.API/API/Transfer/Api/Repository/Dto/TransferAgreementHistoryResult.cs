using System.Collections.Generic;
using API.Transfer.Api.Models;

namespace API.Transfer.Api.Repository.Dto;

public record TransferAgreementHistoryResult(int totalCount, List<TransferAgreementHistoryEntry> items);
