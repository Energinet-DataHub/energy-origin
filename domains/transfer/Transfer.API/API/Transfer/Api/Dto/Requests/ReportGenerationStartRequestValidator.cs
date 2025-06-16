using API.Transfer.Api.Controllers;
using FluentValidation;
using System;

namespace API.Transfer.Api.Dto.Requests;

public class ReportGenerationStartRequestValidator : AbstractValidator<ReportGenerationStartRequest>
{
    public ReportGenerationStartRequestValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => DateTimeOffset.FromUnixTimeSeconds(x.StartDate).AddDays(7).ToUnixTimeSeconds())
            .LessThanOrEqualTo(x => DateTimeOffset.FromUnixTimeSeconds(x.StartDate).AddYears(1).ToUnixTimeSeconds())
            .WithMessage("Must generate the report for at least seven days and less than or equal to one year.");
    }
}
