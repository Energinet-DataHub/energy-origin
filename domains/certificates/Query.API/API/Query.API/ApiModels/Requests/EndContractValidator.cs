using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class EndContractValidator : AbstractValidator<EndContract>
{
    public EndContractValidator()
    {
        var now = DateTimeOffset.UtcNow;
        var utcMidnightNextDay = now.AddDays(1).Subtract(now.TimeOfDay).ToUnixTimeSeconds();

        RuleFor(cs => cs.GSRN)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(gsrn => Regex.IsMatch(gsrn, "^\\d{18}$", RegexOptions.None, TimeSpan.FromSeconds(1))).WithMessage("Invalid {PropertyName}. Must be 18 digits");

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .LessThan(253402300800)
            .WithMessage("{PropertyName} must be before 253402300800 (10000-01-01T00:00:00+00:00)")
            .When(s => s.EndDate != default);
    }

}
