using System.Collections.Generic;
using DataContext.Models;

namespace API.Transfer.Api.Repository.Dto;

public record TransferAgreementHistoryResult(int totalCount, List<TransferAgreementHistoryEntry> items);
