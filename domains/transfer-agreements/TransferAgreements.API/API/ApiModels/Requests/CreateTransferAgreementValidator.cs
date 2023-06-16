using System;
using FluentValidation;

namespace API.ApiModels.Requests
{
    public class CreateTransferAgreementValidator : AbstractValidator<CreateTransferAgreement>
    {
        public CreateTransferAgreementValidator()
        {
            var now = DateTimeOffset.UtcNow;

            RuleFor(t => t.StartDate)
                .NotEmpty()
                .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
                .MustBeBeforeYear10000();

            RuleFor(t => t.EndDate)
                .Cascade(CascadeMode.Stop)
                .Must((dto, endDate) => endDate == null || endDate > dto.StartDate)
                .WithMessage("End Date must be null or later than Start Date.")
                .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
                .When(t => t.EndDate != null)
                .WithMessage("End Date must be null or later than now.")
                .MustBeBeforeYear10000()
                .When(t => t.EndDate != null);
        }
    }
}
