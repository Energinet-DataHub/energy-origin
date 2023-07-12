using System;

namespace API.ContractService;

public abstract record EndContractResult
{
    public record Success : EndContractResult;
    public record EndDateBeforeStartDate(DateTimeOffset StartDate, DateTimeOffset IllegalEndDate) : EndContractResult;
    public record NonExistingContract : EndContractResult;
    public record MeteringPointOwnerNoMatch : EndContractResult;

    private EndContractResult() { }
}
