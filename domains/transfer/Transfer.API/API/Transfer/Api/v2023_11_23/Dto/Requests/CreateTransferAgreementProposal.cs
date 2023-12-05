using System;
using API.Shared.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace API.Transfer.Api.v2023_11_23.Dto.Requests;

public record CreateTransferAgreementProposal(long StartDate,
    long? EndDate,
    string? ReceiverTin);

public class CreateTransferAgreementProposalValidator : AbstractValidator<CreateTransferAgreementProposal>
{
    public CreateTransferAgreementProposalValidator(IHttpContextAccessor context)
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
            .WithMessage("ReceiverTin must be 8 digits without any spaces.")
            .NotEqual(context.HttpContext!.User.FindSubjectTinClaim())
            .WithMessage("ReceiverTin cannot be the same as SenderTin.");
    }
}
