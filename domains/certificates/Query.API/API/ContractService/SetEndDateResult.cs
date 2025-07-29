using System;

namespace API.ContractService;

public abstract record SetEndDateResult
{
    public record Success : SetEndDateResult;
    public record EndDateBeforeStartDate(DateTimeOffset ExistingStartDateOnContract, DateTimeOffset IllegalEndDate) : SetEndDateResult;
    public record NonExistingContract : SetEndDateResult;
    public record MeteringPointOwnerNoMatch : SetEndDateResult;
    public record OrganizationNotFound(string OrganizationId) : SetEndDateResult;

    public record OverlappingContract : SetEndDateResult;

    private SetEndDateResult() { }
}
