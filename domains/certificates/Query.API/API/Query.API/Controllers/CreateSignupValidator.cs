using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.Query.API.Controllers;

public class CreateSignupValidator : AbstractValidator<CreateSignup>
{
    public CreateSignupValidator()
    {
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay).ToUnixTimeSeconds();

        RuleFor(cs => cs.StartDate)
            .GreaterThanOrEqualTo(_ => utcMidnight);

        RuleFor(cs => cs.Gsrn)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(gsrn => Regex.IsMatch(gsrn.Trim(), "^\\d{18}$")).WithMessage("Invalid GSRN. Must be 18 digits");
    }
}
