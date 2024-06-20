using System;
using FluentValidation;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementProposal20240515(
    long StartDate,
    long? EndDate,
    string? ReceiverTin);

public class CreateTransferAgreementProposal20240515Validator : AbstractValidator<CreateTransferAgreementProposal20240515>
{
    public CreateTransferAgreementProposal20240515Validator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(createProposal => createProposal.StartDate)
            .NotEmpty()
            .WithMessage("Start Date cannot be empty.")
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .WithMessage("Start Date cannot be in the past.")
            .MustBeBeforeYear10000();

        RuleFor(createProposal => createProposal.EndDate)
            .Cascade(CascadeMode.Stop)
            .Must((createProposal, endDate) => endDate == null || endDate > createProposal.StartDate)
            .WithMessage("End Date must be null or later than Start Date.")
            .MustBeBeforeYear10000()
            .When(t => t.EndDate != null);

        RuleFor(createProposal => createProposal.ReceiverTin)
            .Length(8)
            .Matches("^[0-9]{8}$")
            .WithMessage("ReceiverTin must be 8 digits without any spaces.");
    }
}
