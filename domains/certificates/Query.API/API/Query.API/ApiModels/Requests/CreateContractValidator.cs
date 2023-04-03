using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class CreateContractValidator : AbstractValidator<CreateContract>
{
    public CreateContractValidator()
    {
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay).ToUnixTimeSeconds();
        var utcMidnightNextDay = now.AddDays(1).Subtract(now.TimeOfDay).ToUnixTimeSeconds();

        RuleFor(cs => cs.StartDate)
            .GreaterThanOrEqualTo(_ => utcMidnight)
            .LessThan(253402300800).WithMessage("{PropertyName} must be before 253402300800 (10000-01-01T00:00:00+00:00)");

        RuleFor(cs => cs.EndDate)
            .GreaterThanOrEqualTo(_ => utcMidnightNextDay)
            .LessThan(253402300800)
            .WithMessage("{PropertyName} must be before 253402300800 (10000-01-01T00:00:00+00:00)");


        RuleFor(cs => cs.GSRN)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(gsrn => Regex.IsMatch(gsrn, "^\\d{18}$", RegexOptions.None, TimeSpan.FromSeconds(1))).WithMessage("Invalid {PropertyName}. Must be 18 digits");
    }
}
