using System;
using FluentValidation;

namespace API.ApiModels.Requests
{
    public class CreateTransferAgreementValidator : AbstractValidator<CreateTransferAgreement>
    {
        public CreateTransferAgreementValidator(string senderTin)
        {
            var now = DateTimeOffset.UtcNow;

            RuleFor(createTransferAgreement => createTransferAgreement.StartDate)
                .NotEmpty()
                .WithMessage("Start Date cannot be empty.")
                .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
                .WithMessage("Start Date cannot be in the past.")
                .MustBeBeforeYear10000();

            RuleFor(createTransferAgreement => createTransferAgreement.EndDate)
                .Cascade(CascadeMode.Stop)
                .Must((createTransferAgreement, endDate) => endDate == null || endDate > createTransferAgreement.StartDate)
                .WithMessage("End Date must be null or later than Start Date.")
                .MustBeBeforeYear10000()
                .When(t => t.EndDate != null);

            RuleFor(createTransferAgreement => createTransferAgreement.ReceiverTin)
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
