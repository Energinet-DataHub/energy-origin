using System;

namespace API.ContractService;

public abstract record EndContractResult
{
    public record Ended(bool End) : EndContractResult;

    public record NonExistingContract(bool? Exist) : EndContractResult;
}
