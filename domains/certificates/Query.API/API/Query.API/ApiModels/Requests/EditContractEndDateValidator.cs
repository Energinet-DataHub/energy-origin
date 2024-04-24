using System;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class EditContractEndDateValidator : AbstractValidator<EditContractEndDate>
{
    public EditContractEndDateValidator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}

public class EditContractEndDate20230101Validator : AbstractValidator<EditContractEndDate20230101>
{
    public EditContractEndDate20230101Validator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}
