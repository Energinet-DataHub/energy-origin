using System;
using FluentValidation;

namespace API.ApiModels.Requests
{
    public class CreateTransferAgreementValidator : AbstractValidator<CreateTransferAgreement>
    {
        public CreateTransferAgreementValidator(string senderTin)
        {
            var now = DateTimeOffset.UtcNow;

            RuleFor(t => t.StartDate)
                .NotEmpty()
                .WithMessage("Start Date cannot be empty.")
                .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
                .WithMessage("Start Date cannot be in the past.")
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

            RuleFor(t => t.ReceiverTin)
                .NotEmpty()
                .WithMessage("ReceiverTin cannot be empty")
                .Length(8)
                .Matches("^[0-9]{8}$")
                .WithMessage("ReceiverTin must be 8 digits without any spaces.")
                .NotEqual(senderTin)
                .WithMessage("ReceiverTin cannot be the same as SenderTin.");
        }
    }
}
