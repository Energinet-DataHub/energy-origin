using System;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class EndContractValidator : AbstractValidator<EndContract>
{
    public EndContractValidator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(cs => cs.ContractId)
            .NotEmpty();

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }

}
