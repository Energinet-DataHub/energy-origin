using System;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests.Internal;

public class CreateContractValidator : AbstractValidator<CreateContract>
{
    public CreateContractValidator()
    {
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay).ToUnixTimeSeconds();

        RuleFor(cs => cs.StartDate)
            .GreaterThanOrEqualTo(_ => utcMidnight)
            .MustBeBeforeYear10000();

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(cs => cs.StartDate)
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);

        RuleFor(cs => cs.GSRN)
            .MustBeValidGsrn();
    }
}
