using System;
using FluentValidation;

namespace API.Transfer.Api.v2023_01_01.Dto.Requests;

public class EditTransferAgreementEndDateValidator : AbstractValidator<EditTransferAgreementEndDate>
{
    public EditTransferAgreementEndDateValidator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(t => t.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}
