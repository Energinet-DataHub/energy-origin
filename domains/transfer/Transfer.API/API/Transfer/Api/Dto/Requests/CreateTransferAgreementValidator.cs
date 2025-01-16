using System;
using FluentValidation;

namespace API.Transfer.Api.Dto.Requests;

public class CreateTransferAgreementValidator : AbstractValidator<CreateTransferAgreementRequest>
{
    public CreateTransferAgreementValidator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(t => t.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);

        RuleFor(t => t.StartDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000();
    }
}
