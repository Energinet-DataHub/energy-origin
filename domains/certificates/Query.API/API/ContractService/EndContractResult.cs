using System;

namespace API.ContractService;

public abstract record EndContractResult
{
    public record Ended() : EndContractResult;
    public record NonExistingContract() : EndContractResult;
    public record MeteringPointOwnerNoMatch() : EndContractResult;
}
