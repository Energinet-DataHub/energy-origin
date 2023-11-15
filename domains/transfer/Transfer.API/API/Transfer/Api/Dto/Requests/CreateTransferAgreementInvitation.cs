using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using API.Shared.Extensions;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementInvitation(long StartDate,
    long? EndDate,
    string ReceiverTin);

public class CreateTransferAgreementInvitationValidator : AbstractValidator<CreateTransferAgreementInvitation>
{
    public CreateTransferAgreementInvitationValidator(IHttpContextAccessor context)
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(createInvitation => createInvitation.StartDate)
            .NotEmpty()
            .WithMessage("Start Date cannot be empty.")
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .WithMessage("Start Date cannot be in the past.")
            .MustBeBeforeYear10000();

        RuleFor(createInvitation => createInvitation.EndDate)
            .Cascade(CascadeMode.Stop)
            .Must((createInvitation, endDate) => endDate == null || endDate > createInvitation.StartDate)
            .WithMessage("End Date must be null or later than Start Date.")
            .MustBeBeforeYear10000()
            .When(t => t.EndDate != null);

        RuleFor(createInvitation => createInvitation.ReceiverTin)
            .NotEmpty()
            .WithMessage("ReceiverTin cannot be empty")
            .Length(8)
            .Matches("^[0-9]{8}$")
            .WithMessage("ReceiverTin must be 8 digits without any spaces.")
            .NotEqual(context.HttpContext!.User.FindSubjectTinClaim())
            .WithMessage("ReceiverTin cannot be the same as SenderTin.");
    }
}
